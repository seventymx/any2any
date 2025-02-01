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

using Any2Any.Prototype.Common;
using Any2Any.Prototype.Helpers;
using Any2Any.Prototype.Services;
using ClosedXML.Excel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Any2Any.Prototype.GrpcServices;

public class DemoService(
    FileProcessingService fileProcessingService,
    LinkingService linkingService,
    ExportService exportService,
    Any2AnyDbContext dbContext,
    ILogger<DemoService> logger
) : Demo.DemoBase
{
    /// <summary>
    ///     Uploads source files sent by the client and processes them.
    /// </summary>
    public override async Task<Empty> UploadSourceFiles(IAsyncStreamReader<FileChunk> requestStream, ServerCallContext context)
    {
        var fileStreams = new List<(Stream fileStream, string fileName, string fileType)>();
        var fileStream = new MemoryStream();

        await foreach (var chunk in requestStream.ReadAllAsync(context.CancellationToken))
        {
            if (chunk.IsFinalChunk)
            {
                fileStream.Seek(0, SeekOrigin.Begin);
                fileStreams.Add((fileStream, chunk.FileName, chunk.FileType));
                fileStream = new();
            }

            await fileStream.WriteAsync(chunk.Content.ToByteArray(), context.CancellationToken);
        }

        if (fileStreams.Any(fs => fs.fileType != "xlsx")) throw new RpcException(new(StatusCode.InvalidArgument, "Invalid file type. Expected .xlsx"));

        await dbContext.Database.EnsureCreatedAsync(context.CancellationToken);

        foreach (var (stream, fileName, _) in fileStreams) await fileProcessingService.ProcessExcelFileAsync(fileName, stream, context.CancellationToken);

        return new();
    }

    /// <summary>
    ///     Fetches the names of columns that are common to multiple entities.
    /// </summary>
    public override Task<ColumnNamesResponse> GetColumnNames(Empty request, ServerCallContext context)
    {
        var commonColumnNames = dbContext.EntityProperties
            .GroupBy(p => p.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        return Task.FromResult(new ColumnNamesResponse { ColumnNames = { commonColumnNames } });
    }

    /// <summary>
    ///     Links records of two entities based on a specific column name.
    /// </summary>
    public override async Task<Empty> SetLinkedColumn(LinkedColumnRequest request, ServerCallContext context)
    {
        await linkingService.LinkEntitiesAsync(request.ColumnName, context.CancellationToken);
        await linkingService.CreateRecordGroupsAsync(context.CancellationToken);

        return new();
    }

    /// <summary>
    ///     Downloads a demo export as a stream of file chunks.
    /// </summary>
    public override async Task DownloadDemoExport(FileDownloadRequest request, IServerStreamWriter<FileChunk> responseStream, ServerCallContext context)
    {
        var demoExport = await exportService.CreateDemoExportAsync(context.CancellationToken);

        if (demoExport == null)
        {
            logger.LogInformation("Demo export could not be created.");
            return;
        }

        using var workbook = new XLWorkbook();
        workbook.Worksheets.Add(demoExport);

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        // 64 KB buffer
        var buffer = new byte[64 * 1024];
        int bytesRead;
        while ((bytesRead = await memoryStream.ReadAsync(buffer, context.CancellationToken)) > 0)
        {
            var isFinalChunk = bytesRead < buffer.Length;

            var fileChunk = new FileChunk
            {
                FileName = "demo_export",
                FileType = "xlsx",
                Content = ByteString.CopyFrom(buffer, 0, bytesRead),
                IsFinalChunk = isFinalChunk
            };

            // Remove the temp database and mark the final message to trigger response trailers in the interceptor
            if (isFinalChunk && responseStream is WrappedServerStreamWriter<FileChunk> wrappedStream)
            {
                wrappedStream.IsFinalMessage = true;
                await CleanupMappingProcessAsync(context);
            }

            await responseStream.WriteAsync(fileChunk);
        }
    }

    /// <summary>
    ///     Cleans up the database and MappingProcessId after export completion.
    /// </summary>
    private async Task CleanupMappingProcessAsync(ServerCallContext context)
    {
        // Retrieve the MappingProcessId
        var mappingIdHeader = context.RequestHeaders.FirstOrDefault(h => h.Key == MappingProcessServerInterceptor.MappingProcessIdHeaderKey)?.Value;
        if (!Guid.TryParse(mappingIdHeader, out _))
        {
            logger.LogWarning("No valid MappingProcessId found. Skipping cleanup.");
            return;
        }

        await dbContext.Database.EnsureDeletedAsync(context.CancellationToken);

        // The MappingProcessId is not added to the response trailers, if the database was deleted
    }
}