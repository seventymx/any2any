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

using Any2Any.Prototype.Services;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Any2Any.Prototype;

public class Program
{
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder().ConfigureServices((_, services) =>
        {
            services.AddLogging(b =>
            {
                b.AddConsole();
                b.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            });
            services.AddSingleton<CancellationTokenSource>();
            services.AddDbContext<Any2AnyDbContext>(b => b.UseLazyLoadingProxies().UseSqlite("Data Source=any2any.db"));
            services.AddTransient<FileProcessingService>();
            services.AddTransient<LinkingService>();
            services.AddTransient<ExportService>();
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
        await using var dbContext = serviceProvider.GetRequiredService<Any2AnyDbContext>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        var cancellationToken = serviceProvider.GetRequiredService<CancellationTokenSource>().Token;

        // Ensure database is created
        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

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

        var fileProcessingService = serviceProvider.GetRequiredService<FileProcessingService>();

        // Load and process Excel files
        foreach (var file in Directory.GetFiles(sourceDir, "*.xlsx"))
        {
            logger.LogInformation($"Processing file: {file}");
            await fileProcessingService.ProcessExcelFileAsync(file);
        }

        // Link tables via the "Name" column
        var linkingService = serviceProvider.GetRequiredService<LinkingService>();
        await linkingService.LinkEntitiesAsync("Name");

        // Create record groups
        await linkingService.CreateRecordGroupsAsync();

        // Generate a demo export
        var exportService = serviceProvider.GetRequiredService<ExportService>();
        var demoExport = await exportService.CreateDemoExportAsync();

        if (demoExport == null)
        {
            logger.LogInformation("Demo export could not be created.");
            return;
        }

        // Check for cancellation before saving the export
        cancellationToken.ThrowIfCancellationRequested();

        // Save the demo export as .xlsx file
        using var workbook = new XLWorkbook();
        workbook.Worksheets.Add(demoExport);
        workbook.SaveAs("demo_export.xlsx");

        logger.LogInformation($"Processed {files.Length} file(s) from '{sourceDir}'.");
        logger.LogInformation("Demo export successfully created.");
    }
}