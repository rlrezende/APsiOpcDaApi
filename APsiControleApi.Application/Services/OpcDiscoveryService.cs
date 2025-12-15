using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using APsiControleApi.Domain.Entities;
using APsiControleApi.Domain.Enum;
using APsiControleApi.Domain.Interfaces.Repositories;
using AutoMapper;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.Net.NetworkInformation;
using System.Net;
using Newtonsoft.Json;

namespace APsiControleApi.Application.Services
{
    public class OpcDiscoveryService : IOpcDiscoveryService
    {
        private readonly IOpcDiscoveredServerRepository _discoveredServerRepository;
        private readonly IOpcServerService _opcServerService;
        private readonly IMapper _mapper;

        public OpcDiscoveryService(
            IOpcDiscoveredServerRepository discoveredServerRepository,
            IOpcServerService opcServerService,
            IMapper mapper)
        {
            _discoveredServerRepository = discoveredServerRepository;
            _opcServerService = opcServerService;
            _mapper = mapper;
        }

        public async Task<OpcDiscoveryResultDTO> ScanNetworkAsync(string? networkRange = null, int timeout = 30)
        {
            var startTime = DateTime.UtcNow;
            var discoveredServers = new List<OpcDiscoveredServerDTO>();

            try
            {
                if (string.IsNullOrEmpty(networkRange))
                {
                    networkRange = GetLocalNetworkRange();
                }

                var ipAddresses = GenerateIpRange(networkRange);
                var tasks = ipAddresses.Select(ip => ScanSingleIpAsync(ip, timeout)).ToArray();
                
                var results = await Task.WhenAll(tasks);
                
                foreach (var result in results.Where(r => r != null))
                {
                    await SaveDiscoveredServerAsync(result, networkRange);
                    discoveredServers.Add(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro durante descoberta: {ex.Message}");
            }

            return new OpcDiscoveryResultDTO
            {
                Servers = discoveredServers,
                ScanDuration = DateTime.UtcNow - startTime,
                TotalFound = discoveredServers.Count,
                NetworkRange = networkRange ?? string.Empty,
                ScanTime = startTime
            };
        }

        public async Task<OpcDiscoveredServerDTO> AddManualServerAsync(string name, string endpoint, string? username = null, string? password = null)
        {
            var isOnline = await TestServerConnectionAsync(endpoint);
            
            var discoveredServer = new OpcDiscoveredServerDTO
            {
                Id = Guid.NewGuid(),
                Name = name,
                Endpoint = endpoint,
                DiscoveryTime = DateTime.UtcNow,
                IsOnline = isOnline,
                SecurityModes = new List<string> { "None" },
                ResponseTime = isOnline ? 100 : 0
            };

            // Salvar e obter o ID do OpcServer criado
            var opcServerId = await SaveDiscoveredServerAsync(discoveredServer);
            
            // Retornar com o ID do OpcServer para que possa ser usado diretamente
            discoveredServer.Id = opcServerId;
            
            return discoveredServer;
        }

        public async Task<List<OpcDiscoveredServerDTO>> GetDiscoveredServersAsync(bool onlineOnly = false)
        {
            var servers = onlineOnly 
                ? await _discoveredServerRepository.GetOnlineServersAsync()
                : await _discoveredServerRepository.GetAllAsync();

            var result = new List<OpcDiscoveredServerDTO>();

            foreach (var server in servers)
            {
                var dto = _mapper.Map<OpcDiscoveredServerDTO>(server);
                
                // Buscar o ID do OpcServer correspondente
                var opcServer = await _opcServerService.GetByEndpointAsync(server.Endpoint);
                if (opcServer != null)
                {
                    dto.Id = opcServer.Id; // Usar o ID do OpcServer
                }
                
                result.Add(dto);
            }

            return result;
        }

        public async Task<bool> TestServerConnectionAsync(string endpoint)
        {
            try
            {
                var config = CreateApplicationConfiguration();
                var selectedEndpoint = CoreClientUtils.SelectEndpoint(config, endpoint, false);
                var endpointConfig = EndpointConfiguration.Create(config);
                var configuredEndpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfig);

                using var session = await Session.Create(config, configuredEndpoint, false, "Test Connection", 5000, null, null);
                return session?.Connected == true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<OpcConnectionStatusDTO> GetConnectionStatusAsync(Guid serverId)
        {
            // Buscar diretamente no sistema de servidores OPC usando o serverId
            var opcServer = await _opcServerService.GetByIdAsync(serverId);
            if (opcServer != null)
            {
                var isConnected = await TestServerConnectionAsync(opcServer.Endpoint);
                
                return new OpcConnectionStatusDTO
                {
                    ServerId = serverId,
                    ServerName = opcServer.Nome,
                    Endpoint = opcServer.Endpoint,
                    IsConnected = isConnected,
                    LastConnection = isConnected ? DateTime.UtcNow : null,
                    Status = isConnected ? "Connected" : "Disconnected",
                    ResponseTime = isConnected ? 100 : 0
                };
            }

            return new OpcConnectionStatusDTO
            {
                ServerId = serverId,
                IsConnected = false,
                Status = "Server not found",
                ServerName = string.Empty,
                Endpoint = string.Empty
            };
        }

        public async Task<List<OpcConnectionStatusDTO>> GetActiveConnectionsAsync()
        {
            var connections = new List<OpcConnectionStatusDTO>();

            // Buscar apenas servidores OPC configurados
            var opcServers = await _opcServerService.GetAllAsync();
            foreach (var server in opcServers)
            {
                var status = await GetConnectionStatusAsync(server.Id);
                if (status.IsConnected)
                {
                    connections.Add(status);
                }
            }

            return connections;
        }

        public async Task<OpcDiscoveredServerDTO> DiscoverLocalhostAsync(int port = 4840)
        {
            var endpoint = $"opc.tcp://localhost:{port}";
            var name = $"Localhost OPC Server (:{port})";

            // Verificar se já existe um OpcServer com esse endpoint
            var existingOpcServer = await _opcServerService.GetByEndpointAsync(endpoint);
            if (existingOpcServer != null)
            {
                // Atualizar status na tabela de descoberta se existir
                var existingDiscovered = await _discoveredServerRepository.GetByEndpointAsync(endpoint);
                if (existingDiscovered != null)
                {
                    var isOnline = await TestServerConnectionAsync(endpoint);
                    existingDiscovered.IsOnline = isOnline;
                    existingDiscovered.DiscoveryTime = DateTime.UtcNow;
                    await _discoveredServerRepository.UpdateAsync(existingDiscovered);
                }

                return new OpcDiscoveredServerDTO
                {
                    Id = existingOpcServer.Id, // Usar ID do OpcServer
                    Name = existingOpcServer.Nome,
                    Endpoint = existingOpcServer.Endpoint,
                    DiscoveryTime = DateTime.UtcNow,
                    IsOnline = await TestServerConnectionAsync(endpoint),
                    SecurityModes = new List<string> { "None" },
                    ResponseTime = 100,
                    NetworkRange = "localhost"
                };
            }

            // Testar conexão
            var isConnected = await TestServerConnectionAsync(endpoint);
            if (!isConnected)
            {
                throw new InvalidOperationException($"Servidor OPC não encontrado em {endpoint}");
            }

            // Criar novo servidor descoberto
            var discoveredServer = new OpcDiscoveredServerDTO
            {
                Id = Guid.NewGuid(),
                Name = name,
                Endpoint = endpoint,
                DiscoveryTime = DateTime.UtcNow,
                IsOnline = true,
                SecurityModes = new List<string> { "None" },
                ResponseTime = 100,
                NetworkRange = "localhost"
            };

            // Salvar e obter o ID do OpcServer criado
            var opcServerId = await SaveDiscoveredServerAsync(discoveredServer);
            discoveredServer.Id = opcServerId; // Usar o ID do OpcServer

            return discoveredServer;
        }

        private async Task<Guid> SaveDiscoveredServerAsync(OpcDiscoveredServerDTO discoveredServer, string? networkRange = null)
        {
            // Salvar na tabela de descoberta
            var existingDiscovered = await _discoveredServerRepository.GetByEndpointAsync(discoveredServer.Endpoint);
            if (existingDiscovered == null)
            {
                var entity = _mapper.Map<OpcDiscoveredServer>(discoveredServer);
                entity.NetworkRange = networkRange ?? string.Empty;
                await _discoveredServerRepository.AddAsync(entity);
            }
            else
            {
                existingDiscovered.IsOnline = discoveredServer.IsOnline;
                existingDiscovered.DiscoveryTime = discoveredServer.DiscoveryTime;
                existingDiscovered.ResponseTime = discoveredServer.ResponseTime;
                await _discoveredServerRepository.UpdateAsync(existingDiscovered);
            }

            // Se online, também salvar como OpcServer para uso nos grupos
            if (discoveredServer.IsOnline)
            {
                var existingOpcServer = await _opcServerService.GetByEndpointAsync(discoveredServer.Endpoint);
                if (existingOpcServer == null)
                {
                    var opcServerDto = new OpcServerDTO
                    {
                        Nome = discoveredServer.Name,
                        Endpoint = discoveredServer.Endpoint,
                        UnidadeId = await GetDefaultUnidadeIdAsync(),
                        Tipo = TipoOpcServer.Ua,
                        Descricao = discoveredServer.ApplicationUri
                    };

                    var createdOpcServer = await _opcServerService.AddAsync(opcServerDto);
                    return createdOpcServer.Id; // Retornar o ID do OpcServer criado
                }
                else
                {
                    return existingOpcServer.Id; // Retornar o ID do OpcServer existente
                }
            }

            // Se não estiver online, retornar um ID temporário (não deve acontecer)
            return Guid.NewGuid();
        }

        private async Task<Guid> GetDefaultUnidadeIdAsync()
        {
            // Implementar lógica para obter unidade padrão
            // Por enquanto, retornar um GUID fixo ou criar uma unidade padrão
            return new Guid("7f9ab23c-9860-4daa-9489-e5806b9f63d1"); // Usar o mesmo GUID que está sendo usado em outros lugares
        }

        private async Task<OpcDiscoveredServerDTO?> ScanSingleIpAsync(string ipAddress, int timeout)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ipAddress, timeout * 1000);
                
                if (reply.Status != IPStatus.Success)
                    return null;

                var endpoint = $"opc.tcp://{ipAddress}:4840";
                var startTime = DateTime.UtcNow;
                
                var isOpcServer = await TestServerConnectionAsync(endpoint);
                var responseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

                if (isOpcServer)
                {
                    return new OpcDiscoveredServerDTO
                    {
                        Id = Guid.NewGuid(),
                        Name = $"OPC Server {ipAddress}",
                        Endpoint = endpoint,
                        DiscoveryTime = DateTime.UtcNow,
                        IsOnline = true,
                        SecurityModes = new List<string> { "None" },
                        ResponseTime = responseTime
                    };
                }
            }
            catch
            {
                // Ignorar erros de conexão
            }

            return null;
        }

