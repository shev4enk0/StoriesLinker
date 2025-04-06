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

        public const bool ReleaseMode = false;
        public const bool OnlyEnglishMode = false;
        public const bool ForLocalizatorsMode = true;

        private string ProjectPath;

        public Form1()
        {
            InitializeComponent();

            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\StoriesLinker");

            if (key != null)
            {
                string _path = key.GetValue("LastPath").ToString();

                folderBrowserDialog1.SelectedPath = _path;
                path_value.Text = _path;
            }
            else
            {
                path_value.Text = "-";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                ProjectPath = folderBrowserDialog1.SelectedPath;

                path_value.Text = ProjectPath;

                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\StoriesLinker");
                key.SetValue("LastPath", ProjectPath);
                key.Close();

                textBox1.Text = ProjectPath;

                string[] _path_parts = ProjectPath.Split('/', '\\');

                proj_name_value.Text = _path_parts[_path_parts.Length - 1];

                //UpdateLocalizTablesExistState();
            }
        }

        private void Form1_Load(object sender, EventArgs e) { }

        private void GenerateCharactersTempMetaTable()
        {
            using (var eP = new ExcelPackage())
            {
                var sheet = eP.Workbook.Worksheets.Add("Characters");

                var row = 1;
                var col = 1;

                row++;

                FantasyBook.Init();

                foreach (KeyValuePair<string, FantBookCharacter> _pair in FantasyBook.Characters)
                {
                    FantBookCharacter _ch_id = _pair.Value;

                    sheet.Cells[row, col].Value = _pair.Key;

                    if (_ch_id.ToString().Contains("Sec_"))
                    {
                        sheet.Cells[row, col + 1].Value = _ch_id.ToString();
                        sheet.Cells[row, col + 2].Value = _ch_id.ToString();
                        sheet.Cells[row, col + 3].Value = _ch_id.ToString();
                    }
                    else
                    {
                        sheet.Cells[row, col + 1].Value
                            = (FantasyBook.ChClothesVariablesMathcing.ContainsKey(_ch_id)
                                   ? FantasyBook.ChClothesVariablesMathcing[_ch_id]
                                   : "-");
                        sheet.Cells[row, col + 2].Value
                            = (FantasyBook.ChAtalsMathcing.ContainsKey(_ch_id)
                                   ? FantasyBook.ChAtalsMathcing[_ch_id]
                                   : "-");
                        sheet.Cells[row, col + 3].Value = _ch_id.ToString();
                    }

                    row++;
                }

                var bin = eP.GetAsByteArray();
                File.WriteAllBytes(ProjectPath + @"\TempMetaCharacters.xlsx", bin);
            }
        }

        private void GenerateLocationsTempMetaTable()
        {
            using (var eP = new ExcelPackage())
            {
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


                foreach (KeyValuePair<string, FantBookLocation> _pair in FantasyBook.Locations)
                {
                    FantBookLocation _loc_id = _pair.Value;

                    sheet.Cells[row, col].Value = 0;
                    sheet.Cells[row, col + 1].Value = _pair.Key;
                    sheet.Cells[row, col + 2].Value = FantasyBook.LocSpriteMatching[_loc_id];
                    sheet.Cells[row, col + 3].Value = FantasyBook.LocSoundMatching[_loc_id];
                    sheet.Cells[row, col + 4].Value = 0;

                    row++;
                }

                var bin = eP.GetAsByteArray();
                File.WriteAllBytes(ProjectPath + @"\TempMetaLocations.xlsx", bin);
            }
        }

        private void GenerateOutputFolderForBundles(object sender, EventArgs e)
        {
            string _flow_json_path = LinkerBin.GetFlowJSONPath(ProjectPath);
            string _strings_xml_path = LinkerBin.GetLocalizTablesPath(ProjectPath);

            if (File.Exists(_flow_json_path) && File.Exists(_strings_xml_path))
            {
                LinkerBin _linker = new LinkerBin(ProjectPath);

                if (ReleaseMode)
                {
                    try
                    {
                        bool _result = _linker.GenerateOutputFolder();

                        StartCheckAfterBundleGeneration(_result);
                    }
                    catch (Exception _e)
                    {
                        if (_e.Message != "")
                        {
                            ShowMessage("Ошибка: " + _e.Message + " " + e.ToString());
                        }
                    }
                }
                else
                {
                    bool _result = _linker.GenerateOutputFolder();

                    StartCheckAfterBundleGeneration(_result);
                }
            }
            else
            {
                ShowMessage("Отсуствует Flow.json или таблица xml.");
            }
        }

        private void StartCheckAfterBundleGeneration(bool _result)
        {
            if (_result)
            {
                LinkerBin _linker = new LinkerBin(ProjectPath);

                AJLinkerMeta _meta = _linker.GetParsedMetaInputJSONFile();

                LinkerAtlasChecker _checker = new LinkerAtlasChecker(_meta, _meta.Characters);

                Dictionary<string, AJObj> _objects_list
                    = _linker.GetAricyBookEntities(_linker.GetParsedFlowJSONFile(), _linker.GetNativeDict());

                foreach (KeyValuePair<string, AJObj> _object in _objects_list)
                {
                    if (_object.Value.EType == AJType.Instruction)
                    {
                        string _expr = _object.Value.Properties.Expression;

                        if (_expr.Contains("Clothes."))
                        {
                            _checker.PassClothesInstruction(_expr);
                        }
                    }
                }

                string _check_result = "";

                if (_meta.UniqueID != "Shism_1" && _meta.UniqueID != "Shism_2")
                    _check_result = _checker.BeginFinalCheck(ProjectPath);

                if (string.IsNullOrEmpty(_check_result))
                {
                    ShowMessage("Иерархия для бандлов успешно сгенерирована.");
                }
                else
                {
                    ShowMessage("Ошибка: " + _check_result);
                }
            }
        }

        private void StartVerificationButton_Click(object sender, EventArgs e) { }
        /*
        private void UpdateLocalizTablesExistState() {
            bool _loc_dir_exists = Directory.Exists(ProjectPath + "\\Localization");

            loc_state_value.Text = _loc_dir_exists ? "Созданы" : "Отсуствуют";
        }*/

        public static void ShowMessage(string _message)
        {
            string prefixedMessage = _message;
            ConsoleColor originalColor = Console.ForegroundColor;
            
            // Добавляем префиксы и устанавливаем цвета в зависимости от типа сообщения
            if (_message.StartsWith("Ошибка") || _message.Contains("isn't translated"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                prefixedMessage = "[ОШИБКА] " + _message;
            }
            else if (_message.StartsWith("==="))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                prefixedMessage = "[СЕКЦИЯ] " + _message;
            }
            else if (_message.StartsWith("Таблица") && _message.Contains("сгенерирована"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                prefixedMessage = "[ГЕНЕРАЦИЯ] " + _message;
            }
            else if (_message.StartsWith("Применяем") || _message.StartsWith("Обработка"))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                prefixedMessage = "[ПРОЦЕСС] " + _message;
            }
            else if (_message.Contains("успешно"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                prefixedMessage = "[УСПЕХ] " + _message;
            }
            else if (_message.StartsWith("Количество") || _message.Contains("start") || _message.EndsWith("xlsx"))
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                prefixedMessage = "[СТАТИСТИКА] " + _message;
            }
            else if (_message.StartsWith("GENERATE"))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                prefixedMessage = "[СИСТЕМА] " + _message;
            }
            else if (_message.StartsWith("String with ID"))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                prefixedMessage = "[ПЕРЕВОД] " + _message;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                prefixedMessage = "[ИНФО] " + _message;
            }
            
            // Вывод в консоль
            Console.WriteLine(prefixedMessage);
            
            // Возвращаем исходный цвет
            Console.ForegroundColor = originalColor;
            
            // Вывод в элемент textBox2 на форме (без префикса)
            Application.OpenForms["Form1"].Controls["textBox2"].Text = _message;
            
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

        private void GenerateLocalizTables(object sender, EventArgs e)
        {
            string _chapters_count_text = chapters_count_value.Text;

            if (!int.TryParse(_chapters_count_text, out AvailableChapters))
            {
                ShowMessage("Некорректное количество глав");

                return;
            }

            string _flow_json_path = LinkerBin.GetFlowJSONPath(ProjectPath);
            string _strings_xml_path = LinkerBin.GetLocalizTablesPath(ProjectPath);

            if (File.Exists(_flow_json_path) && File.Exists(_strings_xml_path))
            {
                ShowMessage("Файлы найдены. Генерация началась...");

                LinkerBin _linker = new LinkerBin(ProjectPath);

                try
                {
                    bool _result = _linker.GenerateLocalizTables();

                    if (_result)
                    {
                        ShowMessage("Таблицы локализации успешно сгенерированы.");
                    }
                }
                catch (Exception _e)
                {
                    if (_e.Message != "")
                    {
                        ShowMessage("Ошибка: " + _e.Message);
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