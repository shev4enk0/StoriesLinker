using System.Reflection;

namespace StoriesLinker
{
    /// <summary>
    /// Расширенная версия LinkerBin с поддержкой Articy X
    /// </summary>
    public class LinkerBinExtended : LinkerBin
    {
        private readonly bool _isArticyX;
        private readonly ArticyXAdapter _articyXAdapter;

        public LinkerBinExtended(string projectPath) : base(projectPath)
        {
            _isArticyX = ArticyXAdapter.IsArticyXProject(projectPath);
            if (!_isArticyX) return;
            
            // Очищаем старые временные файлы
            CleanupTempFiles();
            
            _articyXAdapter = new ArticyXAdapter(projectPath, GetCurrentBaseLanguage());
            PrepareArticyXData();
        }

        /// <summary>
        /// Получает текущий базовый язык
        /// </summary>
        private string GetCurrentBaseLanguage()
        {
            string projectPath = GetProjectPath();
            string locPath = GetLocalizTablesPath(projectPath);
            
            if (locPath.Contains("_ru.xlsx")) return "Russian";
            if (locPath.Contains("_en.xlsx")) return "English";
            if (locPath.Contains("_pl.xlsx")) return "Polish";
            if (locPath.Contains("_de.xlsx")) return "German";
            if (locPath.Contains("_fr.xlsx")) return "French";
            if (locPath.Contains("_es.xlsx")) return "Spanish";
            if (locPath.Contains("_jp.xlsx")) return "Japanese";
            
            return "Russian";
        }

        /// <summary>
        /// Получает путь к проекту через рефлексию
        /// </summary>
        private string GetProjectPath()
        {
            var fieldInfo = typeof(LinkerBin).GetField("_projectPath", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            return fieldInfo?.GetValue(this) as string;
        }

        /// <summary>
        /// Подготавливает данные Articy X
        /// </summary>
        private void PrepareArticyXData()
        {
            try
            {
                string projectPath = GetProjectPath();
                
                // Конвертируем данные Articy X в формат Articy 3
                var articyData = _articyXAdapter.ConvertToArticy3Format();
                
                // Создаем Flow.json файл как в Articy 3
                string flowJsonPath = GetFlowJsonPath(projectPath);
                string json = JsonConvert.SerializeObject(articyData, Formatting.Indented);
                File.WriteAllText(flowJsonPath, json);
                
                // Создаем Excel файл локализации как в Articy 3
                _articyXAdapter.CreateLocalizationExcelFile();

                Form1.ShowMessage("Articy X data successfully converted to Articy 3 format");
            }
            catch (Exception ex)
            {
                Form1.ShowMessage($"Articy X conversion error: {ex.Message}");
                Console.WriteLine($"Conversion details: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Адаптирует проект Articy X под формат Articy 3, если необходимо
        /// </summary>
        private void AdaptArticyXIfNeeded()
        {
            if (!ArticyXAdapter.IsArticyXProject(_projectPath))
            {
                Console.WriteLine("Project uses Articy 3 format - adaptation not required");
                return;
            }

            Console.WriteLine("Articy X project detected - starting adaptation...");

            try
            {
                var adapter = new ArticyXAdapter(_projectPath, _baseLanguage);
                
                // Конвертируем данные объектов (заменяя тексты на ключи)
                var convertedData = adapter.ConvertToArticy3Format();
                
                // Создаем Flow.json в формате Articy 3
                string flowJsonPath = GetFlowJsonPath(_projectPath);
                string flowJson = JsonConvert.SerializeObject(convertedData, Formatting.Indented);
                File.WriteAllText(flowJsonPath, flowJson);
                Console.WriteLine(ConsoleMessages.FlowJsonSaved(flowJsonPath));

                // Создаем файл локализации Excel (с исходными текстами + новыми ключами)
                adapter.CreateLocalizationExcelFile();

                Console.WriteLine(ConsoleMessages.ArticyDataProcessingComplete());
                Console.WriteLine(ConsoleMessages.ConversionModeDetected("Articy 3"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR during Articy X adaptation: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Переопределенный метод генерации таблиц локализации с поддержкой Articy X
        /// </summary>
        public new bool GenerateLocalizTables()
        {
            // Выполняем адаптацию при необходимости
            AdaptArticyXIfNeeded();
            
            // Теперь вызываем базовый метод
            return base.GenerateLocalizTables();
        }

        /// <summary>
        /// Переопределенный метод генерации выходной папки с поддержкой Articy X
        /// </summary>
        public new bool GenerateOutputFolder()
        {
            // Выполняем адаптацию при необходимости
            AdaptArticyXIfNeeded();
            
            // Теперь вызываем базовый метод
            return base.GenerateOutputFolder();
        }

        /// <summary>
        /// Очищает временные файлы-маркеры из предыдущих запусков
        /// </summary>
        private void CleanupTempFiles()
        {
            try
            {
                string projectPath = GetProjectPath();
                string[] tempFiles = Directory.GetFiles(projectPath, "*.temp_marker", SearchOption.AllDirectories);
                
                foreach (string tempFile in tempFiles)
                {
                    File.Delete(tempFile);
                    Console.WriteLine($"Удален временный файл: {tempFile}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Предупреждение: не удалось очистить временные файлы: {ex.Message}");
            }
        }
    }
} 