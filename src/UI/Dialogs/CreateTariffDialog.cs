using System;
using System.Windows;
using System.Windows.Controls;
using OrdTarifManager.Core;

namespace OrdTarifManager.UI.Dialogs
{
    public class CreateTariffDialog : Window
    {
        private TextBox _txtName;
        private RadioButton _rbCost;
        private RadioButton _rbRevenue;
        private RadioButton _rbWeight;
        private RadioButton _rbVolume;
        private RadioButton _rbDistribution;
        private RadioButton _rbReturn;
        private TextBlock _txtSpecPreview;
        private TextBlock _txtOrderKindTag;
        private DatePicker _dpValidFrom;
        private DatePicker _dpValidTo;
        private Button _btnCreate;

        public string TariffName { get; private set; }
        public bool IsCost { get; private set; }
        public bool IsVolume { get; private set; }
        public int SelectedOrderKind { get; private set; }
        public DateTime ValidFrom { get; private set; }
        public DateTime ValidTo { get; private set; }
        public string SpecName { get; private set; }

        public CreateTariffDialog(Window owner) : this(null, owner)
        {
        }

        public CreateTariffDialog(Window owner, bool isEditMode, string currentName, bool isCost, bool isVolume, int orderKind, DateTime validFrom, DateTime validTo)
            : this(null, owner)
        {
            if (isEditMode)
            {
                Title = "Tarifspezifikation anpassen";
                if (_btnCreate != null)
                {
                    _btnCreate.Content = "Tarifspezifikation anwenden";
                }

                if (_txtName != null) _txtName.Text = currentName ?? "";
                if (_rbCost != null) _rbCost.IsChecked = isCost;
                if (_rbRevenue != null) _rbRevenue.IsChecked = !isCost;
                if (_rbVolume != null) _rbVolume.IsChecked = isVolume;
                if (_rbWeight != null) _rbWeight.IsChecked = !isVolume;
                if (_rbDistribution != null) _rbDistribution.IsChecked = (orderKind == 2);
                if (_rbReturn != null) _rbReturn.IsChecked = (orderKind == 3);
                if (_dpValidFrom != null) _dpValidFrom.SelectedDate = validFrom;
                if (_dpValidTo != null) _dpValidTo.SelectedDate = validTo;

                UpdateSpecPreview();
                UpdateCreateButtonState();
            }
        }

        public CreateTariffDialog(TariffEngine engine, Window owner)
        {
            Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Width = 490;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Title = "Neuen XML Tarif erstellen";

            DwmHelper.EnableDarkMode(this);
            try { Icon = XamlHelper.LoadImageSource("app.ico"); } catch { }

            var content = (Border)XamlHelper.LoadElement("CreateTariffDialog.xaml");
            Content = content;

            _txtName = (TextBox)content.FindName("TxtName");
            _rbCost = (RadioButton)content.FindName("RbCost");
            _rbRevenue = (RadioButton)content.FindName("RbRevenue");
            _rbWeight = (RadioButton)content.FindName("RbWeight");
            _rbVolume = (RadioButton)content.FindName("RbVolume");
            _rbDistribution = (RadioButton)content.FindName("RbDistribution");
            _rbReturn = (RadioButton)content.FindName("RbReturn");
            _txtSpecPreview = (TextBlock)content.FindName("TxtSpecPreview");
            _txtOrderKindTag = (TextBlock)content.FindName("TxtOrderKindTag");
            _dpValidFrom = (DatePicker)content.FindName("DpValidFrom");
            _dpValidTo = (DatePicker)content.FindName("DpValidTo");
            var btnCancel = (Button)content.FindName("BtnCancel");
            _btnCreate = (Button)content.FindName("BtnCreate");

            _dpValidFrom.SelectedDate = DateTime.Today;
            _dpValidTo.SelectedDate = new DateTime(2099, 12, 31);

            RoutedEventHandler onOptionChanged = (s, e) => UpdateSpecPreview();
            if (_rbCost != null) _rbCost.Checked += onOptionChanged;
            if (_rbRevenue != null) _rbRevenue.Checked += onOptionChanged;
            if (_rbWeight != null) _rbWeight.Checked += onOptionChanged;
            if (_rbVolume != null) _rbVolume.Checked += onOptionChanged;
            if (_rbDistribution != null) _rbDistribution.Checked += onOptionChanged;
            if (_rbReturn != null) _rbReturn.Checked += onOptionChanged;

            _txtName.TextChanged += (s, e) => UpdateCreateButtonState();

            btnCancel.Click += (s, e) => { DialogResult = false; };
            _btnCreate.Click += BtnCreate_Click;

            UpdateSpecPreview();
            UpdateCreateButtonState();
        }

        private void UpdateSpecPreview()
        {
            bool isCost = _rbCost != null && _rbCost.IsChecked == true;
            bool isVolume = _rbVolume != null && _rbVolume.IsChecked == true;
            int orderKind = (_rbDistribution != null && _rbDistribution.IsChecked == true) ? 2 : 3;

            string spec = (isCost ? "(TC)" : "") + "Stepped" + (isVolume ? "Volume" : "Weight") + "DistanceConsolidation";

            if (_txtSpecPreview != null)
            {
                _txtSpecPreview.Text = spec;
            }

            if (_txtOrderKindTag != null)
            {
                _txtOrderKindTag.Text = (orderKind == 3) ? "Return" : "Delivery";
            }
        }

        private void UpdateCreateButtonState()
        {
            bool hasName = !string.IsNullOrWhiteSpace(_txtName.Text);
            _btnCreate.IsEnabled = hasName;
            if (hasName)
            {
                _btnCreate.Style = (Style)FindResource("SuccessBtn");
            }
            else
            {
                _btnCreate.Style = (Style)FindResource("StandardBtn");
            }
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            TariffName = _txtName.Text != null ? _txtName.Text.Trim() : "";
            IsCost = _rbCost.IsChecked == true;
            IsVolume = _rbVolume.IsChecked == true;
            SelectedOrderKind = _rbDistribution.IsChecked == true ? 2 : 3;
            ValidFrom = _dpValidFrom.SelectedDate ?? DateTime.Today;
            ValidTo = _dpValidTo.SelectedDate ?? new DateTime(2099, 12, 31);
            SpecName = (IsCost ? "(TC)" : "") + "Stepped" + (IsVolume ? "Volume" : "Weight") + "DistanceConsolidation";

            if (string.IsNullOrEmpty(TariffName))
            {
                DarkMessageBox.Show(this, "Bitte geben Sie einen Tarifnamen ein.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }
    }
}
