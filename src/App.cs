using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using OrdTarifManager.Core;
using OrdTarifManager.UI;
using OrdTarifManager.UI.Dialogs;

namespace OrdTarifManager
{
    public class App : Application
    {
        [STAThread]
        public static void Main()
        {
            var app = new App();
            app.DispatcherUnhandledException += App_DispatcherUnhandledException;
            app.InitializeTheme();

            var mainWindow = new MainWindow();
            app.Run(mainWindow);
        }

        private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            DarkMessageBox.Show("Ein unerwarteter Fehler ist aufgetreten:\n" + e.Exception.Message + "\n\nDetails:\n" + e.Exception.StackTrace,
                            "Anwendungsfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void InitializeTheme()
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            try
            {
                ResourceDictionary theme = XamlHelper.LoadResourceDictionary("Theme.xaml");
                Resources.MergedDictionaries.Add(theme);
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show("Fehler beim Laden des Themes: " + ex.Message, "Startfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
