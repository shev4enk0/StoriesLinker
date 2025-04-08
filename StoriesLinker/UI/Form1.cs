using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using OfficeOpenXml;
using System.Diagnostics;
using System.Text;

namespace StoriesLinker
{
    public partial class Form1 : Form
    {
        public static int AvailableChapters;

        public const bool RELEASE_MODE = false;
        public const bool ONLY_ENGLISH_MODE = false;
        public const bool FOR_LOCALIZATORS_MODE = true;

        private string _projectPath;

        public Form1()
        {
            InitializeComponent();

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

        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() != DialogResult.OK) return;
            
            _projectPath = folderBrowserDialog1.SelectedPath;
            path_value.Text = _projectPath;

            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\StoriesLinker");
            key.SetValue("LastPath", _projectPath);
            key.Close();

            textBox1.Text = _projectPath;

            string[] pathParts = _projectPath.Split('/', '\\');

            proj_name_value.Text = pathParts[pathParts.Length - 1];
        }

        private void Form1_Load(object sender, EventArgs e) { }

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
            /*
                int _id_counter = 0;

                foreach (KeyValuePair<WarBookLocation, string> _pair in WarBook.LocSpriteMatching)
                {
                    WarBookLocation _loc_id = _pair.Key;

                    sheet.Cells[row, col].Value = _id_counter++;
                    sheet.Cells[row, col + 1].Value = _loc_id.ToString();
                    sheet.Cells[row, col + 2].Value = _pair.Value.ToString();
                    sheet.Cells[row, col + 3].Value = WarBook.LocSoundMatching[_loc_id];
                    sheet.Cells[row, col + 4].Value = 0;

                    row++;
                }*/


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

        private void GenerateOutputFolderForBundles(object sender, EventArgs eventArgs)
        {
            string flowJsonPath = LinkerBin.GetFlowJsonPath(_projectPath);
            string stringsXmlPath = LinkerBin.GetLocalizTablesPath(_projectPath);

            if (File.Exists(flowJsonPath) && File.Exists(stringsXmlPath))
            {
                LinkerBin linker = new LinkerBin(_projectPath);

                if (RELEASE_MODE)
                {
                    try
                    {
                        bool result = linker.GenerateOutputFolder();

                        StartCheckAfterBundleGeneration(result);
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
                    bool result = linker.GenerateOutputFolder();

                    StartCheckAfterBundleGeneration(result);
                }
            }
            else
            {
                ShowMessage("Отсуствует Flow.json или таблица xml.");
            }
        }

        private void StartCheckAfterBundleGeneration(bool result)
        {
            if (result)
            {
                LinkerBin linker = new LinkerBin(_projectPath);

                AjLinkerMeta meta = linker.GetParsedMetaInputJsonFile();

                LinkerAtlasChecker checker = new LinkerAtlasChecker(meta, meta.Characters);

                Dictionary<string, AjObj> objectsList
                    = linker.GetAricyBookEntities(linker.GetParsedFlowJsonFile(), linker.GetNativeDict());

                foreach (KeyValuePair<string, AjObj> @object in objectsList)
                {
                    if (@object.Value.EType == AjType.Instruction)
                    {
                        string expr = @object.Value.Properties.Expression;

                        if (expr.Contains("Clothes."))
                        {
                            checker.PassClothesInstruction(expr);
                        }
                    }
                }

                string checkResult = "";

                if (meta.UniqueId != "Shism_1" && meta.UniqueId != "Shism_2")
                    checkResult = checker.BeginFinalCheck(_projectPath);

                if (string.IsNullOrEmpty(checkResult))
                {
                    ShowMessage("Иерархия для бандлов успешно сгенерирована.");
                }
                else
                {
                    ShowMessage("Ошибка: " + checkResult);
                }
            }
        }

        private void StartVerificationButton_Click(object sender, EventArgs e) { }
        /*
        private void UpdateLocalizTablesExistState() {
            bool _loc_dir_exists = Directory.Exists(ProjectPath + "\\Localization");

            loc_state_value.Text = _loc_dir_exists ? "Созданы" : "Отсуствуют";
        }*/

        public static void ShowMessage(string message)
        {
            string prefixedMessage = message;
            ConsoleColor originalColor = Console.ForegroundColor;
            
            // Добавляем префиксы и устанавливаем цвета в зависимости от типа сообщения
            if (message.StartsWith("Ошибка") || message.Contains("isn't translated"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                prefixedMessage = "[ОШИБКА] " + message;
            }
            else if (message.StartsWith("==="))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                prefixedMessage = "[СЕКЦИЯ] " + message;
            }
            else if (message.StartsWith("Таблица") && message.Contains("сгенерирована"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                prefixedMessage = "[ГЕНЕРАЦИЯ] " + message;
            }
            else if (message.StartsWith("Применяем") || message.StartsWith("Обработка"))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                prefixedMessage = "[ПРОЦЕСС] " + message;
            }
            else if (message.Contains("успешно"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                prefixedMessage = "[УСПЕХ] " + message;
            }
            else if (message.StartsWith("Количество") || message.Contains("start") || message.EndsWith("xlsx"))
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                prefixedMessage = "[СТАТИСТИКА] " + message;
            }
            else if (message.StartsWith("GENERATE"))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                prefixedMessage = "[СИСТЕМА] " + message;
            }
            else if (message.StartsWith("String with ID"))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                prefixedMessage = "[ПЕРЕВОД] " + message;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                prefixedMessage = "[ИНФО] " + message;
            }
            
            // Вывод в консоль
            Console.WriteLine(prefixedMessage);
            
            // Возвращаем исходный цвет
            Console.ForegroundColor = originalColor;
            
            // Вывод в элемент textBox2 на форме (без префикса)
            Application.OpenForms["Form1"].Controls["textBox2"].Text = message;
            
            // Запись сообщения в лог-файл с корректной кодировкой (с префиксом)
            try
            {
                string logPath = Path.Combine(Application.StartupPath, "log.txt");
                using (StreamWriter writer = new StreamWriter(logPath, true, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": " + prefixedMessage);
                }
            }
            catch
            {
                // Игнорируем ошибки при записи лога
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

            string flowJsonPath = LinkerBin.GetFlowJsonPath(_projectPath);
            string stringsXmlPath = LinkerBin.GetLocalizTablesPath(_projectPath);

            if (File.Exists(flowJsonPath) && File.Exists(stringsXmlPath))
            {
                ShowMessage("Файлы найдены. Генерация началась...");

                LinkerBin linker = new LinkerBin(_projectPath);

                try
                {
                    bool result = linker.GenerateLocalizTables();

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
            else
            {
                ShowMessage("Отсуствует Flow.json или таблица xml.");
            }

            //GenerateLocationsTempMetaTable();
            //GenerateCharactersTempMetaTable();
        }

        private void ChaptersCountLabel_Click(object sender, EventArgs e) { }
    }
}