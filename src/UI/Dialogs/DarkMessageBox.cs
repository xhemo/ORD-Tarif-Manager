using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using OrdTarifManager.Core;

namespace OrdTarifManager.UI.Dialogs
{
    public class DarkMessageBox : Window
    {
        public MessageBoxResult Result { get; private set; }

        private DarkMessageBox(Window owner, string message, string title, MessageBoxButton buttons, MessageBoxImage image, MessageBoxResult defaultResult)
        {
            Result = defaultResult;
            Title = !string.IsNullOrEmpty(title) ? title : "Hinweis";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.WidthAndHeight;
            MinWidth = 440;
            MaxWidth = 620;
            MinHeight = 150;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
            RenderOptions.SetClearTypeHint(this, ClearTypeHint.Enabled);

            if (owner != null && owner.IsVisible)
            {
                Owner = owner;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            BuildUi(message, Title, buttons, image, defaultResult);
        }

        private void BuildUi(string message, string title, MessageBoxButton buttons, MessageBoxImage image, MessageBoxResult defaultResult)
        {
            var rootGrid = new Grid
            {
                Margin = new Thickness(16)
            };

            var container = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x0F, 0x13, 0x1D)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x24, 0x2D, 0x40)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 24,
                    ShadowDepth = 6,
                    Direction = 270,
                    Color = Colors.Black,
                    Opacity = 0.75
                }
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40, GridUnitType.Pixel) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ================= ROW 0: TITLE BAR =================
            var titleBar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x14, 0x1A, 0x27)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x1F, 0x27, 0x3A)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(9, 9, 0, 0),
                Padding = new Thickness(14, 0, 8, 0)
            };
            titleBar.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed) DragMove();
            };

            var titleGrid = new Grid();
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var txtTitle = new TextBlock
            {
                Text = title,
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txtTitle, 0);
            titleGrid.Children.Add(txtTitle);

            var btnClose = new Button
            {
                Content = "\u2715",
                Width = 28,
                Height = 26,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            var closeTemplate = new ControlTemplate(typeof(Button));
            var closeBorderFactory = new FrameworkElementFactory(typeof(Border));
            closeBorderFactory.Name = "b";
            closeBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            closeBorderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            var closePresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            closePresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            closePresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            closeBorderFactory.AppendChild(closePresenterFactory);
            closeTemplate.VisualTree = closeBorderFactory;

            var triggerHover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            triggerHover.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)), "b"));
            triggerHover.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
            closeTemplate.Triggers.Add(triggerHover);
            btnClose.Template = closeTemplate;

            btnClose.Click += (s, e) =>
            {
                CloseWithResult(buttons == MessageBoxButton.YesNo ? MessageBoxResult.No : (buttons == MessageBoxButton.OKCancel || buttons == MessageBoxButton.YesNoCancel ? MessageBoxResult.Cancel : MessageBoxResult.OK));
            };
            Grid.SetColumn(btnClose, 1);
            titleGrid.Children.Add(btnClose);

            titleBar.Child = titleGrid;
            Grid.SetRow(titleBar, 0);
            mainGrid.Children.Add(titleBar);

            // ================= ROW 1: CONTENT BODY =================
            var bodyGrid = new Grid
            {
                Margin = new Thickness(20, 18, 20, 18)
            };
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Icon Badge
            var iconBadge = CreateIconBadge(image);
            Grid.SetColumn(iconBadge, 0);
            bodyGrid.Children.Add(iconBadge);

            // Message Text
            var txtMessage = new TextBlock
            {
                Text = message,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(16, 0, 0, 0)
            };
            Grid.SetColumn(txtMessage, 1);
            bodyGrid.Children.Add(txtMessage);

            Grid.SetRow(bodyGrid, 1);
            mainGrid.Children.Add(bodyGrid);

            // ================= ROW 2: ACTION FOOTER =================
            var footerBar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x0B, 0x0E, 0x15)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x19, 0x20, 0x2E)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                CornerRadius = new CornerRadius(0, 0, 9, 9),
                Padding = new Thickness(16, 12, 16, 12)
            };

            var pnlButtons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            CreateButtons(pnlButtons, buttons, defaultResult);

            footerBar.Child = pnlButtons;
            Grid.SetRow(footerBar, 2);
            mainGrid.Children.Add(footerBar);

            container.Child = mainGrid;
            rootGrid.Children.Add(container);
            Content = rootGrid;

            // Keyboard navigation
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    CloseWithResult(buttons == MessageBoxButton.YesNo ? MessageBoxResult.No : (buttons == MessageBoxButton.OKCancel || buttons == MessageBoxButton.YesNoCancel ? MessageBoxResult.Cancel : MessageBoxResult.OK));
                }
            };
        }

        private static Border CreateIconBadge(MessageBoxImage image)
        {
            var border = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                VerticalAlignment = VerticalAlignment.Center
            };

            var txt = new TextBlock
            {
                FontSize = 19,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            switch (image)
            {
                case MessageBoxImage.Error:
                    border.Background = new SolidColorBrush(Color.FromRgb(0x2E, 0x10, 0x15));
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
                    border.BorderThickness = new Thickness(1.5);
                    txt.Text = "\u2715";
                    txt.Foreground = new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71));
                    txt.FontSize = 18;
                    break;

                case MessageBoxImage.Warning:
                    border.Background = new SolidColorBrush(Color.FromRgb(0x2B, 0x1D, 0x0B));
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06));
                    border.BorderThickness = new Thickness(1.5);
                    txt.Text = "\u26A0";
                    txt.Foreground = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));
                    txt.FontSize = 19;
                    break;

                case MessageBoxImage.Question:
                    border.Background = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x38));
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x63, 0x66, 0xF1));
                    border.BorderThickness = new Thickness(1.5);
                    txt.Text = "?";
                    txt.Foreground = new SolidColorBrush(Color.FromRgb(0xA5, 0xB4, 0xFC));
                    txt.FontSize = 21;
                    break;

                case MessageBoxImage.Information:
                default:
                    border.Background = new SolidColorBrush(Color.FromRgb(0x0C, 0x23, 0x33));
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x02, 0x84, 0xC7));
                    border.BorderThickness = new Thickness(1.5);
                    txt.Text = "\u2139";
                    txt.Foreground = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));
                    txt.FontSize = 20;
                    break;
            }

            border.Child = txt;
            return border;
        }

        private void CreateButtons(StackPanel panel, MessageBoxButton buttons, MessageBoxResult defaultResult)
        {
            switch (buttons)
            {
                case MessageBoxButton.OK:
                    var btnOk = CreateStyledButton("OK", true, true, false);
                    btnOk.Click += (s, e) => CloseWithResult(MessageBoxResult.OK);
                    panel.Children.Add(btnOk);
                    break;

                case MessageBoxButton.OKCancel:
                    var btnCancel = CreateStyledButton("Abbrechen", false, defaultResult == MessageBoxResult.Cancel, true);
                    btnCancel.Click += (s, e) => CloseWithResult(MessageBoxResult.Cancel);
                    btnCancel.Margin = new Thickness(0, 0, 10, 0);
                    panel.Children.Add(btnCancel);

                    var btnOk2 = CreateStyledButton("OK", true, defaultResult != MessageBoxResult.Cancel, false);
                    btnOk2.Click += (s, e) => CloseWithResult(MessageBoxResult.OK);
                    panel.Children.Add(btnOk2);
                    break;

                case MessageBoxButton.YesNo:
                    var btnNo = CreateStyledButton("Nein", false, defaultResult == MessageBoxResult.No, true);
                    btnNo.Click += (s, e) => CloseWithResult(MessageBoxResult.No);
                    btnNo.Margin = new Thickness(0, 0, 10, 0);
                    panel.Children.Add(btnNo);

                    var btnYes = CreateStyledButton("Ja", true, defaultResult != MessageBoxResult.No, false);
                    btnYes.Click += (s, e) => CloseWithResult(MessageBoxResult.Yes);
                    panel.Children.Add(btnYes);
                    break;

                case MessageBoxButton.YesNoCancel:
                    var btnCancel3 = CreateStyledButton("Abbrechen", false, defaultResult == MessageBoxResult.Cancel, true);
                    btnCancel3.Click += (s, e) => CloseWithResult(MessageBoxResult.Cancel);
                    btnCancel3.Margin = new Thickness(0, 0, 10, 0);
                    panel.Children.Add(btnCancel3);

                    var btnNo3 = CreateStyledButton("Nein", false, defaultResult == MessageBoxResult.No, false);
                    btnNo3.Click += (s, e) => CloseWithResult(MessageBoxResult.No);
                    btnNo3.Margin = new Thickness(0, 0, 10, 0);
                    panel.Children.Add(btnNo3);

                    var btnYes3 = CreateStyledButton("Ja", true, defaultResult == MessageBoxResult.Yes, false);
                    btnYes3.Click += (s, e) => CloseWithResult(MessageBoxResult.Yes);
                    panel.Children.Add(btnYes3);
                    break;
            }
        }

        private static Button CreateStyledButton(string text, bool isPrimary, bool isDefault, bool isCancel)
        {
            var btn = new Button
            {
                Content = text,
                MinWidth = 86,
                Height = 32,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                IsDefault = isDefault,
                IsCancel = isCancel
            };

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "b";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(16, 5, 16, 5));

            var presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);
            template.VisualTree = borderFactory;

            if (isPrimary)
            {
                btn.Foreground = Brushes.White;
                borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x02, 0x84, 0xC7)));
                borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(0));

                var trHover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
                trHover.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x0E, 0xA5, 0xE9)), "b"));
                template.Triggers.Add(trHover);

                var trPress = new Trigger { Property = Button.IsPressedProperty, Value = true };
                trPress.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x03, 0x69, 0xA1)), "b"));
                template.Triggers.Add(trPress);
            }
            else
            {
                btn.Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1));
                borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x16, 0x1B, 0x26)));
                borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0x28, 0x33, 0x47)));
                borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));

                var trHover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
                trHover.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x20, 0x27, 0x38)), "b"));
                trHover.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0x3B, 0x4A, 0x66)), "b"));
                trHover.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
                template.Triggers.Add(trHover);

                var trPress = new Trigger { Property = Button.IsPressedProperty, Value = true };
                trPress.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1E)), "b"));
                template.Triggers.Add(trPress);
            }

            btn.Template = template;
            return btn;
        }

        private void CloseWithResult(MessageBoxResult result)
        {
            Result = result;
            DialogResult = result == MessageBoxResult.OK || result == MessageBoxResult.Yes;
            Close();
        }

        // ================= STATIC SHOW OVERLOADS =================

        public static MessageBoxResult Show(string message)
        {
            return Show(null, message, "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
        }

        public static MessageBoxResult Show(string message, string title)
        {
            return Show(null, message, title, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
        }

        public static MessageBoxResult Show(string message, string title, MessageBoxButton buttons)
        {
            return Show(null, message, title, buttons, MessageBoxImage.Information, MessageBoxResult.OK);
        }

        public static MessageBoxResult Show(string message, string title, MessageBoxButton buttons, MessageBoxImage image)
        {
            return Show(null, message, title, buttons, image, MessageBoxResult.OK);
        }

        public static MessageBoxResult Show(Window owner, string message)
        {
            return Show(owner, message, "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
        }

        public static MessageBoxResult Show(Window owner, string message, string title)
        {
            return Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
        }

        public static MessageBoxResult Show(Window owner, string message, string title, MessageBoxButton buttons)
        {
            return Show(owner, message, title, buttons, MessageBoxImage.Information, MessageBoxResult.OK);
        }

        public static MessageBoxResult Show(Window owner, string message, string title, MessageBoxButton buttons, MessageBoxImage image)
        {
            return Show(owner, message, title, buttons, image, MessageBoxResult.OK);
        }

        public static MessageBoxResult Show(Window owner, string message, string title, MessageBoxButton buttons, MessageBoxImage image, MessageBoxResult defaultResult)
        {
            if (Application.Current != null && Application.Current.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                return (MessageBoxResult)Application.Current.Dispatcher.Invoke(new Func<MessageBoxResult>(() =>
                {
                    return ShowInternal(owner, message, title, buttons, image, defaultResult);
                }));
            }
            return ShowInternal(owner, message, title, buttons, image, defaultResult);
        }

        private static MessageBoxResult ShowInternal(Window owner, string message, string title, MessageBoxButton buttons, MessageBoxImage image, MessageBoxResult defaultResult)
        {
            if (owner == null && Application.Current != null)
            {
                owner = Application.Current.MainWindow;
            }

            var dlg = new DarkMessageBox(owner, message, title, buttons, image, defaultResult);
            dlg.ShowDialog();
            return dlg.Result;
        }
    }
}
