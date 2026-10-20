using System;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CarRentalSystem.Views.Windows;

namespace CarRentalSystem.Views.Pages
{
    public partial class BookingPage : Page
    {
        private int _carId;
        private int _userId;
        private Cars _car;
        private decimal _totalCost = 0;

        public BookingPage(int carId, int userId)
        {
            InitializeComponent();
            _carId = carId;
            _userId = userId;

            _car = Core.Context.Cars.FirstOrDefault(c => c.ID_Car == _carId);
            if (_car != null)
            {
                TxtCarInfo.Text = $"Оформление: {_car.Brand} {_car.Model}";
                PickerStart.DisplayDateStart = DateTime.Today;
                PickerEnd.DisplayDateStart = DateTime.Today;
            }
        }

        private void PayMethod_Changed(object sender, RoutedEventArgs e)
        {
            if (CardDetailsBlock == null || SbpQrBlock == null) return;

            if (RadioCard.IsChecked == true)
            {
                CardDetailsBlock.Visibility = Visibility.Visible;
                SbpQrBlock.Visibility = Visibility.Collapsed;
            }
            else if (RadioSbp.IsChecked == true)
            {
                CardDetailsBlock.Visibility = Visibility.Collapsed;
                SbpQrBlock.Visibility = Visibility.Visible;
                GenerateQrCode();
            }
            else if (RadioOffice.IsChecked == true)
            {
                CardDetailsBlock.Visibility = Visibility.Collapsed;
                SbpQrBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void GenerateQrCode()
        {
            if (QrUniformGrid == null) return;
            QrUniformGrid.Children.Clear();

            Random rand = new Random();

            for (int row = 0; row < 15; row++)
            {
                for (int col = 0; col < 15; col++)
                {
                    bool isMarker = (row < 4 && col < 4) || (row < 4 && col > 10) || (row > 10 && col < 4);

                    Border pixel = new Border();

                    if (isMarker)
                    {
                        bool isBorder = row == 0 || row == 3 || col == 0 || col == 3 ||
                                        row == 14 || row == 11 || (row > 10 && (col == 0 || col == 3)) ||
                                        col == 14 || col == 11 || (col > 10 && (row == 0 || row == 3));

                        pixel.Background = isBorder ? Brushes.Black : Brushes.White;
                    }
                    else
                    {
                        pixel.Background = rand.Next(0, 2) == 0 ? Brushes.Black : Brushes.White;
                    }

                    QrUniformGrid.Children.Add(pixel);
                }
            }
        }

        private void DatesOrServices_Changed(object sender, System.EventArgs e)
        {
            if (PickerStart == null || PickerEnd == null || _car == null) return;

            DateTime? start = PickerStart.SelectedDate;
            DateTime? end = PickerEnd.SelectedDate;

            if (start.HasValue && end.HasValue && end.Value >= start.Value)
            {
                int days = (end.Value - start.Value).Days + 1;
                TxtTotalDays.Text = $"{days} дн.";

                decimal pricePerDay = _car.CarCategories.PricePerDay;

                if (CheckChildSeat.IsChecked == true) pricePerDay += 500;
                if (CheckNavigator.IsChecked == true) pricePerDay += 300;
                if (CheckInsurance.IsChecked == true) pricePerDay += 1000;

                _totalCost = days * pricePerDay;
                TxtTotalCost.Text = string.Format("{0:C}", _totalCost);

                if (RadioSbp.IsChecked == true) GenerateQrCode();
            }
            else
            {
                TxtTotalDays.Text = "0 дней";
                TxtTotalCost.Text = "0,00 ₽";
                _totalCost = 0;
            }
        }

        private void TxtCardNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;

            string digits = new string(tb.Text.Where(char.IsDigit).ToArray());
            string formatted = "";

            for (int i = 0; i < digits.Length; i++)
            {
                if (i > 0 && i % 4 == 0) formatted += " ";
                formatted += digits[i];
            }

            tb.TextChanged -= TxtCardNumber_TextChanged;
            tb.Text = formatted;
            tb.SelectionStart = formatted.Length;
            tb.TextChanged += TxtCardNumber_TextChanged;
        }

        private void TxtCardExpiry_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;

            string digits = new string(tb.Text.Where(char.IsDigit).ToArray());
            string formatted = "";

            if (digits.Length > 0)
            {
                formatted += digits.Substring(0, Math.Min(2, digits.Length));
            }
            if (digits.Length > 2)
            {
                formatted += "/" + digits.Substring(2, Math.Min(2, digits.Length - 2));
            }

            tb.TextChanged -= TxtCardExpiry_TextChanged;
            tb.Text = formatted;
            tb.SelectionStart = formatted.Length;
            tb.TextChanged += TxtCardExpiry_TextChanged;
        }

