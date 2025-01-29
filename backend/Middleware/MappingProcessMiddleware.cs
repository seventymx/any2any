/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Author: Steffen70 <steffen@seventy.mx>
 * Creation Date: 2025-01-29
 *
 * Contributors:
 * - Contributor Name <contributor@example.com>
 */

using Any2Any.Prototype.Helpers;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Any2Any.Prototype.Middleware;

public class MappingProcessMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory, ILogger<MappingProcessMiddleware> logger)
{
    public const string Any2AnyDbContextKey = "Any2AnyDbContext";
    public const string MappingProcessIdHeaderKey = "mapping-process-id";

    private const string MappingProcessIdKey = "MappingProcessId";

    public async Task Invoke(HttpContext context)
    {
        // Extract MappingProcessId from request headers
        var mappingIdHeader = context.Request.Headers[MappingProcessIdHeaderKey].FirstOrDefault();
        var parsedSuccessfully = Guid.TryParse(mappingIdHeader, out var mappingProcessId);

        if (!parsedSuccessfully)
        {
            mappingProcessId = Guid.NewGuid();
            logger.LogWarning($"Generated new MappingProcessId: {mappingProcessId}");

            // Store the ID in response headers so the client can reuse it
            context.Response.Headers[MappingProcessIdHeaderKey] = mappingProcessId.ToString();
        }

        // Store MappingProcessId in the HttpContext
        context.Items[MappingProcessIdKey] = mappingProcessId;

        // Create a new database context inside a service scope
        using var scope = serviceScopeFactory.CreateScope();

        var options = new DbContextOptionsBuilder<Any2AnyDbContext>()
            .UseSqlite($"Data Source={Path.Combine(AppContext.BaseDirectory, $"{mappingProcessId}.db")}")
            .Options;

        var dbContext = new Any2AnyDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        // Store the newly created `DbContext` in `HttpContext.Items`
        context.Items[Any2AnyDbContextKey] = dbContext;

        await next(context);
    }
}