using Swashbuckle.AspNetCore.SwaggerGen;
//using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapControllers();

// Serve Swagger UI at the app root (http://localhost:5004/)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SagaOrchestrator v1");
    c.RoutePrefix = string.Empty;
});

app.Run();
