using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using OfficeOpenXml;

namespace StoriesLinker
{
    public partial class Form1 : Form
    {
        #region Константы и поля
        public static int AvailableChapters;

        public const bool RELEASE_MODE = false;
        public const bool ONLY_ENGLISH_MODE = false;
        public const bool FOR_LOCALIZATORS_MODE = true;

        private string _projectPath;
        #endregion

        #region Инициализация формы
        public Form1()
        {
            InitializeComponent();
            LoadLastProjectPath();
            InitializeEventHandlers();
        }

        private void InitializeEventHandlers()
        {
            button1.Click += SelectProjectFolder;
            button3.Click += GenerateLocalizationTables;
            button4.Click += GenerateOutputBundles;
            chapters_count_label.Click += ChaptersCountLabel_Click;
            StartVerificationButton.Click += StartVerificationButton_Click;
        }

        private void LoadLastProjectPath()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\StoriesLinker");

            if (key != null)
            {
                string path = key.GetValue("LastPath").ToString();
                folderBrowserDialog1.SelectedPath = path;
                path_value.Text = path;
            }
            else
            {
                path_value.Text = "-";
            }
        }

        private void Form1_Load(object sender, EventArgs e) { }
        #endregion

        #region Обработка UI событий
        private void SelectProjectFolder(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() != DialogResult.OK) return;
            
            _projectPath = folderBrowserDialog1.SelectedPath;
            path_value.Text = _projectPath;

