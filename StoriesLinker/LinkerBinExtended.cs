using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace StoriesLinker
{
    /// <summary>
    /// Расширенная версия LinkerBin с поддержкой Articy X и Articy 3
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
                
                var articyData = _articyXAdapter.ConvertToArticy3Format();
                
                string flowJsonPath = GetFlowJsonPath(projectPath);
                if (!File.Exists(flowJsonPath))
                {
                    string json = JsonConvert.SerializeObject(articyData, Formatting.Indented);
                    File.WriteAllText(flowJsonPath, json);
                    File.WriteAllText(flowJsonPath + ".temp_marker", "temp");
                }

                string locPath = GetLocalizTablesPath(projectPath);
                if (!File.Exists(locPath))
                {
                    _articyXAdapter.CreateLocalizationExcelFile();
                }

                Form1.ShowMessage("Данные Articy X успешно конвертированы");
            }
            catch (Exception ex)
            {
                Form1.ShowMessage($"Ошибка конвертации Articy X: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Переопределенный метод генерации выходной папки
        /// </summary>
        public new bool GenerateOutputFolder()
        {
            if (_isArticyX)
            {
                Form1.ShowMessage("Обработка проекта Articy X...");
                PrepareArticyXData();
            }

            return base.GenerateOutputFolder();
        }

        /// <summary>
        /// Переопределенный метод генерации таблиц локализации
        /// </summary>
        public new bool GenerateLocalizTables()
        {
            if (_isArticyX)
            {
                Form1.ShowMessage("Генерация таблиц локализации для Articy X...");
                PrepareArticyXData();
            }

            return base.GenerateLocalizTables();
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
                string filePath = Path.Combine(projectPath, "Raw", file);
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
    }
} 