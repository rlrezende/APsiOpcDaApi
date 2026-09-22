# OPC DA — preflight Windows x86 e diagnóstico COM/DCOM

Este procedimento é a primeira barreira de segurança dos smokes reais da `OPC-002`. Ele somente lê o host local e gera evidência sanitizada. Não abre conexão COM, não ativa servidor OPC, não acessa rede industrial, não lê/escreve tag e não altera o registro do Windows.

## Limite desta etapa

O preflight comprova que o processo de diagnóstico é x86, que o executável publicado é PE x86, que DCOM e OPCEnum estão registrados na visão 32-bit, que o ProgId homologado existe nessa mesma visão e que as autorizações foram atestadas. Resultado `PASS` autoriza apenas iniciar, em etapa posterior e na janela aprovada, os smokes explicitamente liberados; ele não comprova discovery, conexão, browse, read, write, subscription ou reconexão.

Não registre no repositório nem na evidência: hostname/endereço do servidor, ProgId, ItemIds, valores, credenciais, domínio, usuário, ticket interno ou caminhos com identificação do cliente.

## Pré-requisitos

- Windows homologado com .NET 9 x86 disponível e PowerShell x86 (`SysWOW64` em Windows 64-bit ou `System32` em Windows 32-bit).
- Publicação `Release/win-x86` desta branch, sem substituição por `AnyCPU`.
- OPC Core Components e componentes do fabricante instalados pelo instalador aprovado para x86.
- DCOM habilitado conforme política do ambiente e OPCEnum registrado na visão 32-bit.
- Servidor e tags declarados não produtivos pelo responsável funcional.
- Autorização vigente para acesso OPC. Escrita exige autorização específica, valor original capturado por procedimento aprovado e janela de restauração.

Não execute `register-opc-libs.cmd` como correção genérica. A presença de DLL no diretório não comprova que ela seja um servidor COM registrável; instalação/registro deve seguir o pacote do fabricante e a política do ambiente.

## Publicar e executar o preflight

Em um terminal de build, publique sem usar banco ou iniciar a API:

```powershell
dotnet publish .\APsiOpcDaApi.API\APsiOpcDaApi.API.csproj `
  -c Release -r win-x86 --self-contained false `
  -o .\artifacts\opcda-win-x86
```

Abra o PowerShell x86 e execute o diagnóstico. Os valores abaixo são fornecidos fora do repositório e não aparecem no JSON:

```powershell
$powerShellX86 = if ([Environment]::Is64BitOperatingSystem) {
  "$env:WINDIR\SysWOW64\WindowsPowerShell\v1.0\powershell.exe"
} else {
  "$env:WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe"
}

& $powerShellX86 -NoProfile `
  -File .\scripts\opcda-smoke-preflight.ps1 `
  -PublishedApiPath .\artifacts\opcda-win-x86\APsiOpcDaApi.API.exe `
  -ServerProgId $env:APSIC_OPC_SMOKE_PROGID `
  -BrowseItemId $env:APSIC_OPC_SMOKE_BROWSE_ITEM `
  -ReadItemId $env:APSIC_OPC_SMOKE_READ_ITEM `
  -EvidencePath .\artifacts\evidence\opcda-preflight.json `
  -HomologatedEnvironmentConfirmed `
  -OpcAccessAuthorized `
  -NonProductionServerConfirmed `
  -NonProductionTagsConfirmed
```

Para uma janela que inclua write, acrescente `-IncludeWrite`, `-WriteItemId $env:APSIC_OPC_SMOKE_WRITE_ITEM`, `-WriteAuthorized` e `-RestoreValueCaptured`. O script bloqueia o recorte de escrita se qualquer uma dessas três condições faltar. O valor original e o valor temporário nunca são argumentos do script nem entram na evidência.

O exit code é `0` somente quando todos os gates aplicáveis passam. Exit code `2` e resultado `BLOCKED` impedem qualquer smoke real. O script recusa sobrescrever evidência existente.

## Diagnóstico de falhas

| Check | Diagnóstico permitido | Tratamento |
|---|---|---|
| `WindowsHost` | Confirmar que o comando foi aberto no agente Windows homologado. | Não contornar com VM/host não aprovado. |
| `ProcessX86` | Em Windows 64-bit, conferir o caminho `SysWOW64\WindowsPowerShell`; em Windows 32-bit, usar `System32\WindowsPowerShell`. | Reabrir o shell correto; não usar PowerShell x64. |
| `PublishedApiIsX86` | Repetir `dotnet publish -r win-x86` e conferir que o `.exe` veio dessa saída. | Não alterar o alvo produtivo para fazer o check passar. |
| `DcomEnabled` | Validar com a equipe Windows a política em `HKLM\SOFTWARE\Microsoft\Ole\EnableDCOM`. | Mudança de DCOM exige procedimento administrativo aprovado. |
| `OpcEnumRegistered32Bit` | Verificar a instalação aprovada dos OPC Core Components x86. | Reinstalar/reparar pelo pacote aprovado; não registrar DLLs às cegas. |
| `ServerProgIdRegistered32Bit` | Confirmar pacote do fabricante, ProgId e visão de registro 32-bit. | Corrigir instalação/configuração com o responsável do servidor. |
| Atestados de ambiente/acesso/tags | Revalidar autorização e classificação não produtiva. | Não executar OPC até todos os responsáveis confirmarem. |
| `WriteSafetySatisfied` | Confirmar tag segura, autorização específica e captura do valor original. | Remover `-IncludeWrite` ou obter as três condições antes da janela. |

Não abra portas, não afrouxe autenticação DCOM e não altere identidade de serviço durante o diagnóstico sem change aprovado. Mensagens de erro podem conter nomes do ambiente; compartilhe apenas a matriz booleana sanitizada gerada pelo script.

## Evidência esperada

O JSON contém apenas versão, timestamp UTC, `PASS`/`BLOCKED`, checks booleanos e nomes de checks falhos. Ele registra explicitamente `SensitiveValuesRecorded: false` e `OpcConnectionAttempted: false`. Armazene a evidência fora do repositório e referencie-a na spec apenas por data, responsável e localização controlada.
