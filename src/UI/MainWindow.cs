using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;
using OrdTarifManager.Core;
using OrdTarifManager.UI.Dialogs;

namespace OrdTarifManager.UI
{
    public class MainWindow : Window
    {
        private const string ProdDirectory = @"N:\intern\Speditionsbüro Ulm_Donautal\ORTEC Docs\Outbound";
        private const string TestDirectory = @"N:\intern\Speditionsbüro Ulm_Donautal\ORTEC Docs\Outbound Test";

        private static string GetBackupDirectory(bool isProd)
        {
            string sub = isProd ? "PROD" : "TEST";
            string correctBase = @"N:\intern\Speditionsbüro Ulm_Donautal\ORTEC Docs\ORD zuletzt hochgeladene Tarife";
            string origBase = @"N:\intern\Speditionsbüro Ulm_Donautal\ORTEC Docs\ORD zuletzt hochgladene Tarife";
            string correctPath = Path.Combine(correctBase, sub);
            string origPath = Path.Combine(origBase, sub);
            if (Directory.Exists(correctPath)) return correctPath;
            if (Directory.Exists(origPath)) return origPath;
            return correctPath;
        }

        private static void CleanupMisplacedOutboundArchive()
        {
            try
            {
                string[] oldBases = new[]
                {
                    @"N:\intern\Speditionsbüro Ulm_Donautal\ORTEC Docs\Outbound\ORD zuletzt hochgeladene Tarife",
                    @"N:\intern\Speditionsbüro Ulm_Donautal\ORTEC Docs\Outbound\ORD zuletzt hochgladene Tarife"
                };
                string targetBase = @"N:\intern\Speditionsbüro Ulm_Donautal\ORTEC Docs\ORD zuletzt hochgeladene Tarife";

                foreach (var oldBase in oldBases)
                {
                    if (Directory.Exists(oldBase))
                    {
                        foreach (string sub in new[] { "PROD", "TEST" })
                        {
                            string oldSub = Path.Combine(oldBase, sub);
                            string targetSub = Path.Combine(targetBase, sub);
                            if (Directory.Exists(oldSub))
                            {
                                if (!Directory.Exists(targetSub)) Directory.CreateDirectory(targetSub);
                                foreach (var file in Directory.GetFiles(oldSub))
                                {
                                    string dest = Path.Combine(targetSub, Path.GetFileName(file));
                                    if (!File.Exists(dest)) File.Move(file, dest);
                                }
                            }
                        }
                        Directory.Delete(oldBase, true);
                    }
                }
            }
            catch { }
        }

        private string _lastDirectory;

        private readonly TariffEngine _engine = new TariffEngine();
        private DataTable _dataTable;
        private readonly Dictionary<string, HashSet<string>> _columnFilters = new Dictionary<string, HashSet<string>>();

        // UI Controls
        private Grid _centralDataArea;
        private Border _headerFrame;
        private Grid _toolbarFrame;
        private Border _pnlPlaceholder;
        private Border _pnlTable;
        private Border _actionFrame;
        private DataGrid _dgTariff;
        private Grid _dragOverlay;
        private Grid _pnlDragValid;
        private Grid _pnlDragInvalid;
        private TextBlock _txtDragInvalidReason;

        private TextBox _txtName;
        private DatePicker _dpValidFrom;
        private DatePicker _dpValidTo;
        private ComboBox _cmbOrderKind;
        private Border _btnSpecSelector;
        private TextBlock _txtSpecDisplay;
        private string _currentSpec = "SteppedWeightDistanceConsolidation";

        private Border _sidebarSpecWeight;
        private Border _sidebarSpecVolume;
        private Border _sidebarSpecTcWeight;
        private Border _sidebarSpecTcVolume;
        private TextBlock _txtSidebarTagWeight;
        private TextBlock _txtSidebarTagVolume;
        private TextBlock _txtSidebarTagTcWeight;
        private TextBlock _txtSidebarTagTcVolume;
        private TextBlock _txtSidebarSpecWeight;
        private TextBlock _txtSidebarSpecVolume;
        private TextBlock _txtSidebarSpecTcWeight;
        private TextBlock _txtSidebarSpecTcVolume;

        private Button _btnAddRow;
        private Button _btnDeleteRows;
        private Button _btnClearFilters;

        private Button _btnCreateTariff;

        private Button _btnMatrixImport;
        private Button _btnBulkUpdate;
        private Button _btnGenerateXml;

        private Grid _pnlExportContainer;
        private FrameworkElement _flyoutExportMenu;
        private TranslateTransform _flyoutTransform;
        private Button _btnExportProd;
        private Button _btnExportTest;
        private FrameworkElement _badgeSuccessNotification;
        private ScaleTransform _badgeScale;
        private TranslateTransform _badgeTranslate;
        private TextBlock _txtSuccessTitle;
        private TextBlock _txtSuccessDetail;
        private DispatcherTimer _successTimer;
        private DispatcherTimer _flyoutCloseTimer;

        // Recent Uploads Sidebar Controls
        private Button _btnRefreshRecent;
        private Button _btnOpenProdDir;
        private Button _btnOpenTestDir;
        private ScrollViewer _scrollRecentProd;
        private ScrollViewer _scrollRecentTest;
        private StackPanel _panelRecentProd;
        private StackPanel _panelRecentTest;

        public MainWindow()
        {
            Title = "ORD Tarif Manager";
            Width = 1320;
            Height = 800;
            MinWidth = 980;
            MinHeight = 620;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
            RenderOptions.SetClearTypeHint(this, ClearTypeHint.Enabled);

            DwmHelper.EnableDarkMode(this);

            try
            {
                Icon = XamlHelper.LoadImageSource("app.ico");
            }
            catch { }

            var root = (Border)XamlHelper.LoadElement("MainWindow.xaml");
            Content = root;

            BindControls(root);
            WireEvents();
            UpdateUiState();
            RefreshRecentFiles();
        }

        private void BindControls(Border root)
        {
            var imgLogo = (Image)root.FindName("ImgAppLogo");
            if (imgLogo != null)
            {
                imgLogo.Source = XamlHelper.LoadImageSource("logo.png");
            }

            _headerFrame = (Border)root.FindName("HeaderFrame");
            _toolbarFrame = (Grid)root.FindName("ToolbarFrame");
            _centralDataArea = (Grid)root.FindName("CentralDataArea");
            _pnlPlaceholder = (Border)root.FindName("PnlPlaceholder");
            _pnlTable = (Border)root.FindName("PnlTable");
            _actionFrame = (Border)root.FindName("ActionFrame");
            _dgTariff = (DataGrid)root.FindName("DgTariff");
            _dragOverlay = (Grid)root.FindName("DragOverlay");
            _pnlDragValid = (Grid)root.FindName("PnlDragValid");
            _pnlDragInvalid = (Grid)root.FindName("PnlDragInvalid");
            _txtDragInvalidReason = (TextBlock)root.FindName("TxtDragInvalidReason");

            _txtName = (TextBox)root.FindName("TxtName");
            _dpValidFrom = (DatePicker)root.FindName("DpValidFrom");
            _dpValidTo = (DatePicker)root.FindName("DpValidTo");
            _cmbOrderKind = (ComboBox)root.FindName("CmbOrderKind");
            _btnSpecSelector = (Border)root.FindName("BtnSpecSelector");
            _txtSpecDisplay = (TextBlock)root.FindName("TxtSpecDisplay");

            _btnAddRow = (Button)root.FindName("BtnAddRow");
            _btnDeleteRows = (Button)root.FindName("BtnDeleteRows");
            _btnClearFilters = (Button)root.FindName("BtnClearFilters");

            _btnCreateTariff = (Button)root.FindName("BtnCreateTariff");

            _sidebarSpecWeight = (Border)root.FindName("SidebarSpecWeight");
            _sidebarSpecVolume = (Border)root.FindName("SidebarSpecVolume");
            _sidebarSpecTcWeight = (Border)root.FindName("SidebarSpecTcWeight");
            _sidebarSpecTcVolume = (Border)root.FindName("SidebarSpecTcVolume");

            _txtSidebarTagWeight = (TextBlock)root.FindName("TxtSidebarTagWeight");
            _txtSidebarTagVolume = (TextBlock)root.FindName("TxtSidebarTagVolume");
            _txtSidebarTagTcWeight = (TextBlock)root.FindName("TxtSidebarTagTcWeight");
            _txtSidebarTagTcVolume = (TextBlock)root.FindName("TxtSidebarTagTcVolume");

            _txtSidebarSpecWeight = (TextBlock)root.FindName("TxtSidebarSpecWeight");
            _txtSidebarSpecVolume = (TextBlock)root.FindName("TxtSidebarSpecVolume");
            _txtSidebarSpecTcWeight = (TextBlock)root.FindName("TxtSidebarSpecTcWeight");
            _txtSidebarSpecTcVolume = (TextBlock)root.FindName("TxtSidebarSpecTcVolume");

            _btnMatrixImport = (Button)root.FindName("BtnMatrixImport");
            _btnBulkUpdate = (Button)root.FindName("BtnBulkUpdate");
            _btnGenerateXml = (Button)root.FindName("BtnGenerateXml");

            _pnlExportContainer = (Grid)root.FindName("PnlExportContainer");
            _flyoutExportMenu = (FrameworkElement)root.FindName("FlyoutExportMenu");
            _flyoutTransform = (TranslateTransform)root.FindName("FlyoutTransform");
            _btnExportProd = (Button)root.FindName("BtnExportProd");
            _btnExportTest = (Button)root.FindName("BtnExportTest");
            _badgeSuccessNotification = (FrameworkElement)root.FindName("BadgeSuccessNotification");
            _badgeScale = (ScaleTransform)root.FindName("BadgeScale");
            _badgeTranslate = (TranslateTransform)root.FindName("BadgeTranslate");
            _txtSuccessTitle = (TextBlock)root.FindName("TxtSuccessTitle");
            _txtSuccessDetail = (TextBlock)root.FindName("TxtSuccessDetail");

            _btnRefreshRecent = (Button)root.FindName("BtnRefreshRecent");
            _btnOpenProdDir = (Button)root.FindName("BtnOpenProdDir");
            _btnOpenTestDir = (Button)root.FindName("BtnOpenTestDir");
            _scrollRecentProd = (ScrollViewer)root.FindName("ScrollRecentProd");
            _scrollRecentTest = (ScrollViewer)root.FindName("ScrollRecentTest");
            _panelRecentProd = (StackPanel)root.FindName("PanelRecentProd");
            _panelRecentTest = (StackPanel)root.FindName("PanelRecentTest");

            _lastDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            _dpValidFrom.SelectedDate = DateTime.Today;
            _dpValidTo.SelectedDate = new DateTime(2099, 12, 31);
        }

