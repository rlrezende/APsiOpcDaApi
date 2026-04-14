# APsiOpcDaApi — API OPC DA (Classic)

## Stack
- ASP.NET Core 9.0 · C# · x86 Windows (obrigatório para COM/OPC Classic)
- PostgreSQL (EF Core 9) · SignalR · Quick.OpcNetApi.Com
- Porta: **5003**

## IMPORTANTE
> Esta API deve rodar em **x86** — obrigatório para interop COM do OPC Classic.
> Não funciona em Linux sem bridge configurada (variável `OPC_DA_BRIDGE_URL`).

## Comandos essenciais
```bash
# Rodar API (Windows x86)
dotnet run --project APsiOpcDaApi.API

# Migrations
dotnet ef migrations add NomeMigracao --project APsiOpcDaApi.Infrastructure --startup-project APsiOpcDaApi.API
dotnet ef database update --project APsiOpcDaApi.Infrastructure --startup-project APsiOpcDaApi.API

# Build
dotnet build APsiOpcDaApi.sln
```

## Controllers principais
| Controller | Rota | Função |
|---|---|---|
| OpcDaController | /api/opcda | CRUD servidores OPC DA |
| OpcDiscoveryController | /api/opc-discovery | Scan rede, add manual, test |
| OpcBrowserController | /api/opcbrowser | Browse hierarquia OPC |
| OpcGroupController | /api/opc-groups | Grupos/subscriptions |
| TagController | /api/tag | Tags CRUD |

## Fluxo OPC DA
```
OPC DA Server (COM/DCOM)
  ↓ OpcDaClientService (STA Thread)
  ↓ Leitura via Opc.Da.Server
  ↓ Persiste em tabela Leitura (PostgreSQL)
  ↓ Publica via SignalR /hub/tagsimulacao
```

## Bridge Mode (Linux/remoto)
Se `OPC_DA_BRIDGE_URL` estiver definido:
- POST `[bridge]/read` com `{host, progId, clsId, itemIds[]}`

## Compartilha DB com APsiControleApi
- Mesmo schema `APsiCDb`, mesmo banco `APsiCAthApiDb`
- Entidades espelhadas: OpcServer, OpcGroup, OpcNode, Tag, Leitura
