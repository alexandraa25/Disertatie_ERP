using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using ERPSystem.Configuration;
using ERPSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.IdentityModel.Tokens;

// Exercise the real routes and JWT middleware; stop before business handlers so no SQL or email is used.
const string secret = "Security-check-only-key-not-for-application-use-123456789";
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
builder.Configuration.Sources.Clear();
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:DefaultConnection"] = "Server=127.0.0.1;Database=Unused;Integrated Security=True",
    ["JwtSettings:SecretKey"] = secret,
    ["JwtSettings:Issuer"] = "security-checks",
    ["JwtSettings:Audience"] = "security-checks",
    ["ERPSystemSettings:BaseUrl"] = "http://localhost:4200"
});
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddHttpContextAccessor();
builder.ConfigureSettings();
builder.ConfigureService();
builder.ConfigureBusinessLogic();
builder.ConfigureContext();
builder.ConfigureCors();
builder.Services.ConfigureApiAuthorization();
builder.Services.AddScoped<PdfService>();
builder.Services.AddHttpClient<INlpAnalysisService, NlpAnalysisService>();
await using var app = builder.Build();
app.UseRouting();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (context.GetEndpoint() is not null)
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }
    await next(context);
});
app.MapAllApiEndpoints();
app.MapGet("/security-check-fallback", () => "Must require authentication");

var publicNames = new HashSet<string>
{
    "ConfirmEmailAsync", "LoginUser", "ConfirmLoginRequest", "ResendLoginCodeAsync",
    "ForgotPasswordAsync", "ResetPasswordAsync", "LogoutUser", "ClientSignContract",
    "GetContractForSigning", "GetFeedbackForm", "SubmitFeedbackForm"
};
await app.StartAsync();
try
{
    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    using var client = new HttpClient { BaseAddress = new Uri(address) };
    var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(s => s.Endpoints).OfType<RouteEndpoint>().ToArray();
    var foundPublic = new HashSet<string>();
    var count = 0;
    foreach (var endpoint in endpoints)
    {
        var name = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName ?? endpoint.RoutePattern.RawText!;
        var anonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
        Check(anonymous == publicNames.Contains(name), $"Unexpected anonymous access: {name}");
        if (anonymous) foundPublic.Add(name);
        var authorization = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        var policies = endpoint.Metadata.GetOrderedMetadata<AuthorizationPolicy>();
        if (!anonymous && name != "/security-check-fallback")
            Check(authorization.Count > 0 || policies.Count > 0, $"Missing endpoint policy: {name}");

        var hasRoles = policies.SelectMany(p => p.Requirements).OfType<RolesAuthorizationRequirement>().Any()
            || authorization.Any(a => !string.IsNullOrEmpty(a.Roles));
        var route = "/" + string.Join("/", endpoint.RoutePattern.PathSegments.Select(segment =>
            string.Concat(segment.Parts.Select(part => part switch
            {
                RoutePatternLiteralPart literal => literal.Content,
                RoutePatternSeparatorPart separator => separator.Content,
                RoutePatternParameterPart parameter => parameter.ParameterPolicies.Any(p => p.Content == "guid")
                    ? "11111111-1111-1111-1111-111111111111" : "1",
                _ => throw new InvalidOperationException("Unsupported route part")
            }))));
        foreach (var method in endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods)
        {
            await Expect(method, route, null, anonymous ? HttpStatusCode.NoContent : HttpStatusCode.Unauthorized);
            await Expect(method, route, Token(), !anonymous && hasRoles ? HttpStatusCode.Forbidden : HttpStatusCode.NoContent);
            await Expect(method, route, Token("Admin"), HttpStatusCode.NoContent);
            count += 3;
        }
    }
    Check(foundPublic.SetEquals(publicNames), "A public token-based flow is missing.");
    await Expect("GET", "/me/profile", "invalid-token", HttpStatusCode.Unauthorized);
    await Expect("GET", "/me/profile", Token("Admin", expired: true), HttpStatusCode.Unauthorized);
    await Expect("GET", "/me/profile", Token("Admin", issuer: "wrong-issuer"), HttpStatusCode.Unauthorized);
    await Expect("GET", "/me/profile", Token("Admin", key: secret + "wrong"), HttpStatusCode.Unauthorized);
    await Expect("POST", "/auth/register", Token("Teacher"), HttpStatusCode.Forbidden);
    await Expect("GET", "/students", Token("HR"), HttpStatusCode.Forbidden);
    await Expect("GET", "/students", Token("Teacher"), HttpStatusCode.NoContent);

    using var preflight = new HttpRequestMessage(HttpMethod.Options, "/students");
    preflight.Headers.Add("Origin", "http://localhost:4200");
    preflight.Headers.Add("Access-Control-Request-Method", "GET");
    preflight.Headers.Add("Access-Control-Request-Headers", "authorization");
    using var preflightResponse = await client.SendAsync(preflight);
    Check(preflightResponse.StatusCode == HttpStatusCode.NoContent, "CORS preflight must not require JWT.");
    Check(preflightResponse.Headers.GetValues("Access-Control-Allow-Origin").Single() == "http://localhost:4200", "CORS origin mismatch.");

    var seed = DatabaseInitializer.LoadTemplates();
    Check(seed.EmailTemplates.Count == 8 && seed.EmailTemplates.Select(t => t.TemplateCode).Distinct().Count() == 8, "Email seed incomplete.");
    Check(seed.ContractTemplates.Count == 2, "Contract seed incomplete.");
    Check(DatabaseInitializer.Roles.Contains("Marketing"), "Marketing role missing.");
    Console.WriteLine($"PASS: {endpoints.Length} routes, {count + 7} JWT/access checks, CORS preflight and embedded seed templates. No database or email calls.");

    async Task Expect(string method, string route, string? token, HttpStatusCode expected)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), route);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        Check(response.StatusCode == expected, $"{method} {route}: expected {(int)expected}, got {(int)response.StatusCode}");
    }
}
finally { await app.StopAsync(); }

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static string Token(string? role = null, bool expired = false, string issuer = "security-checks", string key = secret)
{
    var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "security-check-user") };
    if (role is not null) claims.Add(new Claim(ClaimTypes.Role, role));
    var token = new JwtSecurityToken(issuer, "security-checks", claims,
        notBefore: DateTime.UtcNow.AddHours(-1), expires: DateTime.UtcNow.AddMinutes(expired ? -1 : 5),
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
    return new JwtSecurityTokenHandler().WriteToken(token);
}
