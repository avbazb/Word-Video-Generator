using System;
using System.IO;
using System.Windows.Forms;

namespace WordVideoGenerator.Services
{
    public class LogService
    {
        private readonly string _logPath;
        private static LogService _instance;
        private static readonly object _lock = new object();
        private TextBox _logTextBox;

        private LogService()
        {
            _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(_logPath);
        }

        public static LogService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LogService();
                        }
                    }
                }
                return _instance;
            }
        }

        public void SetLogTextBox(TextBox textBox)
        {
            _logTextBox = textBox;
        }

        public void Log(string message, string type = "INFO")
        {
            var logFile = Path.Combine(_logPath, $"{DateTime.Now:yyyy-MM-dd}.log");
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{type}] {message}";
            
            lock (_lock)
            {
                File.AppendAllText(logFile, logMessage + Environment.NewLine);
                
                if (_logTextBox != null)
                {
                    if (_logTextBox.InvokeRequired)
                    {
                        _logTextBox.Invoke(new Action(() => AppendLogToTextBox(logMessage)));
                    }
                    else
                    {
                        AppendLogToTextBox(logMessage);
                    }
                }
            }
        }

        private void AppendLogToTextBox(string message)
        {
            if (_logTextBox.Lines.Length > 1000)  // 限制日志显示行数
            {
                _logTextBox.Clear();
            }
            
            _logTextBox.AppendText(message + Environment.NewLine);
            _logTextBox.SelectionStart = _logTextBox.TextLength;
            _logTextBox.ScrollToCaret();
        }

        public void LogError(string message, Exception ex = null)
        {
            var errorMessage = ex != null 
                ? $"{message}\nException: {ex.Message}\nStackTrace: {ex.StackTrace}"
                : message;
            Log(errorMessage, "ERROR");
        }
    }
} 