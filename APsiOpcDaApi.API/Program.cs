using System;
using APsiOpcDaApi.API.Extensions;
using APsiOpcDaApi.API.Logging;
using FluentValidation.AspNetCore;
using System.IO;
using System.Data.Common;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using APsiOpcDaApi.API.Hubs;
var builder = WebApplication.CreateBuilder(args);

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

var opcApiBaseUrl = builder.Configuration.GetValue<string>("OpcDaApi:BaseUrl");
if (!string.IsNullOrWhiteSpace(opcApiBaseUrl))
{
    builder.WebHost.UseUrls(opcApiBaseUrl);
}

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

var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
var effectiveConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? string.Empty;

if (!string.IsNullOrWhiteSpace(effectiveConnection))
{
    var csBuilder = new DbConnectionStringBuilder { ConnectionString = effectiveConnection };
    var host = csBuilder.TryGetValue("Host", out var hostObj) ? hostObj?.ToString() : "(n/a)";
    var port = csBuilder.TryGetValue("Port", out var portObj) ? portObj?.ToString() : "(n/a)";
    var database = csBuilder.TryGetValue("Database", out var dbObj) ? dbObj?.ToString() : "(n/a)";
    var username = csBuilder.TryGetValue("Username", out var userObj) ? userObj?.ToString() : "(n/a)";

    startupLogger.LogInformation(
        "DB efetivo APsiOpcDaApi -> Host={Host}; Port={Port}; Database={Database}; Username={Username}",
        host, port, database, username);
}
else
{
    startupLogger.LogWarning("DB efetivo APsiOpcDaApi -> ConnectionString DefaultConnection não encontrada.");
}

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

var enableSwagger = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled");
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "APsiC OPC DA API v1");
        c.DisplayRequestDuration();
        c.EnablePersistAuthorization();
        c.InjectJavascript("/swagger-default-auth.js");
    });

    app.MapGet("/dev/swagger-default-token", () =>
    {
        var token = Environment.GetEnvironmentVariable("SWAGGER_DEFAULT_BEARER_TOKEN");
        return string.IsNullOrWhiteSpace(token)
            ? Results.NoContent()
            : Results.Ok(new { token });
    }).ExcludeFromDescription();
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

