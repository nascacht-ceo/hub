using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.OpenApi
{
    public static class OpenApiEndpointExtensions
    {
        /// <summary>
        /// Adds OpenAPI services to the application builder.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <returns>The updated application builder.</returns>
        public static IEndpointRouteBuilder ExtendOpenApi(this IEndpointRouteBuilder builder)
        {
            // Here you can add middleware or services related to OpenAPI
            // For example, you might add Swagger UI or OpenAPI documentation generation

            // Example: Adding Swagger UI
            // builder.UseSwagger();
            // builder.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));

            builder.MapOpenApi();
            return builder;
        }

    }
}
