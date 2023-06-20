using System;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Inventory_Manager.Windows
{
    /// <summary>
    /// Interaction logic for Window_AddShippingMethod.xaml
    /// </summary>
    public partial class Window_AddShippingMethod : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName]String propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Objects.ShippingMethod shippingMethod;
        public Objects.ShippingMethod ShippingMethod
        {
            get
            {
                if (shippingMethod == null) {
                    shippingMethod = new Objects.ShippingMethod();
                }

                return shippingMethod;
            }
            set
            {
                shippingMethod = value;
                Button_Apply.Content = "Apply";
                OnPropertyChanged();
            }
        }

        public Window_AddShippingMethod()
        {
            InitializeComponent();
        }

        private void Button_Apply_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
