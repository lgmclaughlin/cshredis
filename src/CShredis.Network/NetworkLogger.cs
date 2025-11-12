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
        private static NetworkLogger? _instance;
        private StreamWriter _writer;
        private readonly string _componentName;
        private readonly bool _alsoConsole;
		private static readonly int _maxLogFileSize = 10000000;

        public static LogLevel MinLevel { get; private set; } = LogLevel.Info;
        public static string LogFilePath { get; private set; } = "network.log";

        private NetworkLogger(string componentName, bool alsoConsole)
        {
            _componentName = componentName;
            _alsoConsole = alsoConsole;

            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);

            _writer = new StreamWriter(File.Open(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                AutoFlush = true
            };
        }

        public static void Initialize(string? logFilePath = null, LogLevel? minLevel = null, bool alsoConsole = true)
        {
            lock (_lock)
            {
                if (_instance != null)
                    _instance._writer?.Dispose();

                LogFilePath = logFilePath ?? LogFilePath;
                MinLevel = minLevel ?? MinLevel;

				if (File.Exists(LogFilePath) && new FileInfo(LogFilePath).Length > _maxLogFileSize)
				{
					var rotated = $"{LogFilePath}.{DateTime.Now:yyyyMMddHHmmss}";
					File.Move(LogFilePath, rotated);
				}

                _instance = new NetworkLogger("Global", alsoConsole);
                _instance.Info($"Logger initialized at level {MinLevel} to {LogFilePath}");
            }
        }

        public static NetworkLogger For(string componentName)
        {
            if (_instance == null)
                throw new InvalidOperationException("NetworkLogger not initialized. Call Initialize() first.");

            return new NetworkLogger(componentName, _instance._alsoConsole);
        }

        public void Log(LogLevel level, string message)
        {
            if (level > MinLevel) return;

            var cliLine = $"[{level}] [{_componentName}] {message}";
			var logLine = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} {cliLine}";
            lock (_lock)
            {
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
                _instance?.Info($"Log level changed to {level}");
            }
        }

        public static void Shutdown()
        {
            lock (_lock)
            {
                _instance?.Info("Logger shutting down.");
                _instance?._writer?.Dispose();
                _instance = null;
            }
        }
    }
}
