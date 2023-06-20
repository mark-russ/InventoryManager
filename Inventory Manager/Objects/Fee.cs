using System;

namespace Inventory_Manager.Objects
{
    public enum FeeType
    {
        Fixed,
        Percentage
    }

    public class Fee : Adericium.ObservableObject
    {
        String name;
        public String Name
        {
            get { return name; }
            set { name = value; NotifyPropertyChanged(); }
        }

        Decimal amount;
        public Decimal Amount
        {
            get { return amount; }
            set { amount = value; NotifyPropertyChanged(); }
        }
        
        public String AmountString
        {
            get
            {
                if (Type == FeeType.Percentage)
                {
                    return String.Format("{0:N2}%", Amount);
                }
                else
                {
                    return String.Format("{0:C}", Amount);
                }
            }
        }

        FeeType type;
        public FeeType Type
        {
            get { return type; }
            set { type = value; NotifyPropertyChanged(); }
        }

        public Fee Clone()
        {
            return (Fee)base.MemberwiseClone();
        }
    }
}
