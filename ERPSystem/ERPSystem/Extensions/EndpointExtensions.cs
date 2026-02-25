using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ERPSystem.Extensions
{
    public static class EndpointExtensions
    {
        public static RouteHandlerBuilder WithDefaultApiSettings(
            this RouteHandlerBuilder builder,
            string name,
            string summary,
            string requestType,
            bool needAuthorization = true)
        {
            builder
                .WithName(name)
                .WithSummary(summary)
                .WithDescription($"{summary} (request: {requestType})")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status500InternalServerError);

            if (needAuthorization)
                builder.RequireAuthorization();

            return builder;
        }

        public static RouteGroupBuilder CreateApiGroup(
               this IEndpointRouteBuilder app,
               string route,
               string tag,
               bool requireAuth = false,
               string? description = null)
        {
            var group = app.MapGroup(route)
                .WithTags(tag)
                .WithSummary($"{tag} API Endpoints")
                .WithDescription(description ?? $"{tag} related operations");

            if (requireAuth)
            {
                group.RequireAuthorization();
            }
            else
            {
                group.AllowAnonymous(); // 🔴 FOARTE IMPORTANT
            }

            return group;
        }

        public static RouteGroupBuilder CreateSubGroup(
        this RouteGroupBuilder parent,
        string route,
        string tag,
        string? description = null)
        {
            return parent.MapGroup(route)
                .WithTags(tag)
                .WithSummary($"{tag} subgroup")
                .WithDescription(description ?? $"{tag} related operations");
        }
    }
}
