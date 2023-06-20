using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Inventory_Manager.Objects
{
    public class eBayListingItem : Adericium.ObservableObject
    {
        private string title;
        public string Title { get { return title; } set { title = value; NotifyPropertyChanged(); } }
        
        private string description;
        public string Description { get { return description; } set { description = value; NotifyPropertyChanged(); } }

        private string upc;
        public string UPC { get { return upc; } set { upc = value; NotifyPropertyChanged(); } }

        private decimal price;
        public decimal Price { get { return price; } set { price = value; NotifyPropertyChanged(); } }

        private int quantity;
        public int Quantity { get { return quantity; } set { quantity = value; NotifyPropertyChanged(); } }

        private decimal weight;
        public decimal Weight { get { return weight; } set { weight = value; NotifyPropertyChanged(); } }

        private eBay.Service.Core.Soap.ListingTypeCodeType listingType;
        public eBay.Service.Core.Soap.ListingTypeCodeType ListingType { get { return listingType; } set { listingType = value; NotifyPropertyChanged(); } }

        private String duration;
        public String Duration { get { return duration; } set { duration = value; NotifyPropertyChanged(); } }

        private System.Collections.ObjectModel.ObservableCollection<PictureItem> pictures = new System.Collections.ObjectModel.ObservableCollection<PictureItem>();
        public System.Collections.ObjectModel.ObservableCollection<PictureItem> Pictures { get { return pictures; } set { pictures = value; NotifyPropertyChanged(); } }

        public static eBayListingItem GenerateListing(InventoryItem item) {
            eBayListingItem listing = new eBayListingItem();
            listing.Title = item.GenerateName();
            listing.Description = item.GenerateDescription();
            listing.UPC = item.UPC;
            listing.Price = (Decimal)item.Price;
            listing.Quantity = item.Stock;
            listing.Weight = item.Weight;


            var images = eBayListingUtil.FindImages(listing.UPC)?.ToList();

            if (images != null)
            {
                Adericium.ArrayExtensions.ArrayExtensions.SortNatural(images);

                foreach (String pic in images)
                {
                    if (listing.Pictures.Count == 12)
                        break;

                    PictureItem picItem = new PictureItem();
                    picItem.ImageSrc = new System.Windows.Media.Imaging.BitmapImage(new Uri(pic));
                    listing.Pictures.Add(picItem);
                }
            }

            return listing;
        }
    }
}
