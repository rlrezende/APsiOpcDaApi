using System;
using APsiControleApi.API.Extensions;
using APsiControleApi.API.Logging;
using FluentValidation.AspNetCore;
using System.IO;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using APsiControleApi.API.Hubs;
using APsiControleApi.Application.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Registra o AssemblyResolve das DLLs OPC DA antes de tocar nos serviços.
OpcAssemblyResolver.EnsureInitialized();

var shouldLogToFile = string.Equals(Environment.GetEnvironmentVariable("LOG_TO_FILE"), "true", StringComparison.OrdinalIgnoreCase)
    || builder.Configuration.GetValue<bool?>("Logging:FileLoggingEnabled") == true;

if (shouldLogToFile)
{
    var logDir = Path.Combine(builder.Environment.ContentRootPath, "resources", "log");
    Directory.CreateDirectory(logDir);

    var sanitizedAppName = builder.Environment.ApplicationName?
        .Replace('.', '-')
        .Replace(' ', '-')
        .ToLowerInvariant()
        ?? "api";

    var logPath = Path.Combine(logDir, $"{sanitizedAppName}.log");
    builder.Logging.AddProvider(new FileLoggerProvider(logPath));
}

// Configuração do Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = null;
});

// SignalR com erros detalhados
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.EnableDetailedErrors = true;
});

// Formulário grande
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;
});

// Serviços da aplicação
builder.Services.ConfigureServices(builder.Configuration);

// CORS para SignalR e frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsComAutenticacao", builder =>
    {
        builder
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:3020") // ✅ endereços permitidos do frontend
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // ✅ necessário para usar token com SignalR
    });
});

// Controllers e JSON
builder.Services.AddControllers(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    options.MaxModelBindingCollectionSize = int.MaxValue;
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
});

// FluentValidation
builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();

var app = builder.Build();

// Remove limite de tamanho por request
app.Use(async (context, next) =>
{
    var maxRequestBodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
    if (maxRequestBodySizeFeature != null)
    {
        maxRequestBodySizeFeature.MaxRequestBodySize = null;
    }

    await next.Invoke();
});

app.UseRouting();

app.UseCors("CorsComAutenticacao"); // 👈 deve estar entre routing e autenticação

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();           // ✅ ESSENCIAL para JWT + SignalR
app.UseAuthorization();
app.MapControllers();
app.MapHub<TagSimulacaoHub>("/hub/tagsimulacao");

app.Run();
