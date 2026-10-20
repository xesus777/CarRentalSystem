using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CarRentalSystem.Views.Windows;
using Microsoft.Win32;

namespace CarRentalSystem.Views.Pages
{
    public partial class AdminCarsPage : Page
    {
        private int? _selectedCarId = null;
        private string _tempSelectedPhotoPath = null;

        public AdminCarsPage()
        {
            InitializeComponent();
            ComboFormCategory.ItemsSource = Core.Context.CarCategories.ToList();
            ComboFormStatus.ItemsSource = Core.Context.CarStatuses.ToList();
            RefreshAdminCarsList();
        }

        private void RefreshAdminCarsList()
        {
            try
            {
                var query = Core.Context.Cars
                    .Include(c => c.CarCategories)
                    .Include(c => c.CarStatuses)
                    .AsQueryable();

                string search = TxtSearchCar.Text.Trim().ToLower();
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c => c.Brand.ToLower().Contains(search) || c.Model.ToLower().Contains(search) || c.LicensePlate.ToLower().Contains(search));
                }

                ListAdminCars.ItemsSource = query.ToList();
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка загрузки автопарка: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }

        private void TxtSearchCar_TextChanged(object sender, TextChangedEventArgs e) => RefreshAdminCarsList();

        private void ListAdminCars_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var car = ListAdminCars.SelectedItem as Cars;
            if (car == null) return;

            _selectedCarId = car.ID_Car;
            TxtBrand.Text = car.Brand;
            TxtModel.Text = car.Model;
            TxtYear.Text = car.ManufactureYear.ToString();
            TxtPlate.Text = car.LicensePlate;
            ComboFormCategory.SelectedItem = Core.Context.CarCategories.FirstOrDefault(c => c.ID_Category == car.FK_Category);
            ComboFormStatus.SelectedItem = Core.Context.CarStatuses.FirstOrDefault(s => s.ID_Status == car.FK_Status);
            TxtFormTitle.Text = "РЕДАКТИРОВАНИЕ АВТОМОБИЛЯ";

