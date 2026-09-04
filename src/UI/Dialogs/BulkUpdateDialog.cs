using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OrdTarifManager.Core;

namespace OrdTarifManager.UI.Dialogs
{
    public class BulkUpdateDialog : Window
    {
        private readonly TariffEngine _engine;
        private readonly DataTable _dataTable;
        private readonly List<int> _selectedRows;

        private ComboBox _cmbColumns;
        private TextBox _txtPercentage;
        private Border _btnPlus;
        private TextBlock _txtPlus;
        private Border _btnMinus;
        private TextBlock _txtMinus;
        private Border _previewBanner;
        private TextBlock _txtPreview;
        private bool _isPlus = true;

        public BulkUpdateDialog(TariffEngine engine, DataTable dataTable, List<int> selectedRows, Window owner)
        {
            _engine = engine;
            _dataTable = dataTable;
            _selectedRows = selectedRows;

            Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Width = 430;
            Height = 400;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;

            bool hasSelection = selectedRows != null && selectedRows.Count > 0;
            Title = "Preisanpassung " + (hasSelection ? "(Auswahl)" : "(Alle Zeilen)");

            DwmHelper.EnableDarkMode(this);
            try { Icon = XamlHelper.LoadImageSource("app.ico"); } catch { }

            var content = (Border)XamlHelper.LoadElement("BulkUpdateDialog.xaml");
            Content = content;

            var bannerBorder = (Border)content.FindName("BannerBorder");
            var txtInfo = (TextBlock)content.FindName("TxtInfo");
            _cmbColumns = (ComboBox)content.FindName("CmbColumns");
            _txtPercentage = (TextBox)content.FindName("TxtPercentage");
            _btnPlus = (Border)content.FindName("BtnPlus");
            _txtPlus = (TextBlock)content.FindName("TxtPlus");
            _btnMinus = (Border)content.FindName("BtnMinus");
            _txtMinus = (TextBlock)content.FindName("TxtMinus");
            _previewBanner = (Border)content.FindName("PreviewBanner");
            _txtPreview = (TextBlock)content.FindName("TxtPreview");

            var btnCancel = (Button)content.FindName("BtnCancel");
            var btnApply = (Button)content.FindName("BtnApply");

            int totalRows = _dataTable != null ? _dataTable.Rows.Count : 0;
            if (hasSelection)
            {
                txtInfo.Text = string.Format("\u2139\uFE0F Anpassung f\u00FCr {0} ausgew\u00E4hlte Zeile(n).", selectedRows.Count);
                bannerBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#13261C"));
                bannerBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A402C"));
                txtInfo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            }
            else
            {
                txtInfo.Text = string.Format("\u26A0\uFE0F Anpassung gilt f\u00FCr ALLE {0} Zeile(n)!", totalRows);
                bannerBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A1E11"));
                bannerBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#483015"));
                txtInfo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            }

            if (_dataTable != null)
            {
                int priceIndex = -1;
                for (int i = 0; i < _dataTable.Columns.Count; i++)
                {
                    string colName = _dataTable.Columns[i].ColumnName;
                    _cmbColumns.Items.Add(colName);
                    if (colName.Equals("price", StringComparison.OrdinalIgnoreCase))
                    {
                        priceIndex = i;
                    }
                }

                if (priceIndex >= 0)
                {
                    _cmbColumns.SelectedIndex = priceIndex;
                }
                else if (_cmbColumns.Items.Count > 0)
                {
                    _cmbColumns.SelectedIndex = 0;
                }
            }

            // Click Handlers for +/- Selector
            if (_btnPlus != null)
            {
                _btnPlus.MouseLeftButtonDown += (s, e) => SetSign(true);
                _btnPlus.MouseEnter += (s, e) =>
                {
                    if (!_isPlus)
                    {
                        _btnPlus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2333"));
                        _txtPlus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                    }
                };
                _btnPlus.MouseLeave += (s, e) =>
                {
                    if (!_isPlus)
                    {
                        _btnPlus.Background = Brushes.Transparent;
                        _txtPlus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5A6578"));
                    }
                };
            }

            if (_btnMinus != null)
            {
                _btnMinus.MouseLeftButtonDown += (s, e) => SetSign(false);
                _btnMinus.MouseEnter += (s, e) =>
                {
                    if (_isPlus)
                    {
                        _btnMinus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2333"));
                        _txtMinus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                    }
                };
                _btnMinus.MouseLeave += (s, e) =>
                {
                    if (_isPlus)
                    {
                        _btnMinus.Background = Brushes.Transparent;
                        _txtMinus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5A6578"));
                    }
                };
            }

            // Keyboard interception inside TextBox (+ and - switch toggle immediately)
            if (_txtPercentage != null)
            {
                _txtPercentage.PreviewTextInput += (s, e) =>
                {
                    if (e.Text == "+")
                    {
                        SetSign(true);
                        e.Handled = true;
                        return;
                    }
                    if (e.Text == "-" || e.Text == "\u2212")
                    {
                        SetSign(false);
                        e.Handled = true;
                        return;
                    }
                    if (e.Text == "%")
                    {
                        e.Handled = true;
                        return;
                    }
                };

                _txtPercentage.PreviewKeyDown += (s, e) =>
                {
                    if (e.Key == Key.OemPlus || e.Key == Key.Add)
                    {
                        SetSign(true);
                        e.Handled = true;
                    }
                    else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
                    {
                        SetSign(false);
                        e.Handled = true;
                    }
                };

                DataObject.AddPastingHandler(_txtPercentage, (s, e) =>
                {
                    if (e.DataObject.GetDataPresent(DataFormats.Text))
                    {
                        string text = (string)e.DataObject.GetData(DataFormats.Text);
                        if (!string.IsNullOrEmpty(text))
                        {
                            if (text.Contains("-") || text.Contains("\u2212")) SetSign(false);
                            else if (text.Contains("+")) SetSign(true);
                            string clean = text.Replace("-", "").Replace("\u2212", "").Replace("+", "").Replace("%", "").Trim();
                            e.CancelCommand();
                            _txtPercentage.SelectedText = clean;
                        }
                    }
                });

                _txtPercentage.TextChanged += (s, e) => UpdatePreview();
            }

            SetSign(true);

            _txtPercentage.Text = "0";
            _txtPercentage.SelectAll();
            _txtPercentage.Focus();

            btnCancel.Click += (s, e) => { DialogResult = false; };
            btnApply.Click += BtnApply_Click;
        }

