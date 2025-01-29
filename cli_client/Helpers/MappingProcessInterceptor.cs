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

using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Any2Any.Prototype.CliClient.Helpers;

public class MappingProcessInterceptor(ILogger<MappingProcessInterceptor> logger)
    : Interceptor
{
    private const string MappingProcessIdHeaderKey = "mapping-process-id";
    private static string? _mappingProcessId;
    private static readonly object Lock = new();

    /// <summary>
    ///     Handles unary (standard async) gRPC calls.
    /// </summary>
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var newContext = AddMappingProcessIdToRequest(context);
        var call = continuation(request, newContext);

        return new(
            call.ResponseAsync.ContinueWith(async responseTask =>
            {
                var response = await responseTask;
                CaptureMappingProcessIdFromResponse(call.GetTrailers());
                return response;
            }).Unwrap(),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    /// <summary>
    ///     Handles server streaming gRPC calls.
    /// </summary>
    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var newContext = AddMappingProcessIdToRequest(context);
        var call = continuation(request, newContext);

        return new(
            call.ResponseStream,
            call.ResponseHeadersAsync.ContinueWith(headersTask =>
            {
                CaptureMappingProcessIdFromResponse(headersTask.Result);
                return headersTask.Result;
            }),
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    /// <summary>
    ///     Handles client streaming gRPC calls (used in file uploads).
    /// </summary>
    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var newContext = AddMappingProcessIdToRequest(context);
        var call = continuation(newContext);

        return new(
            call.RequestStream,
            call.ResponseAsync.ContinueWith(async responseTask =>
            {
                var response = await responseTask;
                CaptureMappingProcessIdFromResponse(call.GetTrailers());
                return response;
            }).Unwrap(),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    /// <summary>
    ///     Handles duplex streaming gRPC calls.
    /// </summary>
    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var newContext = AddMappingProcessIdToRequest(context);
        var call = continuation(newContext);

        return new(
            call.RequestStream,
            call.ResponseStream,
            call.ResponseHeadersAsync.ContinueWith(headersTask =>
            {
                CaptureMappingProcessIdFromResponse(headersTask.Result);
                return headersTask.Result;
            }),
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    /// <summary>
    ///     Adds the MappingProcessId to the request if available.
    /// </summary>
    private ClientInterceptorContext<TRequest, TResponse> AddMappingProcessIdToRequest<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context) where TRequest : class where TResponse : class
    {
        string? mappingProcessId;
        lock (Lock)
        {
            mappingProcessId = _mappingProcessId;
        }

        if (!string.IsNullOrEmpty(mappingProcessId))
        {
            logger.LogDebug($"Adding MappingProcessId: {mappingProcessId} to request.");
            var metadata = new Metadata { new(MappingProcessIdHeaderKey, mappingProcessId) };
            var newOptions = context.Options.WithHeaders(metadata);
            return new(context.Method, context.Host, newOptions);
        }

        return context;
    }

    /// <summary>
    ///     Captures the MappingProcessId from the response headers.
    /// </summary>
    private void CaptureMappingProcessIdFromResponse(Metadata headers)
    {
        var mappingProcessId = headers.FirstOrDefault(h => h.Key == MappingProcessIdHeaderKey)?.Value;

        lock (Lock)
        {
            if (!string.IsNullOrEmpty(mappingProcessId))
            {
                logger.LogInformation($"Received MappingProcessId from server: {mappingProcessId}");
                _mappingProcessId = mappingProcessId;
            }
            else if (_mappingProcessId != null)
            {
                logger.LogInformation("MappingProcessId was not in the response. Removing from cache.");
                _mappingProcessId = null;
            }
        }
    }
}