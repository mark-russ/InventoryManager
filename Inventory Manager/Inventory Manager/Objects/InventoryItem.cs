using System;
using Adericium;
using Adericium.ArrayExtensions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Inventory_Manager.Objects
{
    public class InventoryItem : ObservableObject
    {
        public UInt32? Id { get; set; } = null;
        public Boolean IsHistory { get; set; } = false;

        private String upc;
        public String UPC
        {
            get
            {
                return upc;
            }

            set
            {
                upc = value;
                NotifyPropertyChanged();
            }
        }

        private String name;
        public String Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
                NotifyPropertyChanged();
            }
        }

        private Decimal? price = null;
        public Decimal? Price
        {
            get
            {
                return price;
            }

            set
            {
                price = value;
                NotifyPropertyChanged();
            }
        }

        private Decimal weight;
        public Decimal Weight
        {
            get
            {
                return weight;
            }

            set
            {
                weight = value;
                NotifyPropertyChanged();
            }
        }

        private Int32 stock;
        public Int32 Stock
        {
            get
            {
                return stock;
            }

            set
            {
                stock = value;
                NotifyPropertyChanged();
            }
        }

        private String box;
        public String Box
        {
            get
            {
                return box;
            }

            set
            {
                box = value;
                NotifyPropertyChanged();
            }
        }

        private ItemStates state = ItemStates.None;
        public ItemStates State
        {
            get
            {
                return state;
            }

            set
            {
                state = value;
                NotifyPropertyChanged();
                IsSold = (state == ItemStates.Sold);
            }
        }

        private String note;
        public String Note
        {
            get
            {
                return note;
            }

            set
            {
                note = value;
                NotifyPropertyChanged();
            }
        }

        private String extra;
        public String Extra
        {
            get
            {
                return extra;
            }

            set
            {
                extra = value;
                NotifyPropertyChanged();
            }
        }

        private String listingId;
        public String ListingId
        {
            get
            {
                return listingId;
            }

            set
            {
                listingId = value;
                NotifyPropertyChanged();
            }
        }

        private Boolean isSold;
        public Boolean IsSold
        {
            get {
                return isSold;
            }

            private set {
                if (value == true)
                    Stock = 0;

                isSold = value;
                NotifyPropertyChanged();
            }
        }

        public override String ToString()
        {
            if (String.IsNullOrWhiteSpace(Name) != true)
            {
                return Name;
            }
            else if (String.IsNullOrWhiteSpace(UPC) != true)
            {
                return UPC;
            }
            else
            {
                return "Item with database ID: " + Id;
            }
        }

        private String ExpandDateString(String shorthand)
        {
            String yearPart = shorthand.Substring(shorthand.IndexOf('/') + 1);

            if (yearPart.Length == 2)
                shorthand = shorthand.Substring(0, shorthand.IndexOf('/')) + $"/20{yearPart}";
            else if (yearPart.Length != 4)
                return null;

            return shorthand;
        }

        private DateTime[] StringToDateRange(String dateRange)
        {
            if (String.IsNullOrWhiteSpace(dateRange))
                return null;

            var dtRange = new DateTime[2];

            if (dateRange.Contains("-"))
            {
                String[] parts = dateRange.Split('-');

                if (!DateTime.TryParse(ExpandDateString(parts[0]), out dtRange[0]) || !DateTime.TryParse(ExpandDateString(parts[parts.Length - 1]), out dtRange[1]))
                    return null;
            }
            else
            {
                if (!DateTime.TryParse(ExpandDateString(dateRange), out dtRange[0]))
                    return null;

                dtRange[1] = dtRange[0];
            }

            return dtRange;
        }

        private String StringDateToDateTime(String date)
        {
            return DateTime.Parse(date).ToString("M/yy");
        }

        public String GenerateName()
        {
            if (String.IsNullOrWhiteSpace(Name))
            {
                return null;
            }

            String extraInfo = String.Empty;
            List<InventoryItem> items = new List<InventoryItem>() { this };
            if (App.Settings.Profile.CurrentItemCategory.GenerateDescriptionVariants)
            {
                var results = Database.SearchForVariants(this, true);

                if (results != null)
                    items.AddRange(results);
            }

            if (App.Settings.Profile.CurrentItemCategory.ExtraBehavior == ExtraBehaviors.Dates)
            {
                DateTime?[] dateRange = new DateTime?[] { null, null };

                foreach (var item in items)
                {
                    if (item != this && (item.State != ItemStates.None || item.State == ItemStates.Sold))
                        continue;

                    if (String.IsNullOrWhiteSpace(item.Extra) == false)
                    {
                        DateTime[] itemDate = StringToDateRange(item.Extra);

                        if (itemDate == null)
                        {
                            DialogBox.Show((String)App.Current.Resources["INVALID DATE GEN"], null, Win32.Imaging.SystemIcons.Warning);
                            continue;
                        }

                        if (dateRange[0] == null || itemDate[0] < dateRange[0])
                            dateRange[0] = itemDate[0];

                        if (dateRange[1] == null || itemDate[1] > dateRange[1])
                            dateRange[1] = itemDate[1];
                    }
                }

                if (dateRange[0] != null)
                {
                    if (dateRange[0] != dateRange[1])
                        extraInfo = ($"{((DateTime)dateRange[0]).ToString("M/yy")}-{((DateTime)dateRange[1]).ToString("M/yy")}");
                    else
                        extraInfo = ((DateTime)dateRange[0]).ToString("M/yy");
                }
            }
            else if (App.Settings.Profile.CurrentItemCategory.ExtraBehavior == ExtraBehaviors.Inactive)
            {
                List<String> extraFields = new List<String>();

                foreach (var item in items)
                {
                    if (item != this && (item.State != ItemStates.None || String.IsNullOrWhiteSpace(item.Extra)))
                        continue;

                    extraFields.Add(item.Extra);
                }

                if (extraFields.Count > 0)
                {
                    extraInfo = extraFields.ToCSV().Alphabetize();
                }
            }

            if (extraInfo.Trim().Equals(String.Empty) == false && App.Settings.Profile.CurrentItemCategory.NamingMode != NamingModes.Verbatim)
            {
                if (App.Settings.Profile.CurrentItemCategory.ExtraBehavior == ExtraBehaviors.Inactive)
                {
                    if (App.Settings.Profile.CurrentItemCategory.NamingMode == NamingModes.Prefix)
                        extraInfo = $"{extraInfo} {Name}";
                    else if (App.Settings.Profile.CurrentItemCategory.NamingMode == NamingModes.Suffix)
                        extraInfo = $"{Name} {extraInfo}";
                }
                else
                {
                    if (App.Settings.Profile.CurrentItemCategory.NamingMode == NamingModes.Prefix)
                        extraInfo = $"EXP {extraInfo} {Name}";
                    else if (App.Settings.Profile.CurrentItemCategory.NamingMode == NamingModes.Suffix)
                        extraInfo = $"{Name} EXP {extraInfo}";
                }
            }
            else
            {
                extraInfo = Name;
            }

            extraInfo = extraInfo.Trim();

            if (extraInfo.Length > 80)
                DialogBox.Show((String)App.Current.Resources["NAME TOO LONG"], null, Win32.Imaging.SystemIcons.Error, "Ok");

            return extraInfo;
        }

        public String GenerateDescription()
        {
            if (String.IsNullOrWhiteSpace(Name)) {
                return null;
            }

            String extraInfo = String.Empty;
            List<InventoryItem> items = new List<InventoryItem>() { this };
            if (App.Settings.Profile.CurrentItemCategory.GenerateDescriptionVariants)
            {
                var results = Database.SearchForVariants(this, true);

                if (results != null)
                    items.AddRange(results);
            }

            if (App.Settings.Profile.CurrentItemCategory.ExtraBehavior == ExtraBehaviors.Dates)
            {
                DateTime?[] dateRange = new DateTime?[] { null, null };

                foreach (var item in items)
                {
                    if (item != this && (item.State != ItemStates.None || item.State == ItemStates.Sold))
                        continue;

                    if (String.IsNullOrWhiteSpace(item.Extra) == false)
                    {
                        DateTime[] itemDate = StringToDateRange(item.Extra);

                        if (itemDate == null)
                        {
                            DialogBox.Show((String)App.Current.Resources["INVALID DATE GEN"], null, Win32.Imaging.SystemIcons.Warning);
                            continue;
                        }

                        if (dateRange[0] == null || itemDate[0] < dateRange[0])
                            dateRange[0] = itemDate[0];

                        if (dateRange[1] == null || itemDate[1] > dateRange[1])
                            dateRange[1] = itemDate[1];
                    }
                }

                if (dateRange[0] != null)
                {
                    if (dateRange[0] != dateRange[1])
                        extraInfo = ($"{((DateTime)dateRange[0]).ToString("M/yy")}-{((DateTime)dateRange[1]).ToString("M/yy")}");
                    else
                        extraInfo = ((DateTime)dateRange[0]).ToString("M/yy");
                }
            }
            else if (App.Settings.Profile.CurrentItemCategory.ExtraBehavior == ExtraBehaviors.Inactive)
            {
                List<String> extraFields = new List<String>();

                foreach (var item in items)
                {
                    if (item.State != ItemStates.None || String.IsNullOrWhiteSpace(item.Extra))
                        continue;

                    extraFields.Add(item.Extra);
                }

                if (extraFields.Count > 0)
                {
                    extraInfo = extraFields.ToCSV().Alphabetize();
                }
            }

            var priceRange = new Decimal?[2];

            foreach (var item in items)
            {
                if (item != this && (item.State != ItemStates.None || String.IsNullOrWhiteSpace(item.Extra)))
                    continue;

                if (priceRange[0] == null || item.Price < priceRange[0])
                    priceRange[0] = item.Price;

                if (priceRange[1] == null || item.Price > priceRange[1])
                    priceRange[1] = item.Price;
            }

            extraInfo = extraInfo.Trim();

            String priceInfo;
            if (priceRange[0] == priceRange[1] || priceRange[0] != null && priceRange[1] == null)
                priceInfo = String.Format("{0:C}", priceRange[0]);
            else if (priceRange[1] != null)
                priceInfo = String.Format("{0:C} - {1:C}", priceRange[0], priceRange[1]);
            else
                priceInfo = "Varies";

            String description = App.Settings.Profile.CurrentItemCategory.DescriptionTemplate
                .Replace("{TITLE}", Name)
                .Replace("{UPC}", UPC)
                .Replace("{EXTRA}", extraInfo)
                .Replace("{PRICE}", priceInfo);

            return description.Trim();
        }
    }

    public enum ItemStates
    {
        [Description("None")]
        None,
        [Description("Listed")]
        Listed,
        [Description("Sold Out")]
        Sold,
        [Description("Bulk Bin")]
        NoProfit,
        [Description("Unsellable")]
        Unsellable,
    }
}