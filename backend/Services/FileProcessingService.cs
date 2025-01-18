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

using Any2Any.Prototype.Model;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;

namespace Any2Any.Prototype.Services;

public class FileProcessingService(Any2AnyDbContext dbContext, ILogger<FileProcessingService> logger, CancellationTokenSource cancellationTokenSource)
{
    private CancellationToken CancellationToken => cancellationTokenSource.Token;

    public async Task ProcessExcelFileAsync(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);

        foreach (var sheet in workbook.Worksheets)
        {
            CancellationToken.ThrowIfCancellationRequested();

            var fileInfo = new FileInfo(filePath);
            var entityName = $"{fileInfo.Name.Replace(fileInfo.Extension, string.Empty)}.{sheet.Name}";
            logger.LogInformation($"Processing sheet: {entityName}");

            // Load table data
            var entity = new Entity { Name = entityName };

            var firstRow = sheet.FirstRowUsed();
            var firstCol = sheet.FirstColumnUsed();
            var lastCol = sheet.LastColumnUsed();

            // Add columns as EntityProperties
            foreach (var col in sheet.Range(firstRow!.RowNumber(), firstCol!.ColumnNumber(), firstRow.RowNumber(), lastCol!.ColumnNumber()).Cells())
            {
                if (string.IsNullOrWhiteSpace(col.GetString())) break;

                var property = new EntityProperty
                {
                    Name = col.GetString(),
                    // Assuming all columns are string for simplicity
                    DataType = DataType.String
                };

                entity.Properties.Add(property);
            }

            // Add rows as Records
            foreach (var row in sheet.RowsUsed().Skip(1))
            {
                var record = new Record();

                foreach (var (property, cell) in entity.Properties.Zip(row.Cells(1, entity.Properties.Count)))
                {
                    var value = new Value
                    {
                        PropertyId = property.Id
                    };

                    value.SetSerializedValue(cell.GetString());
                    record.Values.Add(value);
                }

                entity.Records.Add(record);
            }

            dbContext.Entities.Add(entity);
        }

        await dbContext.SaveChangesAsync(CancellationToken);
    }
}