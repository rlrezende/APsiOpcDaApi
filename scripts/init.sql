-- Verifica se o banco de dados existe antes de criar
DO $$
BEGIN
   IF NOT EXISTS (SELECT FROM pg_database WHERE datname = 'APsiOpcDaApiDb') THEN
      CREATE DATABASE APsiOpcDaApiDb;
   END IF;
END
$$;

-- Conecta-se ao banco de dados criado
\c APsiOpcDaApiDb

-- Verifica se o usuário existe antes de criar
DO $$
BEGIN
   IF NOT EXISTS (SELECT FROM pg_catalog.pg_user WHERE usename = 'postgres') THEN
      CREATE USER postgres WITH PASSWORD 'Teste2010';
      ALTER USER postgres WITH SUPERUSER;
   END IF;
END
$$;

DO $$
BEGIN
   IF NOT EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = 'APsiCDb') THEN
      CREATE SCHEMA APsiCDb AUTHORIZATION postgres;
   END IF;
END
$$;

-- Atribui permissões completas ao usuário no esquema
GRANT ALL PRIVILEGES ON SCHEMA APsiCDb TO postgres;

