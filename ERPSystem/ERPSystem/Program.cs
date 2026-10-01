using ERPSystem.Configuration;
using ERPSystem.Data;
using QuestPDF.Infrastructure;
var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureApiAuthorization();
builder.Services.AddHttpContextAccessor();

builder.ConfigureAllServices();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<PdfService>();
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddHttpClient<INlpAnalysisService, NlpAnalysisService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Nlp:BaseUrl"] ?? "http://127.0.0.1:8000");
});

var app = builder.Build();

if (args.Contains("--initialize-database", StringComparer.OrdinalIgnoreCase))
{
    await DatabaseInitializer.InitializeAsync(app.Services, app.Configuration);
    return;
}


app.UseCors("frontend");
// Swagger is a local development interface and must be served before the
// application's default authentication policy is applied to API endpoints.
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

app.MapAllApiEndpoints();

app.MapOpenApi();

app.Run();
