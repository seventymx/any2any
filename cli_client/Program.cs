/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Author: Steffen70 <steffen@seventy.mx>
 * Creation Date: 2025-01-26
 *
 * Contributors:
 * - Contributor Name <contributor@example.com>
 */

using System.Security.Cryptography.X509Certificates;
using Any2Any.Prototype.Common;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static Any2Any.Prototype.Common.Demo;

namespace Any2Any.Prototype.CliClient;

public class Program
{
    private const string ApiPortEnvironmentVariable = "API_PORT";

    private const string HttpClientName = "CustomHttpClient";

    public static void Main(string[] args)
    {
        // Get the API port from the environment variable
        var apiPort = int.Parse(Environment.GetEnvironmentVariable(ApiPortEnvironmentVariable) ?? throw new InvalidOperationException($"{ApiPortEnvironmentVariable} environment variable not set"));
        var baseAddress = new Uri($"https://localhost:{apiPort}/");

        var host = Host.CreateDefaultBuilder().ConfigureServices((_, services) =>
        {
            services.AddLogging(builder => { builder.AddConsole(); });

            // Add the cancellation token source to handle Ctrl+C
            services.AddSingleton<CancellationTokenSource>();

            // Add custom HttpClient with the certificate handler to talk to the gRPC services
            services.AddHttpClient(HttpClientName).ConfigurePrimaryHttpMessageHandler(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

                // Load the certificate
                var certificate = new X509Certificate2("./cert/localhost.crt");

                // Expected thumbprint and issuer of the certificate for validation
                var expectedThumbprint = certificate.Thumbprint;
                var expectedIssuer = certificate.Issuer;

                logger.LogInformation($"Creating custom HttpClient with certificate handler for {expectedIssuer}");

                // Create the gRPC channels and clients with the custom certificate handler
                var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(certificate);

                handler.ServerCertificateCustomValidationCallback = (_, cert, _, _) =>
                    cert?.Issuer == expectedIssuer && cert.Thumbprint == expectedThumbprint;

                return handler;
            });

            // Add the custom HttpClient to the service provider
            services.AddTransient(serviceProvider =>
            {
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Creating custom HttpClient with certificate handler");

                return httpClientFactory.CreateClient(HttpClientName);
            });

            // Add the demo client as singleton to the service provider
            services.AddSingleton(serviceProvider =>
            {
                var httpClient = serviceProvider.GetRequiredService<HttpClient>();
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

                logger.LogInformation($"Creating gRPC channel for address: {baseAddress}");

                return GrpcChannel.ForAddress(baseAddress, new() { HttpClient = httpClient });
            });
        }).Build();

        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Handle Ctrl+C
        var cancellationTokenSource = services.GetRequiredService<CancellationTokenSource>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        Console.CancelKeyPress += (_, e) =>
        {
            // Prevent immediate termination
            e.Cancel = true;

            logger.LogInformation("Terminating application...");
            cancellationTokenSource.Cancel();
        };

        try
        {
            // Run the main application logic
            MainAsync(services).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred during execution.");
        }
    }

    private static async Task MainAsync(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        var cancellationToken = serviceProvider.GetRequiredService<CancellationTokenSource>().Token;

        var demoClient = serviceProvider.GetRequiredService<DemoClient>();

        // Log server log in client log
        _ = Task.Run(async () =>
        {
            await foreach (var logMessage in demoClient.GetLogStream(new()).ResponseStream.ReadAllAsync(cancellationToken)) logger.LogInformation("[Server] {Timestamp} [{Level}] {Message}", logMessage.Timestamp, logMessage.Level, logMessage.Message);
        }, cancellationToken);

        // Directory containing the source files
        const string sourceDir = "./source_files";

        if (!Directory.Exists(sourceDir))
        {
            logger.LogError($"Source directory '{sourceDir}' does not exist.");
            return;
        }

        var files = Directory.GetFiles(sourceDir, "*.xlsx");
        if (files.Length == 0)
        {
            logger.LogWarning($"No Excel files found in the directory: {sourceDir}");
            return;
        }

        await UploadFilesAsync(demoClient, files, logger, cancellationToken);

        await demoClient.SetLinkedColumnAsync(new() { ColumnName = "Name" });

        await DownloadDemoExportAsync(demoClient, "./demo_export.xlsx", logger, cancellationToken);
    }

    /// <summary>
    ///     Uploads files to the gRPC endpoint using streaming.
    /// </summary>
    private static async Task UploadFilesAsync(DemoClient demoClient, string[] files, ILogger logger, CancellationToken cancellationToken)
    {
        using var call = demoClient.UploadSourceFiles(cancellationToken: cancellationToken);

        foreach (var file in files)
        {
            logger.LogInformation($"Uploading file: {file}");

            await using var fileStream = File.OpenRead(file);

            // 64KB chunk size
            var buffer = new byte[64 * 1024];
            int bytesRead;

            while ((bytesRead = await fileStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                var chunk = new FileChunk
                {
                    Content = ByteString.CopyFrom(buffer, 0, bytesRead),
                    FileName = Path.GetFileName(file),
                    FileType = "xlsx",
                    IsFinalChunk = false
                };

                await call.RequestStream.WriteAsync(chunk, cancellationToken);
            }

            // Send final chunk
            await call.RequestStream.WriteAsync(new()
            {
                FileName = Path.GetFileName(file),
                FileType = "xlsx",
                IsFinalChunk = true
            }, cancellationToken);

            logger.LogInformation($"Finished uploading: {file}");
        }

        // Close the request stream and wait for the response
        await call.RequestStream.CompleteAsync();
        _ = await call.ResponseAsync;
        logger.LogInformation("Upload completed successfully.");
    }

    /// <summary>
    ///     Downloads the demo export file from the backend and saves it locally.
    /// </summary>
    private static async Task DownloadDemoExportAsync(DemoClient demoClient, string outputPath, ILogger logger, CancellationToken cancellationToken)
    {
        logger.LogInformation("Downloading demo export...");

        using var call = demoClient.DownloadDemoExport(new(), cancellationToken: cancellationToken);

        await using var fileStream = File.Create(outputPath);

        await foreach (var chunk in call.ResponseStream.ReadAllAsync(cancellationToken)) await fileStream.WriteAsync(chunk.Content.ToByteArray(), cancellationToken);

        logger.LogInformation($"Demo export saved successfully: {outputPath}");
    }
}