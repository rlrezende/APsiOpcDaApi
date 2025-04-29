using APsiControleApi.API.Extensions;
using FluentValidation.AspNetCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using APsiControleApi.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

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
            .WithOrigins("http://localhost:3000") // ✅ endereço exato do seu frontend
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("CorsComAutenticacao"); // 👈 deve estar antes de routing/autenticação


app.UseHttpsRedirection();

app.UseRouting();


app.UseAuthentication();           // ✅ ESSENCIAL para JWT + SignalR
app.UseAuthorization();
app.MapControllers();
app.MapHub<TagSimulacaoHub>("/hub/tagsimulacao");

app.Run();
