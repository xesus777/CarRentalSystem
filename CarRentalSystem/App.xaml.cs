using CarRentalSystem.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CarRentalSystem
{
    
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            CultureInfo russianCulture = new CultureInfo("ru-RU");
            Thread.CurrentThread.CurrentCulture = russianCulture;
            Thread.CurrentThread.CurrentUICulture = russianCulture;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage("ru-RU"))
            );

            OrderBackgroundService.CheckAndUpdateExpiredOrders();
        }
    }
}
