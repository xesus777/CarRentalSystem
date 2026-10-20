using System;
using System.Windows;

namespace CarRentalSystem.Views.Windows
{
    public partial class CustomDialogWindow : Window
    {
        public enum DialogType { Ok, YesNo }
        public enum DialogIcon { Info, Error, Question, Warning }

        private CustomDialogWindow(string message, DialogType type, DialogIcon icon)
        {
            InitializeComponent();
            TxtMessage.Text = message;

            

            if (type == DialogType.YesNo)
            {
                BtnOk.Visibility = Visibility.Collapsed;
                BtnYes.Visibility = Visibility.Visible;
                BtnNo.Visibility = Visibility.Visible;

                if (icon == DialogIcon.Question)
                {
                    var accentBrush = Application.Current.Resources["BgPanelAccent"] as System.Windows.Media.Brush;
                    if (accentBrush != null)
                    {
                        BtnYes.Background = accentBrush;
                    }
                }
            }
        }

        public static void ShowOk(string message, DialogIcon icon = DialogIcon.Info)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new CustomDialogWindow(message, DialogType.Ok, icon);
                if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
                dialog.ShowDialog();
            });
        }

        public static bool ShowYesNo(string message, DialogIcon icon = DialogIcon.Question)
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new CustomDialogWindow(message, DialogType.YesNo, icon);
                if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
                var result = dialog.ShowDialog();
                return result == true;
            });
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
