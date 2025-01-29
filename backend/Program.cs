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
using System.Text.Json;
using Any2Any.Prototype.Extensions;
using Any2Any.Prototype.Model;
using Any2Any.Prototype.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.InMemory;

namespace Any2Any.Prototype;

public class Program
{
    private const string CertificateSettingsEnvironmentVariable = "CERTIFICATE_SETTINGS";
    private const string ApiPortEnvironmentVariable = "API_PORT";

    private const string CorsPolicyName = "ClientPolicy";

    internal const string HttpClientName = "CustomHttpClient";

    public static void Main(string[] args)
    {
        // Working directory is not always the same as the executable directory - base directory is more reliable
        var basePath = AppContext.BaseDirectory;

        var builder = WebApplication.CreateBuilder(args);

        // Set up an in-memory log sink
        builder.Host.UseSerilog((_, configuration) =>
        {
            configuration
                .MinimumLevel.Debug()
                // Override for EF Core logs
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                // Log to the console for development
                .WriteTo.Console()
                // Collect logs in memory
                .WriteTo.InMemory(LogEventLevel.Information);
        });

        // Get the certificate settings from the environment variable
        var certSettings = JsonSerializer.Deserialize<CertificateSettings>(
            Environment.GetEnvironmentVariable(CertificateSettingsEnvironmentVariable) ?? throw new InvalidOperationException($"{CertificateSettingsEnvironmentVariable} environment variable not set"),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        )!;

        // Get the API port from the environment variable
        var apiPort = int.Parse(Environment.GetEnvironmentVariable(ApiPortEnvironmentVariable) ?? throw new InvalidOperationException($"{ApiPortEnvironmentVariable} environment variable not set"));

        // Configure the Kestrel server with the certificate and the API port
        builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(apiPort, listenOptions =>
        {
            var certificatePath = Path.Combine(basePath, $"{certSettings.Path}.pfx");

            var logger = listenOptions.ApplicationServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation($"Using certificate from {certificatePath}");

            listenOptions.UseHttps(new X509Certificate2(certificatePath, certSettings.Password));
            // Enable HTTP/2 and HTTP/1.1 for gRPC-Web compatibility
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        }));

        // Allow all origins
        builder.Services.AddCors(o => o.AddPolicy(CorsPolicyName, policyBuilder =>
        {
            policyBuilder
                // Allow all ports on localhost
                .SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                // Allow all methods and headers
                .AllowAnyMethod()
                .AllowAnyHeader()
                // Expose the gRPC-Web headers
                .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
        }));

        builder.Services.AddGrpc();

        builder.Services.AddSingleton(certSettings);

        // Add custom HttpClient with the certificate handler to talk to the gRPC services
        builder.Services.AddHttpClient(HttpClientName).ConfigurePrimaryHttpMessageHandler(serviceProvider =>
        {
            var certificateSettings = serviceProvider.GetRequiredService<CertificateSettings>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            var publicKeyPath = Path.Combine(basePath, $"{certificateSettings.Path}.crt");
            logger.LogInformation($"Using public key from {publicKeyPath}");
            
            // Load the certificate from the environment variable
            var certificate = new X509Certificate2(publicKeyPath);

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
        builder.Services.AddTransient(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Creating custom HttpClient with certificate handler");

            return httpClientFactory.CreateClient(HttpClientName);
        });

        // Add the database context and services
        builder.Services.AddDbContext<Any2AnyDbContext>(b => b.UseLazyLoadingProxies().UseSqlite($"Data Source={Path.Combine(basePath, "any2any.db")}"));
        builder.Services.AddTransient<FileProcessingService>();
        builder.Services.AddTransient<LinkingService>();
        builder.Services.AddTransient<ExportService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.

        // Enable the HTTPS redirection - only use HTTPS
        app.UseHttpsRedirection();

        // Enable CORS - allow all origins and add gRPC-Web headers
        app.UseCors(CorsPolicyName);

        // Enable gRPC-Web for all services
        app.UseGrpcWeb(new() { DefaultEnabled = true });

        // Add all services in the Services namespace
        app.MapGrpcServices();

        app.Run();
    }
}