        private void WireEvents()
        {
            _btnCreateTariff.Click += (s, e) => CreateNewTariff();

            _btnAddRow.Click += (s, e) => AddRow();
            _btnDeleteRows.Click += (s, e) => DeleteRowsAction();
            _btnClearFilters.Click += (s, e) => ClearAllFilters();

            _btnMatrixImport.Click += (s, e) => OpenMatrixImport();
            _btnBulkUpdate.Click += (s, e) => OpenBulkUpdateDialog();
            _btnGenerateXml.Click += (s, e) => GenerateXml();

            if (_pnlExportContainer != null)
            {
                _pnlExportContainer.MouseEnter += (s, e) =>
                {
                    if (_flyoutCloseTimer != null) _flyoutCloseTimer.Stop();
                    ShowExportFlyout();
                };
                _pnlExportContainer.MouseLeave += (s, e) =>
                {
                    if (_flyoutCloseTimer != null) _flyoutCloseTimer.Stop();
                    _flyoutCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(220) };
                    _flyoutCloseTimer.Tick += (ts, te) =>
                    {
                        _flyoutCloseTimer.Stop();
                        HideExportFlyout(false);
                    };
                    _flyoutCloseTimer.Start();
                };
            }

            if (_flyoutExportMenu != null)
            {
                _flyoutExportMenu.MouseEnter += (s, e) =>
                {
                    if (_flyoutCloseTimer != null) _flyoutCloseTimer.Stop();
                };
            }

            if (_btnExportProd != null)
            {
                _btnExportProd.Click += (s, e) => ExportDirectly(ProdDirectory, GetBackupDirectory(true), "PROD");
            }

            if (_btnExportTest != null)
            {
                _btnExportTest.Click += (s, e) => ExportDirectly(TestDirectory, GetBackupDirectory(false), "TEST");
            }

            _cmbOrderKind.SelectionChanged += (s, e) => UpdateOrderKindInTable();

            if (_btnSpecSelector != null)
            {
                var normBg = (Brush)new BrushConverter().ConvertFrom("#0F1219");
                var normBorder = (Brush)new BrushConverter().ConvertFrom("#1E2433");
                var hoverBg = (Brush)new BrushConverter().ConvertFrom("#141924");
                var hoverBorder = (Brush)new BrushConverter().ConvertFrom("#2E3A50");

                _btnSpecSelector.MouseEnter += (s, e) =>
                {
                    _btnSpecSelector.Background = hoverBg;
                    _btnSpecSelector.BorderBrush = hoverBorder;
                };
                _btnSpecSelector.MouseLeave += (s, e) =>
                {
                    _btnSpecSelector.Background = normBg;
                    _btnSpecSelector.BorderBrush = normBorder;
                };
                _btnSpecSelector.MouseLeftButtonDown += (s, e) =>
                {
                    OpenEditSpecificationDialog();
                };
            }

            if (_sidebarSpecWeight != null) _sidebarSpecWeight.MouseLeftButtonDown += (s, e) => OnSidebarSpecClicked("SteppedWeightDistanceConsolidation");
            if (_sidebarSpecVolume != null) _sidebarSpecVolume.MouseLeftButtonDown += (s, e) => OnSidebarSpecClicked("SteppedVolumeDistanceConsolidation");
            if (_sidebarSpecTcWeight != null) _sidebarSpecTcWeight.MouseLeftButtonDown += (s, e) => OnSidebarSpecClicked("(TC)SteppedWeightDistanceConsolidation");
            if (_sidebarSpecTcVolume != null) _sidebarSpecTcVolume.MouseLeftButtonDown += (s, e) => OnSidebarSpecClicked("(TC)SteppedVolumeDistanceConsolidation");

            if (_btnRefreshRecent != null) _btnRefreshRecent.Click += (s, e) => RefreshRecentFiles();
            if (_btnOpenProdDir != null)
            {
                _btnOpenProdDir.Click += (s, e) => OpenUploadDirectory(true);
                _btnOpenProdDir.ContextMenu = CreateDirectoryContextMenu(true);
            }
            if (_btnOpenTestDir != null)
            {
                _btnOpenTestDir.Click += (s, e) => OpenUploadDirectory(false);
                _btnOpenTestDir.ContextMenu = CreateDirectoryContextMenu(false);
            }

            if (_scrollRecentProd != null) _scrollRecentProd.PreviewMouseWheel += HandleChildListPreviewMouseWheel;
            if (_scrollRecentTest != null) _scrollRecentTest.PreviewMouseWheel += HandleChildListPreviewMouseWheel;

            _dgTariff.SelectionChanged += (s, e) => UpdateDeleteButtonState();
            _dgTariff.Sorting += DgTariff_Sorting;
            _txtName.TextChanged += (s, e) => { _txtName.ToolTip = _txtName.Text; };

            // Background click to deselect
            MouseDown += (s, e) =>
            {
                if (!IsDescendantOf(e.OriginalSource as DependencyObject, _dgTariff))
                {
                    _dgTariff.UnselectAll();
                }
            };

            // Drag & Drop XML files into the central data area
            AllowDrop = true;
            PreviewDragEnter += Window_PreviewDragEnter;
            PreviewDragOver += Window_PreviewDragOver;
            PreviewDragLeave += Window_PreviewDragLeave;
            DragLeave += Window_DragLeave;
            PreviewDrop += Window_PreviewDrop;

            if (_centralDataArea != null)
            {
                _centralDataArea.AllowDrop = true;
                _centralDataArea.DragLeave += CentralArea_DragLeave;
            }

            if (_dragOverlay != null)
            {
                _dragOverlay.DragLeave += CentralArea_DragLeave;
            }

