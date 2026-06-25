using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace UserService.API.Presentation.OpenApi;

public sealed class AuthorizeOnlyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var hasAuthorize = endpointMetadata.OfType<IAuthorizeData>().Any();
        var hasAllowAnonymous = endpointMetadata.OfType<IAllowAnonymous>().Any();

        if (!hasAuthorize || hasAllowAnonymous)
        {
            operation.Security = [];
        }
    }
}