        private async void BtnPayAndBook_Click(object sender, RoutedEventArgs e)
        {
            if (!PickerStart.SelectedDate.HasValue || !PickerEnd.SelectedDate.HasValue || PickerEnd.SelectedDate.Value < PickerStart.SelectedDate.Value)
            {
                CustomDialogWindow.ShowOk("Укажите корректные даты аренды!", CustomDialogWindow.DialogIcon.Warning);
                return;
            }

            int paymentType = 1;
            int paymentStatus = 1;
            int orderStatus = 1;

            if (RadioSbp.IsChecked == true)
            {
                paymentType = 3;
            }
            else if (RadioOffice.IsChecked == true)
            {
                paymentType = 2;
                paymentStatus = 2;
                orderStatus = 1;
            }

            if (RadioCard.IsChecked == true)
            {
                if (TxtCardNumber.Text.Length < 19 || TxtCardExpiry.Text.Length < 5 || TxtCardCvc.Password.Length < 3 || string.IsNullOrWhiteSpace(TxtCardHolder.Text))
                {
                    CustomDialogWindow.ShowOk("Заполните все реквизиты банковской карты!", CustomDialogWindow.DialogIcon.Warning);
                    return;
                }
            }

            PaymentPanel.Visibility = Visibility.Collapsed;
            LoadingPanel.Visibility = Visibility.Visible;

            if (RadioCard.IsChecked == true)
            {
                TxtLoadingStatus.Text = "Авторизация банковской карты...";
                await Task.Delay(2000);
            }
            else if (RadioSbp.IsChecked == true)
            {
                TxtLoadingStatus.Text = "Ожидание подтверждения транзакции СБП...";
                await Task.Delay(2500);
            }
            else
            {
                TxtLoadingStatus.Text = "Формирование бронирования в офисе...";
                await Task.Delay(500);
            }

            using (var transaction = Core.Context.Database.BeginTransaction())
            {
                try
                {
                    var clientCardExists = Core.Context.Clients.Any(c => c.ID_Client == _userId);
                    if (!clientCardExists)
                    {
                        var staffUser = Core.Context.Users.FirstOrDefault(u => u.ID_User == _userId);
                        if (staffUser != null)
                        {
                            Random rng = new Random();
                            string uniquePassport = $"{rng.Next(1000, 9999)} {rng.Next(100000, 999999)}";
                            string uniqueLicense = $"{rng.Next(1000, 9999)} {rng.Next(100000, 999999)}";

                            Clients autoCard = new Clients
                            {
                                ID_Client = staffUser.ID_User,
                                LastName = !string.IsNullOrEmpty(staffUser.LastName) ? staffUser.LastName : "Администрация",
                                FirstName = !string.IsNullOrEmpty(staffUser.FirstName) ? staffUser.FirstName : "Сотрудник",
                                MiddleName = staffUser.MiddleName ?? "-",
                                Phone = !string.IsNullOrEmpty(staffUser.Phone) ? staffUser.Phone : $"8999{rng.Next(1000000, 9999999)}",
                                PassportNumber = !string.IsNullOrEmpty(staffUser.PassportNumber) ? staffUser.PassportNumber : uniquePassport,
                                DriverLicense = !string.IsNullOrEmpty(staffUser.DriverLicense) ? staffUser.DriverLicense : uniqueLicense
                            };
                            Core.Context.Clients.Add(autoCard);
                            Core.Context.SaveChanges();
                        }
                    }

                    Orders newOrder = new Orders
                    {
                        FK_Client = _userId,
                        FK_Car = _carId,
                        StartDate = PickerStart.SelectedDate.Value,
                        EndDate = PickerEnd.SelectedDate.Value,
                        TotalCost = _totalCost,
                        FK_OrderStatus = orderStatus
                    };
                    Core.Context.Orders.Add(newOrder);
                    Core.Context.SaveChanges();
                    Payments newPayment = new Payments
                    {
                        FK_Order = newOrder.ID_Order,
                        Amount = _totalCost,
                        PaymentDate = DateTime.Now,
                        FK_PaymentStatus = paymentStatus,
                        FK_PaymentType = paymentType
                    };
                    Core.Context.Payments.Add(newPayment);
                    if (_car != null)
                    {
                        _car.FK_Status = 4;
                    }
                    Core.Context.SaveChanges();
                    transaction.Commit();
                    CustomDialogWindow.ShowOk("Транзакция успешно проведена. Автомобиль забронирован!", CustomDialogWindow.DialogIcon.Info);
                    NavigationService.Navigate(new MainPage(_userId));
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    LoadingPanel.Visibility = Visibility.Collapsed;
                    PaymentPanel.Visibility = Visibility.Visible;
                    string innerMessage = ex.InnerException?.InnerException?.Message ?? ex.Message;
                    CustomDialogWindow.ShowOk($"Критический сбой СУБД при фиксации заказа:\n\n{innerMessage}", CustomDialogWindow.DialogIcon.Error);
                }
            }
        }
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                NavigationService.Navigate(new MainPageNavigator());
            }
        }
    }
}