/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Author: Steffen70 <steffen@seventy.mx>
 * Creation Date: 2025-01-31
 *
 * Contributors:
 * - Contributor Name <contributor@example.com>
 */

using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Any2Any.Prototype.Helpers;

public class MappingProcessServerInterceptor(IServiceScopeFactory serviceScopeFactory, ILogger<MappingProcessServerInterceptor> logger, IHttpContextAccessor httpContextAccessor)
    : Interceptor
{
    public const string Any2AnyDbContextKey = "Any2AnyDbContext";
    public const string MappingProcessIdHeaderKey = "mapping-process-id";

    /// <summary>
    ///     Handles unary (standard async) gRPC calls.
    /// </summary>
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        await ApplyInterceptorLogic(context);
        return await continuation(request, context);
    }

    /// <summary>
    ///     Handles server streaming gRPC calls.
    /// </summary>
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        await ApplyInterceptorLogic(context);
        // TODO: ResponseTrailers are never added - should they be added after the continuation call?
        await continuation(request, responseStream, context);
    }

    /// <summary>
    ///     Handles client streaming gRPC calls.
    /// </summary>
    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        await ApplyInterceptorLogic(context);
        return await continuation(requestStream, context);
    }

    /// <summary>
    ///     Handles bidirectional streaming gRPC calls.
    /// </summary>
    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        // TODO: Test this interceptor with a bidirectional streaming service - no endpoint available at the moment
        await ApplyInterceptorLogic(context);
        await continuation(requestStream, responseStream, context);
    }

    /// <summary>
    ///     Centralized logic for assigning MappingProcessId and Database Context.
    /// </summary>
    private async Task ApplyInterceptorLogic(ServerCallContext context)
    {
        // Retrieve HttpContext (gRPC requests go through Kestrel, so this is available)
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
            throw new InvalidOperationException("No HttpContext available in gRPC request.");

        // Extract MappingProcessId from gRPC request metadata
        var mappingProcessId = context.RequestHeaders
            .FirstOrDefault(h => h.Key == MappingProcessIdHeaderKey)?
            .Value;

        var isValidGuid = Guid.TryParse(mappingProcessId, out var mappingProcessGuid);

        if (!isValidGuid)
        {
            mappingProcessGuid = Guid.NewGuid();
            logger.LogWarning($"Generated new MappingProcessId: {mappingProcessGuid}");
        }

        // Store MappingProcessId in the context user state (available in gRPC services)
        context.UserState[MappingProcessIdHeaderKey] = mappingProcessGuid;

        // Create a database context per request inside a service scope
        using var scope = serviceScopeFactory.CreateScope();
        var dbOptions = new DbContextOptionsBuilder<Any2AnyDbContext>()
            .UseLazyLoadingProxies()
            .UseSqlite($"Data Source={Path.Combine(AppContext.BaseDirectory, $"{mappingProcessGuid}.db")}")
            .Options;

        var dbContext = new Any2AnyDbContext(dbOptions);
        await dbContext.Database.EnsureCreatedAsync();

        // Store `DbContext` in `HttpContext.Items` so it can be resolved via DI
        httpContext.Items[Any2AnyDbContextKey] = dbContext;

        // Check if the db file still exists - don't add the MappingProcessId to the response trailers if it doesn't
        if (!File.Exists(dbContext.Database.GetDbConnection().DataSource))
            return;

        // Append MappingProcessId as a response trailer
        context.ResponseTrailers.Add(MappingProcessIdHeaderKey, mappingProcessGuid.ToString());
    }
}