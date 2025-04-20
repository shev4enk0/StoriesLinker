using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace StoriesLinker
{
    public static class Logger
    {
        // Инициализация консоли для правильной работы с кодировками
        static Logger()
        {
            try
            {
                // Попытка использовать Debug вместо Console для вывода
                InitConsole();
            }
            catch
            {
                // Игнорируем ошибки инициализации
            }
        }

        // Windows API для создания консоли
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        private static void InitConsole()
        {
            // Только для Windows и только если вывод перенаправлен (что обычно при запуске из Rider)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && 
                (Console.IsOutputRedirected || Console.IsErrorRedirected))
            {
                try
                {
                    // Попытка создать новую консоль для приложения
                    AllocConsole();
                    Console.OutputEncoding = Encoding.UTF8;
                    Console.InputEncoding = Encoding.UTF8;
                }
                catch
                {
                    // Если не удалось создать консоль, просто игнорируем
                }
            }
        }

        public enum LogType
        {
            Info,
            Warning,
            Error,
            Success,
            System,
            Section,
            Translation,
            Generation,
            Statistic,
            Process
        }

        // Делегат для проброса сообщений в UI (например, в TextBox)
        public static Action<string> UiLogHandler;

        public static void Log(string message, LogType type = LogType.Info)
        {
            string prefix = GetPrefix(type);
            ConsoleColor color = GetColor(type);
            string prefixedMessage = $"{prefix} {message}";
            
            // Запись в файл (всегда работает с UTF-8)
            WriteToFile($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {prefixedMessage}");
            
            try
            {
                // Вывод в консоль с цветом
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(prefixedMessage);
                Console.ForegroundColor = originalColor;
            }
            catch
            {
                // Если с консолью проблемы, используем Debug.WriteLine
                Debug.WriteLine(prefixedMessage);
            }

            // Проброс в UI, если назначен
            UiLogHandler?.Invoke(message);
        }

        public static void LogRaw(string message)
        {
            // Запись в файл
            WriteToFile($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
            
            try
            {
                // Без префикса и цвета в консоль
                Console.WriteLine(message);
            }
            catch
            {
                // Если с консолью проблемы, используем Debug.WriteLine
                Debug.WriteLine(message);
            }
            
            // Проброс в UI
            UiLogHandler?.Invoke(message);
        }

        private static void WriteToFile(string message)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
                using (StreamWriter writer = new StreamWriter(logPath, true, Encoding.UTF8))
                {
                    writer.WriteLine(message);
                }
            }
            catch
            {
                // Игнорируем ошибки при записи лога
            }
        }

        private static string GetPrefix(LogType type)
        {
            return type switch
            {
                LogType.Error => "[ОШИБКА]",
                LogType.Warning => "[ВНИМАНИЕ]",
                LogType.Success => "[УСПЕХ]",
                LogType.System => "[СИСТЕМА]",
                LogType.Section => "[СЕКЦИЯ]",
                LogType.Translation => "[ПЕРЕВОД]",
                LogType.Generation => "[ГЕНЕРАЦИЯ]",
                LogType.Statistic => "[СТАТИСТИКА]",
                LogType.Process => "[ПРОЦЕСС]",
                _ => "[ИНФО]"
            };
        }

        private static ConsoleColor GetColor(LogType type)
        {
            return type switch
            {
                LogType.Error => ConsoleColor.Red,
                LogType.Warning => ConsoleColor.Yellow,
                LogType.Success => ConsoleColor.Green,
                LogType.System => ConsoleColor.Magenta,
                LogType.Section => ConsoleColor.Cyan,
                LogType.Translation => ConsoleColor.Yellow,
                LogType.Generation => ConsoleColor.Green,
                LogType.Statistic => ConsoleColor.DarkYellow,
                LogType.Process => ConsoleColor.DarkGray,
                _ => ConsoleColor.White
            };
        }
    }
}