using APsiControleApi.API.Extensions;
using FluentValidation;
using FluentValidation.AspNetCore;
using APsiControleApi.Application.Validators;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Http.Features;  // Import necessário para configurar limites

var builder = WebApplication.CreateBuilder(args);

// Configuração global do limite de tamanho de requisição para Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = null;  // Desabilita o limite global de tamanho
});

// Configuração global para limites de requisição em controladores
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;  // Permite arquivos grandes em requisições multipart
});

// Configuração dos serviços
builder.Services.ConfigureServices(builder.Configuration);

// Configuração do CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddControllers(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;

    // Desabilitar temporariamente o limite máximo de requisição por controlador
    options.MaxModelBindingCollectionSize = int.MaxValue;
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
});

// Configuração do FluentValidation
builder.Services.AddFluentValidationAutoValidation()  // Habilita a validação automática do FluentValidation
                .AddFluentValidationClientsideAdapters();  // Habilita a validação no lado cliente

var app = builder.Build();

// Middleware para desabilitar o limite em tempo de execução
app.Use(async (context, next) =>
{
    var maxRequestBodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
    if (maxRequestBodySizeFeature != null)
    {
        maxRequestBodySizeFeature.MaxRequestBodySize = null;  // Remove o limite de requisição por request
    }

    await next.Invoke();
});

// Habilitar o middleware CORS antes do roteamento
app.UseCors("AllowAll");

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();
