using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Inventory_Manager.Objects
{
    public class Profile : Adericium.ObservableObject
    {
        public void UseDefaults()
        {
            Fees = new ObservableCollection<Fee>() {
                new Fee() { Name = "Ebay Fee",                  Amount = 9.15M,     Type = FeeType.Percentage },
                new Fee() { Name = "PayPal Transaction Fee",    Amount =  2.9M,     Type = FeeType.Percentage },
                new Fee() { Name = "PayPal Processing Fee",     Amount = 0.30M,     Type = FeeType.Fixed }
            };
            ShippingMethods = new ObservableCollection<ShippingMethod>() {
                new ShippingMethod() { Name = "FRE",   Cost =  6.55M },
                new ShippingMethod() { Name = "FRS",   Cost =  7.05M },
                new ShippingMethod() { Name = "FRM",   Cost = 12.85M }
            };
            UseDefaultCategory();
        }

        public void UseDefaultCategory()
        {
            ItemCategories = new ObservableCollection<ItemCategory>()
            {
                new ItemCategory() {
                    Label = "General",
                    DescriptionTemplate = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("PGRpdiBzdHlsZT0iZm9udC1mYW1pbHk6IEFyaWFsLCBzYW5zLXNlcmlmOyBmb250LXNpemU6IDE0cHQ7IHRleHQtYWxpZ246IGNlbnRlcjsgbWFyZ2luLWJvdHRvbTogMXJlbSI+CiAgICA8ZGl2PntUSVRMRX08L2Rpdj4KICAgIDxkaXY+e0VYVFJBfTwvZGl2PgogICAgPGRpdj5VUEM6IHtVUEN9PC9kaXY+CiAgICA8ZGl2PlByaWNlOiB7UFJJQ0V9PC9kaXY+CjwvZGl2Pgo8ZGl2IHN0eWxlPSJmb250LWZhbWlseTogQXJpYWwsIHNhbnMtc2VyaWY7IGZvbnQtc2l6ZTogMTRwdCI+CiAgICA8ZGl2PgogICAgICAgIDxzcGFuIHN0eWxlPSJjb2xvcjogIzAwNzBDMCI+UHJvZHVjdCBJbmZvcm1hdGlvbjo8L3NwYW4+CiAgICAgICAgPHNwYW4+SXRlbSBpcyBzZWFsZWQuIFRoZSBzZWFsIGhhcyBiZWVuIGRvdWJsZSBjaGVja2VkIHRvIGVuc3VyZSBpdCBpcyBzdGlsbCBpbnRhY3QuPC9zcGFuPgogICAgPC9kaXY+CiAgICA8ZGl2PgogICAgICAgIDxzcGFuIHN0eWxlPSJjb2xvcjogIzAwNzBDMCI+UGF5bWVudDo8L3NwYW4+CiAgICAgICAgPHNwYW4+SSBjdXJyZW50bHkgb25seSBhY2NlcHQgcGF5bWVudCB2aWEgUGF5UGFsLiBJdCZhcG9zO3Mgc2FmZXIgZm9yIG1lIGFzIGEgc2VsbGVyIGFuZCBmb3IgeW91IGFzIGEgYnV5ZXIuPC9zcGFuPgogICAgPC9kaXY+CiAgICA8ZGl2PgogICAgICAgIDxzcGFuIHN0eWxlPSJjb2xvcjogIzAwNzBDMCI+U2FsZXMgVGF4Ojwvc3Bhbj4KICAgICAgICA8c3Bhbj5TYWxlcyB0YXggaGFzIGFscmVhZHkgYmVlbiBwYWlkIGJ5IG15c2VsZi4gU28sIHRoZXJlIHdpbGwgYmUgbm8gc2FsZXMgdGF4IGNoYXJnZWQgdG8gdGhlIGJ1eWVyLjwvc3Bhbj4KICAgIDwvZGl2PgogICAgPGRpdj4KICAgICAgICA8c3BhbiBzdHlsZT0iY29sb3I6ICMwMDcwQzAiPlJldHVybnM6PC9zcGFuPgogICAgICAgIDxzcGFuPkkgZG8gb2ZmZXIgcmV0dXJucy4gSSBhc2sgdGhhdCB5b3UgY29udGFjdCBtZSB2aWEgZUJheSBhcyBzb29uIGFzIHBvc3NpYmxlIHNvIHdlIGNhbiBnZXQgdGhlIGlzc3VlIHJlc29sdmVkLiBUaGUgcmV0dXJuIHByb2Nlc3MgaXMgZGlmZmVyZW50IGJhc2VkIG9mZiBvZiBkaWZmZXJlbnQgc2l0dWF0aW9uczo8L3NwYW4+CiAgICA8L2Rpdj4KCiAgICA8dWw+CiAgICAgICAgPGxpPgogICAgICAgICAgICA8ZGl2IHN0eWxlPSJjb2xvcjogI0ZGMDAwMCI+V3JvbmcgSXRlbSBSZWNlaXZlZDwvZGl2PgogICAgICAgICAgICA8dWw+CiAgICAgICAgICAgICAgICA8bGk+QnV5ZXIgcGF5cyBub3RoaW5nLiBJIHdpbGwgcGF5IGFsbCB0aGUgY29zdHMgcmVxdWlyZWQgdG8gZ2V0IHlvdSB0aGUgY29ycmVjdCBpdGVtLjwvbGk+CiAgICAgICAgICAgICAgICA8bGk+T25jZSBJIHJlY2VpdmUgdGhlIHdyb25nIGl0ZW0sIGFuZCB2ZXJpZnkgaXQmYXBvcztzIHN0aWxsIGluIGl0cyBvcmlnaW5hbCBjb25kaXRpb24sIEkgd2lsbCBzZW5kIHRoZSBjb3JyZWN0IGl0ZW0gdXNpbmcgdGhlIHNhbWUgc2hpcHBpbmcgbWV0aG9kLjwvbGk+CiAgICAgICAgICAgICAgICA8bGk+SW4gdGhpcyBzaXR1YXRpb24sIEkgd2lsbCBhbHNvIGFsbG93IHlvdSwgdGhlIGJ1eWVyLCB0byBwaWNrIGFuIGl0ZW0gb2YgZXF1YWwgb3IgbGVzc2VyIHZhbHVlLCBtYXggb2YgJDE1LjAwLCBhbmQgd2lsbCBzZW5kIGl0IGFsb25nIHdpdGggdGhlIGNvcnJlY3QgaXRlbSBhcyBhIHdheSB0byBjb21wZW5zYXRlIHlvdSBmb3IgdGhlIHRyb3VibGUuPC9saT4KICAgICAgICAgICAgPC91bD4KICAgICAgICA8L2xpPgogICAgICAgIDxsaT4KICAgICAgICAgICAgPHNwYW4gc3R5bGU9ImNvbG9yOiAjRkYwMDAwIj5CdXllciBDaGFuZ2VzIE1pbmQ8L3NwYW4+CiAgICAgICAgICAgIDx1bD4KICAgICAgICAgICAgICAgIDxsaT5JdGVtIG11c3QgYmUgaW4gdGhlIHNhbWUgY29uZGl0aW9uIGl0IHdhcyByZWNlaXZlZCBpbi48L2xpPgogICAgICAgICAgICAgICAgPGxpPkJ1eWVyIHBheXMgcmV0dXJuIHNoaXBwaW5nIGFuZCBhICQyLjAwIGZlZSB3aWxsIGJlIGFwcGxpZWQuPC9saT4KICAgICAgICAgICAgICAgIDxsaT5PbmNlIHRoZSBpdGVtIGlzIHJlY2VpdmVkIGJ5IG1lLCBJJmFwb3M7bGwgaXNzdWUgdGhlIHJlZnVuZCwgbWludXMgdGhlIGZlZS48L2xpPgogICAgICAgICAgICA8L3VsPgogICAgICAgIDwvbGk+CiAgICAgICAgPGxpPgogICAgICAgICAgICA8c3BhbiBzdHlsZT0iY29sb3I6ICNGRjAwMDAiPkl0ZW0gRGVmZWN0aXZlPC9zcGFuPgogICAgICAgICAgICA8dWw+CiAgICAgICAgICAgICAgICA8bGk+VGhlc2Ugc2l0dWF0aW9ucyB3aWxsIGJlIGhlbGQgb24gYSBjYXNlIGJ5IGNhc2UgYmFzaXMuPC9saT4KICAgICAgICAgICAgPC91bD4KICAgICAgICA8L2xpPgogICAgPC91bD4KCiAgICA8ZGl2PgogICAgICAgIDxzcGFuIHN0eWxlPSJjb2xvcjogIzAwNzBDMCI+U2hpcHBpbmc6PC9zcGFuPgogICAgICAgIDxzcGFuPlNoaXBwaW5nIGlzIGFsd2F5cyBmcmVlLiBJIHNoaXAgdG8gYWxsIDUwIFN0YXRlcyBhbmQgdGhlIERpc3RyaWN0IG9mIENvbHVtYmlhLiBJIGRvIDx1Pk5PVDwvdT4gc2hpcCB0byB0aGUgVVMgUHJvdGVjdG9yYXRlcywgQVBPL0ZQTyBvciBpbnRlcm5hdGlvbmFsbHkuIEFsbCBpdGVtcyB0aGF0IHdlaWdoIGxlc3MgdGhhbiBvbmUgKDEpIHBvdW5kIHdpbGwgYmUgc2hpcHBlZCB2aWEgRmlyc3QgQ2xhc3MgUGFja2FnZSB3aXRoIHRoZSBVU1BTLiBJdGVtcyB0aGF0IHdlaWdoIG1vcmUgdGhhbiBvbmUgKDEpIHBvdW5kIHdpbGwgYmUgc2hpcHBlZCB2aWEgUHJpb3JpdHkgTWFpbCBGbGF0IFJhdGUgYnkgdGhlIFVTUFMuPC9zcGFuPgogICAgPC9kaXY+CiAgICA8ZGl2PgogICAgICAgIDxzcGFuIHN0eWxlPSJjb2xvcjogIzAwNzBDMCI+SGFuZGxpbmc6PC9zcGFuPgogICAgICAgIDxzcGFuPkl0ZW1zIG9yZGVyZWQgYmVmb3JlIDNwbSBFU1Qgd2lsbCBiZSBzaGlwcGVkIHRoZSBzYW1lIGRheSA5OSUgb2YgdGhlIHRpbWUuIE9uIHRoZSByYXJlIG9jY2FzaW9uIHRoYXQgdGhpcyBjYW4mYXBvczt0IGJlIGFjaGlldmVkLCB0aGUgaXRlbSB3aWxsIGJlIHNoaXBwZWQgdGhlIG5leHQgYnVzaW5lc3MgZGF5LCBndWFyYW50ZWVkLjwvc3Bhbj4KICAgIDwvZGl2Pgo8L2Rpdj4=")),
                    NamingMode = NamingModes.Verbatim,
                    ExtraBehavior = ExtraBehaviors.Inactive,
                    DeleteVariations = false,
                    SoldOutClearExtra = false
                }
            };
        }

        public Profile Clone()
        {
            return (Profile)MemberwiseClone();
        }

        private String label;
        public String Label
        {
            get { return label; }
            set { label = value; NotifyPropertyChanged(); }
        }

        private Int32 maxSearchResults = 10;
        public Int32 MaxSearchResults
        {
            get { return maxSearchResults; }
            set { maxSearchResults = value; NotifyPropertyChanged(); }
        }

        public Decimal PackingCost { get; set; } = 0.53m;
        public Decimal PackingWeight { get; set; } = 0.6m;
        public Boolean KeepBoxNumber { get; set; } = true;
        public String DatabaseHostname { get; set; } = "127.0.0.1";
        public Int32 DatabasePort { get; set; } = 3306;
        public String DatabaseName { get; set; }
        public String DatabaseUsername { get;
            set; }

        private Int32 databaseTimeout = 10;
        public Int32 DatabaseTimeout
        {
            get { return databaseTimeout; }
            set { databaseTimeout = value; NotifyPropertyChanged(); }
        }

        public String DatabasePassword { get; set; } = String.Empty;

        private String databaseSSLMode = "Preferred";
        public String DatabaseSSLMode
        {
            get { return databaseSSLMode; }
            set { databaseSSLMode = value; NotifyPropertyChanged(); }
        }
        
        private ObservableCollection<Fee> fees;
        public ObservableCollection<Fee> Fees
        {
            get { return fees; }
            set { fees = value; NotifyPropertyChanged(); }
        }

        private ObservableCollection<ShippingMethod> shippingMethods;
        public ObservableCollection<ShippingMethod> ShippingMethods
        {
            get { return shippingMethods; }
            set { shippingMethods = value; NotifyPropertyChanged(); }
        }

        private ObservableCollection<ItemCategory> itemCategories;
        public ObservableCollection<ItemCategory> ItemCategories
        {
            get { return itemCategories; }
            set { itemCategories = value; NotifyPropertyChanged(); }
        }
        
        private Int32 currentItemCategoryIndex;
        public Int32 CurrentItemCategoryIndex
        {
            get
            {
                return currentItemCategoryIndex;
            }
            set
            {
                currentItemCategoryIndex = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("CurrentItemCategory");
            }
        }

        public ItemCategory CurrentItemCategory
        {
            get { return ItemCategories[currentItemCategoryIndex]; }
        }

        // QUICKLIST
        public Boolean EnableQuicklist { get; set; } = false;
        public String PayPalEmail { get; set; }
        public eBay.Service.Core.Soap.SiteCodeType SiteCode { get; set; } = eBay.Service.Core.Soap.SiteCodeType.US;
        public eBay.Service.Core.Soap.BuyerPaymentMethodCodeType PaymentMethod { get; set; } = eBay.Service.Core.Soap.BuyerPaymentMethodCodeType.PayPal;
        public eBay.Service.Core.Soap.CurrencyCodeType Currency { get; set; } = eBay.Service.Core.Soap.CurrencyCodeType.USD;

        public String ShippingService { get;
            set;
        } = "USPSFirstClass";
        public Int32 ListingStyle { get; set; }
        public Boolean BuyItNow { get; set; }
        public Boolean AllowOffers { get; set; }
        public Boolean RequireImmediatePayment { get; set; }

        private String imageFolder;
        public String ImageFolder
        {
            get { return imageFolder; }
            set { imageFolder = value; NotifyPropertyChanged(); }
        }
    }
}
