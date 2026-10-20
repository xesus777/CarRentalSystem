using System;
using System.Linq;
using System.Data.Entity;

namespace CarRentalSystem.Services
{
    public static class OrderBackgroundService
    {
        public static void CheckAndUpdateExpiredOrders()
        {
            try
            {
                DateTime today = DateTime.Today;

                var expiredOrders = Core.Context.Orders
                    .Where(o => o.FK_OrderStatus == 2 && o.EndDate < today)
                    .ToList();

                if (expiredOrders.Count == 0) return;

                foreach (var order in expiredOrders)
                {
                    order.FK_OrderStatus = 3;

                    if (order.Cars != null)
                    {
                        order.Cars.FK_Status = 1;
                    }
                }

                Core.Context.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OrderBackgroundService Error]: {ex.Message}");
            }
        }
    }
}
