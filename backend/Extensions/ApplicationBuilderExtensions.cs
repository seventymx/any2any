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

using System.Reflection;

namespace Any2Any.Prototype.Extensions;

public static class ApplicationBuilderExtensions
{
    private const string ServiceNamespace = ".GrpcServices";

    public static IApplicationBuilder MapGrpcServices(this IApplicationBuilder app, string serviceNamespace = ServiceNamespace)
    {
        // Get the base namespace of the assembly
        var baseNamespace = Assembly.GetExecutingAssembly().GetName().Name;

        // Combine the base namespace with the service namespace
        var serviceTypeNamespace = $"{baseNamespace}{ServiceNamespace}";

        // Get all service types in the assembly
        var serviceTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x => x.Namespace?.StartsWith(serviceTypeNamespace) == true);

        // Filter out nested types - only top-level service types (gRPC services)
        serviceTypes = serviceTypes.Where(x => !x.IsNested);

        // Map each service type
        foreach (var serviceTypeToMap in serviceTypes)
        {
            var method = typeof(GrpcEndpointRouteBuilderExtensions)
                .GetMethod(nameof(GrpcEndpointRouteBuilderExtensions.MapGrpcService))
                !.MakeGenericMethod(serviceTypeToMap);

            _ = method.Invoke(null, [app]);
        }

        return app;
    }
}