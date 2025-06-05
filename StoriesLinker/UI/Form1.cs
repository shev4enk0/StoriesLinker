using Microsoft.Win32;

namespace StoriesLinker
{
    public partial class Form1 : Form
    {
        public static int AvailableChapters;
        private string _projectPath;

        public Form1()
        {
            InitializeComponent();

            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\StoriesLinker");

            if (key != null)
            {
                var path = key.GetValue("LastPath").ToString();

                folderBrowserDialog1.SelectedPath = path;
                path_value.Text = path;

                // Также назначаем путь переменной _projectPath
                _projectPath = path;

                // Обновляем имя проекта в интерфейсе
                string[] pathParts = _projectPath.Split('/', '\\');
                proj_name_value.Text = pathParts[pathParts.Length - 1];

                // Загружаем сохраненное количество глав для этого проекта
                LoadChaptersCountForProject(_projectPath);
            }
            else
            {
                path_value.Text = "-";
                // Устанавливаем значение по умолчанию - 1 глава
                chapters_count_value.Text = "1";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                _projectPath = folderBrowserDialog1.SelectedPath;

                path_value.Text = _projectPath;

                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\StoriesLinker");
                key.SetValue("LastPath", _projectPath);
                key.Close();

                textBox1.Text = _projectPath;

                string[] pathParts = _projectPath.Split('/', '\\');

                proj_name_value.Text = pathParts[pathParts.Length - 1];

                // Загружаем сохраненное количество глав для выбранного проекта
                LoadChaptersCountForProject(_projectPath);
            }
        }

        private void GenerateOutputFolderForBundles(object sender, EventArgs eventArgs)
        {
            // Сохраняем количество глав для текущего проекта
            if (!string.IsNullOrEmpty(_projectPath) && int.TryParse(chapters_count_value.Text, out int chaptersCount))
            {
                SaveChaptersCountForProject(_projectPath, chaptersCount);
                AvailableChapters = chaptersCount; // Устанавливаем значение для использования в LinkerBin
            }

            // СНАЧАЛА проверяем тип проекта
            bool isArticyX = ArticyXAdapter.IsArticyXProject(_projectPath);
            
            if (isArticyX)
            {
                ShowMessage("Найден проект Articy X. Выполняется конвертация в Articy 3...");
                
                try
                {
                    // Создаем LinkerBinExtended, который автоматически выполнит конвертацию
                    var linker = new LinkerBinExtended(_projectPath);
                    
                    // После конвертации проверяем наличие необходимых файлов
                    string flowJsonPath = LinkerBin.GetFlowJsonPath(_projectPath);
                    string stringsXmlPath = LinkerBin.GetLocalizTablesPath(_projectPath);
                    
                    if (!File.Exists(flowJsonPath) || !File.Exists(stringsXmlPath))
                    {
                        ShowMessage("Ошибка: после конвертации не найдены необходимые файлы (Flow.json или таблицы локализации).");
                        return;
                    }
                    
                    ShowMessage("Конвертация завершена. Генерация выходной папки...");
                   
                    bool result = linker.GenerateOutputFolder();
                    if (result)
                    {
                        ShowMessage($"✅ Папка Temp с бандлами создана в: {_projectPath}\\Temp\\");
                    }
                    StartCheckAfterBundleGeneration(result);
                }
                catch (Exception e)
                {
                    ShowMessage("Ошибка при конвертации или генерации: " + e.Message);
                    return;
                }
            }
            else
            {
                // Для Articy 3 используем стандартную логику
                string flowJsonPath = LinkerBin.GetFlowJsonPath(_projectPath);
                string stringsXmlPath = LinkerBin.GetLocalizTablesPath(_projectPath);
                
                if (!File.Exists(flowJsonPath) || !File.Exists(stringsXmlPath))
                {
                    ShowMessage("Отсуствует Flow.json или таблица xml.");
                    return;
                }

                var linker = new LinkerBinExtended(_projectPath);
              
                bool result = linker.GenerateOutputFolder();
                if (result)
                {
                    ShowMessage($"✅ Папка Temp с бандлами создана в: {_projectPath}\\Temp\\");
                }
                StartCheckAfterBundleGeneration(result);
            }
        }

        private void StartCheckAfterBundleGeneration(bool result)
        {
            if (result)
            {
                var linker = new LinkerBin(_projectPath);

                AjLinkerMeta meta = linker.GetParsedMetaInputJsonFile();

                var checker = new LinkerAtlasChecker(meta, meta.Characters);

                Dictionary<string, AjObj> objectsList
                    = linker.GetAricyBookEntities(linker.GetParsedFlowJsonFile(), linker.GetNativeDict());

                foreach (KeyValuePair<string, AjObj> @object in objectsList)
                    if (@object.Value.EType == AjType.Instruction)
                    {
                        string expr = @object.Value.Properties.Expression;

                        if (expr.Contains("Clothes.")) checker.PassClothesInstruction(expr);
                    }

                var checkResult = "";

                if (meta.UniqueId != "Shism_1" && meta.UniqueId != "Shism_2")
                    checkResult = checker.BeginFinalCheck(_projectPath);

                if (string.IsNullOrEmpty(checkResult))
                    ShowMessage("Иерархия для бандлов успешно сгенерирована.");
                else
                    ShowMessage("Ошибка: " + checkResult);
            }
        }

