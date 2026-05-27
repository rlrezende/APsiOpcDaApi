using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using APsiOpcDaApi.Application.Configuration;
using APsiOpcDaApi.Application.DTOs;
using APsiOpcDaApi.Application.Interfaces;
using APsiOpcDaApi.Domain.Enum;
using Microsoft.Extensions.Logging;
using Opc;
using Opc.Da;
using OpcCom;

namespace APsiOpcDaApi.Application.Services
{
    public class OpcDaClientService : IOpcDaClientService
    {
        private static readonly PropertyID[] DefaultPropertyIds;

        private readonly ILogger<OpcDaClientService> _logger;

        private const string BridgeEnvVar = "OPC_DA_BRIDGE_URL";

        static OpcDaClientService()
        {
            OpcAssemblyResolver.Initialize();
            DefaultPropertyIds = new[]
            {
                Property.DESCRIPTION,
                Property.DATATYPE,
                Property.ACCESSRIGHTS,
                Property.EUINFO,
                Property.HIGHEU,
                Property.LOWEU
            };
        }

        public OpcDaClientService(ILogger<OpcDaClientService> logger)
        {
            _logger = logger;
        }

        public bool IsSupported => OperatingSystem.IsWindows();

        public Task<OpcBrowseResultDTO> BrowseAsync(OpcServerDTO server, string? itemId = null)
        {
            EnsureCanUseDa(server);
            return Task.Run(() => RunOnSta(() => BrowseInternal(server, itemId)));
        }

        private bool TryGetBridgeUrl(out string url)
        {
            url = Environment.GetEnvironmentVariable(BridgeEnvVar) ?? string.Empty;
            return !string.IsNullOrWhiteSpace(url);
        }

        private async Task<IReadOnlyList<OpcTagDTO>> ReadViaBridgeAsync(OpcServerDTO server, List<string> itemIds)
        {
            try
            {
                if (!TryGetBridgeUrl(out var url))
                {
                    return Array.Empty<OpcTagDTO>();
                }

                using var client = new System.Net.Http.HttpClient();
                var payload = new
                {
                    host = server.Host ?? "localhost",
                    progId = server.ProgId ?? server.Endpoint ?? string.Empty,
                    clsId = server.ClsId ?? string.Empty,
                    itemIds = itemIds
                };
                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url.TrimEnd('/') + "/read", content);
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                var bridgeResponse = System.Text.Json.JsonSerializer.Deserialize<BridgeReadResponse>(body);
                var tags = new List<OpcTagDTO>();

                if (bridgeResponse?.Items != null)
                {
                    foreach (var item in bridgeResponse.Items)
                    {
                        tags.Add(new OpcTagDTO
                        {
                            NodeId = item.ItemId ?? string.Empty,
                            DisplayName = item.ItemId ?? string.Empty,
                            BrowseName = item.ItemId ?? string.Empty,
                            NodeClass = "Variable",
                            DataType = string.Empty,
                            ValorAtual = item.Value,
                            Quality = item.Quality ?? string.Empty,
                            Timestamp = DateTime.TryParse(item.Timestamp, out var ts) ? ts : DateTime.UtcNow
                        });
                    }
                }

                if (bridgeResponse?.Errors != null && bridgeResponse.Errors.Count > 0)
                {
                    _logger.LogWarning("OPC DA Bridge retornou erros: {Errors}", string.Join("; ", bridgeResponse.Errors));
                }

                return tags;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao ler via OPC DA Bridge");
                return Array.Empty<OpcTagDTO>();
            }
        }

        private sealed class BridgeReadResponse
        {
            public List<BridgeTagValue>? Items { get; set; }
            public List<string>? Errors { get; set; }
        }

        private sealed class BridgeTagValue
        {
            public string? ItemId { get; set; }
            public string? Value { get; set; }
            public string? Quality { get; set; }
            public string? Timestamp { get; set; }
        }

