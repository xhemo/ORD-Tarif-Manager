using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using OrdTarifManager.Core;

namespace OrdTarifManager.UI.Dialogs
{
    public class MatrixImportDialog : Window
    {
        private readonly List<string> _currentColumns;
        private ContentControl _contentArea;
        private List<List<string>> _rows;

        private ComboBox _comboTop;
        private ComboBox _comboLeft;
        private string _unit;

        public List<Dictionary<string, object>> ResultData { get; private set; }
        public bool ReplaceMode { get; private set; }

        public MatrixImportDialog(List<string> currentColumns, Window owner, string unit = "kg")
        {
            _currentColumns = currentColumns ?? new List<string>();
            _unit = !string.IsNullOrWhiteSpace(unit) ? unit : "kg";
            Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Width = 920;
            Height = 650;
            MinWidth = 700;
            MinHeight = 500;
            ShowInTaskbar = false;
            Title = "Tarif aus Zwischenablage einf\u00FCgen";

            DwmHelper.EnableDarkMode(this);
            try { Icon = XamlHelper.LoadImageSource("app.ico"); } catch { }

            var content = (Border)XamlHelper.LoadElement("MatrixImportDialog.xaml");
            Content = content;

            _contentArea = (ContentControl)content.FindName("ContentArea");

            LoadFromClipboard();
        }

        private void LoadFromClipboard()
        {
            _rows = new List<List<string>>();
            try
            {
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();
                    if (!string.IsNullOrEmpty(text))
                    {
                        string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                        foreach (string rawLine in lines)
                        {
                            if (rawLine == null) continue;
                            string line = rawLine.TrimEnd('\r', '\n');
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            char sep = line.Contains("\t") ? '\t' : (line.Contains(";") ? ';' : '\t');
                            string[] parts = line.Split(sep);
                            var rowList = new List<string>(parts);
                            _rows.Add(rowList);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show(this, "Fehler beim Lesen der Zwischenablage: " + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (_rows.Count == 0 || (_rows.Count == 1 && _rows[0].Count <= 1))
            {
                ShowTutorialView();
            }
            else
            {
                ShowDataView();
            }
        }

        private void ShowTutorialView()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var centerStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 550
            };

            var title = new TextBlock
            {
                Text = "\U0001F4CB Keine Daten in der Zwischenablage",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };
            centerStack.Children.Add(title);

            var instr = new TextBlock
            {
                Text = "Die Zwischenablage scheint leer zu sein oder enth\u00E4lt keine g\u00FCltige Tabelle.\n\n" +
                       "So funktioniert der Import:\n" +
                       "1. \u00D6ffnen Sie Ihre Excel- oder CSV-Datei.\n" +
                       "2. Markieren Sie die gesamte Tabelle inklusive Kopfzeilen.\n" +
                       "3. Dr\u00FCcken Sie Strg+C (Kopieren).\n" +
                       "4. Klicken Sie unten auf 'Erneut pr\u00FCfen'.",
                FontSize = 13.5,
                LineHeight = 22,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 26)
            };
            centerStack.Children.Add(instr);

            var retryBtn = new Button
            {
                Content = "\U0001F504 Jetzt erneut pr\u00FCfen",
                Style = (Style)FindResource("PrimaryBtn"),
                Width = 220,
                Height = 38,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            retryBtn.Click += (s, e) => LoadFromClipboard();
            centerStack.Children.Add(retryBtn);

            Grid.SetRow(centerStack, 0);
            grid.Children.Add(centerStack);

            var bottomBar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 12, 0, 0)
            };
            var cancelBtn = new Button
            {
                Content = "Abbrechen",
                Width = 100,
                Height = 34
            };
            cancelBtn.Click += (s, e) => { DialogResult = false; };
            bottomBar.Children.Add(cancelBtn);

            Grid.SetRow(bottomBar, 1);
            grid.Children.Add(bottomBar);

            _contentArea.Content = grid;
        }