        private void GenerateLocalizTables(object sender, EventArgs eventArgs)
        {
            string chaptersCountText = chapters_count_value.Text;

            if (!int.TryParse(chaptersCountText, out AvailableChapters))
            {
                ShowMessage("Некорректное количество глав");

                return;
            }

            // Сохраняем количество глав для текущего проекта
            if (!string.IsNullOrEmpty(_projectPath))
            {
                SaveChaptersCountForProject(_projectPath, AvailableChapters);
            }

            // СНАЧАЛА проверяем тип проекта
            bool isArticyX = ArticyXAdapter.IsArticyXProject(_projectPath);
            
            if (isArticyX)
            {
                ShowMessage("Найден проект Articy X. Выполняется конвертация в Articy 3...");
                
                try
                {
                    // Создаем LinkerBinExtended, который автоматически выполнит конвертацию
                    var linker = new LinkerBinExtended(_projectPath);
                    
                    // После конвертации проверяем наличие необходимых файлов
                    string flowJsonPath = LinkerBin.GetFlowJsonPath(_projectPath);
                    string stringsXmlPath = LinkerBin.GetLocalizTablesPath(_projectPath);
                    
                    if (!File.Exists(flowJsonPath) || !File.Exists(stringsXmlPath))
                    {
                        ShowMessage("Ошибка: после конвертации не найдены необходимые файлы (Flow.json или таблицы локализации).");
                        return;
                    }
                    
                    ShowMessage("Конвертация завершена. Генерация таблиц локализации...");
                    
                    bool result = linker.GenerateLocalizTables();

                    if (result) ShowMessage("Таблицы локализации успешно сгенерированы.");
                }
                catch (Exception e)
                {
                    ShowMessage("Ошибка при конвертации или генерации: " + e.Message);
                    return;
                }
            }
            else
            {
                // Для Articy 3 используем стандартную логику
                string flowJsonPath = LinkerBin.GetFlowJsonPath(_projectPath);
                string stringsXmlPath = LinkerBin.GetLocalizTablesPath(_projectPath);
                
                if (File.Exists(flowJsonPath) && File.Exists(stringsXmlPath))
                {
                    ShowMessage("Найден проект Articy 3. Генерация началась...");
                }
                else
                {
                    ShowMessage("Отсуствует Flow.json или таблица xml.");
                    return;
                }

                var linker = new LinkerBinExtended(_projectPath);
                
                try
                {
                    bool result = linker.GenerateLocalizTables();

                    if (result) ShowMessage("Таблицы локализации успешно сгенерированы.");
                }
                catch (Exception e)
                {
                    if (e.Message != "") ShowMessage("Ошибка: " + e.Message);
                }
            }
        }

        /// <summary>
        /// Загружает сохраненное количество глав для указанного проекта из реестра
        /// </summary>
        /// <param name="projectPath">Путь к проекту</param>
        private void LoadChaptersCountForProject(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                chapters_count_value.Text = "1";
                return;
            }

            try
            {
                // Создаем уникальный ключ на основе пути проекта
                string projectKey = CreateProjectRegistryKey(projectPath);
                
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\StoriesLinker\Projects\" + projectKey);
                
                if (key != null)
                {
                    var chaptersCount = key.GetValue("ChaptersCount");
                    if (chaptersCount != null && int.TryParse(chaptersCount.ToString(), out int count) && count > 0)
                    {
                        chapters_count_value.Text = count.ToString();
                    }
                    else
                    {
                        chapters_count_value.Text = "1";
                    }
                    key.Close();
                }
                else
                {
                    chapters_count_value.Text = "1";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке количества глав: {ex.Message}");
                chapters_count_value.Text = "1";
            }
        }

        /// <summary>
        /// Сохраняет количество глав для текущего проекта в реестр
        /// </summary>
        /// <param name="projectPath">Путь к проекту</param>
        /// <param name="chaptersCount">Количество глав</param>
        private void SaveChaptersCountForProject(string projectPath, int chaptersCount)
        {
            if (string.IsNullOrEmpty(projectPath) || chaptersCount <= 0)
                return;

            try
            {
                // Создаем уникальный ключ на основе пути проекта
                string projectKey = CreateProjectRegistryKey(projectPath);
                
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\StoriesLinker\Projects\" + projectKey);
                key.SetValue("ChaptersCount", chaptersCount);
                key.SetValue("ProjectPath", projectPath); // Сохраняем также полный путь для справки
                key.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении количества глав: {ex.Message}");
            }
        }

        /// <summary>
        /// Создает безопасный ключ реестра на основе пути проекта
        /// </summary>
        /// <param name="projectPath">Путь к проекту</param>
        /// <returns>Безопасный ключ для реестра</returns>
        private string CreateProjectRegistryKey(string projectPath)
        {
            // Берем имя папки проекта и заменяем недопустимые символы
            string projectName = Path.GetFileName(projectPath.TrimEnd('\\', '/'));
            
            // Заменяем недопустимые символы на подчеркивания
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                projectName = projectName.Replace(c, '_');
            }
            
            // Добавляем хеш от полного пути для уникальности
            int pathHash = Math.Abs(projectPath.GetHashCode());
            
            return $"{projectName}_{pathHash}";
        }

        public static void ShowMessage(string message)
        {
            Application.OpenForms["Form1"].Controls["textBox2"].Text = message;
        }
    }
}