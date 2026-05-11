using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Application.Services;
using APsiOpcDaApi.Domain.Interfaces.Repositories;
using APsiOpcDaApi.Infrastructure;
using APsiOpcDaApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using APsiOpcDaApi.Application.Mappings;
using Microsoft.AspNetCore.Authorization;
using APsiOpcDaApi.API.Services;
using APsiOpcDaApi.Application.Infrastructure.HostedServices;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace APsiOpcDaApi.API.Extensions
{
    public static class ServiceExtensions
    {
        public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // ConfiguraÃ§Ã£o do DbContext com Lazy Loading habilitado
            services.AddDbContext<APsiOpcDaApiContext>(options =>
                options.UseLazyLoadingProxies() // Habilita Lazy Loading
                       .UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsqlOptions => npgsqlOptions.CommandTimeout(120)));

            // ConfiguraÃ§Ã£o do Swagger
            services.AddEndpointsApiExplorer();
            ConfigureSwagger(services);

            // ConfiguraÃ§Ã£o do AutoMapper
            services.AddAutoMapper(typeof(MappingProfile));

            // Registro dos RepositÃ³rios e UnitOfWork
            RegisterRepositories(services);

            // Registro dos ServiÃ§os
            RegisterServices(services);

            // ConfiguraÃ§Ã£o da autenticaÃ§Ã£o JWT
            ConfigureAuthentication(services, configuration);

            // ConfiguraÃ§Ã£o da autorizaÃ§Ã£o
            services.AddAuthorization(options =>
            {
                // Definir a polÃ­tica padrÃ£o de autorizaÃ§Ã£o para exigir autenticaÃ§Ã£o
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });
        }

        private static void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "APsiC OPC DA API",
                    Version = "v1",
                    Description = "API de conexÃ£o OPC DA, descoberta, grupos, nÃ³s, tags e apoio x86 para integraÃ§Ã£o legada."
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Informe o JWT no formato: Bearer {token}"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                options.OperationFilter<SwaggerOperationDocumentationFilter>();

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                }
            });
        }

        private static void RegisterRepositories(IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<ILeituraRepository, LeituraRepository>();
            services.AddScoped<IOpcServerRepository, OpcServerRepository>();
            services.AddScoped<IOpcNodeRepository, OpcNodeRepository>();
            services.AddScoped<IOpcGroupRepository, OpcGroupRepository>();
            services.AddScoped<IOpcDiscoveredServerRepository, OpcDiscoveredServerRepository>();
            services.AddScoped<IDatabaseMetadataRepository, DatabaseMetadataRepository>();
        }

        private static void RegisterServices(IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericService<,>), typeof(GenericService<,>));
            services.AddScoped<ITagService, TagService>();
            services.AddScoped<ILeituraService, LeituraService>();
            services.AddHttpContextAccessor();
            services.AddScoped<IUserContextService, UserContextService>();
            services.AddScoped<INotificadorSimulacao, SignalRNotificadorSimulacao>();
            services.AddHostedService<OpcMonitorBackgroundService>();
            services.AddHostedService<DatabaseMonitorBackgroundService>();
            services.AddScoped<IOpcMonitoringService, OpcMonitoringService>();
            services.AddScoped<IOpcServerService, OpcServerService>();
            services.AddScoped<IOpcNodeService, OpcNodeService>();
            services.AddScoped<IOpcDaClientService, OpcDaClientService>();
            services.AddScoped<IOpcBrowserService, OpcBrowserService>();
            services.AddScoped<IOpcGroupService, OpcGroupService>();
            services.AddScoped<IOpcDiscoveryService, OpcDiscoveryService>();
            services.AddScoped<IDatabaseBrowserService, DatabaseBrowserService>();
            services.AddScoped<IDatabaseMonitoringService, DatabaseMonitoringService>();
            // ServiÃ§o de manipulaÃ§Ã£o web que requer arquitetura x86
            services.AddScoped<IWebBrowserService, WebBrowserService>();
        }

        private static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            // Chave secreta usada para gerar o token JWT
            var key = Encoding.ASCII.GetBytes(configuration["Jwt:Secret"] ?? "default-secret-key");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };

                // ðŸ” Permitir o uso do token via query string (necessÃ¡rio para SignalR)
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        // Verifica se o request Ã© para o hub
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/hub/tagsimulacao"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });
        }
    }
}