        private void SetSign(bool isPlus)
        {
            _isPlus = isPlus;
            if (_btnPlus == null || _txtPlus == null || _btnMinus == null || _txtMinus == null) return;

            if (_isPlus)
            {
                _btnPlus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                _txtPlus.Foreground = Brushes.White;
                _btnMinus.Background = Brushes.Transparent;
                _txtMinus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5A6578"));
            }
            else
            {
                _btnPlus.Background = Brushes.Transparent;
                _txtPlus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5A6578"));
                _btnMinus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                _txtMinus.Foreground = Brushes.White;
            }
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (_txtPreview == null || _previewBanner == null || _txtPercentage == null) return;

            string valStr = _txtPercentage.Text != null ? _txtPercentage.Text.Trim() : "";
            double rawVal = NumberParser.CleanNumber(valStr);

            if (rawVal == 0.0 && valStr != "0" && valStr != "0.0" && valStr != "0,0")
            {
                _previewBanner.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A202C"));
                _previewBanner.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F3B52"));
                _txtPreview.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8A96A8"));
                _txtPreview.Text = "Bitte geben Sie einen g\u00FCltigen Prozentsatz ein.";
                return;
            }

            string formatted = rawVal.ToString("0.##", CultureInfo.InvariantCulture);

            if (rawVal == 0.0)
            {
                _previewBanner.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A202C"));
                _previewBanner.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F3B52"));
                _txtPreview.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8A96A8"));
                _txtPreview.Text = "Keine \u00C4nderung (0 %)";
            }
            else if (_isPlus)
            {
                _previewBanner.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#132E22"));
                _previewBanner.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B4D36"));
                _txtPreview.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                _txtPreview.Text = string.Format("\uD83D\uDCC8 +{0} % Aufschlag (Tarif wird um {0} % erh\u00F6ht)", formatted);
            }
            else
            {
                _previewBanner.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E1418"));
                _previewBanner.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4D1B22"));
                _txtPreview.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171"));
                _txtPreview.Text = string.Format("\uD83D\uDCC9 \u2212{0} % Abschlag (Tarif wird um {0} % reduziert)", formatted);
            }
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            string col = _cmbColumns.SelectedItem != null ? _cmbColumns.SelectedItem.ToString() : "";
            string valStr = _txtPercentage.Text != null ? _txtPercentage.Text.Trim() : "";

            if (string.IsNullOrEmpty(col))
            {
                DarkMessageBox.Show(this, "Keine Spalte gew\u00E4hlt.", "Warnung", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double rawVal = NumberParser.CleanNumber(valStr);
            if (rawVal == 0.0 && valStr != "0" && valStr != "0.0" && valStr != "0,0")
            {
                DarkMessageBox.Show(this, "Ung\u00FCltiger Zahlenwert f\u00FCr die prozentuale \u00C4nderung.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double pct = _isPlus ? Math.Abs(rawVal) : -Math.Abs(rawVal);

            try
            {
                _engine.ApplyBulkChange(_dataTable, col, pct, _selectedRows);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show(this, "Fehler beim Anwenden der \u00C4nderung:\n" + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
