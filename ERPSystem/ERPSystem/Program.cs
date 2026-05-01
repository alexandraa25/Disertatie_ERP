using ERPSystem.Configuration;
using QuestPDF.Infrastructure;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();

builder.ConfigureAllServices();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<PdfService>();
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddHttpClient<INlpAnalysisService, NlpAnalysisService>(client =>
{
    client.BaseAddress = new Uri("http://127.0.0.1:8000");
});

var app = builder.Build();


app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapAllApiEndpoints();
app.UseSwagger();
app.UseSwaggerUI();

app.MapOpenApi();

app.Run();
