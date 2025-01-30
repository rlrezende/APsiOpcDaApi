using APsiControleApi.API.Extensions;
using FluentValidation;
using FluentValidation.AspNetCore;
using APsiControleApi.Application.Validators;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Server.Kestrel.Https;

var builder = WebApplication.CreateBuilder(args);

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
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
});

// Configuração do FluentValidation
builder.Services.AddFluentValidationAutoValidation() // Habilita a validação automática do FluentValidation
                .AddFluentValidationClientsideAdapters(); // Habilita a validação no lado cliente

var app = builder.Build();

// Habilitar o middleware CORS antes do roteamento
app.UseCors("AllowAll");

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();
