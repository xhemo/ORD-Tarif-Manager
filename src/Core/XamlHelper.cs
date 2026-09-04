using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;

namespace OrdTarifManager.Core
{
    public static class XamlHelper
    {
        public static ResourceDictionary LoadResourceDictionary(string resourceName)
        {
            var asm = Assembly.GetExecutingAssembly();
            using (Stream stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, resourceName);
                    if (File.Exists(filePath))
                    {
                        using (FileStream fs = File.OpenRead(filePath))
                        {
                            return (ResourceDictionary)XamlReader.Load(fs);
                        }
                    }
                    throw new FileNotFoundException("Cannot find embedded resource or file: " + resourceName);
                }
                return (ResourceDictionary)XamlReader.Load(stream);
            }
        }

        public static FrameworkElement LoadElement(string resourceName)
        {
            var asm = Assembly.GetExecutingAssembly();
            using (Stream stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, resourceName);
                    if (File.Exists(filePath))
                    {
                        using (FileStream fs = File.OpenRead(filePath))
                        {
                            return (FrameworkElement)XamlReader.Load(fs);
                        }
                    }
                    throw new FileNotFoundException("Cannot find embedded resource or file: " + resourceName);
                }
                return (FrameworkElement)XamlReader.Load(stream);
            }
        }

        public static System.Windows.Media.ImageSource LoadImageSource(string resourceName)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                Stream stream = asm.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, resourceName);
                    if (File.Exists(filePath))
                    {
                        stream = File.OpenRead(filePath);
                    }
                }

                if (stream != null)
                {
                    using (stream)
                    {
                        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                }
            }
            catch
            {
                // Graceful fallback
            }
            return null;
        }
    }
}
