using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarRentalSystem
{
    public class FlatOrderReport
    {
        public int ID_Order { get; set; }
        public string ClientFio { get; set; }
        public string CarInfo { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalCost { get; set; }
        public string StatusName { get; set; }
        public decimal PricePerDay { get; set; }
    }
    internal class Core
    {
        
        public static CarRentalDBEntities Context = new CarRentalDBEntities();

        public static int CurrentUserId { get; set; }

        
    }


}
