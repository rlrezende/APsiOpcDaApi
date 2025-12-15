using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Enum;
using Microsoft.Extensions.Logging;
using TitaniumAS.Opc.Client.Common;
using TitaniumAS.Opc.Client.Da;
using TitaniumAS.Opc.Client.Da.Browsing;

namespace APsiControleApi.Application.Services
{
    /// <summary>
    /// Serviço de acesso a servidores OPC DA.
    /// Fluxo típico:
    /// 1) Descoberta do servidor (host / ProgId) em outro ponto da aplicação;
    /// 2) Navegação (BrowseAsync) para listar nós/tags;
    /// 3) Leitura de valores (ReadValuesAsync) passando os itemIds.
    /// 
    /// IMPORTANTE:
    /// - Necessário chamar TitaniumAS.Opc.Client.Bootstrap.Initialize()
    ///   no Program/Main da aplicação (uma vez por processo).
    /// - OPC DA só funciona em Windows (COM/DCOM).
    /// </summary>
    public class OpcDaClientService : IOpcDaClientService
    {
        private readonly ILogger<OpcDaClientService> _logger;

        public OpcDaClientService(ILogger<OpcDaClientService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Indica se o ambiente suporta OPC DA (Windows).
        /// </summary>
        public bool IsSupported => OperatingSystem.IsWindows();

        /// <summary>
        /// Navega pela árvore de itens (nós + tags) de um servidor OPC DA.
        /// Se itemId for null, começa a partir da raiz.
        /// </summary>
        public async Task<OpcBrowseResultDTO> BrowseAsync(OpcServerDTO server, string? itemId = null)
        {
            EnsureCanUseDa(server);

            return await Task.Run(() => RunOnSta(() => BrowseInternal(server, itemId)));
        }

        /// <summary>
        /// Lê os valores atuais de uma lista de itemIds de um servidor OPC DA.
        /// </summary>
        public async Task<IReadOnlyList<OpcTagDTO>> ReadValuesAsync(OpcServerDTO server, IEnumerable<string> itemIds)
        {
            EnsureCanUseDa(server);

            var ids = itemIds?
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<string>();

            if (ids.Count == 0)
            {
                return Array.Empty<OpcTagDTO>();
            }

            return await Task.Run(() => RunOnSta(() =>
            {
                using var connection = Connect(server);

                if (!connection.Server.IsConnected)
                {
                    throw new InvalidOperationException("Servidor OPC DA não está conectado. Não é possível ler valores.");
                }

                var results = connection.Server.Read(
                    ids,
                    Enumerable.Repeat(TimeSpan.Zero, ids.Count).ToList()
                );

                var tags = new List<OpcTagDTO>(ids.Count);

                for (int index = 0; index < ids.Count; index++)
                {
                    var value = results[index];

                    tags.Add(new OpcTagDTO
                    {
                        NodeId = ids[index],
                        DisplayName = ids[index],
                        BrowseName = ids[index],
                        NodeClass = "Variable",
                        ValorAtual = value.Value?.ToString(),
                        DataType = value.Value?.GetType().FullName ?? string.Empty,
                        Quality = value.Quality.ToString(),
                        Timestamp = value.Timestamp.UtcDateTime
                    });
                }

                return (IReadOnlyList<OpcTagDTO>)tags;
            }));
        }

        /// <summary>
        /// Implementação interna da navegação (browse).
        /// </summary>
        private OpcBrowseResultDTO BrowseInternal(OpcServerDTO server, string? itemId)
        {
            using var connection = Connect(server);

            if (!connection.Server.IsConnected)
            {
                throw new InvalidOperationException("Servidor OPC DA não está conectado. Não é possível navegar.");
            }

            var browser = new OpcDaBrowserAuto(connection.Server);

            var filter = new OpcDaElementFilter
            {
                ElementType = OpcDaBrowseFilter.All
            };

            // true = também busca propriedades (descrição, tipo, etc.)
            var query = new OpcDaPropertiesQuery(false, new[]
            {
                OpcDaItemPropertyIds.OPC_PROP_DESC,
                OpcDaItemPropertyIds.OPC_PROP_CDT,
                OpcDaItemPropertyIds.OPC_PROP_RIGHTS
            });

            var nodes = new List<OpcNodeBrowseDTO>();
            var tags = new List<OpcTagDTO>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // itemId == null => começa da raiz
            BrowseBranch(browser, itemId, filter, query, nodes, tags, visited);

            return new OpcBrowseResultDTO
            {
                Nodes = nodes,
                Tags = tags
            };
        }

        /// <summary>
        /// Percorre recursivamente um branch da árvore OPC.
        /// </summary>
        private void BrowseBranch(
            OpcDaBrowserAuto browser,
            string? parentItemId,
            OpcDaElementFilter filter,
            OpcDaPropertiesQuery query,
            List<OpcNodeBrowseDTO> nodes,
            List<OpcTagDTO> tags,
            HashSet<string> visited)
        {
            OpcDaBrowseElement[] elements;

            var browseId = parentItemId ?? string.Empty;

            try
            {
                _logger.LogDebug("Browsing OPC DA branch. ItemId={ItemId}", string.IsNullOrEmpty(browseId) ? "<root>" : browseId);
                elements = browser.GetElements(browseId, filter, query) ?? Array.Empty<OpcDaBrowseElement>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao navegar em '{ItemId}'.", string.IsNullOrEmpty(browseId) ? "<root>" : browseId);
                return;
            }

            foreach (var element in elements)
            {
                var identifier = element.ItemId ?? element.Name;
                if (string.IsNullOrWhiteSpace(identifier))
                {
                    continue;
                }

                // evita loops / referências circulares
                if (!visited.Add(identifier))
                {
                    continue;
                }

                var displayName = !string.IsNullOrWhiteSpace(element.Name)
                    ? element.Name
                    : identifier;

                var description = GetPropertyValue<string>(element, OpcDaItemPropertyIds.OPC_PROP_DESC);
                var dataType = GetDataTypeName(element);
                var accessRights = GetPropertyValue<OpcDaAccessRights?>(element, OpcDaItemPropertyIds.OPC_PROP_RIGHTS)?.ToString();

                // Nó (pasta / objeto)
                if (element.HasChildren)
                {
                    nodes.Add(new OpcNodeBrowseDTO
                    {
                        NodeId = identifier,
                        DisplayName = displayName,
                        BrowseName = identifier,
                        NodeClass = "Object",
                        HasChildren = true,
                        Description = description,
                        Icon = "folder"
                    });

                    // recursão para navegar dentro do branch
                    BrowseBranch(browser, identifier, filter, query, nodes, tags, visited);
                }

                // Tag (variável)
                if (element.IsItem)
                {
                    tags.Add(new OpcTagDTO
                    {
                        NodeId = identifier,
                        DisplayName = displayName,
                        BrowseName = identifier,
                        NodeClass = "Variable",
                        HasChildren = false,
                        Description = description,
                        DataType = dataType ?? string.Empty,
                        AccessLevel = accessRights ?? "Unknown",
                        Icon = "tag"
                    });
                }
            }
        }

        /// <summary>
        /// Tenta descobrir o tipo de dado da tag (a partir das propriedades OPC).
        /// </summary>
        private static string? GetDataTypeName(OpcDaBrowseElement element)
        {
            var type = GetPropertyValue<Type>(element, OpcDaItemPropertyIds.OPC_PROP_CDT);
            if (type != null)
            {
                return type.FullName;
            }

            var value = GetPropertyValue<object>(element, OpcDaItemPropertyIds.OPC_PROP_CDT);

            return value switch
            {
                Type runtimeType => runtimeType.FullName,
                int typeCode => ((VarEnum)typeCode).ToString(),
                _ => null
            };
        }

        /// <summary>
        /// Utilitário genérico para ler uma propriedade OPC do elemento.
        /// </summary>
        private static T? GetPropertyValue<T>(OpcDaBrowseElement element, OpcDaItemPropertyIds propertyId)
        {
            var property = element.ItemProperties?.Properties?
                .FirstOrDefault(p => p.PropertyId == (int)propertyId);

            if (property == null || property.Value == null)
            {
                return default;
            }

            if (property.Value is T typed)
            {
                return typed;
            }

            try
            {
                return (T)Convert.ChangeType(property.Value, typeof(T), CultureInfo.InvariantCulture);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Executa uma função dentro de um thread STA, necessário para chamadas COM do OPC DA.
        /// </summary>
        private static T RunOnSta<T>(Func<T> func)
        {
            if (!OperatingSystem.IsWindows() || Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                return func();
            }

            T result = default!;
            ExceptionDispatchInfo? capturedException = null;

            var thread = new Thread(() =>
            {
                try
                {
                    result = func();
                }
                catch (Exception ex)
                {
                    capturedException = ExceptionDispatchInfo.Capture(ex);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            capturedException?.Throw();

            return result;
        }

        /// <summary>
        /// Centraliza validações gerais de uso de OPC DA.
        /// </summary>
        private void EnsureCanUseDa(OpcServerDTO server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (server.Tipo != TipoOpcServer.Da)
            {
                throw new InvalidOperationException("Este serviço só deve ser usado para servidores OPC DA.");
            }

            if (!IsSupported)
            {
                throw new PlatformNotSupportedException("OPC DA só é suportado em ambientes Windows.");
            }
        }

        /// <summary>
        /// Abre conexão com o servidor OPC DA (faz Connect).
        /// </summary>
        private DaConnection Connect(OpcServerDTO server)
        {
            var host = ResolveHost(server);
            var progId = ResolveProgId(server);

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(progId))
            {
                throw new InvalidOperationException(
                    $"Host e ProgId são obrigatórios para servidores OPC DA. Host='{host}', ProgId='{progId}'.");
            }

            _logger.LogInformation("Conectando ao OPC DA. Host={Host}, ProgId={ProgId}", host, progId);

            var opcServer = new OpcDaServer(progId, host);

            try
            {
                // se falhar, vai lançar COMException ou Exception
                opcServer.Connect();
                opcServer.Culture = CultureInfo.InvariantCulture;

                _logger.LogInformation("Conexão OPC DA bem-sucedida. Host={Host}, ProgId={ProgId}", host, progId);

                return new DaConnection(opcServer);
            }
            catch (COMException ex)
            {
                _logger.LogError(
                    ex,
                    "Erro COM ao conectar no OPC DA. Host={Host}, ProgId={ProgId}, HResult=0x{HResult:X8}",
                    host, progId, ex.HResult);

                opcServer.Dispose();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao conectar no OPC DA. Host={Host}, ProgId={ProgId}",
                    host, progId);

                opcServer.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Resolve o host a partir do DTO (Host direto ou a partir do Endpoint).
        /// </summary>
        private static string? ResolveHost(OpcServerDTO server)
        {
            if (!string.IsNullOrWhiteSpace(server.Host))
            {
                return server.Host;
            }

            if (!string.IsNullOrWhiteSpace(server.Endpoint) &&
                Uri.TryCreate(server.Endpoint, UriKind.Absolute, out var endpointUri))
            {
                return endpointUri.Host;
            }

            // fallback: se Endpoint for algo tipo "NOMEHOST" simples
            return server.Endpoint;
        }

        /// <summary>
        /// Resolve o ProgId a partir do DTO (ProgId direto ou a partir do Endpoint).
        /// </summary>
        private static string? ResolveProgId(OpcServerDTO server)
        {
            if (!string.IsNullOrWhiteSpace(server.ProgId))
            {
                return server.ProgId;
            }

            if (!string.IsNullOrWhiteSpace(server.Endpoint) &&
                Uri.TryCreate(server.Endpoint, UriKind.Absolute, out var endpointUri))
            {
                var segments = endpointUri.AbsolutePath
                    .Trim('/')
                    .Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length > 0)
                {
                    // Ex.: opcda://host/Matrikon.OPC.Simulation.1
                    return string.Join("/", segments);
                }
            }

            return null;
        }

        /// <summary>
        /// Wrapper para garantir disconnect/dispose corretos.
        /// </summary>
        private sealed class DaConnection : IDisposable
        {
            public DaConnection(OpcDaServer server)
            {
                Server = server;
            }

            public OpcDaServer Server { get; }

            public void Dispose()
            {
                try
                {
                    if (Server.IsConnected)
                    {
                        Server.Disconnect();
                    }
                }
                catch
                {
                    // ignora erros no disconnect
                }
                finally
                {
                    Server.Dispose();
                }
            }
        }
    }
}