            _tempSelectedPhotoPath = car.CarImagePath;
            if (!string.IsNullOrEmpty(car.CarImagePath) && File.Exists(car.CarImagePath))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(car.CarImagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                ImgFormPhoto.Source = bitmap;
            }
            else
            {
                ImgFormPhoto.Source = null;
            }
        }

        private void BtnSelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Изображения (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png";

            if (openFileDialog.ShowDialog() == true)
            {
                _tempSelectedPhotoPath = openFileDialog.FileName;
                ImgFormPhoto.Source = new BitmapImage(new Uri(_tempSelectedPhotoPath, UriKind.Absolute));
            }
        }

        private string SavePhotoToProjectFolder(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath)) return null;

            string targetDirectory = @"C:\Users\Глеб\source\repos\CarRentalSystem\CarRentalSystem\res\images\cars";

            if (sourcePath.StartsWith(targetDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return sourcePath;
            }

            try
            {
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                string originalFileName = Path.GetFileName(sourcePath);
                string destinationPath = Path.Combine(targetDirectory, originalFileName);

                File.Copy(sourcePath, destinationPath, true);
                return destinationPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения фото: {ex.Message}");
                return null;
            }
        }

        private void BtnAddCar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtBrand.Text) || string.IsNullOrEmpty(TxtModel.Text) ||
                string.IsNullOrEmpty(TxtYear.Text) || string.IsNullOrEmpty(TxtPlate.Text) ||
                ComboFormCategory.SelectedItem == null || ComboFormStatus.SelectedItem == null)
            {
                CustomDialogWindow.ShowOk("Заполните все характеристики для добавления машины!", CustomDialogWindow.DialogIcon.Warning);
                return;
            }

            if (!int.TryParse(TxtYear.Text, out int year) || year < 2015 || year > DateTime.Now.Year)
            {
                CustomDialogWindow.ShowOk("Некорректный год выпуска авто (допустимо с 2015 по текущий)!", CustomDialogWindow.DialogIcon.Warning);
                return;
            }

            try
            {
                string plate = TxtPlate.Text.Trim().ToUpper();

                if (Core.Context.Cars.Any(c => c.LicensePlate == plate))
                {
                    CustomDialogWindow.ShowOk("Автомобиль с таким госномером уже зарегистрирован в базе данных!", CustomDialogWindow.DialogIcon.Warning);
                    return;
                }

                string finalSavedPath = SavePhotoToProjectFolder(_tempSelectedPhotoPath);

                Cars newCar = new Cars
                {
                    Brand = TxtBrand.Text.Trim(),
                    Model = TxtModel.Text.Trim(),
                    ManufactureYear = year,
                    LicensePlate = plate,
                    FK_Category = ((CarCategories)ComboFormCategory.SelectedItem).ID_Category,
                    FK_Status = ((CarStatuses)ComboFormStatus.SelectedItem).ID_Status,
                    CarImagePath = finalSavedPath
                };

                Core.Context.Cars.Add(newCar);
                Core.Context.SaveChanges();

                CustomDialogWindow.ShowOk($"Автомобиль {newCar.Brand} {newCar.Model} успешно добавлен в базу данных!", CustomDialogWindow.DialogIcon.Info);
                BtnClearForm_Click(null, null);
                RefreshAdminCarsList();
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка при добавлении автомобиля: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }

        private void BtnEditCar_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCarId == null)
            {
                CustomDialogWindow.ShowOk("Выберите автомобиль из списка слева, чтобы изменить его данные!", CustomDialogWindow.DialogIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(TxtBrand.Text) || string.IsNullOrEmpty(TxtModel.Text) ||
                string.IsNullOrEmpty(TxtYear.Text) || string.IsNullOrEmpty(TxtPlate.Text) ||
                ComboFormCategory.SelectedItem == null || ComboFormStatus.SelectedItem == null)
            {
                CustomDialogWindow.ShowOk("Характеристики машины не могут быть пустыми!", CustomDialogWindow.DialogIcon.Warning);
                return;
            }

            if (!int.TryParse(TxtYear.Text, out int year) || year < 2015 || year > DateTime.Now.Year)
            {
                CustomDialogWindow.ShowOk("Некорректный год выпуска!", CustomDialogWindow.DialogIcon.Warning);
                return;
            }

            try
            {
                var car = Core.Context.Cars.FirstOrDefault(c => c.ID_Car == _selectedCarId.Value);
                if (car != null)
                {
                    string finalSavedPath = SavePhotoToProjectFolder(_tempSelectedPhotoPath);

                    car.Brand = TxtBrand.Text.Trim();
                    car.Model = TxtModel.Text.Trim();
                    car.ManufactureYear = year;
                    car.LicensePlate = TxtPlate.Text.Trim().ToUpper();
                    car.FK_Category = ((CarCategories)ComboFormCategory.SelectedItem).ID_Category;
                    car.FK_Status = ((CarStatuses)ComboFormStatus.SelectedItem).ID_Status;
                    car.CarImagePath = finalSavedPath;

                    Core.Context.SaveChanges();
                    CustomDialogWindow.ShowOk("Изменения успешно сохранены в базе данных!", CustomDialogWindow.DialogIcon.Info);
                    BtnClearForm_Click(null, null);
                    RefreshAdminCarsList();
                }
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка при обновлении данных: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }

        private void BtnDeleteCar_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCarId == null)
            {
                CustomDialogWindow.ShowOk("Выберите автомобиль из списка для снятия с линии!", CustomDialogWindow.DialogIcon.Warning);
                return;
            }

            var car = Core.Context.Cars.FirstOrDefault(c => c.ID_Car == _selectedCarId.Value);
            if (car != null)
            {
                if (car.FK_Status == 2)
                {
                    CustomDialogWindow.ShowOk("Невозможно отправить автомобиль на ТО, так как транспортное средство в данный момент находится в активной аренде у клиента!", CustomDialogWindow.DialogIcon.Error);
                    return;
                }
                if (CustomDialogWindow.ShowYesNo($"Вы действительно хотите снять автомобиль {car.Brand} {car.Model} с линии каталога и отправить его на ТО? История прошлых заказов и оплат при этом полностью сохранится.", CustomDialogWindow.DialogIcon.Question))
                {
                    try
                    {
                        car.FK_Status = 3;
                        Core.Context.SaveChanges();
                        CustomDialogWindow.ShowOk("Автомобиль успешно снят с линии каталога и переведен в статус 'На ТО'.", CustomDialogWindow.DialogIcon.Info);
                        BtnClearForm_Click(null, null);
                        RefreshAdminCarsList();
                    }
                    catch (Exception ex)
                    {
                        CustomDialogWindow.ShowOk($"Ошибка при изменении состояния автомобиля: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
                    }
                }
            }
        }
        private void BtnClearForm_Click(object sender, RoutedEventArgs e)
        {
            _selectedCarId = null;
            _tempSelectedPhotoPath = null;
            TxtBrand.Clear();
            TxtModel.Clear();
            TxtYear.Clear();
            TxtPlate.Clear();
            ComboFormCategory.SelectedIndex = -1;
            ComboFormStatus.SelectedIndex = -1;
            ImgFormPhoto.Source = null;
            TxtFormTitle.Text = "ДОБАВЛЕНИЕ / ИЗМЕНЕНИЕ АВТО";
            ListAdminCars.SelectedIndex = -1;
        }
    }
}