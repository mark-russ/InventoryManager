using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Inventory_Manager.Objects
{
    public class ShippingMethod : Adericium.ObservableObject
    {
        String name;
        public String Name
        {
            get { return name; }
            set { name = value; NotifyPropertyChanged(); }
        }

        Decimal cost;
        public Decimal Cost
        {
            get { return cost; }
            set { cost = value; NotifyPropertyChanged(); NotifyPropertyChanged("CostString"); }
        }
        
        public String CostString
        {
            get { return String.Format("{0:C}", Cost);  }
        }

        public ShippingMethod Clone()
        {
            return (ShippingMethod)base.MemberwiseClone();
        }
    }
}
