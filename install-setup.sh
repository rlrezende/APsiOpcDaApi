#!/bin/bash

echo "Aguardando a conexão com o banco de dados..."
# Aguarde até que o banco de dados esteja disponível (use um script de espera ou um loop)
until nc -z -v -w30 apsic_postgres 5432; do
   echo "Aguardando o banco de dados iniciar..."
   sleep 5
done

echo "Banco de dados conectado. Executando migrações..."
dotnet ef database update --project APsiControleApi.Infrastructure --startup-project APsiControleApi.API

if [ $? -eq 0 ]; then
    echo "Migrações aplicadas com sucesso."
else
    echo "Erro ao aplicar as migrações."
    exit 1
fi

echo "Executando seed..."
dotnet run --project APsiControleApi.API --no-build -- SeedDatabase

if [ $? -eq 0 ]; then
    echo "Seed aplicado com sucesso."
else
    echo "Erro ao aplicar o seed."
    exit 1
fi

echo "Iniciando a aplicação..."
exec dotnet APsiControleApi.API.dll
