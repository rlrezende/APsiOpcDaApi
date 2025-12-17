#if SOFTING_OPC
using System;
using Microsoft.Extensions.Logging;
using Softing.OPCToolbox;
using Softing.OPCToolbox.Client;

namespace APsiControleApi.Application.Opc
{
    public static class SoftingOpcDaBootstrapper
    {
        private static readonly object SyncRoot = new();
        private static bool _initialized;
        private static bool _shutdownHooked;

        public static void Initialize(ILogger? logger = null)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("Softing OPC Toolkit só está disponível em Windows.");
            }

            lock (SyncRoot)
            {
                if (_initialized)
                {
                    return;
                }

                var application = Application.Instance;
                application.VersionOtb = 447;

                var result = application.Initialize();
                if (!ResultCode.SUCCEEDED(result))
                {
                    throw new InvalidOperationException($"Falha ao inicializar Softing OPC Toolkit. Resultado=0x{result:X8}");
                }

                _initialized = true;
                logger?.LogInformation("Softing OPC Toolkit inicializado.");

                if (!_shutdownHooked)
                {
                    AppDomain.CurrentDomain.ProcessExit += (_, __) => Shutdown(logger);
                    AppDomain.CurrentDomain.DomainUnload += (_, __) => Shutdown(logger);
                    _shutdownHooked = true;
                }
            }
        }

        public static void Shutdown(ILogger? logger = null)
        {
            lock (SyncRoot)
            {
                if (!_initialized)
                {
                    return;
                }

                try
                {
                    Application.Instance.Terminate();
                    logger?.LogInformation("Softing OPC Toolkit finalizado.");
                }
                finally
                {
                    _initialized = false;
                }
            }
        }
    }
}
#endif
