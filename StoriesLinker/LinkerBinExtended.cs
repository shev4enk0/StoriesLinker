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
                string flowJson = Newtonsoft.Json.JsonConvert.SerializeObject(convertedData, Newtonsoft.Json.Formatting.Indented);
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
                    Form1.ShowMessage($"Articy X file missing: {file}");
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
                Console.WriteLine($"Cleaning up error: {ex.Message}");
            }
        }

        /// <summary>
        /// Тестирует конвертацию Articy X
        /// </summary>
        public void TestArticyXConversion()
        {
            if (!_isArticyX)
            {
                Form1.ShowMessage("Project is not Articy X");
                return;
            }

            try
            {
                Form1.ShowMessage("Starting Articy X conversion test...");
                
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
                            Console.WriteLine($"✅ Object {obj.Properties.Id}: Text = '{obj.Properties.Text}' (localization key)");
                        }
                        else
                        {
                            objectsWithTranslatedText++;
                            Console.WriteLine($"⚠️  Object {obj.Properties.Id}: Text = '{obj.Properties.Text.Substring(0, Math.Min(50, obj.Properties.Text.Length))}...' (translated text)");
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(obj.Properties.DisplayName))
                    {
                        if (obj.Properties.DisplayName.StartsWith("DFr_") || obj.Properties.DisplayName.StartsWith("FFr_") || obj.Properties.DisplayName.StartsWith("Dlg_"))
                        {
                            Console.WriteLine($"✅ Object {obj.Properties.Id}: DisplayName = '{obj.Properties.DisplayName}' (localization key)");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️  Object {obj.Properties.Id}: DisplayName = '{obj.Properties.DisplayName}' (translated text)");
                        }
                    }
                }
                
                Form1.ShowMessage($"Test completed. Checked {checkedObjects} objects, {objectsWithKeys} with localization keys, {objectsWithTranslatedText} with translated text.");
                
                if (objectsWithKeys > 0)
                {
                    Console.WriteLine("✅ TEST PASSED: Localization keys found in Articy 3 style!");
                }
                else
                {
                    Console.WriteLine("❌ TEST FAILED: Localization keys not found!");
                }
            }
            catch (Exception ex)
            {
                Form1.ShowMessage($"Test error: {ex.Message}");
                Console.WriteLine($"Test details: {ex}");
            }
        }
    }
} 