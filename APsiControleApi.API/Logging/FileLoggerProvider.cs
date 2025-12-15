using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace APsiControleApi.API.Logging
{
    internal sealed class FileLoggerProvider : ILoggerProvider
    {
        private readonly TextWriter _writer;
        private readonly LogLevel _minLevel;

        public FileLoggerProvider(string path, LogLevel minLevel = LogLevel.Information)
        {
            var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            var streamWriter = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
            _writer = TextWriter.Synchronized(streamWriter);
            _minLevel = minLevel;
        }

        public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _writer, _minLevel);

        public void Dispose()
        {
            _writer.Dispose();
        }

        private sealed class FileLogger : ILogger
        {
            private readonly string _category;
            private readonly TextWriter _writer;
            private readonly LogLevel _minLevel;

            public FileLogger(string category, TextWriter writer, LogLevel minLevel)
            {
                _category = category;
                _writer = writer;
                _minLevel = minLevel;
            }

            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel && logLevel != LogLevel.None;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel) || formatter == null)
                {
                    return;
                }

                var message = formatter(state, exception);
                if (string.IsNullOrEmpty(message) && exception == null)
                {
                    return;
                }

                var logLine = $"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ} [{logLevel}] {_category} {eventId.Id}:{eventId.Name} - {message}";
                _writer.WriteLine(logLine);

                if (exception != null)
                {
                    _writer.WriteLine(exception);
                }
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();

            public void Dispose()
            {
            }
        }
    }
}
