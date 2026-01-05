# APsiOpcDaApi

Microserviço responsável exclusivamente pelas rotinas de OPC DA/UA usadas pelo desktop (cadastro de servidores/grupos/tags, discovery, browser e jobs de monitoração). O código foi extraído da API original e roda isolado em `net9.0`, alvo `win-x86`, reutilizando as mesmas tabelas do banco existente – nenhuma migration nova é executada aqui.

## Como rodar

```bash
cd APsiOpcDaApi
dotnet restore
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=apsic;Username=postgres;Password=postgres" \
dotnet run --project APsiOpcDaApi.API
```

A URL padrão é `http://0.0.0.0:5100` (configurável via `appsettings.json → OpcDaApi:BaseUrl`). O frontend deve usar `http://localhost:5100/api` para todos os endpoints OPC.

## Publicação x86

```bash
dotnet publish APsiOpcDaApi.API/APsiOpcDaApi.API.csproj \
  -c Release -r win-x86 \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  -o ../../artifacts/APsiOpcDaApi
```

Esse passo também é disparado por `npm run build:artifacts`, que copia o resultado para `apps/APsiOpcDaApi` e embala junto ao Electron.

## Pontos importantes

- **Banco:** usa o mesmo schema e connection string da API principal (`APsiOpcDaApi`). Execute migrations somente no serviço original; aqui apenas consumimos as tabelas já criadas (`OpcServer`, `OpcGroup`, `OpcNode`, `Tag`, `Leitura`, etc.).
- **DLLs OPC DA:** continuam em `APsiOpcDaApi/Libs/Opc/*.dll` e são copiadas para o publish automaticamente.
- **Hosted services:** `OpcMonitorBackgroundService` e `DatabaseMonitorBackgroundService` estão habilitados para manter os cron jobs de leitura.
- **Configuração do frontend:** defina `NEXT_PUBLIC_API_OPC=http://localhost:5100/api` (ou ajuste `public/config.json`) para que os serviços `opc*` e `TagService` usem essa base via `opcApiService`.
- **Empacotamento:** `package.json` e `scripts/setup/config.json` agora referenciam `artifacts/APsiOpcDaApi → apps/APsiOpcDaApi`. Ajuste caminhos parecidos nos instaladores se mudar o nome da pasta.

