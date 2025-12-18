using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace APsiControleApi.Application.Configuration
{
    /// <summary>
    /// Garante que as DLLs clássicas do OPC Foundation sejam localizadas mesmo
    /// quando o Visual Studio não copia os arquivos para o diretório de saída.
    /// O inicializador de módulo roda assim que o assembly é carregado, antes
    /// que qualquer classe da API do OPC DA seja usada, habilitando o handler
    /// de AssemblyResolve para responder às tentativas de carga.
    /// </summary>
    public static class OpcAssemblyResolver
    {
        private static bool _initialized;

#pragma warning disable CA2255 // o cenário aqui é intencional: o módulo precisa registrar o AssemblyResolve cedo.
        [ModuleInitializer]
        internal static void Initialize()
        {
            EnsureInitialized();
        }
#pragma warning restore CA2255

        public static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            LoadOpcAssembly("OpcNetApi.dll");
            LoadOpcAssembly("OpcNetApi.Com.dll");
            LoadOpcAssembly("OpcNetApi.Xml.dll");

            AppDomain.CurrentDomain.AssemblyResolve += ResolveOpcAssemblies;
            _initialized = true;
        }

        private static Assembly? ResolveOpcAssemblies(object? sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name;
            if (name is null)
            {
                return null;
            }

            return name switch
            {
                "OpcNetApi" => LoadOpcAssembly("OpcNetApi.dll"),
                "OpcNetApi.Com" => LoadOpcAssembly("OpcNetApi.Com.dll"),
                "OpcNetApi.Xml" => LoadOpcAssembly("OpcNetApi.Xml.dll"),
                _ => null
            };
        }

        private static Assembly? LoadOpcAssembly(string fileName)
        {
            try
            {
                foreach (var path in GetProbePaths(fileName))
                {
                    if (File.Exists(path))
                    {
                        return Assembly.LoadFrom(path);
                    }
                }
            }
            catch
            {
                // ignorado de propósito – o CLR ainda tentará os diretórios padrões.
            }

            return null;
        }

        private static IEnumerable<string> GetProbePaths(string fileName)
        {
            var baseDir = AppContext.BaseDirectory;
            if (!string.IsNullOrWhiteSpace(baseDir))
            {
                yield return Path.Combine(baseDir, fileName);
            }

            var current = baseDir?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            for (int i = 0; i < 8 && !string.IsNullOrEmpty(current); i++)
            {
                var libsDir = Path.Combine(current, "Libs", "Opc");
                yield return Path.Combine(libsDir, fileName);

                var parent = Path.GetDirectoryName(current);
                if (string.IsNullOrEmpty(parent) ||
                    string.Equals(parent, current, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                current = parent;
            }

            var envPaths = new[]
            {
                Environment.GetEnvironmentVariable("OPCNETAPI_PATH"),
                Environment.GetEnvironmentVariable("OPC_CLASSIC_DLL_PATH"),
                Environment.GetEnvironmentVariable("SOFTING_OPC_SDK_DIR")
            };

            foreach (var envPath in envPaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                var normalized = envPath!;
                yield return Path.Combine(normalized, fileName);
                yield return Path.Combine(normalized, "Libs", "Opc", fileName);
            }
        }
    }
}
