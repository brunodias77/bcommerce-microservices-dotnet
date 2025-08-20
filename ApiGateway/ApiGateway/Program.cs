using ApiGateway.Configurations;
using ApiGateway.Middleware;
using ApiGateway.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar serviços do gateway
builder.Services.AddGatewayServices(builder.Configuration);

// Registrar serviços personalizados
builder.Services.AddScoped<ITokenValidationService, TokenValidationService>();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "B-Commerce API Gateway V1");
        c.RoutePrefix = "swagger";
    });
}

// Middleware personalizado para logging
app.UseMiddleware<RequestLoggingMiddleware>();

// CORS
app.UseCors("AllowSpecificOrigins");

// Autenticação e autorização
app.UseAuthentication();
app.UseAuthorization();

// Health checks
app.MapHealthChecks("/health");

// Controllers
app.MapControllers();

// YARP Reverse Proxy
app.MapReverseProxy();

app.Run();
