using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class AddAuthorizationHeader : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var securityRequirements = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<Microsoft.AspNetCore.Mvc.ApiControllerAttribute>()
            .Any();

        if (securityRequirements)
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Swagger_password",
                In = ParameterLocation.Header,
                Required = true,
                Description = "Enter the Swagger password here",
                Schema = new OpenApiSchema { Type = "string" }
            });
        }
    }
}
