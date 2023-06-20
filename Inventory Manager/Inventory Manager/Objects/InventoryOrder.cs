using System;

namespace Inventory_Manager.Objects
{
    public class InventoryOrder : Adericium.ObservableObject
    {
        public static InventoryOrder FromItem(InventoryItem item)
        {
            return new InventoryOrder() {
                Id = item.Id,
                UPC = item.UPC,
                Label = item.ToString(),
                Stock = item.Stock,
                Box = item.Box,
                State = item.State,
                Note = item.Note,
                Extra = item.Extra,
                ListingId = item.ListingId
            };
        } 

        public UInt32? Id { get; set; } = null;

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

        private String label;
        public String Label
        {
            get
            {
                return label;
            }

            set
            {
                label = value;
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

        private Int32 quantityOrdered = 1;
        public Int32 QuantityOrdered
        {
            get
            {
                return quantityOrdered;
            }

            set
            {
                quantityOrdered = value;
                NotifyPropertyChanged();
            }
        }

        private String box;
        public String Box
        {
            get
            {
                if (String.IsNullOrWhiteSpace(box))
                    box = "NONE";

                return $"Box {box}";
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
    }
}