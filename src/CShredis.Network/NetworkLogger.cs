using System;
using System.IO;
using System.Threading;

namespace CShredis.Network
{
    public enum LogLevel
    {
        Error = 0,
        Warn = 1,
        Info = 2,
        Debug = 3,
        Trace = 4
    }

    public sealed class NetworkLogger
    {
        private static readonly object _lock = new();
        private static StreamWriter _writer;
        private static bool _alsoConsole;
		private static string _logFilePath = "log/cshredis-server.log";

		private readonly string _componentName;
		private static readonly int _maxLogFileSize = 10000000;

        public static LogLevel MinLevel { get; private set; } = LogLevel.Info;
		
        private NetworkLogger(string componentName)
        {
			_componentName = componentName;
        }

		public static NetworkLogger For(string componentName)
		{
			if (_writer == null)
				throw new InvalidOperationException("Initialize first.");

			return new NetworkLogger(componentName);
		}

        public static void Initialize(LogLevel? minLevel = null, bool alsoConsole = true)
        {
            lock (_lock)
            {
				_alsoConsole = alsoConsole;
                MinLevel = minLevel ?? MinLevel;

				Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath)!);
				if (File.Exists(_logFilePath) && new FileInfo(_logFilePath).Length > _maxLogFileSize)
				{
					var rotated = $"{_logFilePath}.{DateTime.Now:yyyyMMddHHmmssfff}";
					File.Move(_logFilePath, rotated);
				}

				_writer?.Dispose();
				_writer = new StreamWriter(File.Open(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
				{
					AutoFlush = true
				};

                For("Global").Info($"Logger initialized at level {MinLevel} to {_logFilePath}");
            }
        }

        public void Log(LogLevel level, string message)
        {
            if (level > MinLevel) return;

            var cliLine = $"[{level}] [{_componentName}] {message}";
			var logLine = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} {cliLine}";
            lock (_lock)
            {
				if (_writer is null) return;

                _writer.WriteLine(logLine);
                if (_alsoConsole) Console.WriteLine(cliLine);
            }
        }

        public void Error(string msg) => Log(LogLevel.Error, msg);
        public void Warn(string msg) => Log(LogLevel.Warn, msg);
        public void Info(string msg) => Log(LogLevel.Info, msg);
        public void Debug(string msg) => Log(LogLevel.Debug, msg);
        public void Trace(string msg) => Log(LogLevel.Trace, msg);

        public static void SetLevel(LogLevel level)
        {
            lock (_lock)
            {
                MinLevel = level;
                For("Global").Info($"Log level changed to {level}");
            }
        }

        public static void Shutdown()
        {
            lock (_lock)
            {
                _writer?.WriteLine($"Logger shutting down.");
                _writer?.Dispose();
				_writer = null;
            }
        }
    }
}
