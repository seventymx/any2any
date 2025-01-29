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

using Any2Any.Prototype.Helpers;
using Any2Any.Prototype.Model;
using Microsoft.EntityFrameworkCore;

namespace Any2Any.Prototype.Services;

public class LinkingService(Any2AnyDbContext dbContext, ILogger<LinkingService> logger)
{
    public async Task LinkEntitiesAsync(string propertyName, CancellationToken cancellationToken)
    {
        var properties = await dbContext.EntityProperties
            .Include(p => p.Entity)
            .Where(p => p.Name == propertyName)
            .ToListAsync(cancellationToken);

        if (properties.Count < 2)
        {
            logger.LogWarning("Not enough entities with the specified property to link.");
            return;
        }

        var entities = properties.Select(p => p.Entity).Distinct().ToList();

        // Create links between all matching entities
        var linkedRecords = new List<RecordLink>();

        for (var i = 0; i < entities.Count; i++)
        for (var j = i + 1; j < entities.Count; j++)
        {
            var entity1 = entities[i];
            var entity2 = entities[j];

            var links = entity1.LinkRecords(entity2, propertyName)
                .Select(link => new RecordLink
                {
                    Record1Id = link.Record1.Id,
                    Record2Id = link.Record2.Id
                });

            linkedRecords.AddRange(links);
        }

        await dbContext.RecordLinks.AddRangeAsync(linkedRecords, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Entities successfully linked.");
    }


    public async Task CreateRecordGroupsAsync(CancellationToken cancellationToken)
    {
        var allRecords = await dbContext.Records
            .Include(r => r.Values)
            .ThenInclude(v => v.EntityProperty)
            .ToListAsync(cancellationToken);

        var hasCreatedGroups = false;

        foreach (var record in allRecords)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check if the record is already part of a group
            if (dbContext.RecordGroupLinks.Any(rgl => rgl.RecordId == record.Id))
                continue;

            // Initialize a set to track processed records
            var processedRecords = new HashSet<Guid>();
            var toProcessRecords = new Queue<Guid>();

            toProcessRecords.Enqueue(record.Id);

            // Collect all linked records using recursion-like traversal
            var allLinkedRecords = new List<Record>();

            while (toProcessRecords.Count > 0)
            {
                var currentRecordId = toProcessRecords.Dequeue();

                if (!processedRecords.Add(currentRecordId))
                    continue;

                var currentRecord = await dbContext.Records
                    .Include(r => r.Values)
                    .ThenInclude(v => v.EntityProperty)
                    .FirstOrDefaultAsync(r => r.Id == currentRecordId, cancellationToken);

                if (currentRecord == null) continue;

                allLinkedRecords.Add(currentRecord);

                var linkedRecordIds = dbContext.RecordLinks
                    .Where(link => link.Record1Id == currentRecordId || link.Record2Id == currentRecordId)
                    .Select(link => link.Record1Id == currentRecordId ? link.Record2Id : link.Record1Id)
                    .Distinct();

                foreach (var linkedId in linkedRecordIds)
                    if (!processedRecords.Contains(linkedId))
                        toProcessRecords.Enqueue(linkedId);
            }

            // Create new record group for the linked records
            var recordGroup = new RecordGroup();

            hasCreatedGroups = true;

            dbContext.RecordGroups.Add(recordGroup);
            await dbContext.SaveChangesAsync(cancellationToken);

            var recordGroupLinks = allLinkedRecords.Select(linkedRecord => new RecordGroupLink
            {
                RecordGroupId = recordGroup.Id,
                RecordId = linkedRecord.Id
            });

            dbContext.RecordGroupLinks.AddRange(recordGroupLinks);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!hasCreatedGroups)
        {
            logger.LogInformation("No record groups created.");
            return;
        }

        logger.LogInformation("Record groups created successfully.");
    }
}