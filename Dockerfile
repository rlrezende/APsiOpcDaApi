# Use a imagem base do .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copie todos os arquivos da solução
COPY . .

# Instale a ferramenta dotnet-ef
RUN dotnet tool install --global dotnet-ef

# Adicione o caminho global das ferramentas ao PATH
ENV PATH="$PATH:/root/.dotnet/tools"

# Restaure e publique a solução
RUN dotnet restore APsiControleApi.API/APsiControleApi.API.csproj
RUN dotnet publish APsiControleApi.API/APsiControleApi.API.csproj -c Release -o out

# Use uma imagem mais leve para rodar a aplicação
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Defina o comando de entrada para iniciar a API
ENTRYPOINT ["dotnet", "APsiControleApi.API.dll"]
