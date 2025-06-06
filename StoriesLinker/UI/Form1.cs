﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using OfficeOpenXml;
using System.Diagnostics;
using System.Text;
using System.Linq;

namespace StoriesLinker
{
    public partial class Form1 : Form
    {
        public static int AvailableChapters;

        public const bool ReleaseMode = false;
        public const bool OnlyEnglishMode = false;
        public const bool ForLocalizatorsMode = true;

        private string ProjectPath;
        private LinkerBin _linker = null;
        private string _mainLanguage = "Russian"; // Кэш основного языка
        private ToolTip _projectPathToolTip = new ToolTip();

        public Form1()
        {
            InitializeComponent();
            Logger.UiLogHandler = msg =>
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => textBox2.Text = msg));
                }
                else
                {
                    textBox2.Text = msg;
                }
            };

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

        private void InitializeProject(string path)
        {
            ProjectPath = path;
            path_value.Text = ProjectPath;
            _projectPathToolTip.SetToolTip(path_value, ProjectPath);
            Logger.Log($"Выбран проект: {ProjectPath}", Logger.LogType.Info);
            textBox1.Text = ProjectPath;
            string[] _path_parts = ProjectPath.Split('/', '\\');
            proj_name_value.Text = _path_parts[_path_parts.Length - 1];
            //UpdateLocalizTablesExistState();

            // --- Заполнение языков ---
            comboBoxMainLanguage.Items.Clear();
            var defaultLangs = new List<string> { "Russian", "English" };
            string locPath = Path.Combine(ProjectPath ?? "", "Localization");
            if (Directory.Exists(locPath))
            {
                var langs = Directory.GetDirectories(locPath)
                                     .Select(dir => Path.GetFileName(dir))
                                     .Where(name => !string.IsNullOrWhiteSpace(name))
                                     .ToList();
                foreach (var lang in langs)
                    if (!comboBoxMainLanguage.Items.Contains(lang))
                        comboBoxMainLanguage.Items.Add(lang);
            }
            string rawPath = Path.Combine(ProjectPath ?? "", "Raw");
            if (Directory.Exists(rawPath))
            {
                var files = Directory.GetFiles(rawPath, "loc_All objects_*.xlsx");
                foreach (var file in files)
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    var parts = name.Split('_');
                    if (parts.Length >= 4)
                    {
                        string suffix = parts[3];
                        string lang;
                        switch (suffix)
                        {
                            case "ru":
                                lang = "Russian";
                                break;
                            case "en":
                                lang = "English";
                                break;
                            case "de":
                                lang = "German";
                                break;
                            case "fr":
                                lang = "French";
                                break;
                            default:
                                lang = suffix;
                                break;
                        }
                        if (!comboBoxMainLanguage.Items.Contains(lang))
                            comboBoxMainLanguage.Items.Add(lang);
                    }
                }
            }
            if (comboBoxMainLanguage.Items.Count == 0)
            {
                foreach (var lang in defaultLangs)
                    comboBoxMainLanguage.Items.Add(lang);
            }
            if (comboBoxMainLanguage.Items.Contains("Russian"))
                comboBoxMainLanguage.SelectedItem = "Russian";
            else if (comboBoxMainLanguage.Items.Count > 0)
                comboBoxMainLanguage.SelectedIndex = 0;
            // Кэшируем выбранный язык
            _mainLanguage = comboBoxMainLanguage.SelectedItem?.ToString() ?? "Russian";
            // Сброс кэша LinkerBin при смене проекта
            _linker = null;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = folderBrowserDialog1.SelectedPath;
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\StoriesLinker");
                key.SetValue("LastPath", selectedPath);
                key.Close();
                InitializeProject(selectedPath);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Logger.Log("Form1_Load called", Logger.LogType.Info);
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\StoriesLinker");
            if (key != null)
            {
                string lastPath = key.GetValue("LastPath")?.ToString();
                if (!string.IsNullOrEmpty(lastPath) && Directory.Exists(lastPath))
                {
                    InitializeProject(lastPath);
                }
            }
        }

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
            string _strings_xml_path = LinkerBin.GetLocalizTablesPath(ProjectPath, _mainLanguage);

            if (File.Exists(_flow_json_path) && File.Exists(_strings_xml_path))
            {
                if (_linker == null)
                    _linker = new LinkerBin(ProjectPath, _mainLanguage);

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
                            Logger.Log("Ошибка: " + _e.Message + " " + e.ToString(), Logger.LogType.Error);
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
                Logger.Log("Отсуствует Flow.json или таблица xml.", Logger.LogType.Error);
            }
        }

        private void StartCheckAfterBundleGeneration(bool _result)
        {
            if (!_result) return;
            
            if (_linker == null)
                _linker = new LinkerBin(ProjectPath, _mainLanguage);

            AJLinkerMeta _meta = _linker.GetParsedMetaInputJSONFile();

            LinkerAtlasChecker _checker = new LinkerAtlasChecker(_meta, _meta.Characters);

            Dictionary<string, AJObj> _objects_list
                = _linker.GetAricyBookEntities(_linker.GetParsedFlowJSONFile(), _linker.GetNativeDict());

            foreach (KeyValuePair<string, AJObj> _object in _objects_list)
            {
                if (_object.Value.EType != AJType.Instruction) continue;
                
                string _expr = _object.Value.Properties.Expression;

                if (_expr.Contains("Clothes."))
                {
                    _checker.PassClothesInstruction(_expr);
                }
            }

            string _check_result = "";

            if (_meta.UniqueID != "Shism_1" && _meta.UniqueID != "Shism_2")
                _check_result = _checker.BeginFinalCheck(ProjectPath);

            if (string.IsNullOrEmpty(_check_result))
            {
                Logger.Log("Иерархия для бандлов успешно сгенерирована.", Logger.LogType.Success);
            }
            else
            {
                Logger.Log("Ошибка: " + _check_result, Logger.LogType.Error);
            }
        }

        private void StartVerificationButton_Click(object sender, EventArgs e) { }
        /*
        private void UpdateLocalizTablesExistState() {
            bool _loc_dir_exists = Directory.Exists(ProjectPath + "\\Localization");

            loc_state_value.Text = _loc_dir_exists ? "Созданы" : "Отсуствуют";
        }*/

        private void GenerateLocalizTables(object sender, EventArgs e)
        {
            string _chapters_count_text = chapters_count_value.Text;

            if (!int.TryParse(_chapters_count_text, out AvailableChapters))
            {
                Logger.Log("Некорректное количество глав", Logger.LogType.Error);
                return;
            }

            string _flow_json_path = LinkerBin.GetFlowJSONPath(ProjectPath);
            string _strings_xml_path = LinkerBin.GetLocalizTablesPath(ProjectPath, _mainLanguage);

            if (File.Exists(_flow_json_path) && File.Exists(_strings_xml_path))
            {
                Logger.Log("Файлы найдены. Генерация началась...", Logger.LogType.Info);
                if (_linker == null)
                    _linker = new LinkerBin(ProjectPath, _mainLanguage);

                try
                {
                    bool _result = _linker.GenerateLocalizTables();

                    if (_result)
                    {
                        Logger.Log("Таблицы локализации успешно сгенерированы.", Logger.LogType.Success);
                    }
                }
                catch (Exception _e)
                {
                    if (_e.Message != "")
                    {
                        Logger.Log("Ошибка: " + _e.Message, Logger.LogType.Error);
                    }
                }
            }
            else
            {
                Logger.Log("Отсуствует Flow.json или таблица xml.", Logger.LogType.Error);
            }

            //GenerateLocationsTempMetaTable();
            //GenerateCharactersTempMetaTable();
        }

        private void ChaptersCountLabel_Click(object sender, EventArgs e) { }

        private void comboBoxMainLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            _mainLanguage = comboBoxMainLanguage.SelectedItem?.ToString() ?? "Russian";
            // Сброс кэша LinkerBin при смене языка
            _linker = null;
        }
    }
}