        private static string CleanDisplayVal(string val, int c, string unit)
        {
            if (string.IsNullOrWhiteSpace(val)) return "";
            val = val.Trim();

            if (!Regex.IsMatch(val, @"\d"))
            {
                return val;
            }

            try
            {
                double cleaned = NumberParser.CleanNumber(val);
                var deCulture = new System.Globalization.CultureInfo("de-DE");
                if (c == 0)
                {
                    // Left axis (weight or volume: e.g. 10 kg, 20 kg or 10 cbm, 20 cbm)
                    string numFormatted;
                    if (Math.Abs(cleaned % 1) > 0.0001)
                    {
                        numFormatted = cleaned.ToString("#,##0.##", deCulture);
                    }
                    else
                    {
                        numFormatted = ((long)Math.Round(cleaned)).ToString("#,##0", deCulture);
                    }
                    return !string.IsNullOrEmpty(unit) ? string.Format("{0} {1}", numFormatted, unit) : numFormatted;
                }
                else
                {
                    // Data cells (prices: 25,83 €, 185,00 €)
                    return cleaned.ToString("#,##0.00", deCulture) + " \u20AC";
                }
            }
            catch
            {
                return val;
            }
        }

        private void ShowDataView()
        {
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var hintTop = new TextBlock
            {
                Text = "\u00DCberpr\u00FCfen Sie die Daten aus der Zwischenablage:",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("BrushTextSecondary"),
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(hintTop, 0);
            mainGrid.Children.Add(hintTop);

            // Preview Table via DataGrid
            var previewGrid = new DataGrid
            {
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                CanUserReorderColumns = false,
                CanUserResizeRows = false,
                CanUserSortColumns = false,
                IsReadOnly = true,
                Margin = new Thickness(0, 0, 0, 14),
                HeadersVisibility = DataGridHeadersVisibility.All,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                AutoGenerateColumns = false
            };

            // Build Preview DataTable
            var previewDt = new DataTable();
            int maxCols = 0;
            for (int r = 0; r < _rows.Count; r++)
            {
                if (_rows[r].Count > maxCols) maxCols = _rows[r].Count;
            }

            for (int c = 0; c < maxCols; c++)
            {
                string colProp = string.Format("Col{0}", c);
                previewDt.Columns.Add(colProp, typeof(string));
            }

            // Data rows start at index 1 (index 0 is used for column headers!)
            for (int r = 1; r < _rows.Count; r++)
            {
                var rList = _rows[r];
                var dr = previewDt.NewRow();
                for (int c = 0; c < maxCols; c++)
                {
                    string rawVal = c < rList.Count ? rList[c] : "";
                    dr[c] = CleanDisplayVal(rawVal, c, _unit);
                }
                previewDt.Rows.Add(dr);
            }

            previewGrid.Columns.Clear();
            for (int c = 0; c < maxCols; c++)
            {
                string colProp = string.Format("Col{0}", c);
                string headerTitle;
                if (c == 0)
                {
                    headerTitle = "";
                }
                else
                {
                    string topVal = _rows.Count > 0 && c < _rows[0].Count ? _rows[0][c].Trim() : "";
                    double topNum;
                    if (NumberParser.TryParseClean(topVal, out topNum) && Math.Abs(topNum % 1) < 0.00001)
                    {
                        var deCulture = new CultureInfo("de-DE");
                        headerTitle = ((long)topNum).ToString("#,##0", deCulture) + " km";
                    }
                    else if (!string.IsNullOrEmpty(topVal))
                    {
                        if (Regex.IsMatch(topVal, @"^\d+$"))
                        {
                            headerTitle = topVal + " km";
                        }
                        else
                        {
                            headerTitle = topVal;
                        }
                    }
                    else
                    {
                        headerTitle = string.Format("Spalte {0}", c);
                    }
                }

                var col = new DataGridTextColumn
                {
                    Header = headerTitle,
                    Binding = new System.Windows.Data.Binding(string.Format("[{0}]", colProp)),
                    CanUserSort = false,
                    MinWidth = 85,
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                };

                var textStyle = new Style(typeof(TextBlock));
                textStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
                textStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                textStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(6, 2, 6, 2)));

                if (c == 0)
                {
                    var cornerHeaderStyle = new Style(typeof(DataGridColumnHeader), (Style)FindResource(typeof(DataGridColumnHeader)));
                    cornerHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, (Brush)FindResource("BrushHeaderBg")));
                    cornerHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, (Brush)FindResource("BrushHeaderBorder")));
                    col.HeaderStyle = cornerHeaderStyle;

                    var cellStyle = new Style(typeof(DataGridCell), (Style)FindResource(typeof(DataGridCell)));
                    cellStyle.Setters.Add(new Setter(DataGridCell.BackgroundProperty, (Brush)FindResource("BrushHeaderBg")));
                    cellStyle.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, (Brush)FindResource("BrushHeaderBorder")));
                    cellStyle.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
                    col.CellStyle = cellStyle;

                    textStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
                    textStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, 12.0));
                    textStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, (Brush)FindResource("BrushTextPrimary")));
                }
                else
                {
                    textStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, (Brush)FindResource("BrushTextPrimary")));
                }

                col.ElementStyle = textStyle;
                previewGrid.Columns.Add(col);
            }

            previewGrid.ItemsSource = previewDt.DefaultView;
            Grid.SetRow(previewGrid, 1);
            mainGrid.Children.Add(previewGrid);

            // Mapping Controls
            var mapBorder = new Border
            {
                Background = (Brush)FindResource("BrushSidebarBg"),
                BorderBrush = (Brush)FindResource("BrushCardBorder"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var mapStack = new StackPanel();

            var mapGrid = new Grid();
            mapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            mapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var pnlTop = new StackPanel();
            pnlTop.Children.Add(new Label { Content = "Header (Oben):" });
            _comboTop = new ComboBox { Height = 34 };
            _comboTop.Items.Add("(Ignorieren)");
            foreach (string c in _currentColumns) _comboTop.Items.Add(c);
            pnlTop.Children.Add(_comboTop);
            Grid.SetColumn(pnlTop, 0);
            mapGrid.Children.Add(pnlTop);

            var pnlLeft = new StackPanel();
            pnlLeft.Children.Add(new Label { Content = "Header (Links):" });
            _comboLeft = new ComboBox { Height = 34 };
            _comboLeft.Items.Add("(Ignorieren)");
            foreach (string c in _currentColumns) _comboLeft.Items.Add(c);

            int defaultTopIndex = _currentColumns.Count > 0 ? 1 : 0;
            int defaultLeftIndex = _currentColumns.Count > 1 ? 2 : 0;
            for (int i = 0; i < _currentColumns.Count; i++)
            {
                string c = _currentColumns[i];
                if (c.Equals("maxDistance", StringComparison.OrdinalIgnoreCase))
                {
                    defaultTopIndex = i + 1;
                }
                else if (c.Equals("maxWeight", StringComparison.OrdinalIgnoreCase) || c.Equals("maxVolume", StringComparison.OrdinalIgnoreCase))
                {
                    defaultLeftIndex = i + 1;
                }
            }
            if (defaultTopIndex == 1)
            {
                for (int i = 0; i < _currentColumns.Count; i++)
                {
                    if (_currentColumns[i].IndexOf("Distance", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        defaultTopIndex = i + 1;
                        break;
                    }
                }
            }
            if (defaultLeftIndex <= 1 && _currentColumns.Count > 1)
            {
                for (int i = 0; i < _currentColumns.Count; i++)
                {
                    if (_currentColumns[i].IndexOf("Weight", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        _currentColumns[i].IndexOf("Volume", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        defaultLeftIndex = i + 1;
                        break;
                    }
                }
            }

            _comboTop.SelectedIndex = defaultTopIndex;
            _comboLeft.SelectedIndex = defaultLeftIndex;

            Action updateLeftAxisUnit = () =>
            {
                string sel = _comboLeft.SelectedItem != null ? _comboLeft.SelectedItem.ToString() : "";
                string newUnit = _unit;
                if (sel.IndexOf("Volume", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    newUnit = "cbm";
                }
                else if (sel.IndexOf("Weight", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    newUnit = "kg";
                }
                else if (sel.IndexOf("Distance", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    newUnit = "km";
                }

                _unit = newUnit;
                for (int r = 1; r < _rows.Count; r++)
                {
                    if (r - 1 < previewDt.Rows.Count)
                    {
                        string rawVal = _rows[r].Count > 0 ? _rows[r][0] : "";
                        previewDt.Rows[r - 1][0] = CleanDisplayVal(rawVal, 0, _unit);
                    }
                }
            };
            _comboLeft.SelectionChanged += (s, e) => updateLeftAxisUnit();
            updateLeftAxisUnit();

            pnlLeft.Children.Add(_comboLeft);
            Grid.SetColumn(pnlLeft, 2);
            mapGrid.Children.Add(pnlLeft);

            mapStack.Children.Add(mapGrid);

            var cleanHint = new TextBlock
            {
                Text = "\u2139\uFE0F Die Zellenwerte werden automatisch bereinigt (z.B. '31,27 \u20AC' -> 31.27).",
                FontSize = 12,
                Foreground = (Brush)FindResource("BrushTextMuted"),
                Margin = new Thickness(0, 8, 0, 0)
            };
            mapStack.Children.Add(cleanHint);

            mapBorder.Child = mapStack;
            Grid.SetRow(mapBorder, 2);
            mainGrid.Children.Add(mapBorder);

            // Action Buttons
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var cancelBtn = new Button { Content = "Abbrechen", Width = 100, Height = 34 };
            cancelBtn.Click += (s, e) => { DialogResult = false; };
            Grid.SetColumn(cancelBtn, 0);
            btnGrid.Children.Add(cancelBtn);

            var actionStack = new StackPanel { Orientation = Orientation.Horizontal };

            var appendBtn = new Button
            {
                Content = "\u2795 Werte hinzuf\u00FCgen",
                Style = (Style)FindResource("PrimaryBtn"),
                Height = 34,
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(0, 0, 10, 0),
                ToolTip = "F\u00FCgt diese Werte am Ende der Tabelle an."
            };
            appendBtn.Click += (s, e) => { ReplaceMode = false; ProcessImport(); };
            actionStack.Children.Add(appendBtn);

            var replaceBtn = new Button
            {
                Content = "\U0001F504 Werte ersetzen",
                Style = (Style)FindResource("StandardBtn"),
                Height = 34,
                Padding = new Thickness(14, 6, 14, 6),
                ToolTip = "L\u00F6scht die aktuelle Tabelle und f\u00FCgt diese Werte ein."
            };
            replaceBtn.Click += (s, e) => { ReplaceMode = true; ProcessImport(); };
            actionStack.Children.Add(replaceBtn);

            Grid.SetColumn(actionStack, 2);
            btnGrid.Children.Add(actionStack);

            Grid.SetRow(btnGrid, 3);
            mainGrid.Children.Add(btnGrid);

            _contentArea.Content = mainGrid;
        }

        private void ProcessImport()
        {
            string colNameTop = _comboTop.SelectedItem != null ? _comboTop.SelectedItem.ToString() : "";
            string colNameLeft = _comboLeft.SelectedItem != null ? _comboLeft.SelectedItem.ToString() : "";

            if (colNameTop == "(Ignorieren)" || colNameLeft == "(Ignorieren)" || string.IsNullOrEmpty(colNameTop) || string.IsNullOrEmpty(colNameLeft))
            {
                DarkMessageBox.Show(this, "Bitte w\u00E4hlen Sie f\u00FCr beide Achsen eine g\u00FCltige Spalte aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var topValues = new List<double>();
                for (int j = 1; j < _rows[0].Count; j++)
                {
                    string valStr = _rows[0][j].Trim();
                    topValues.Add(NumberParser.CleanNumber(valStr));
                }

                string colMinTop = colNameTop.Contains("max") ? colNameTop.Replace("max", "min") : null;
                string colMinLeft = colNameLeft.Contains("max") ? colNameLeft.Replace("max", "min") : null;

                var topMins = new List<double>();
                topMins.Add(0.0);
                for (int k = 0; k < topValues.Count - 1; k++)
                {
                    topMins.Add(topValues[k]);
                }

                var newRows = new List<Dictionary<string, object>>();
                double prevLeftVal = 0.0;

                for (int i = 1; i < _rows.Count; i++)
                {
                    var rowData = _rows[i];
                    if (rowData.Count == 0) continue;

                    string leftValStr = rowData[0].Trim();
                    double leftVal = NumberParser.CleanNumber(leftValStr);
                    double minLeftVal = prevLeftVal;

                    for (int j = 1; j < rowData.Count; j++)
                    {
                        if (j - 1 >= topValues.Count) break;

                        string priceStr = rowData[j].Trim();
                        if (string.IsNullOrEmpty(priceStr)) continue;

                        double price = NumberParser.CleanNumber(priceStr);

                        var rowDict = new Dictionary<string, object>();
                        rowDict[colNameTop] = topValues[j - 1];
                        rowDict[colNameLeft] = leftVal;
                        rowDict["price"] = price;

                        if (colMinTop != null && _currentColumns.Contains(colMinTop))
                        {
                            rowDict[colMinTop] = topMins[j - 1];
                        }
                        if (colMinLeft != null && _currentColumns.Contains(colMinLeft))
                        {
                            rowDict[colMinLeft] = minLeftVal;
                        }

                        newRows.Add(rowDict);
                    }

                    prevLeftVal = leftVal;
                }

                ResultData = newRows;
                try
                {
                    DialogResult = true;
                }
                catch
                {
                    Close();
                }
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show(this, "Fehler beim Verarbeiten des Imports:\n" + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