        public Task<IReadOnlyList<OpcTagDTO>> ReadValuesAsync(OpcServerDTO server, IEnumerable<string> itemIds)
        {
            EnsureCanUseDa(server);

            if (itemIds == null)
            {
                return Task.FromResult<IReadOnlyList<OpcTagDTO>>(Array.Empty<OpcTagDTO>());
            }

            var normalizedIds = itemIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizedIds.Count == 0)
            {
                return Task.FromResult<IReadOnlyList<OpcTagDTO>>(Array.Empty<OpcTagDTO>());
            }

            if (TryGetBridgeUrl(out _))
            {
                return ReadViaBridgeAsync(server, normalizedIds);
            }

            return Task.Run(() => RunOnSta(() => ReadInternal(server, normalizedIds)));
        }

        private OpcBrowseResultDTO BrowseInternal(OpcServerDTO server, string? itemId)
        {
            using var scope = CreateConnection(server);

            var nodes = new List<OpcNodeBrowseDTO>();
            var tags = new List<OpcTagDTO>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            ItemIdentifier? startIdentifier = null;
            if (!string.IsNullOrWhiteSpace(itemId))
            {
                startIdentifier = new ItemIdentifier(itemId);
            }

            BrowseRecursive(scope.Server, startIdentifier, nodes, tags, visited);

            return new OpcBrowseResultDTO
            {
                Nodes = nodes,
                Tags = tags
            };
        }

        private void BrowseRecursive(
            Opc.Da.Server server,
            ItemIdentifier? parentIdentifier,
            List<OpcNodeBrowseDTO> nodes,
            List<OpcTagDTO> tags,
            HashSet<string> visited)
        {
            var filters = new BrowseFilters
            {
                BrowseFilter = browseFilter.all,
                ReturnAllProperties = false,
                ReturnPropertyValues = false
            };

            BrowsePosition? position;
            var elements = server.Browse(parentIdentifier, filters, out position) ?? Array.Empty<BrowseElement>();
            var childIdentifiers = ProcessElements(server, elements, nodes, tags, visited);

            while (position != null)
            {
                var next = server.BrowseNext(ref position) ?? Array.Empty<BrowseElement>();
                childIdentifiers.AddRange(ProcessElements(server, next, nodes, tags, visited));
            }

            foreach (var child in childIdentifiers)
            {
                BrowseRecursive(server, child, nodes, tags, visited);
            }
        }

        private List<ItemIdentifier> ProcessElements(
            Opc.Da.Server server,
            IEnumerable<BrowseElement> elements,
            List<OpcNodeBrowseDTO> nodes,
            List<OpcTagDTO> tags,
            HashSet<string> visited)
        {
            var childIdentifiers = new List<ItemIdentifier>();

            foreach (var element in elements)
            {
                var key = BuildVisitedKey(element);
                if (string.IsNullOrWhiteSpace(key) || !visited.Add(key))
                {
                    continue;
                }

                var identifier = !string.IsNullOrWhiteSpace(element.ItemName)
                    ? element.ItemName!
                    : element.Name ?? key;

                var displayName = string.IsNullOrWhiteSpace(element.Name) ? identifier : element.Name!;

                if (element.HasChildren && !string.IsNullOrWhiteSpace(identifier))
                {
                    nodes.Add(new OpcNodeBrowseDTO
                    {
                        NodeId = identifier,
                        DisplayName = displayName,
                        BrowseName = identifier,
                        NodeClass = "Object",
                        HasChildren = true,
                        Description = null,
                        Icon = "folder"
                    });

                    var childIdentifier = CreateItemIdentifier(element);
                    if (childIdentifier != null)
                    {
                        childIdentifiers.Add(childIdentifier);
                    }
                }

                if (element.IsItem || !string.IsNullOrWhiteSpace(element.ItemName))
                {
                    var tag = CreateTagFromElement(server, element, identifier, displayName);
                    tags.Add(tag);
                }
            }

            return childIdentifiers;
        }

        private OpcTagDTO CreateTagFromElement(
            Opc.Da.Server server,
            BrowseElement element,
            string identifier,
            string displayName)
        {
            var tag = new OpcTagDTO
            {
                NodeId = identifier,
                DisplayName = displayName,
                BrowseName = identifier,
                NodeClass = "Variable",
                HasChildren = element.HasChildren,
                Icon = "tag",
                AccessLevel = "Unknown"
            };

            var properties = GetProperties(server, element);
            if (properties != null)
            {
                foreach (ItemProperty property in properties)
                {
                    if (!property.ResultID.Succeeded())
                    {
                        continue;
                    }

                    if (property.ID == Property.DESCRIPTION)
                    {
                        tag.Description = property.Value?.ToString();
                    }
                    else if (property.ID == Property.DATATYPE)
                    {
                        tag.DataType = property.DataType?.FullName ?? property.Value?.ToString() ?? string.Empty;
                    }
                    else if (property.ID == Property.ACCESSRIGHTS && property.Value != null)
                    {
                        tag.AccessLevel = property.Value.ToString() ?? "Unknown";
                    }
                    else if (property.ID == Property.EUINFO)
                    {
                        tag.Unit = property.Value?.ToString();
                    }
                    else if (property.ID == Property.HIGHEU)
                    {
                        tag.MaxValue = ConvertToDouble(property.Value);
                    }
                    else if (property.ID == Property.LOWEU)
                    {
                        tag.MinValue = ConvertToDouble(property.Value);
                    }
                }
            }

            return tag;
        }

        private ItemPropertyCollection? GetProperties(Opc.Da.Server server, BrowseElement element)
        {
            try
            {
                var identifier = CreateItemIdentifier(element) ?? new ItemIdentifier(element.ItemName ?? element.Name ?? string.Empty)
                {
                    ItemPath = element.ItemPath
                };

                var collections = server.GetProperties(new[] { identifier }, DefaultPropertyIds, true);
                return collections?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Erro ao obter propriedades da tag {Tag}", element.ItemName ?? element.Name ?? "??");
                return null;
            }
        }

        private IReadOnlyList<OpcTagDTO> ReadInternal(OpcServerDTO server, List<string> itemIds)
        {
            using var scope = CreateConnection(server);

            var items = itemIds
                .Select(id => new Item { ItemName = id })
                .ToArray();

            ItemValueResult[]? results = null;
            try
            {
                // TENTATIVA 1: Usar Read() normal
                results = scope.Server.Read(items);
            }
            catch (TypeLoadException ex)
            {
                _logger.LogWarning(ex, "❌ ERRO TYPELOAD no Read() - Tentando método alternativo via Browse...");
                
                // ALTERNATIVA: Usar GetProperties via Browse (que funciona!)
                return ReadViaProperties(scope.Server, itemIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao ler valores OPC DA.");
                throw;
            }

            var tags = new List<OpcTagDTO>(itemIds.Count);
            for (var i = 0; i < itemIds.Count; i++)
            {
                var result = results != null && i < results.Length ? results[i] : null;

                var tag = new OpcTagDTO
                {
                    NodeId = itemIds[i],
                    DisplayName = itemIds[i],
                    BrowseName = itemIds[i],
                    NodeClass = "Variable",
                    Icon = "tag",
                    HasChildren = false
                };

                if (result != null && result.ResultID.Succeeded())
                {
                    tag.ValorAtual = FormatValue(result.Value);
                    tag.DataType = result.Value?.GetType().FullName ?? string.Empty;
                    tag.Quality = result.QualitySpecified ? result.Quality.ToString() : "Good";
                    tag.Timestamp = result.TimestampSpecified ? result.Timestamp : null;
                }
                else if (result != null)
                {
                    tag.ValorAtual = "Erro";
                    tag.DataType = string.Empty;
                    tag.Quality = result.ResultID.ToString();
                    tag.Timestamp = null;
                }
                else
                {
                    tag.ValorAtual = "Sem retorno";
                    tag.DataType = string.Empty;
                    tag.Quality = "Unknown";
                    tag.Timestamp = null;
                }

                tags.Add(tag);
            }

            return tags;
        }

        private ServerScope CreateConnection(OpcServerDTO serverDto)
        {
            var host = ResolveHost(serverDto);
            var progId = ResolveProgId(serverDto);

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(progId))
            {
                throw new InvalidOperationException($"Host e ProgId são obrigatórios para servidores OPC DA. Host='{host}', ProgId='{progId}'.");
            }

            var urlString = BuildServerUrl(host, progId, serverDto.ClsId);
            var url = new URL(urlString);
            var server = new Opc.Da.Server(new OpcCom.Factory(), null);

            ConnectData? connectData = null;
            if (!string.IsNullOrWhiteSpace(serverDto.Username))
            {
                var credentials = new NetworkCredential(serverDto.Username, serverDto.Password ?? string.Empty);
                connectData = new ConnectData(credentials);
            }

            _logger.LogInformation("Conectando ao OPC DA. Host={Host}, ProgId={ProgId}, Url={Url}", host, progId, urlString);

            server.Connect(url, connectData);
            _logger.LogInformation("Conexão OPC DA concluída. Host={Host}, ProgId={ProgId}", host, progId);

            return new ServerScope(server);
        }

        private static string BuildServerUrl(string host, string progId, string? clsId)
        {
            var builder = new System.Text.StringBuilder();
            builder.Append("opcda://");
            builder.Append(string.IsNullOrWhiteSpace(host) ? string.Empty : host.Trim());
            builder.Append('/');
            builder.Append(progId.Trim());

            if (!string.IsNullOrWhiteSpace(clsId))
            {
                var formatted = clsId.Trim();
                if (!formatted.StartsWith("{", StringComparison.Ordinal))
                {
                    formatted = "{" + formatted;
                }
                if (!formatted.EndsWith("}", StringComparison.Ordinal))
                {
                    formatted += "}";
                }

                builder.Append('/');
                builder.Append(formatted);
            }

            return builder.ToString();
        }

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

            return server.Endpoint;
        }

        private static ItemIdentifier? CreateItemIdentifier(BrowseElement element)
        {
            if (string.IsNullOrWhiteSpace(element.ItemName) && string.IsNullOrWhiteSpace(element.ItemPath))
            {
                return null;
            }

            return new ItemIdentifier(element.ItemName ?? element.Name ?? string.Empty)
            {
                ItemPath = element.ItemPath
            };
        }

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
                    return string.Join("/", segments);
                }
            }

            return string.IsNullOrWhiteSpace(server.Endpoint) ? null : server.Endpoint;
        }

        private static double? ConvertToDouble(object? value)
        {
            if (value == null)
            {
                return null;
            }

            try
            {
                return System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        private static string? FormatValue(object? value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is Array array)
            {
                var list = array.Cast<object?>().Select(v => FormatValue(v) ?? "null");
                return "[" + string.Join(", ", list) + "]";
            }

            return System.Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static string BuildVisitedKey(BrowseElement element)
        {
            return $"{element.ItemPath ?? string.Empty}|{element.ItemName ?? element.Name ?? string.Empty}";
        }

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

        private static T RunOnSta<T>(Func<T> action)
        {
            if (!OperatingSystem.IsWindows() || Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                return action();
            }

            T result = default!;
            Exception? capturedException = null;

            var thread = new Thread(() =>
            {
                try
                {
                    result = action();
                }
                catch (Exception ex)
                {
                    capturedException = ex;
                }
            })
            {
                IsBackground = true
            };

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (capturedException != null)
            {
                throw capturedException;
            }

            return result;
        }
        private sealed class ServerScope : IDisposable
        {
            public ServerScope(Opc.Da.Server server)
            {
                Server = server;
            }

            public Opc.Da.Server Server { get; }

            public void Dispose()
            {
                try
                {
                    Server.Disconnect();
                }
                catch
                {
                    // ignored
                }
                finally
                {
                    Server.Dispose();
                }
            }
        }

        /// <summary>
        /// Método alternativo para ler valores usando GetProperties (que funciona quando Read() falha)
        /// </summary>
        private IReadOnlyList<OpcTagDTO> ReadViaProperties(Opc.Da.Server server, List<string> itemIds)
        {
            _logger.LogInformation("🔄 Usando método alternativo de leitura via Properties...");
            
            var tags = new List<OpcTagDTO>();
            
            foreach (var itemId in itemIds)
            {
                try
                {
                    // Usar GetProperties para obter valor atual (funciona porque usa Browse internamente)
                    var identifier = new ItemIdentifier(itemId);
                    var properties = server.GetProperties(new[] { identifier }, new[] { Property.VALUE }, false);
                    
                    if (properties?.Length > 0 && properties[0]?.Count > 0)
                    {
                        var valueProperty = properties[0][0];
                        var tag = new OpcTagDTO
                        {
                            NodeId = itemId,
                            DisplayName = itemId,
                            BrowseName = itemId,
                            NodeClass = "Variable",
                            DataType = valueProperty.Value?.GetType().Name ?? "Unknown",
                            ValorAtual = valueProperty.Value?.ToString(),
                            Quality = "Good", // GetProperties não retorna qualidade, assumir Good
                            Timestamp = DateTime.UtcNow
                        };
                        
                        tags.Add(tag);
                        _logger.LogInformation("✅ Tag {TagId} lida via Properties: {Value}", itemId, valueProperty.Value);
                    }
                    else
                    {
                        // Tag não encontrada - adicionar com valor nulo
                        var tag = new OpcTagDTO
                        {
                            NodeId = itemId,
                            DisplayName = itemId,
                            BrowseName = itemId,
                            NodeClass = "Variable",
                            DataType = "Unknown",
                            ValorAtual = null,
                            Quality = "Bad",
                            Timestamp = DateTime.UtcNow
                        };
                        
                        tags.Add(tag);
                        _logger.LogWarning("⚠️ Tag {TagId} não encontrada via Properties", itemId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erro ao ler tag {TagId} via Properties", itemId);
                    
                    // Adicionar tag com erro
                    var errorTag = new OpcTagDTO
                    {
                        NodeId = itemId,
                        DisplayName = itemId,
                        BrowseName = itemId,
                        NodeClass = "Variable",
                        DataType = "Error",
                        ValorAtual = null,
                        Quality = "Bad",
                        Timestamp = DateTime.UtcNow
                    };
                    
                    tags.Add(errorTag);
                }
            }
            
            _logger.LogInformation("📊 Leitura alternativa concluída: {Count} tags processadas", tags.Count);
            return tags;
        }

        public Task<bool> WriteValueAsync(OpcServerDTO server, string itemId, double value)
        {
            EnsureCanUseDa(server);
            return Task.Run(() => RunOnSta(() => WriteInternal(server, itemId, value)));
        }

        private bool WriteInternal(OpcServerDTO serverDto, string itemId, double value)
        {
            using var scope = CreateConnection(serverDto);

            var itemValue = new ItemValue
            {
                ItemName = itemId,
                Value = value,
                Quality = new Quality(qualityBits.good),
                QualitySpecified = true,
                TimestampSpecified = false
            };

            IdentifiedResult[]? results = null;
            try
            {
                results = scope.Server.Write(new[] { itemValue });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao escrever valor OPC DA para {ItemId}", itemId);
                return false;
            }

            if (results == null || results.Length == 0)
            {
                _logger.LogWarning("Write OPC DA para {ItemId} não retornou resultado", itemId);
                return false;
            }

            var ok = results[0].ResultID.Succeeded();
            if (!ok)
                _logger.LogWarning("Write OPC DA falhou para {ItemId}: {Code}", itemId, results[0].ResultID);

            return ok;
        }
    }
}