        private string GetLocalNetworkRange()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var localIp = host.AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork 
                                         && !IPAddress.IsLoopback(ip));

                if (localIp != null)
                {
                    var ipBytes = localIp.GetAddressBytes();
                    return $"{ipBytes[0]}.{ipBytes[1]}.{ipBytes[2]}.0/24";
                }
            }
            catch
            {
                // Fallback para range comum
            }

            return "192.168.1.0/24";
        }

        private List<string> GenerateIpRange(string networkRange)
        {
            var ips = new List<string>();
            
            try
            {
                var parts = networkRange.Split('/');
                var baseIp = parts[0];
                var cidr = int.Parse(parts[1]);

                var ipParts = baseIp.Split('.').Select(int.Parse).ToArray();
                
                if (cidr == 24)
                {
                    for (int i = 1; i <= 254; i++)
                    {
                        ips.Add($"{ipParts[0]}.{ipParts[1]}.{ipParts[2]}.{i}");
                    }
                }
            }
            catch
            {
                for (int i = 1; i <= 254; i++)
                {
                    ips.Add($"192.168.1.{i}");
                }
            }

            return ips;
        }

        private ApplicationConfiguration CreateApplicationConfiguration()
        {
            return new ApplicationConfiguration
            {
                ApplicationName = "OPC Discovery",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    AutoAcceptUntrustedCertificates = true
                },
                TransportQuotas = new TransportQuotas { OperationTimeout = 5000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 5000 }
            };
        }
    }
}
