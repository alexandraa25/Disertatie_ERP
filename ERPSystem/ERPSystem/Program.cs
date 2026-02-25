using ERPSystem.Configuration;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();

builder.ConfigureAllServices();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapAllApiEndpoints();
app.UseSwagger();
app.UseSwaggerUI();

app.MapOpenApi();

app.Run();
