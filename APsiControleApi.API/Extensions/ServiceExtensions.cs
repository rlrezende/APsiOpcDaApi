using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Application.Services;
using APsiControleApi.Domain.Interfaces.Repositories;
using APsiControleApi.Infrastructure;
using APsiControleApi.Infrastructure.Repositories;
using APsiControleApi.Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using AutoMapper;
using APsiControleApi.Application.Mappings;
using FluentValidation;
using APsiControleApi.Application.Validators;
using Microsoft.AspNetCore.Authorization;
using APsiControleApi.API.Services;
using APsiControleApi.Application.Infrastructure.HostedServices;

namespace APsiControleApi.API.Extensions
{
    public static class ServiceExtensions
    {
        public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configuração do DbContext com Lazy Loading habilitado
            services.AddDbContext<APsiControleApiContext>(options =>
                options.UseLazyLoadingProxies() // Habilita Lazy Loading
                       .UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // Executar apenas migrações ao inicializar
            ApplyMigrations(services);

            // Configuração do Swagger
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // Configuração do AutoMapper e FluentValidation
            services.AddAutoMapper(typeof(MappingProfile));

            // Registro dos Repositórios e UnitOfWork
            RegisterRepositories(services);

            // Registro dos Serviços Externos
            RegisterExternalServices(services);

            // Registro dos Serviços
            RegisterServices(services);

            // Configuração da autenticação JWT
            ConfigureAuthentication(services, configuration);

            // Configuração da autorização
            services.AddAuthorization(options =>
            {
                // Definir a política padrão de autorização para exigir autenticação
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });
        }

        private static void ApplyMigrations(IServiceCollection services)
        {
            // Cria um provedor de serviços temporário para resolver o DbContext
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<APsiControleApiContext>();

            try
            {
                context.Database.Migrate(); // Aplica as migrações
            }
            catch (Exception ex)
            {
                // Adicione logs apropriados para tratamento de erros
                Console.WriteLine($"Erro ao aplicar migrações: {ex.Message}");
            }
        }

        private static void RegisterRepositories(IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IControleRepository, ControleRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<ILeituraRepository, LeituraRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IOpcServerRepository, OpcServerRepository>();
            services.AddScoped<IOpcNodeRepository, OpcNodeRepository>();
            services.AddScoped<IOpcGroupRepository, OpcGroupRepository>();
            services.AddScoped<IOpcDiscoveredServerRepository, OpcDiscoveredServerRepository>();
            services.AddScoped<IDatabaseMetadataRepository, DatabaseMetadataRepository>();
            services.AddScoped<IDetectModelRepository, DetectModelRepository>();
            services.AddScoped<IDetectTrainingJobRepository, DetectTrainingJobRepository>();
        }

        private static void RegisterExternalServices(IServiceCollection services)
        {
            // Adicionando HttpClient com Polly para resiliência
            services.AddHttpClient<IUnidadeExternalService, UnidadeExternalService>(client =>
            {
                client.BaseAddress = new Uri("https://unidade-service-url");  // Substitua pela URL real do serviço Unidade
            })
            .AddPolicyHandler(GetRetryPolicy());  // Retry 3 vezes em caso de falha

            // Adicione outros serviços externos aqui
        }

        private static void RegisterServices(IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericService<,>), typeof(GenericService<,>));
            services.AddScoped<IControleService, ControleService>();
            services.AddScoped<ITagService, TagService>();
            services.AddScoped<ILeituraService, LeituraService>();
            services.AddHttpContextAccessor();
            services.AddScoped<IUserContextService, UserContextService>();
            services.AddScoped<ISimuladorLeituraService, SimuladorLeituraService>();
            services.AddScoped<INotificadorSimulacao, SignalRNotificadorSimulacao>();
        //    services.AddHostedService<OpcMonitorBackgroundService>();
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
            services.AddScoped<IDetectService, DetectService>();
            
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

                // 🔐 Permitir o uso do token via query string (necessário para SignalR)
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        // Verifica se o request é para o hub
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

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
    }
}
