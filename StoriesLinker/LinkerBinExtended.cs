using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using System.Linq;

namespace StoriesLinker
{
    /// <summary>
    /// Расширенная версия LinkerBin с поддержкой Articy X
    /// </summary>
    public class LinkerBinExtended : LinkerBin
    {
        private readonly bool _isArticyX;
        private ArticyXAdapter _articyXAdapter;

        public LinkerBinExtended(string projectPath) : base(projectPath)
        {
            _isArticyX = ArticyXAdapter.IsArticyXProject(projectPath);
            
            if (_isArticyX)
            {
                _articyXAdapter = new ArticyXAdapter(projectPath, GetCurrentBaseLanguage());
                PrepareArticyXData();
            }
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
                File.WriteAllText(flowJsonPath + ".temp_marker", "temp");
                
                // Создаем Excel файл локализации как в Articy 3
                _articyXAdapter.CreateLocalizationExcelFile();

                Form1.ShowMessage("Данные Articy X успешно конвертированы в формат Articy 3");
            }
            catch (Exception ex)
            {
                Form1.ShowMessage($"Ошибка конвертации Articy X: {ex.Message}");
                Console.WriteLine($"Подробности ошибки: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Проверяет, поддерживается ли данный проект (Articy 3 или Articy X)
        /// </summary>
        public static bool IsProjectSupported(string projectPath)
        {
            // Проверяем наличие Flow.json (Articy 3)
            if (File.Exists(GetFlowJsonPath(projectPath)))
                return true;

            // Проверяем наличие папки X (Articy X)
            if (ArticyXAdapter.IsArticyXProject(projectPath))
                return true;

            return false;
        }

        /// <summary>
        /// Адаптирует проект Articy X под формат Articy 3, если необходимо
        /// </summary>
        public void AdaptArticyXIfNeeded()
        {
            if (!ArticyXAdapter.IsArticyXProject(_projectPath))
            {
                Console.WriteLine("Проект использует формат Articy 3 - адаптация не требуется");
                return;
            }

            Console.WriteLine("Обнаружен проект Articy X - начинаем адаптацию...");

            try
            {
                var adapter = new ArticyXAdapter(_projectPath, _baseLanguage);
                
                // Конвертируем данные объектов (заменяя тексты на ключи)
                var convertedData = adapter.ConvertToArticy3Format();
                
                // Создаем Flow.json в формате Articy 3
                string flowJsonPath = GetFlowJsonPath(_projectPath);
                string flowJson = Newtonsoft.Json.JsonConvert.SerializeObject(convertedData, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(flowJsonPath, flowJson);
                Console.WriteLine($"Создан файл: {flowJsonPath}");

                // Создаем файл локализации Excel (с исходными текстами + новыми ключами)
                adapter.CreateLocalizationExcelFile();

                Console.WriteLine("✅ Адаптация Articy X завершена успешно!");
                Console.WriteLine("Теперь проект совместим с форматом Articy 3");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при адаптации Articy X: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Переопределенный метод для получения потока с адаптацией Articy X
        /// </summary>
        public new AjFile GetParsedFlowJsonFile()
        {
            // Выполняем адаптацию при необходимости
            AdaptArticyXIfNeeded();
            
            // Теперь вызываем базовый метод, который работает с уже созданным Flow.json
            return base.GetParsedFlowJsonFile();
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
        /// Получает тип проекта
        /// </summary>
        public string GetProjectType()
        {
            return _isArticyX ? "Articy X" : "Articy 3";
        }

        /// <summary>
        /// Валидация проекта Articy X
        /// </summary>
        public bool ValidateArticyXProject()
        {
            if (!_isArticyX) return true;

            string projectPath = GetProjectPath();
            
            var requiredFiles = new[]
            {
                "manifest.json",
                "hierarchy.json", 
                "global_variables.json"
            };

            foreach (string file in requiredFiles)
            {
                string filePath = Path.Combine(projectPath, "Raw", "X", file);
                if (!File.Exists(filePath))
                {
                    Form1.ShowMessage($"Отсутствует файл Articy X: {file}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Очистка временных файлов
        /// </summary>
        public void CleanupTemporaryFiles()
        {
            if (!_isArticyX) return;

            try
            {
                string projectPath = GetProjectPath();
                string flowJsonPath = GetFlowJsonPath(projectPath);
                string tempMarkerPath = flowJsonPath + ".temp_marker";
                
                if (File.Exists(tempMarkerPath))
                {
                    File.Delete(flowJsonPath);
                    File.Delete(tempMarkerPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка очистки: {ex.Message}");
            }
        }

        /// <summary>
        /// Тестирует конвертацию Articy X
        /// </summary>
        public void TestArticyXConversion()
        {
            if (!_isArticyX)
            {
                Form1.ShowMessage("Проект не является Articy X");
                return;
            }

            try
            {
                Form1.ShowMessage("Начинаем тестирование конвертации Articy X...");
                
                var articyData = _articyXAdapter.ConvertToArticy3Format();
                
                // Проверяем первые несколько объектов
                int checkedObjects = 0;
                int objectsWithKeys = 0;
                int objectsWithTranslatedText = 0;
                
                foreach (var obj in articyData.Packages[0].Models.Take(10))
                {
                    checkedObjects++;
                    
                    if (!string.IsNullOrEmpty(obj.Properties.Text))
                    {
                        // Проверяем, что текст содержит ключи локализации (DFr_xxxxx.Text)
                        if (obj.Properties.Text.StartsWith("DFr_") && obj.Properties.Text.Contains(".Text"))
                        {
                            objectsWithKeys++;
                            Console.WriteLine($"✅ Объект {obj.Properties.Id}: Text = '{obj.Properties.Text}' (ключ локализации)");
                        }
                        else
                        {
                            objectsWithTranslatedText++;
                            Console.WriteLine($"⚠️  Объект {obj.Properties.Id}: Text = '{obj.Properties.Text.Substring(0, Math.Min(50, obj.Properties.Text.Length))}...' (переведенный текст)");
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(obj.Properties.DisplayName))
                    {
                        if (obj.Properties.DisplayName.StartsWith("DFr_") || obj.Properties.DisplayName.StartsWith("FFr_") || obj.Properties.DisplayName.StartsWith("Dlg_"))
                        {
                            Console.WriteLine($"✅ Объект {obj.Properties.Id}: DisplayName = '{obj.Properties.DisplayName}' (ключ локализации)");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️  Объект {obj.Properties.Id}: DisplayName = '{obj.Properties.DisplayName}' (переведенный текст)");
                        }
                    }
                }
                
                Form1.ShowMessage($"Тестирование завершено. Проверено {checkedObjects} объектов, {objectsWithKeys} с ключами локализации, {objectsWithTranslatedText} с переведенным текстом.");
                
                if (objectsWithKeys > 0)
                {
                    Console.WriteLine("✅ ТЕСТ ПРОЙДЕН: Найдены ключи локализации в стиле Articy 3!");
                }
                else
                {
                    Console.WriteLine("❌ ТЕСТ НЕ ПРОЙДЕН: Ключи локализации не найдены!");
                }
            }
            catch (Exception ex)
            {
                Form1.ShowMessage($"Ошибка тестирования: {ex.Message}");
                Console.WriteLine($"Подробности ошибки: {ex}");
            }
        }
    }
} 