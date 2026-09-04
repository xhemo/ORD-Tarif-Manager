using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using OrdTarifManager.Core;

namespace OrdTarifManager.UI.Dialogs
{
    public class FilterDialog : Window
    {
        private readonly List<string> _values;
        private readonly HashSet<string> _activeFilters;
        private readonly List<KeyValuePair<string, CheckBox>> _checkboxes = new List<KeyValuePair<string, CheckBox>>();

        private TextBox _txtSearch;
        private CheckBox _cbSelectAll;
        private StackPanel _pnlItems;

        public HashSet<string> ResultFilters { get; private set; }

        public FilterDialog(List<string> sortedValues, HashSet<string> activeFilters, string colName, Window owner)
        {
            _values = sortedValues ?? new List<string>();
            _activeFilters = activeFilters != null ? new HashSet<string>(activeFilters) : new HashSet<string>(_values);

            Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Width = 320;
            Height = 440;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            ShowInTaskbar = false;
            Title = "Filter: " + colName;

            DwmHelper.EnableDarkMode(this);
            try { Icon = XamlHelper.LoadImageSource("app.ico"); } catch { }

            var content = (Border)XamlHelper.LoadElement("FilterDialog.xaml");
            Content = content;

            _txtSearch = (TextBox)content.FindName("TxtSearch");
            _cbSelectAll = (CheckBox)content.FindName("CbSelectAll");
            _pnlItems = (StackPanel)content.FindName("PnlItems");
            var btnCancel = (Button)content.FindName("BtnCancel");
            var btnOk = (Button)content.FindName("BtnOk");

            bool isAllSelected = _activeFilters.Count == _values.Count;
            _cbSelectAll.IsChecked = isAllSelected;

            // Populate checkboxes
            foreach (string val in _values)
            {
                string displayStr = string.IsNullOrEmpty(val) ? "(Leer)" : NumberParser.FormatValueForDisplay(val, colName);
                var cb = new CheckBox
                {
                    Content = displayStr,
                    IsChecked = _activeFilters.Contains(val),
                    Margin = new Thickness(0, 3, 0, 3)
                };
                _pnlItems.Children.Add(cb);
                _checkboxes.Add(new KeyValuePair<string, CheckBox>(val, cb));
            }

            _txtSearch.TextChanged += TxtSearch_TextChanged;
            _cbSelectAll.Click += CbSelectAll_Click;

            btnCancel.Click += (s, e) => { DialogResult = false; };
            btnOk.Click += (s, e) =>
            {
                ResultFilters = new HashSet<string>();
                foreach (var pair in _checkboxes)
                {
                    if (pair.Value.IsChecked == true)
                    {
                        ResultFilters.Add(pair.Key);
                    }
                }
                DialogResult = true;
            };
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = _txtSearch.Text != null ? _txtSearch.Text.Trim().ToLowerInvariant() : "";
            foreach (var pair in _checkboxes)
            {
                string keyStr = pair.Key.ToLowerInvariant();
                string displayStr = (pair.Value.Content != null ? pair.Value.Content.ToString() : "").ToLowerInvariant();
                pair.Value.Visibility = string.IsNullOrEmpty(filter) || keyStr.Contains(filter) || displayStr.Contains(filter)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void CbSelectAll_Click(object sender, RoutedEventArgs e)
        {
            bool check = _cbSelectAll.IsChecked == true;
            string filter = _txtSearch.Text != null ? _txtSearch.Text.Trim().ToLowerInvariant() : "";

            foreach (var pair in _checkboxes)
            {
                string keyStr = pair.Key.ToLowerInvariant();
                string displayStr = (pair.Value.Content != null ? pair.Value.Content.ToString() : "").ToLowerInvariant();
                if (string.IsNullOrEmpty(filter) || keyStr.Contains(filter) || displayStr.Contains(filter))
                {
                    pair.Value.IsChecked = check;
                }
            }
        }
    }
}
