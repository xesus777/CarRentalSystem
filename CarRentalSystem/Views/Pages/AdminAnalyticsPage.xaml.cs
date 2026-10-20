using System;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CarRentalSystem.Views.Windows;

namespace CarRentalSystem.Views.Pages
{
    public partial class AdminAnalyticsPage : Page
    {
        public AdminAnalyticsPage()
        {
            InitializeComponent();
            LoadStatistics();
            UpdateAnalyticsChart();
        }

        private void LoadStatistics()
        {
            try
            {
                foreach (var entry in Core.Context.ChangeTracker.Entries())
                {
                    entry.Reload();
                }

                var allPayments = Core.Context.Payments.ToList();
                var allCars = Core.Context.Cars.ToList();

                decimal totalEarned = allPayments
                    .Where(p => p.FK_PaymentStatus == 1)
                    .Sum(p => p.Amount);

                TxtTotalEarned.Text = string.Format("{0:C}", totalEarned);

                int rentedCars = allCars.Count(c => c.FK_Status == 2 || c.FK_Status == 4);
                TxtRentedCarsCount.Text = $"{rentedCars} авто";
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка загрузки статистики: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }

        private void UpdateAnalyticsChart()
        {
            if (ChartCanvas == null) return;
            ChartCanvas.Children.Clear();

            double width = ChartCanvas.ActualWidth;
            double height = ChartCanvas.ActualHeight;
            if (width <= 0 || height <= 0) return;

            try
            {
                foreach (var entry in Core.Context.ChangeTracker.Entries())
                {
                    entry.Reload();
                }

                int currentYear = DateTime.Now.Year;

                var payments = Core.Context.Payments
                    .Where(p => p.PaymentDate.Year == currentYear && p.FK_PaymentStatus == 1)
                    .ToList();

                decimal[] monthlyRevenues = new decimal[12];
                for (int m = 1; m <= 12; m++)
                {
                    monthlyRevenues[m - 1] = payments.Where(p => p.PaymentDate.Month == m).Sum(p => p.Amount);
                }

                decimal maxRevenue = monthlyRevenues.Max();
                if (maxRevenue == 0) maxRevenue = 10000;

                double barWidth = (width / 12) - 10;

                Brush barBrush = Application.Current.Resources["ChartBarBrush"] as Brush ?? Brushes.Teal;
                Brush badgeBgBrush = Application.Current.Resources["ChartBadgeBgBrush"] as Brush ?? Brushes.DarkBlue;

                Brush textSumBrush = Application.Current.Resources["TextLight"] as Brush ?? Brushes.White;
                Brush mutedBrush = Application.Current.Resources["TextSecondary"] as Brush ?? Brushes.Gray;

                for (int i = 0; i < 12; i++)
                {
                    double barHeight = ((double)monthlyRevenues[i] / (double)maxRevenue) * (height - 40);

                    if (monthlyRevenues[i] > 0)
                    {
                        Border txtBackground = new Border
                        {
                            Background = badgeBgBrush,
                            CornerRadius = new CornerRadius(3),
                            Padding = new Thickness(4, 2, 4, 2)
                        };

                        TextBlock txtSum = new TextBlock
                        {
                            Text = monthlyRevenues[i].ToString("N0"),
                            FontSize = 9,
                            FontWeight = FontWeights.Bold,
                            Foreground = textSumBrush
                        };

                        txtBackground.Child = txtSum;

                        Canvas.SetLeft(txtBackground, (i * (width / 12)) + 5 - 2);
                        Canvas.SetTop(txtBackground, height - barHeight - 20);
                        ChartCanvas.Children.Add(txtBackground);
                    }

                    Rectangle rect = new Rectangle
                    {
                        Width = Math.Max(barWidth, 2),
                        Height = Math.Max(barHeight, 2),
                        Fill = barBrush,
                        RadiusX = 3,
                        RadiusY = 3
                    };

                    double left = (i * (width / 12)) + 5;
                    double top = height - barHeight;

                    Canvas.SetLeft(rect, left);
                    Canvas.SetTop(rect, top);
                    ChartCanvas.Children.Add(rect);

                    TextBlock txtMonth = new TextBlock
                    {
                        Text = new DateTime(currentYear, i + 1, 1).ToString("MMM"),
                        FontSize = 10,
                        Foreground = mutedBrush
                    };
                    Canvas.SetLeft(txtMonth, left + (barWidth / 6));
                    Canvas.SetTop(txtMonth, height + 4);
                    ChartCanvas.Children.Add(txtMonth);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка отрисовки графика: {ex.Message}");
            }
        }




        private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateAnalyticsChart();
    }
}