            PreviewMouseMove += (s, e) =>
            {
                if (e.LeftButton != MouseButtonState.Pressed && _dragOverlay != null && _dragOverlay.Visibility != Visibility.Collapsed)
                {
                    _dragOverlay.Visibility = Visibility.Collapsed;
                }
            };

            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape && _dragOverlay != null && _dragOverlay.Visibility != Visibility.Collapsed)
                {
                    _dragOverlay.Visibility = Visibility.Collapsed;
                }
            };

            Deactivated += (s, e) =>
            {
                if (_dragOverlay != null && _dragOverlay.Visibility != Visibility.Collapsed)
                {
                    _dragOverlay.Visibility = Visibility.Collapsed;
                }
            };
        }

        private bool IsDescendantOf(DependencyObject node, DependencyObject target)
        {
            while (node != null)
            {
                if (node == target) return true;
                node = VisualTreeHelper.GetParent(node);
            }
            return false;
        }

        private void UpdateUiState()
        {
            bool hasData = _dataTable != null && _dataTable.Columns.Count > 0;
            if (hasData)
            {
                _pnlPlaceholder.Visibility = Visibility.Collapsed;
                _pnlTable.Visibility = Visibility.Visible;
                _toolbarFrame.Visibility = Visibility.Visible;
                _actionFrame.Visibility = Visibility.Visible;
            }
            else
            {
                _pnlPlaceholder.Visibility = Visibility.Visible;
                _pnlTable.Visibility = Visibility.Collapsed;
                _toolbarFrame.Visibility = Visibility.Collapsed;
                _actionFrame.Visibility = Visibility.Collapsed;
            }

            UpdateSpecificationSidebar(hasData ? GetCurrentSpecification() : null);

            UpdateDeleteButtonState();
        }

        private void UpdateSpecificationSidebar(string specName)
        {
            SetSidebarSpecItemActive(_sidebarSpecWeight, _txtSidebarTagWeight, _txtSidebarSpecWeight, false);
            SetSidebarSpecItemActive(_sidebarSpecVolume, _txtSidebarTagVolume, _txtSidebarSpecVolume, false);
            SetSidebarSpecItemActive(_sidebarSpecTcWeight, _txtSidebarTagTcWeight, _txtSidebarSpecTcWeight, false);
            SetSidebarSpecItemActive(_sidebarSpecTcVolume, _txtSidebarTagTcVolume, _txtSidebarSpecTcVolume, false);

            if (string.IsNullOrWhiteSpace(specName)) return;

            string s = specName.Trim();
            if (s.Equals("SteppedWeightDistanceConsolidation", StringComparison.OrdinalIgnoreCase))
            {
                SetSidebarSpecItemActive(_sidebarSpecWeight, _txtSidebarTagWeight, _txtSidebarSpecWeight, true);
            }
            else if (s.Equals("SteppedVolumeDistanceConsolidation", StringComparison.OrdinalIgnoreCase))
            {
                SetSidebarSpecItemActive(_sidebarSpecVolume, _txtSidebarTagVolume, _txtSidebarSpecVolume, true);
            }
            else if (s.Equals("(TC)SteppedWeightDistanceConsolidation", StringComparison.OrdinalIgnoreCase))
            {
                SetSidebarSpecItemActive(_sidebarSpecTcWeight, _txtSidebarTagTcWeight, _txtSidebarSpecTcWeight, true);
            }
            else if (s.Equals("(TC)SteppedVolumeDistanceConsolidation", StringComparison.OrdinalIgnoreCase))
            {
                SetSidebarSpecItemActive(_sidebarSpecTcVolume, _txtSidebarTagTcVolume, _txtSidebarSpecTcVolume, true);
            }
        }

        private void SetSidebarSpecItemActive(Border border, TextBlock tag, TextBlock specText, bool isActive)
        {
            if (border == null || tag == null || specText == null) return;

            if (isActive)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(0x13, 0x2A, 0x1F));
                border.BorderBrush = (Brush)FindResource("BrushSuccess");
                border.BorderThickness = new Thickness(1.2);
                tag.Foreground = (Brush)FindResource("BrushSuccess");
                tag.FontWeight = FontWeights.Bold;
                specText.Foreground = new SolidColorBrush(Colors.White);
                specText.FontWeight = FontWeights.Bold;
            }
            else
            {
                border.Background = new SolidColorBrush(Color.FromRgb(0x0F, 0x12, 0x19));
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x24, 0x33));
                border.BorderThickness = new Thickness(1.0);
                tag.Foreground = new SolidColorBrush(Color.FromRgb(0x5A, 0x65, 0x78));
                tag.FontWeight = FontWeights.SemiBold;
                specText.Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x96, 0xA8));
                specText.FontWeight = FontWeights.Normal;
            }
        }

        private void UpdateDeleteButtonState()
        {
            if (_dgTariff.SelectedItems != null && _dgTariff.SelectedItems.Count > 0)
            {
                _btnDeleteRows.Content = "\u2796 Ausgew\u00E4hlte Zeilen l\u00F6schen";
            }
            else
            {
                _btnDeleteRows.Content = "\u2796 Alle Zeilen l\u00F6schen";
            }
        }

        private void CreateNewTariff()
        {
            var dlg = new CreateTariffDialog(_engine, this);
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    TariffMetadata meta;
                    _dataTable = _engine.CreateTariff(dlg.TariffName, dlg.IsCost, dlg.IsVolume, dlg.SelectedOrderKind, dlg.ValidFrom, dlg.ValidTo, out meta);

                    _txtName.Text = dlg.TariffName;
                    _txtName.ToolTip = dlg.TariffName;
                    SetSpecificationByName(dlg.SpecName, false);
                    _dpValidFrom.SelectedDate = dlg.ValidFrom;
                    _dpValidTo.SelectedDate = dlg.ValidTo;

                    if (dlg.SelectedOrderKind == 3)
                    {
                        _cmbOrderKind.SelectedIndex = 1;
                    }
                    else
                    {
                        _cmbOrderKind.SelectedIndex = 0;
                    }

                    BuildGridColumns();
                    UpdateUiState();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Fehler beim Erstellen des Tarifs:\n" + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnSidebarSpecClicked(string targetSpec)
        {
            if (_dataTable == null || _dataTable.Columns.Count == 0)
            {
                CreateNewTariff();
                return;
            }
            SetSpecificationByName(targetSpec, true);
        }

        private void OpenEditSpecificationDialog()
        {
            string currentName = _txtName.Text != null ? _txtName.Text.Trim() : "";
            string currentSpec = GetCurrentSpecification();
            bool isCost = currentSpec.IndexOf("(TC)", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isVolume = currentSpec.IndexOf("Volume", StringComparison.OrdinalIgnoreCase) >= 0;
            int orderKind = (_cmbOrderKind != null && _cmbOrderKind.SelectedIndex == 1) ? 3 : 2;
            DateTime validFrom = _dpValidFrom.SelectedDate ?? DateTime.Today;
            DateTime validTo = _dpValidTo.SelectedDate ?? new DateTime(2099, 12, 31);

            var dlg = new CreateTariffDialog(this, true, currentName, isCost, isVolume, orderKind, validFrom, validTo);
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    _txtName.Text = dlg.TariffName;
                    _txtName.ToolTip = dlg.TariffName;

                    _dpValidFrom.SelectedDate = dlg.ValidFrom;
                    _dpValidTo.SelectedDate = dlg.ValidTo;

                    if (_cmbOrderKind != null)
                    {
                        _cmbOrderKind.SelectedIndex = (dlg.SelectedOrderKind == 3) ? 1 : 0;
                    }

                    string newSpec = dlg.SpecName;
                    SetSpecificationByName(newSpec, false);

                    if (_dataTable != null && _dataTable.Columns.Count > 0)
                    {
                        _engine.ApplySpecificationChange(_dataTable, dlg.IsCost, dlg.IsVolume, dlg.SelectedOrderKind, newSpec);

                        var meta = new TariffMetadata
                        {
                            Id = _txtName.Text != null ? _txtName.Text.Trim() : "",
                            Name = _txtName.Text != null ? _txtName.Text.Trim() : "",
                            ValidFrom = _dpValidFrom.SelectedDate.HasValue ? _dpValidFrom.SelectedDate.Value.ToString("yyyy-MM-dd") : DateTime.Today.ToString("yyyy-MM-dd"),
                            ValidTo = _dpValidTo.SelectedDate.HasValue ? _dpValidTo.SelectedDate.Value.ToString("yyyy-MM-dd") : new DateTime(2099, 12, 31).ToString("yyyy-MM-dd"),
                            Spec = newSpec,
                            OrderKind = dlg.SelectedOrderKind
                        };
                        _engine.UpdateMetadata(meta);

                        BuildGridColumns();
                        if (_dgTariff.ItemsSource != null)
                        {
                            _dgTariff.Items.Refresh();
                        }
                        UpdateUiState();
                    }
                    else
                    {
                        TariffMetadata meta;
                        _dataTable = _engine.CreateTariff(dlg.TariffName, dlg.IsCost, dlg.IsVolume, dlg.SelectedOrderKind, dlg.ValidFrom, dlg.ValidTo, out meta);
                        BuildGridColumns();
                        UpdateUiState();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Fehler beim Anpassen der Tarifspezifikation:\n" + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ApplyCurrentSpecification()
        {
            string currentSpec = GetCurrentSpecification();
            if (_btnSpecSelector != null)
            {
                _btnSpecSelector.ToolTip = currentSpec + "\n\n💡 Klicken, um Tarifspezifikation anzupassen";
            }
            UpdateSpecificationSidebar(currentSpec);

            if (_dataTable == null || _dataTable.Columns.Count == 0) return;

            bool isCost = currentSpec.IndexOf("(TC)", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isVolume = currentSpec.IndexOf("Volume", StringComparison.OrdinalIgnoreCase) >= 0;
            int orderKind = (_cmbOrderKind != null && _cmbOrderKind.SelectedIndex == 1) ? 3 : 2;

            _engine.ApplySpecificationChange(_dataTable, isCost, isVolume, orderKind, currentSpec);

            var meta = new TariffMetadata
            {
                Id = _txtName.Text != null ? _txtName.Text.Trim() : "",
                Name = _txtName.Text != null ? _txtName.Text.Trim() : "",
                ValidFrom = _dpValidFrom.SelectedDate.HasValue ? _dpValidFrom.SelectedDate.Value.ToString("yyyy-MM-dd") : DateTime.Today.ToString("yyyy-MM-dd"),
                ValidTo = _dpValidTo.SelectedDate.HasValue ? _dpValidTo.SelectedDate.Value.ToString("yyyy-MM-dd") : new DateTime(2099, 12, 31).ToString("yyyy-MM-dd"),
                Spec = currentSpec,
                OrderKind = orderKind
            };
            _engine.UpdateMetadata(meta);

            BuildGridColumns();
            if (_dgTariff.ItemsSource != null)
            {
                _dgTariff.Items.Refresh();
            }

            UpdateUiState();
        }

        private void SetSpecificationByName(string specName, bool applyToTariff = true)
        {
            if (string.IsNullOrWhiteSpace(specName)) return;

            string trimmed = specName.Trim();
            _currentSpec = trimmed;

            if (_txtSpecDisplay != null)
            {
                _txtSpecDisplay.Text = trimmed;
            }

            if (_btnSpecSelector != null)
            {
                _btnSpecSelector.ToolTip = trimmed + "\n\n💡 Klicken, um Tarifspezifikation anzupassen";
            }

            UpdateSpecificationSidebar(trimmed);

            if (applyToTariff)
            {
                ApplyCurrentSpecification();
            }
        }

        private string GetCurrentSpecification()
        {
            if (!string.IsNullOrEmpty(_currentSpec))
            {
                return _currentSpec;
            }
            if (_txtSpecDisplay != null && !string.IsNullOrEmpty(_txtSpecDisplay.Text))
            {
                return _txtSpecDisplay.Text.Trim();
            }
            return "SteppedWeightDistanceConsolidation";
        }

        private static bool CheckDragFileType(DragEventArgs e, out string reason)
        {
            reason = "Nur XML-Tarife (*.xml) werden unterst\u00FCtzt.";
            if (e.Data == null)
            {
                reason = "Keine Dateidaten erkannt.";
                return false;
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Length > 0)
                {
                    foreach (string file in files)
                    {
                        if (!string.IsNullOrEmpty(file) && file.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }

                    string firstFile = files[0];
                    string ext = Path.GetExtension(firstFile);
                    if (!string.IsNullOrEmpty(ext))
                    {
                        reason = string.Format("Dateityp {0} wird nicht unterst\u00FCtzt. Nur XML-Tarife (*.xml) m\u00F6glich.", ext.ToUpperInvariant());
                    }
                    else
                    {
                        reason = "Dateiformat ohne Erweiterung wird nicht unterst\u00FCtzt. Nur XML-Tarife (*.xml) m\u00F6glich.";
                    }
                    return false;
                }
            }

            reason = "Keine Datei erkannt. Bitte eine *.xml Datei ablegen.";
            return false;
        }

        private static bool IsXmlDrop(DragEventArgs e)
        {
            string reason;
            return CheckDragFileType(e, out reason);
        }

        private bool IsCursorInCentralArea(DragEventArgs e)
        {
            if (_centralDataArea == null || !_centralDataArea.IsVisible) return false;
            try
            {
                Point pos = e.GetPosition(_centralDataArea);
                return pos.X >= 0 && pos.Y >= 0 && pos.X <= _centralDataArea.ActualWidth && pos.Y <= _centralDataArea.ActualHeight;
            }
            catch
            {
                return false;
            }
        }

        private void HandleDragOverOrEnter(DragEventArgs e)
        {
            if (IsCursorInCentralArea(e))
            {
                string reason;
                bool isXml = CheckDragFileType(e, out reason);

                if (_dragOverlay != null && _dragOverlay.Visibility != Visibility.Visible)
                {
                    _dragOverlay.Visibility = Visibility.Visible;
                }

                if (isXml)
                {
                    e.Effects = DragDropEffects.Copy;
                    if (_pnlDragValid != null && _pnlDragValid.Visibility != Visibility.Visible)
                    {
                        _pnlDragValid.Visibility = Visibility.Visible;
                    }
                    if (_pnlDragInvalid != null && _pnlDragInvalid.Visibility != Visibility.Collapsed)
                    {
                        _pnlDragInvalid.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                    if (_pnlDragValid != null && _pnlDragValid.Visibility != Visibility.Collapsed)
                    {
                        _pnlDragValid.Visibility = Visibility.Collapsed;
                    }
                    if (_pnlDragInvalid != null && _pnlDragInvalid.Visibility != Visibility.Visible)
                    {
                        _pnlDragInvalid.Visibility = Visibility.Visible;
                    }
                    if (_txtDragInvalidReason != null)
                    {
                        _txtDragInvalidReason.Text = reason;
                    }
                }

                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
                if (_dragOverlay != null && _dragOverlay.Visibility != Visibility.Collapsed)
                {
                    _dragOverlay.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Window_PreviewDragEnter(object sender, DragEventArgs e)
        {
            HandleDragOverOrEnter(e);
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            HandleDragOverOrEnter(e);
        }

        private void Window_PreviewDragLeave(object sender, DragEventArgs e)
        {
            if (!IsCursorInCentralArea(e))
            {
                if (_dragOverlay != null && _dragOverlay.Visibility != Visibility.Collapsed)
                {
                    _dragOverlay.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void CentralArea_DragLeave(object sender, DragEventArgs e)
        {
            if (_dragOverlay != null && _dragOverlay.Visibility != Visibility.Collapsed)
            {
                _dragOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            if (_dragOverlay != null && _dragOverlay.Visibility != Visibility.Collapsed)
            {
                _dragOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_PreviewDrop(object sender, DragEventArgs e)
        {
            if (_dragOverlay != null)
            {
                _dragOverlay.Visibility = Visibility.Collapsed;
            }

            string reason;
            if (CheckDragFileType(e, out reason) && IsCursorInCentralArea(e))
            {
                if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                    if (files != null && files.Length > 0)
                    {
                        foreach (string file in files)
                        {
                            if (!string.IsNullOrEmpty(file) && file.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                            {
                                LoadXmlFile(file);
                                break;
                            }
                        }
                    }
                }
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        public void LoadXmlFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show(this, "Die angegebene Datei konnte nicht gefunden werden:\n" + filePath, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                try
                {
                    string dir = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        _lastDirectory = dir;
                    }
                }
                catch { }

                string errorMsg;
                if (!_engine.LoadTemplate(filePath, out errorMsg))
                {
                    MessageBox.Show(this, "Fehler beim Laden der XML-Datei:\n" + errorMsg, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                TariffMetadata meta = _engine.GetMetadata();
                _txtName.Text = !string.IsNullOrEmpty(meta.Name) ? meta.Name : Path.GetFileNameWithoutExtension(filePath);
                _txtName.ToolTip = _txtName.Text;
                SetSpecificationByName(!string.IsNullOrEmpty(meta.Spec) ? meta.Spec : "SteppedWeightDistanceConsolidation", false);

                DateTime vf;
                if (DateTime.TryParseExact(meta.ValidFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out vf))
                {
                    _dpValidFrom.SelectedDate = vf;
                }
                else if (DateTime.TryParse(meta.ValidFrom, out vf))
                {
                    _dpValidFrom.SelectedDate = vf;
                }

                DateTime vt;
                if (DateTime.TryParseExact(meta.ValidTo, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out vt))
                {
                    _dpValidTo.SelectedDate = vt;
                }
                else if (DateTime.TryParse(meta.ValidTo, out vt))
                {
                    _dpValidTo.SelectedDate = vt;
                }

                List<string> schema;
                _dataTable = _engine.ExtractTuplesCheckSchema(out schema);

                // Auto-detect Order Kind
                if (_dataTable.Columns.Contains("id_orderkind") && _dataTable.Rows.Count > 0)
                {
                    object kindValObj = _dataTable.Rows[0]["id_orderkind"];
                    int kindVal;
                    if (kindValObj != null && int.TryParse(kindValObj.ToString(), out kindVal))
                    {
                        if (kindVal == 3)
                        {
                            _cmbOrderKind.SelectedIndex = 1;
                        }
                        else
                        {
                            _cmbOrderKind.SelectedIndex = 0;
                        }
                    }
                }

                BuildGridColumns();
                UpdateUiState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Fehler beim Einlesen der XML-Datei:\n" + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuildGridColumns()
        {
            _dgTariff.Columns.Clear();
            _columnFilters.Clear();

            if (_dataTable == null) return;

            // Define centered text style for cells
            var centerStyle = new Style(typeof(TextBlock));
            centerStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
            centerStyle.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center));
            centerStyle.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));

            // Define centered editing style for cells
            var editStyle = new Style(typeof(TextBox));
            editStyle.Setters.Add(new Setter(TextBox.TextAlignmentProperty, TextAlignment.Center));
            editStyle.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch));
            editStyle.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));

            foreach (DataColumn col in _dataTable.Columns)
            {
                string colName = col.ColumnName;

                var colBinding = new Binding(string.Format("[{0}]", colName))
                {
                    Converter = new TariffCellDisplayConverter(colName),
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                };

                var gridCol = new DataGridTextColumn
                {
                    Binding = colBinding,
                    Width = DataGridLength.Auto,
                    MinWidth = Math.Max(110, colName.Length * 9 + 44),
                    CanUserSort = true,
                    SortMemberPath = colName,
                    ElementStyle = centerStyle,
                    EditingElementStyle = editStyle
                };

                // Custom Header with Filter Button
                var headerGrid = new Grid();
                headerGrid.HorizontalAlignment = HorizontalAlignment.Center;
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var headerTitle = new TextBlock
                {
                    Text = colName,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };
                Grid.SetColumn(headerTitle, 0);
                headerGrid.Children.Add(headerTitle);

                var filterBtn = new Button
                {
                    Content = "\u25BE",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(4, 0, 4, 0),
                    Margin = new Thickness(4, 0, 0, 0),
                    Cursor = Cursors.Hand,
                    Tag = colName
                };

                string capturedName = colName;
                Button capturedBtn = filterBtn;
                filterBtn.Click += (s, e) =>
                {
                    e.Handled = true;
                    OpenColumnFilter(capturedName, capturedBtn);
                };

                Grid.SetColumn(filterBtn, 1);
                headerGrid.Children.Add(filterBtn);

                gridCol.Header = headerGrid;
                _dgTariff.Columns.Add(gridCol);
            }

            _dgTariff.ItemsSource = _dataTable.DefaultView;
        }

        private void DgTariff_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;
            string colName = e.Column.SortMemberPath;
            if (string.IsNullOrEmpty(colName) || _dataTable == null) return;

            ListSortDirection newDirection = (e.Column.SortDirection != ListSortDirection.Ascending)
                ? ListSortDirection.Ascending
                : ListSortDirection.Descending;

            foreach (var col in _dgTariff.Columns)
            {
                col.SortDirection = null;
            }
            e.Column.SortDirection = newDirection;

            SortDataTable(colName, newDirection);
        }

        private void SortDataTable(string colName, ListSortDirection direction)
        {
            if (_dataTable == null || _dataTable.Rows.Count <= 1) return;

            bool isNumeric = true;
            int numericCount = 0;
            foreach (DataRow row in _dataTable.Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;
                string s = row[colName] != null ? row[colName].ToString().Trim() : "";
                if (!string.IsNullOrEmpty(s))
                {
                    double d;
                    if (NumberParser.TryParseClean(s, out d))
                    {
                        numericCount++;
                    }
                    else
                    {
                        isNumeric = false;
                        break;
                    }
                }
            }
            if (numericCount == 0) isNumeric = false;

            var rows = new List<DataRow>();
            foreach (DataRow r in _dataTable.Rows)
            {
                if (r.RowState != DataRowState.Deleted) rows.Add(r);
            }

            rows.Sort(delegate(DataRow r1, DataRow r2)
            {
                string s1 = r1[colName] != null ? r1[colName].ToString().Trim() : "";
                string s2 = r2[colName] != null ? r2[colName].ToString().Trim() : "";

                if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return 0;
                if (string.IsNullOrEmpty(s1)) return direction == ListSortDirection.Ascending ? 1 : -1;
                if (string.IsNullOrEmpty(s2)) return direction == ListSortDirection.Ascending ? -1 : 1;

                int cmp;
                if (isNumeric)
                {
                    double d1, d2;
                    bool p1 = NumberParser.TryParseClean(s1, out d1);
                    bool p2 = NumberParser.TryParseClean(s2, out d2);
                    if (p1 && p2)
                    {
                        cmp = d1.CompareTo(d2);
                    }
                    else if (p1)
                    {
                        cmp = -1;
                    }
                    else if (p2)
                    {
                        cmp = 1;
                    }
                    else
                    {
                        cmp = string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase);
                    }
                }
                else
                {
                    cmp = string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase);
                }

                return direction == ListSortDirection.Ascending ? cmp : -cmp;
            });

            _dataTable.BeginLoadData();
            DataTable copy = _dataTable.Clone();
            foreach (var r in rows) copy.ImportRow(r);

            _dataTable.Rows.Clear();
            foreach (DataRow r in copy.Rows) _dataTable.ImportRow(r);
            _dataTable.EndLoadData();

            _dataTable.DefaultView.Sort = "";
            if (_dgTariff.ItemsSource != null)
            {
                _dgTariff.Items.Refresh();
            }
        }

        private void OpenColumnFilter(string colName, Button filterBtn)
        {
            if (_dataTable == null) return;

            // Gather sorted unique values
            var uniqueValues = new HashSet<string>();
            foreach (DataRow row in _dataTable.Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;
                string val = row[colName] != null ? row[colName].ToString() : "";
                uniqueValues.Add(val);
            }

            var sortedList = new List<string>(uniqueValues);
            sortedList.Sort(delegate(string a, string b)
            {
                double da, db;
                bool pa = NumberParser.TryParseClean(a, out da);
                bool pb = NumberParser.TryParseClean(b, out db);
                if (pa && pb) return da.CompareTo(db);
                if (pa) return -1;
                if (pb) return 1;
                return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            });

            HashSet<string> active = null;
            if (_columnFilters.ContainsKey(colName))
            {
                active = _columnFilters[colName];
            }

            var dlg = new FilterDialog(sortedList, active, colName, this);
            if (dlg.ShowDialog() == true)
            {
                if (dlg.ResultFilters.Count == sortedList.Count)
                {
                    _columnFilters.Remove(colName);
                    filterBtn.Content = "\u25BE";
                    filterBtn.Foreground = (Brush)FindResource("BrushTextSecondary");
                }
                else
                {
                    _columnFilters[colName] = dlg.ResultFilters;
                    filterBtn.Content = "\u25BC";
                    filterBtn.Foreground = (Brush)FindResource("BrushAccent");
                }

                ApplyAllFilters();
            }
        }

        private void ApplyAllFilters()
        {
            if (_dataTable == null) return;

            if (_columnFilters.Count == 0)
            {
                _dataTable.DefaultView.RowFilter = "";
                if (_dgTariff.ItemsSource != null)
                {
                    _dgTariff.Items.Refresh();
                }
                return;
            }

            var clauses = new List<string>();
            foreach (var kvp in _columnFilters)
            {
                string col = kvp.Key;
                HashSet<string> allowed = kvp.Value;

                if (allowed.Count == 0)
                {
                    clauses.Add("1=0");
                    continue;
                }

                var valList = new List<string>();
                bool hasEmpty = false;
                foreach (string val in allowed)
                {
                    if (string.IsNullOrEmpty(val))
                    {
                        hasEmpty = true;
                    }
                    else
                    {
                        valList.Add("'" + val.Replace("'", "''") + "'");
                    }
                }

                var colConditions = new List<string>();
                if (valList.Count > 0)
                {
                    colConditions.Add(string.Format("[{0}] IN ({1})", col, string.Join(",", valList.ToArray())));
                }
                if (hasEmpty)
                {
                    colConditions.Add(string.Format("([{0}] IS NULL OR [{0}] = '')", col));
                }

                if (colConditions.Count > 0)
                {
                    clauses.Add("(" + string.Join(" OR ", colConditions.ToArray()) + ")");
                }
            }

            _dataTable.DefaultView.RowFilter = string.Join(" AND ", clauses.ToArray());
            if (_dgTariff.ItemsSource != null)
            {
                _dgTariff.Items.Refresh();
            }
        }

        private void ClearAllFilters()
        {
            _columnFilters.Clear();
            if (_dataTable != null)
            {
                _dataTable.DefaultView.RowFilter = "";
                _dataTable.DefaultView.Sort = "";
            }

            // Reset header filter button visual indicators
            foreach (DataGridColumn c in _dgTariff.Columns)
            {
                c.SortDirection = null;
                var grid = c.Header as Grid;
                if (grid != null && grid.Children.Count > 1)
                {
                    var btn = grid.Children[1] as Button;
                    if (btn != null)
                    {
                        btn.Content = "\u25BE";
                        btn.Foreground = (Brush)FindResource("BrushTextSecondary");
                    }
                }
            }

            if (_dgTariff.ItemsSource != null)
            {
                _dgTariff.Items.Refresh();
            }
        }

        private void AddRow()
        {
            if (_dgTariff != null)
            {
                _dgTariff.CommitEdit(DataGridEditingUnit.Row, true);
                _dgTariff.CommitEdit();
            }

            if (_dataTable == null || _dataTable.Columns.Count == 0)
            {
                MessageBox.Show(this, "Kein Schema verfügbar. Bitte laden oder erstellen Sie zuerst einen Tarif.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Dictionary<string, string> defaults = _engine.GetParameterDefaults();
            DataRow newRow = _dataTable.NewRow();

            foreach (DataColumn col in _dataTable.Columns)
            {
                string cName = col.ColumnName;
                if (defaults.ContainsKey(cName))
                {
                    newRow[cName] = defaults[cName];
                }
                else if (cName == "id_orderkind")
                {
                    newRow[cName] = _cmbOrderKind.SelectedIndex == 1 ? "3" : "2";
                }
                else
                {
                    newRow[cName] = "0.00";
                }
            }

            _dataTable.Rows.Add(newRow);
            UpdateUiState();
        }

        private void DeleteRowsAction()
        {
            if (_dgTariff != null)
            {
                _dgTariff.CommitEdit(DataGridEditingUnit.Row, true);
                _dgTariff.CommitEdit();
            }

            if (_dataTable == null || _dataTable.Rows.Count == 0) return;

            var selectedItems = _dgTariff.SelectedItems;
            if (selectedItems == null || selectedItems.Count == 0)
            {
                // Delete all rows confirmation
                var result = MessageBox.Show(this, "M\u00F6chten Sie wirklich ALLE Zeilen l\u00F6schen?", "L\u00F6schen best\u00E4tigen", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _dataTable.Rows.Clear();
                    UpdateUiState();
                }
            }
            else
            {
                // Delete selected rows confirmation
                int count = selectedItems.Count;
                var result = MessageBox.Show(this, string.Format("M\u00F6chten Sie wirklich {0} ausgew\u00E4hlte Zeile(n) l\u00F6schen?", count), "L\u00F6schen best\u00E4tigen", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    var rowsToDelete = new List<DataRow>();
                    foreach (var item in selectedItems)
                    {
                        var drv = item as DataRowView;
                        if (drv != null)
                        {
                            rowsToDelete.Add(drv.Row);
                        }
                    }

                    foreach (DataRow r in rowsToDelete)
                    {
                        _dataTable.Rows.Remove(r);
                    }

                    _dgTariff.UnselectAll();
                    UpdateDeleteButtonState();
                }
            }
        }

        private void UpdateOrderKindInTable()
        {
            if (_dataTable == null || !_dataTable.Columns.Contains("id_orderkind")) return;

            int kindVal = _cmbOrderKind.SelectedIndex == 1 ? 3 : 2;
            _engine.SetOrderKind(_dataTable, kindVal);
            if (_dgTariff.ItemsSource != null)
            {
                _dgTariff.Items.Refresh();
            }
        }

        private void OpenMatrixImport()
        {
            if (_dataTable == null || _dataTable.Columns.Count == 0)
            {
                MessageBox.Show(this, "Bitte erstelle erst einen neuen oder \u00F6ffne einen bestehenden Tarif.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cols = new List<string>();
            foreach (DataColumn c in _dataTable.Columns) cols.Add(c.ColumnName);

            bool isVolume = _dataTable.Columns.Contains("minVolume") || (GetCurrentSpecification().IndexOf("Volume", StringComparison.OrdinalIgnoreCase) >= 0);
            string unit = isVolume ? "cbm" : "kg";

            var dlg = new MatrixImportDialog(cols, this, unit);
            if (dlg.ShowDialog() == true && dlg.ResultData != null && dlg.ResultData.Count > 0)
            {
                if (dlg.ReplaceMode)
                {
                    _dataTable.Rows.Clear();
                }

                Dictionary<string, string> defaults = _engine.GetParameterDefaults();
                int orderKindVal = _cmbOrderKind.SelectedIndex == 1 ? 3 : 2;

                _dataTable.BeginLoadData();
                foreach (var rowDict in dlg.ResultData)
                {
                    DataRow dr = _dataTable.NewRow();
                    foreach (string col in cols)
                    {
                        if (rowDict.ContainsKey(col))
                        {
                            dr[col] = NumberParser.FormatDisplay(rowDict[col], col);
                        }
                        else if (col == "id_orderkind")
                        {
                            dr[col] = orderKindVal.ToString();
                        }
                        else if (defaults.ContainsKey(col))
                        {
                            dr[col] = defaults[col];
                        }
                        else
                        {
                            dr[col] = "0.00";
                        }
                    }
                    _dataTable.Rows.Add(dr);
                }
                _dataTable.EndLoadData();
                UpdateUiState();
            }
        }

        private void OpenBulkUpdateDialog()
        {
            if (_dgTariff != null)
            {
                _dgTariff.CommitEdit(DataGridEditingUnit.Row, true);
                _dgTariff.CommitEdit();
            }

            if (_dataTable == null || _dataTable.Rows.Count == 0)
            {
                MessageBox.Show(this, "Tabelle ist leer. Bitte laden Sie zuerst einen Tarif.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var selectedIndices = new List<int>();
            if (_dgTariff.SelectedItems != null && _dgTariff.SelectedItems.Count > 0)
            {
                foreach (var item in _dgTariff.SelectedItems)
                {
                    var drv = item as DataRowView;
                    if (drv != null)
                    {
                        int index = _dataTable.Rows.IndexOf(drv.Row);
                        if (index >= 0) selectedIndices.Add(index);
                    }
                }
            }

            var dlg = new BulkUpdateDialog(_engine, _dataTable, selectedIndices, this);
            if (dlg.ShowDialog() == true)
            {
                if (_dgTariff.ItemsSource != null)
                {
                    _dgTariff.Items.Refresh();
                }
            }
        }

        private void GenerateXml()
        {
            if (_dgTariff != null)
            {
                _dgTariff.CommitEdit(DataGridEditingUnit.Row, true);
                _dgTariff.CommitEdit();
            }

            if (_dataTable == null || _dataTable.Rows.Count == 0)
            {
                MessageBox.Show(this, "Es sind keine Zeilen zum Generieren vorhanden.", "Warnung", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var meta = new TariffMetadata
                {
                    Id = _txtName.Text != null ? _txtName.Text.Trim() : "NEW_TARIFF",
                    Name = _txtName.Text != null ? _txtName.Text.Trim() : "New Tariff",
                    ValidFrom = _dpValidFrom.SelectedDate != null ? _dpValidFrom.SelectedDate.Value.ToString("yyyy-MM-dd") : DateTime.Today.ToString("yyyy-MM-dd"),
                    ValidTo = _dpValidTo.SelectedDate != null ? _dpValidTo.SelectedDate.Value.ToString("yyyy-MM-dd") : "2099-12-31",
                    Spec = GetCurrentSpecification()
                };
                _engine.UpdateMetadata(meta);

                int kindVal = _cmbOrderKind.SelectedIndex == 1 ? 3 : 2;
                _engine.SetOrderKind(_dataTable, kindVal);
                _engine.UpdateTuples(_dataTable);

                string safeName = SanitizeFileName(!string.IsNullOrEmpty(meta.Name) ? meta.Name : "Tarif");
                var sfd = new SaveFileDialog
                {
                    Title = "XML speichern",
                    FileName = safeName + ".xml",
                    Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*"
                };

                if (!string.IsNullOrEmpty(_lastDirectory) && Directory.Exists(_lastDirectory))
                {
                    sfd.InitialDirectory = _lastDirectory;
                }

                if (sfd.ShowDialog(this) == true)
                {
                    _lastDirectory = Path.GetDirectoryName(sfd.FileName);
                    _engine.SaveToFile(sfd.FileName);
                    HideExportFlyout(true);
                    ShowSuccessAnimation("Lokal", Path.GetFileName(sfd.FileName));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Fehler beim Generieren der XML-Datei:\n" + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportDirectly(string targetDir, string backupDir, string envName)
        {
            if (_dgTariff != null)
            {
                _dgTariff.CommitEdit(DataGridEditingUnit.Row, true);
                _dgTariff.CommitEdit();
            }

            if (_dataTable == null || _dataTable.Rows.Count == 0)
            {
                MessageBox.Show(this, "Es sind keine Zeilen zum Generieren vorhanden.", "Warnung", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var meta = new TariffMetadata
                {
                    Id = _txtName.Text != null ? _txtName.Text.Trim() : "NEW_TARIFF",
                    Name = _txtName.Text != null ? _txtName.Text.Trim() : "New Tariff",
                    ValidFrom = _dpValidFrom.SelectedDate != null ? _dpValidFrom.SelectedDate.Value.ToString("yyyy-MM-dd") : DateTime.Today.ToString("yyyy-MM-dd"),
                    ValidTo = _dpValidTo.SelectedDate != null ? _dpValidTo.SelectedDate.Value.ToString("yyyy-MM-dd") : "2099-12-31",
                    Spec = GetCurrentSpecification()
                };
                _engine.UpdateMetadata(meta);

                int kindVal = _cmbOrderKind.SelectedIndex == 1 ? 3 : 2;
                _engine.SetOrderKind(_dataTable, kindVal);
                _engine.UpdateTuples(_dataTable);

                string safeName = SanitizeFileName(!string.IsNullOrEmpty(meta.Name) ? meta.Name : "Tarif");
                string fileName = safeName + ".xml";

                // 1. Verzeichnisse prüfen und bei Bedarf erstellen
                if (!Directory.Exists(targetDir))
                {
                    try
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, string.Format("Das {0}-Verzeichnis konnte nicht erreicht werden:\n{1}\n\nFehler: {2}\n\nBitte stellen Sie sicher, dass das Netzlaufwerk N:\\ verbunden und erreichbar ist.", envName, targetDir, ex.Message),
                            "Netzwerkpfad nicht erreichbar", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                if (!Directory.Exists(backupDir))
                {
                    try
                    {
                        Directory.CreateDirectory(backupDir);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, string.Format("Das Archiv-Verzeichnis für {0} konnte nicht erreicht oder erstellt werden:\n{1}\n\nFehler: {2}\n\nBitte stellen Sie sicher, dass das Netzlaufwerk N:\\ verbunden und erreichbar ist.", envName, backupDir, ex.Message),
                            "Archivpfad nicht erreichbar", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                string targetFullPath = Path.Combine(targetDir, fileName);
                string backupFullPath = Path.Combine(backupDir, fileName);

                bool targetExists = File.Exists(targetFullPath);
                bool backupExists = File.Exists(backupFullPath);

                // 2. Pre-Flight Check: Prüfen, ob die Datei bereits im Ziel- oder Archivordner existiert
                if (targetExists || backupExists)
                {
                    DateTime lastModified = DateTime.MinValue;
                    if (targetExists)
                    {
                        try { lastModified = File.GetLastWriteTime(targetFullPath); } catch { }
                    }
                    else if (backupExists)
                    {
                        try { lastModified = File.GetLastWriteTime(backupFullPath); } catch { }
                    }

                    string dateStr = lastModified != DateTime.MinValue
                        ? string.Format(" (zuletzt geändert: {0:dd.MM.yyyy 'um' HH:mm 'Uhr'})", lastModified)
                        : "";

                    string warnMessage = string.Format(
                        "⚠️ Achtung: Tarif existiert bereits auf {0}!\n\n" +
                        "Auf {0} ist die Datei \"{1}\" bereits vorhanden{2}.\n\n" +
                        "Achtung: Wenn Sie diese Datei überschreiben, wird auch der aktive Tarif im ORD überschrieben!\n\n" +
                        "Möchten Sie den bestehenden Tarif wirklich im ORD überschreiben?",
                        envName, fileName, dateStr);

                    var result = MessageBox.Show(this, warnMessage,
                        string.Format("Tarif auf {0} überschreiben?", envName),
                        MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

                    if (result != MessageBoxResult.Yes)
                    {
                        HideExportFlyout(true);
                        return; // Abgebrochen -> Keine Datei wird geschrieben!
                    }
                }

                // 3. Datei im Hauptverzeichnis speichern (ORD Outbound)
                _engine.SaveToFile(targetFullPath);

                // 4. Exakte Sicherungskopie im Archivordner ablegen
                try
                {
                    _engine.SaveToFile(backupFullPath);
                }
                catch (Exception backupEx)
                {
                    MessageBox.Show(this, string.Format("Der Tarif wurde auf {0} hochgeladen, aber die Sicherungskopie im Archiv konnte nicht gespeichert werden:\n{1}", envName, backupEx.Message),
                        "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                HideExportFlyout(true);
                ShowSuccessAnimation(envName, fileName);
                RefreshRecentFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Fehler beim Speichern der XML-Datei:\n" + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowExportFlyout()
        {
            if (_flyoutCloseTimer != null) _flyoutCloseTimer.Stop();
            if (_flyoutExportMenu == null) return;
            if (_badgeSuccessNotification != null && _badgeSuccessNotification.Visibility == Visibility.Visible)
            {
                return;
            }

            _flyoutExportMenu.Visibility = Visibility.Visible;

            var animY = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(160),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var animOpacity = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(160)
            };
            animOpacity.Completed += (s, e) =>
            {
                // Detach animation clock so WPF re-enables full subpixel ClearType rendering
                _flyoutExportMenu.BeginAnimation(UIElement.OpacityProperty, null);
                _flyoutExportMenu.Opacity = 1.0;
                if (_flyoutTransform != null)
                {
                    _flyoutTransform.BeginAnimation(TranslateTransform.YProperty, null);
                    _flyoutTransform.Y = 0;
                }
            };

            if (_flyoutTransform != null)
            {
                _flyoutTransform.BeginAnimation(TranslateTransform.YProperty, animY);
            }
            _flyoutExportMenu.BeginAnimation(UIElement.OpacityProperty, animOpacity);
        }

        private void HideExportFlyout(bool immediate)
        {
            if (_flyoutCloseTimer != null) _flyoutCloseTimer.Stop();
            if (_flyoutExportMenu == null || _flyoutExportMenu.Visibility != Visibility.Visible) return;

            if (immediate)
            {
                _flyoutExportMenu.BeginAnimation(UIElement.OpacityProperty, null);
                if (_flyoutTransform != null) _flyoutTransform.BeginAnimation(TranslateTransform.YProperty, null);
                _flyoutExportMenu.Visibility = Visibility.Collapsed;
                _flyoutExportMenu.Opacity = 0;
                if (_flyoutTransform != null) _flyoutTransform.Y = 15;
                return;
            }

            var animY = new DoubleAnimation
            {
                To = 15,
                Duration = TimeSpan.FromMilliseconds(140),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            var animOpacity = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(140)
            };
            animOpacity.Completed += (s, e) =>
            {
                _flyoutExportMenu.BeginAnimation(UIElement.OpacityProperty, null);
                if (_flyoutTransform != null) _flyoutTransform.BeginAnimation(TranslateTransform.YProperty, null);
                _flyoutExportMenu.Visibility = Visibility.Collapsed;
                _flyoutExportMenu.Opacity = 0;
                if (_flyoutTransform != null) _flyoutTransform.Y = 15;
            };

            if (_flyoutTransform != null)
            {
                _flyoutTransform.BeginAnimation(TranslateTransform.YProperty, animY);
            }
            _flyoutExportMenu.BeginAnimation(UIElement.OpacityProperty, animOpacity);
        }

        private void ShowSuccessAnimation(string envName, string fileName)
        {
            if (_badgeSuccessNotification == null) return;

            HideExportFlyout(true);

            if (_txtSuccessTitle != null)
            {
                _txtSuccessTitle.Text = string.Format("Auf {0} gespeichert! \u2714", envName);
            }
            if (_txtSuccessDetail != null)
            {
                _txtSuccessDetail.Text = fileName;
            }

            _badgeSuccessNotification.Visibility = Visibility.Visible;

            var animScaleX = new DoubleAnimation
            {
                From = 0.55,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(260),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.35 }
            };
            var animScaleY = new DoubleAnimation
            {
                From = 0.55,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(260),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.35 }
            };
            var animTranslateY = new DoubleAnimation
            {
                From = 12,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var animOpacity = new DoubleAnimation
            {
                From = 0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(180)
            };
            animOpacity.Completed += (s, e) =>
            {
                _badgeSuccessNotification.BeginAnimation(UIElement.OpacityProperty, null);
                _badgeSuccessNotification.Opacity = 1.0;
                if (_badgeTranslate != null)
                {
                    _badgeTranslate.BeginAnimation(TranslateTransform.YProperty, null);
                    _badgeTranslate.Y = 0;
                }
                if (_badgeScale != null)
                {
                    _badgeScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    _badgeScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                    _badgeScale.ScaleX = 1.0;
                    _badgeScale.ScaleY = 1.0;
                }
            };

            if (_badgeScale != null)
            {
                _badgeScale.BeginAnimation(ScaleTransform.ScaleXProperty, animScaleX);
                _badgeScale.BeginAnimation(ScaleTransform.ScaleYProperty, animScaleY);
            }
            if (_badgeTranslate != null)
            {
                _badgeTranslate.BeginAnimation(TranslateTransform.YProperty, animTranslateY);
            }
            _badgeSuccessNotification.BeginAnimation(UIElement.OpacityProperty, animOpacity);

            if (_successTimer != null)
            {
                _successTimer.Stop();
            }
            _successTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2500) };
            _successTimer.Tick += (s, e) =>
            {
                _successTimer.Stop();
                HideSuccessBadge();
            };
            _successTimer.Start();
        }

        private void HideSuccessBadge()
        {
            if (_badgeSuccessNotification == null || _badgeSuccessNotification.Visibility != Visibility.Visible) return;

            var animOpacity = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            var animTranslateY = new DoubleAnimation
            {
                To = -8,
                Duration = TimeSpan.FromMilliseconds(250)
            };
            animOpacity.Completed += (s, e) =>
            {
                _badgeSuccessNotification.BeginAnimation(UIElement.OpacityProperty, null);
                if (_badgeTranslate != null) _badgeTranslate.BeginAnimation(TranslateTransform.YProperty, null);
                _badgeSuccessNotification.Visibility = Visibility.Collapsed;
                _badgeSuccessNotification.Opacity = 0;
            };

            if (_badgeTranslate != null)
            {
                _badgeTranslate.BeginAnimation(TranslateTransform.YProperty, animTranslateY);
            }
            _badgeSuccessNotification.BeginAnimation(UIElement.OpacityProperty, animOpacity);
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Tarif";
            char[] invalids = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (char c in name)
            {
                if (Array.IndexOf(invalids, c) < 0)
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }
            string res = sb.ToString().Trim();
            return string.IsNullOrEmpty(res) ? "Tarif" : res;
        }

        // ==================== ZULETZT HOCHGELADENE TARIFE ====================

        private class RecentTariffFile
        {
            public string FullPath { get; set; }
            public string NameWithoutExt { get; set; }
            public DateTime LastWriteTime { get; set; }
        }

        private void HandleChildListPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var sv = sender as ScrollViewer;
            if (sv == null) return;

            if ((e.Delta > 0 && sv.VerticalOffset <= 0) ||
                (e.Delta < 0 && sv.VerticalOffset >= sv.ScrollableHeight))
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = sender
                };
                var parent = sv.Parent as UIElement;
                if (parent != null) parent.RaiseEvent(eventArg);
            }
        }

        public void RefreshRecentFiles()
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                CleanupMisplacedOutboundArchive();

                string prodBackup = GetBackupDirectory(true);
                string prodMain = ProdDirectory;
                bool prodReachable = Directory.Exists(prodBackup) || Directory.Exists(prodMain);
                var prodFiles = ScanTariffDirectory(prodBackup, prodMain);

                string testBackup = GetBackupDirectory(false);
                string testMain = TestDirectory;
                bool testReachable = Directory.Exists(testBackup) || Directory.Exists(testMain);
                var testFiles = ScanTariffDirectory(testBackup, testMain);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    PopulateRecentList(_panelRecentProd, prodFiles, prodReachable);
                    PopulateRecentList(_panelRecentTest, testFiles, testReachable);
                }));
            });
        }

        private List<RecentTariffFile> ScanTariffDirectory(string primaryDir, string secondaryDir)
        {
            var dict = new Dictionary<string, RecentTariffFile>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (!string.IsNullOrEmpty(primaryDir) && Directory.Exists(primaryDir))
                {
                    var dirInfo = new DirectoryInfo(primaryDir);
                    foreach (var fi in dirInfo.GetFiles("*.xml"))
                    {
                        dict[fi.Name] = new RecentTariffFile
                        {
                            FullPath = fi.FullName,
                            NameWithoutExt = Path.GetFileNameWithoutExtension(fi.Name),
                            LastWriteTime = fi.LastWriteTime
                        };
                    }
                }
            }
            catch { }

            try
            {
                if (!string.IsNullOrEmpty(secondaryDir) && Directory.Exists(secondaryDir))
                {
                    var dirInfo = new DirectoryInfo(secondaryDir);
                    foreach (var fi in dirInfo.GetFiles("*.xml"))
                    {
                        RecentTariffFile existing;
                        if (!dict.TryGetValue(fi.Name, out existing) || fi.LastWriteTime > existing.LastWriteTime)
                        {
                            dict[fi.Name] = new RecentTariffFile
                            {
                                FullPath = fi.FullName,
                                NameWithoutExt = Path.GetFileNameWithoutExtension(fi.Name),
                                LastWriteTime = fi.LastWriteTime
                            };
                        }
                    }
                }
            }
            catch { }

            var list = new List<RecentTariffFile>(dict.Values);
            list.Sort((a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
            return list;
        }

        private void PopulateRecentList(StackPanel panel, List<RecentTariffFile> files, bool isReachable)
        {
            if (panel == null) return;
            panel.Children.Clear();

            if (!isReachable)
            {
                var txtUnreachable = new TextBlock
                {
                    Text = "Netzlaufwerk N:\\ nicht erreichbar",
                    FontSize = 9.5,
                    FontStyle = FontStyles.Italic,
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#5A6578"),
                    Margin = new Thickness(4, 8, 4, 8),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center
                };
                panel.Children.Add(txtUnreachable);
                return;
            }

            if (files == null || files.Count == 0)
            {
                var txtEmpty = new TextBlock
                {
                    Text = "Keine Tarife vorhanden",
                    FontSize = 9.5,
                    FontStyle = FontStyles.Italic,
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#5A6578"),
                    Margin = new Thickness(4, 8, 4, 8),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center
                };
                panel.Children.Add(txtEmpty);
                return;
            }

            foreach (var file in files)
            {
                var itemBorder = CreateRecentFileItem(file);
                panel.Children.Add(itemBorder);
            }
        }

        private Border CreateRecentFileItem(RecentTariffFile file)
        {
            string name = file.NameWithoutExt;
            string fullPath = file.FullPath;
            DateTime time = file.LastWriteTime;

            var itemBorder = new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 0, 0, 2),
                Cursor = Cursors.Hand,
                ToolTip = string.Format("{0}\n\nPfad: {1}\nZuletzt geändert: {2:dd.MM.yyyy HH:mm:ss}\n\n💡 Doppelklick zum Laden in den Editor\nRechtsklick: Optionen", name, fullPath, time)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var spLeft = new StackPanel();

            var txtName = new TextBlock
            {
                Text = name,
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#CBD5E1"),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            spLeft.Children.Add(txtName);

            var txtDate = new TextBlock
            {
                Text = time.ToString("dd.MM.yy HH:mm"),
                FontSize = 8.5,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#64748B"),
                Margin = new Thickness(0, 1, 0, 0)
            };
            spLeft.Children.Add(txtDate);

            Grid.SetColumn(spLeft, 0);
            grid.Children.Add(spLeft);

            var txtArrow = new TextBlock
            {
                Text = "›",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#475569"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            Grid.SetColumn(txtArrow, 1);
            grid.Children.Add(txtArrow);

            itemBorder.Child = grid;

            var hoverBg = (Brush)new BrushConverter().ConvertFrom("#161E2E");
            var hoverBorder = (Brush)new BrushConverter().ConvertFrom("#2A374F");
            var hoverText = (Brush)new BrushConverter().ConvertFrom("#38BDF8");

            itemBorder.MouseEnter += (s, e) =>
            {
                itemBorder.Background = hoverBg;
                itemBorder.BorderBrush = hoverBorder;
                txtName.Foreground = hoverText;
                txtArrow.Foreground = (Brush)new BrushConverter().ConvertFrom("#94A3B8");
            };

            itemBorder.MouseLeave += (s, e) =>
            {
                itemBorder.Background = Brushes.Transparent;
                itemBorder.BorderBrush = Brushes.Transparent;
                txtName.Foreground = (Brush)new BrushConverter().ConvertFrom("#CBD5E1");
                txtArrow.Foreground = (Brush)new BrushConverter().ConvertFrom("#475569");
            };

            itemBorder.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    e.Handled = true;
                    LoadXmlFile(fullPath);
                }
            };

            // Right-click context menu on file item
            var menu = new ContextMenu();
            var itemLoad = new MenuItem { Header = "⚡ Tarif in Editor laden" };
            itemLoad.Click += (s, e) => LoadXmlFile(fullPath);
            menu.Items.Add(itemLoad);

            var itemExplorer = new MenuItem { Header = "📂 Im Explorer anzeigen" };
            itemExplorer.Click += (s, e) =>
            {
                try
                {
                    if (File.Exists(fullPath))
                    {
                        Process.Start("explorer.exe", string.Format("/select,\"{0}\"", fullPath));
                    }
                    else
                    {
                        string dir = Path.GetDirectoryName(fullPath);
                        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                        {
                            Process.Start("explorer.exe", dir);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Fehler beim Öffnen:\n" + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            menu.Items.Add(itemExplorer);

            itemBorder.ContextMenu = menu;

            return itemBorder;
        }

        private void OpenUploadDirectory(bool isProd)
        {
            try
            {
                string backupDir = GetBackupDirectory(isProd);
                string mainDir = isProd ? ProdDirectory : TestDirectory;
                string target = Directory.Exists(backupDir) ? backupDir : (Directory.Exists(mainDir) ? mainDir : null);

                if (target != null && Directory.Exists(target))
                {
                    Process.Start("explorer.exe", target);
                }
                else
                {
                    string shownPath = Directory.Exists(backupDir) ? backupDir : mainDir;
                    MessageBox.Show(this,
                        string.Format("Das Verzeichnis für {0} ist derzeit nicht erreichbar:\n{1}\n\nBitte prüfen Sie, ob das Netzlaufwerk N:\\ verbunden ist.",
                            isProd ? "PROD" : "TEST", shownPath),
                        "Verzeichnis nicht erreichbar",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Fehler beim Öffnen des Verzeichnisses:\n" + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ContextMenu CreateDirectoryContextMenu(bool isProd)
        {
            var menu = new ContextMenu();
            string backupDir = GetBackupDirectory(isProd);
            string mainDir = isProd ? ProdDirectory : TestDirectory;

            var itemBackup = new MenuItem
            {
                Header = string.Format("📁 Archivordner öffnen ({0})", isProd ? "PROD" : "TEST")
            };
            itemBackup.Click += (s, e) => TryOpenFolder(backupDir);
            menu.Items.Add(itemBackup);

            var itemMain = new MenuItem
            {
                Header = string.Format("📁 Hauptordner öffnen ({0})", isProd ? "Outbound" : "Outbound Test")
            };
            itemMain.Click += (s, e) => TryOpenFolder(mainDir);
            menu.Items.Add(itemMain);

            return menu;
        }

        private void TryOpenFolder(string folderPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                {
                    Process.Start("explorer.exe", folderPath);
                }
                else
                {
                    MessageBox.Show(this, "Das Verzeichnis konnte nicht erreicht werden:\n" + folderPath, "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Fehler beim Öffnen des Verzeichnisses:\n" + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
