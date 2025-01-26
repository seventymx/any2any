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

using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Any2Any.Prototype.Services;

public class ExportService(Any2AnyDbContext dbContext, ILogger<ExportService> logger)
{
    public async Task<DataTable?> CreateDemoExportAsync(CancellationToken cancellationToken)
    {
        // Record groups including linked records, values and properties
        var recordGroups = await dbContext.RecordGroups
            .Include(rg => rg.RecordGroupLinks)
            .ThenInclude(rgl => rgl.Record)
            .ThenInclude(r => r.Values)
            .ThenInclude(v => v.EntityProperty)
            // Enable split queries to avoid loading all records at once - provides a good balance of performance and simplicity for deeply nested relationships
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        if (recordGroups.Count == 0)
        {
            logger.LogInformation("No record groups found.");
            return null;
        }

        // Columns for the export table
        var uniqueProperties = dbContext.EntityProperties.Select(e => e.Name).Distinct().ToList();

        var dataTable = new DataTable("DemoExport");

        // Add columns for each property
        foreach (var propertyName in uniqueProperties) dataTable.Columns.Add(propertyName);

        var usersEntity = dbContext.Entities.First(e => e.Name == "users.Sheet1");

        foreach (var recordGroup in recordGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var userEntityRecord = recordGroup.RecordGroupLinks
                .Select(rgl => rgl.Record)
                .First(r => r.Values.Any(v => v.EntityProperty.EntityId == usersEntity.Id));

            var transactionRecords = recordGroup.RecordGroupLinks
                .Select(rgl => rgl.Record)
                .Where(r => r.Id != userEntityRecord.Id);

            const string sumColumnName = "Gutschrift";

            var balanceValues = transactionRecords
                .SelectMany(r => r.Values)
                .Where(v => v.EntityProperty.Name == sumColumnName).ToList()
                .Select(v => v.GetDeserializedValue()).ToList();

            var row = dataTable.NewRow();

            foreach (var value in userEntityRecord.Values) row[value.EntityProperty.Name] = value.GetDeserializedValue();

            row[sumColumnName] = balanceValues.Sum(v => v is decimal d ? d : Convert.ToDecimal(v));

            dataTable.Rows.Add(row);
        }

        return dataTable;
    }
}