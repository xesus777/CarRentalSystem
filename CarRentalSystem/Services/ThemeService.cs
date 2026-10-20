using System;
using System.Windows;

namespace CarRentalSystem.Services
{
    public static class ThemeService
    {
        private static bool _isDark = false;
        public static bool IsDarkTheme => _isDark;

        public static void SetTheme(bool isDark)
        {
            _isDark = isDark;
            string themePath = isDark ? "Styles/DarkTheme.xaml" : "Styles/LightTheme.xaml";

            try
            {
                var newDict = new ResourceDictionary
                {
                    Source = new Uri(themePath, UriKind.Relative)
                };

                // ЖЕСТКИЙ ФИКС: Безопасное точечное удаление и вставка через методы коллекции
                if (Application.Current.Resources.MergedDictionaries.Count > 0)
                {
                    Application.Current.Resources.MergedDictionaries.RemoveAt(0);
                }
                Application.Current.Resources.MergedDictionaries.Insert(0, newDict);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка смены оформления: {ex.Message}");
            }
        }
    }
}
