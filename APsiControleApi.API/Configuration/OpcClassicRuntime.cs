using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace APsiControleApi.API.Configuration
{
    internal static class OpcClassicRuntime
    {
        private static readonly string[] AssemblyFiles =
        {
            "OpcNetApi.dll",
            "OpcNetApi.Com.dll",
            "OpcNetApi.Xml.dll"
        };

        private static bool _initialized;

        [ModuleInitializer]
        public static void InitializeModule()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            foreach (var file in AssemblyFiles)
            {
                TryLoad(file);
            }

            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
            _initialized = true;
        }

        private static Assembly? HandleAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return AssemblyFiles.FirstOrDefault(f => string.Equals(Path.GetFileNameWithoutExtension(f), name, StringComparison.OrdinalIgnoreCase)) is { } file
                ? TryLoad(file)
                : null;
        }

        private static Assembly? TryLoad(string fileName)
        {
            foreach (var path in GetProbePaths(fileName))
            {
                try
                {
                    if (File.Exists(path))
                    {
                        return Assembly.LoadFrom(path);
                    }
                }
                catch
                {
                    // ignorado - continuamos tentando outros caminhos
                }
            }

            return null;
        }

        private static IEnumerable<string> GetProbePaths(string fileName)
        {
            var baseDir = AppContext.BaseDirectory?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!string.IsNullOrEmpty(baseDir))
            {
                yield return Path.Combine(baseDir, fileName);
            }

            var current = baseDir;
            for (var i = 0; i < 8 && !string.IsNullOrEmpty(current); i++)
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
                yield return Path.Combine(envPath!, fileName);
                yield return Path.Combine(envPath!, "Libs", "Opc", fileName);
            }
        }
    }
}