            SaveProjectPathToRegistry();
            UpdateProjectInfo();
        }

        private void SaveProjectPathToRegistry()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\StoriesLinker");
            key.SetValue("LastPath", _projectPath);
            key.Close();
        }

        private void UpdateProjectInfo()
        {
            textBox1.Text = _projectPath;
            string[] pathParts = _projectPath.Split('/', '\\');
            proj_name_value.Text = pathParts[pathParts.Length - 1];
        }

        private void ChaptersCountLabel_Click(object sender, EventArgs e) { }

        private void StartVerificationButton_Click(object sender, EventArgs e) { }
        #endregion

        #region Генерация метаданных
        private void GenerateCharactersTempMetaTable()
        {
            using var eP = new ExcelPackage();
            var sheet = eP.Workbook.Worksheets.Add("Characters");

            var row = 1;
            var col = 1;

            row++;

            FantasyBook.Init();

            foreach (KeyValuePair<string, FantBookCharacter> pair in FantasyBook.Characters)
            {
                FantBookCharacter chId = pair.Value;

                sheet.Cells[row, col].Value = pair.Key;

                if (chId.ToString().Contains("Sec_"))
                {
                    sheet.Cells[row, col + 1].Value = chId.ToString();
                    sheet.Cells[row, col + 2].Value = chId.ToString();
                    sheet.Cells[row, col + 3].Value = chId.ToString();
                }
                else
                {
                    sheet.Cells[row, col + 1].Value
                        = (FantasyBook.ChClothesVariablesMathcing.ContainsKey(chId)
                               ? FantasyBook.ChClothesVariablesMathcing[chId]
                               : "-");
                    sheet.Cells[row, col + 2].Value
                        = (FantasyBook.ChAtalsMathcing.ContainsKey(chId)
                               ? FantasyBook.ChAtalsMathcing[chId]
                               : "-");
                    sheet.Cells[row, col + 3].Value = chId.ToString();
                }

                row++;
            }

            var bin = eP.GetAsByteArray();
            File.WriteAllBytes(_projectPath + @"\TempMetaCharacters.xlsx", bin);
        }

        private void GenerateLocationsTempMetaTable()
        {
            using var eP = new ExcelPackage();
            var sheet = eP.Workbook.Worksheets.Add("Locations");

            var row = 1;
            var col = 1;

            row++;

            FantasyBook.Init();

            foreach (KeyValuePair<string, FantBookLocation> pair in FantasyBook.Locations)
            {
                FantBookLocation locId = pair.Value;

                sheet.Cells[row, col].Value = 0;
                sheet.Cells[row, col + 1].Value = pair.Key;
                sheet.Cells[row, col + 2].Value = FantasyBook.LocSpriteMatching[locId];
                sheet.Cells[row, col + 3].Value = FantasyBook.LocSoundMatching[locId];
                sheet.Cells[row, col + 4].Value = 0;

                row++;
            }

            var bin = eP.GetAsByteArray();
            File.WriteAllBytes(_projectPath + @"\TempMetaLocations.xlsx", bin);
        }
        #endregion

        #region Генерация бандлов и локализации
        private void GenerateOutputBundles(object sender, EventArgs eventArgs)
        {
            string flowJsonPath = LinkerBin.GetFlowJsonPath(_projectPath);
            string stringsXmlPath = LinkerBin.GetLocalizationTablesPath(_projectPath);

            if (!File.Exists(flowJsonPath) || !File.Exists(stringsXmlPath))
            {
                ShowMessage("Отсуствует Flow.json или таблица xml.");
                return;
            }

            LinkerBin linker = new LinkerBin(_projectPath);

            if (RELEASE_MODE)
            {
                try
                {
                    bool result = linker.GenerateOutputStructure();
                    VerifyGeneratedBundles(result);
                }
                catch (Exception e)
                {
                    if (e.Message != "")
                    {
                        ShowMessage("Ошибка: " + e.Message + " " + e.ToString());
                    }
                }
            }
            else
            {
                bool result = linker.GenerateOutputStructure();
                VerifyGeneratedBundles(result);
            }
        }

        private void VerifyGeneratedBundles(bool result)
        {
            if (!result) return;

            LinkerBin linker = new LinkerBin(_projectPath);
            AjLinkerMeta meta = linker.ParseMetaDataFromExcel();
            LinkerAtlasChecker checker = new LinkerAtlasChecker(meta, meta.Characters);

            Dictionary<string, AjObj> objectsList = 
                linker.ExtractBookEntities(linker.ParseFlowJsonFile(), linker.GetLocalizationDictionary());

            foreach (KeyValuePair<string, AjObj> @object in objectsList)
            {
                if (@object.Value.EType == AjType.Instruction)
                {
                    string expr = @object.Value.Properties.Expression;
                    if (expr.Contains("Clothes."))
                    {
                        checker.ProcessClothesInstruction(expr);
                    }
                }
            }

            string checkResult = "";
            if (meta.UniqueId != "Shism_1" && meta.UniqueId != "Shism_2")
            {
                checkResult = checker.ValidateAtlases(_projectPath);
            }

            ShowMessage(string.IsNullOrEmpty(checkResult) 
                ? "Иерархия для бандлов успешно сгенерирована." 
                : "Ошибка: " + checkResult);
        }

        private void GenerateLocalizationTables(object sender, EventArgs eventArgs)
        {
            if (!ValidateChaptersCount())
                return;

            string flowJsonPath = LinkerBin.GetFlowJsonPath(_projectPath);
            string stringsXmlPath = LinkerBin.GetLocalizationTablesPath(_projectPath);

            if (!File.Exists(flowJsonPath) || !File.Exists(stringsXmlPath))
            {
                ShowMessage("Отсуствует Flow.json или таблица xml.");
                return;
            }

            ShowMessage("Файлы найдены. Генерация началась...");
            LinkerBin linker = new LinkerBin(_projectPath);

            try
            {
                bool result = linker.GenerateLocalizationTables();
                if (result)
                {
                    ShowMessage("Таблицы локализации успешно сгенерированы.");
                }
            }
            catch (Exception e)
            {
                if (e.Message != "")
                {
                    ShowMessage("Ошибка: " + e.Message);
                }
            }
        }

        private bool ValidateChaptersCount()
        {
            string chaptersCountText = chapters_count_value.Text;
            if (!int.TryParse(chaptersCountText, out AvailableChapters))
            {
                ShowMessage("Некорректное количество глав");
                return false;
            }
            return true;
        }
        #endregion

        #region Логирование
        public static void ShowMessage(string message)
        {
            string prefixedMessage = FormatMessage(message);
            WriteToConsole(prefixedMessage);
            WriteToTextBox(message);
            WriteToLogFile(prefixedMessage);
        }

        private static string FormatMessage(string message)
        {
            if (message.StartsWith("Ошибка") || message.Contains("isn't translated"))
                return "[ОШИБКА] " + message;
            if (message.StartsWith("==="))
                return "[СЕКЦИЯ] " + message;
            if (message.StartsWith("Таблица") && message.Contains("сгенерирована"))
                return "[ГЕНЕРАЦИЯ] " + message;
            if (message.StartsWith("Применяем") || message.StartsWith("Обработка"))
                return "[ПРОЦЕСС] " + message;
            if (message.Contains("успешно"))
                return "[УСПЕХ] " + message;
            if (message.StartsWith("Количество") || message.Contains("start") || message.EndsWith("xlsx"))
                return "[СТАТИСТИКА] " + message;
            if (message.StartsWith("GENERATE"))
                return "[СИСТЕМА] " + message;
            if (message.StartsWith("String with ID"))
                return "[ПЕРЕВОД] " + message;
            
            return "[ИНФО] " + message;
        }

        private static void WriteToConsole(string prefixedMessage)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            
            if (prefixedMessage.StartsWith("[ОШИБКА]"))
                Console.ForegroundColor = ConsoleColor.Red;
            else if (prefixedMessage.StartsWith("[СЕКЦИЯ]"))
                Console.ForegroundColor = ConsoleColor.Cyan;
            else if (prefixedMessage.StartsWith("[ГЕНЕРАЦИЯ]"))
                Console.ForegroundColor = ConsoleColor.Green;
            else if (prefixedMessage.StartsWith("[ПРОЦЕСС]"))
                Console.ForegroundColor = ConsoleColor.DarkGray;
            else if (prefixedMessage.StartsWith("[УСПЕХ]"))
                Console.ForegroundColor = ConsoleColor.Green;
            else if (prefixedMessage.StartsWith("[СТАТИСТИКА]"))
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            else if (prefixedMessage.StartsWith("[СИСТЕМА]"))
                Console.ForegroundColor = ConsoleColor.Magenta;
            else if (prefixedMessage.StartsWith("[ПЕРЕВОД]"))
                Console.ForegroundColor = ConsoleColor.Yellow;
            else
                Console.ForegroundColor = ConsoleColor.White;
            
            Console.WriteLine(prefixedMessage);
            Console.ForegroundColor = originalColor;
        }

        private static void WriteToTextBox(string message)
        {
            Application.OpenForms["Form1"].Controls["textBox2"].Text = message;
        }

        private static void WriteToLogFile(string prefixedMessage)
        {
            try
            {
                string logPath = Path.Combine(Application.StartupPath, "log.txt");
                using StreamWriter writer = new StreamWriter(logPath, true, System.Text.Encoding.UTF8);
                writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": " + prefixedMessage);
            }
            catch
            {
                // Игнорируем ошибки при записи лога
            }
        }
        #endregion
    }
}