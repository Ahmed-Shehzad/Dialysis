using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>
/// Maps <c>/api/v1/admin/*</c> routes for flow lifecycle and import/export. The per-sub-area route
/// maps live in the sibling <c>ManagementEndpointExtensions.*.cs</c> partial files; this file owns
/// the single entry point that composes them onto one route group.
/// </summary>
public static partial class ManagementEndpointExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>Registers management endpoints (optionally protected by JWT when configured).</summary>
        public IEndpointRouteBuilder MapSmartConnectManagementRoutes()
        {
            var admin = endpoints.MapGroup("/api/v1/admin").WithTags("SmartConnect Admin");

            MapFlowCrudEndpoints(admin);
            MapFlowLifecycleEndpoints(admin);
            MapChannelAttachmentBlobEndpoints(admin);
            MapFlowImportExportEndpoints(admin);
            MapMessageBrowserEndpoints(admin);
            MapConnectorSchemaEndpoints(admin);
            MapScriptDebugEndpoints(admin);

            return endpoints;
        }
    }
}
