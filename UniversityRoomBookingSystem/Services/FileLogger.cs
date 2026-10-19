using System;
using System.IO;

namespace UniversityRoomBookingSystem.Services
{
    public class FileLogger
    {
        private readonly string _logFilePath;
        private readonly object _lock = new();

        public string LogFilePath => _logFilePath;
        public string? LastLoggingError { get; private set; }

        public FileLogger(string? logFilePath = null)
        {
            string baseDir = AppContext.BaseDirectory;
            _logFilePath = string.IsNullOrWhiteSpace(logFilePath)
                ? Path.Combine(baseDir, "activity_log.txt")
                : (Path.IsPathRooted(logFilePath) ? logFilePath : Path.Combine(baseDir, logFilePath));
        }

        public bool Log(string message, string level = "INFO")
        {
            lock (_lock)
            {
                try
                {
                    string? dir = Path.GetDirectoryName(_logFilePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    string entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [{level}] {message}{Environment.NewLine}";
                    File.AppendAllText(_logFilePath, entry);
                    LastLoggingError = null;
                    return true;
                }
                catch (Exception ex)
                {
                    LastLoggingError = $"Failed to write log to '{_logFilePath}': {ex.Message}";
                    return false;
                }
            }
        }
    }
}