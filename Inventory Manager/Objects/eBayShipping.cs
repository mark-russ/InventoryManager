using System;
using eBay.Service.Core.Soap;

namespace Inventory_Manager.Objects
{
    public class eBayShipping
    {
        public String ID { get; set; }
        public String CategoryID { get; set; }
        public String CategoryName { get; set; }
        public String Name { get; set; }
        public String ETA { get; set; }

        public eBayShipping(ShippingServiceDetailsType obj, ShippingCategoryDetailsType cat)
        {
            ID = obj.ShippingService;
            CategoryID = obj.ShippingCategory;
            CategoryName = cat.Description;
            Name = obj.Description;

            if (obj.ShippingTimeMaxSpecified && obj.ShippingTimeMinSpecified)
            {
                if (obj.ShippingTimeMin == obj.ShippingTimeMax)
                    ETA = $"{obj.ShippingTimeMin} day(s)";
                else
                    ETA = $"{obj.ShippingTimeMin} - {obj.ShippingTimeMax} day(s)";
            }
            else if (obj.ShippingTimeMinSpecified)
            {
                ETA = $"{obj.ShippingTimeMin} day(s)";
            }
            else
            {
                ETA = String.Empty;
            }
        }
    }
}
