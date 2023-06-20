using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Inventory_Manager.Windows
{
    /// <summary>
    /// Interaction logic for Window_AddFee.xaml
    /// </summary>
    public partial class Window_AddFee : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName]String propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private Objects.Fee fee;
        public Objects.Fee Fee
        {
            get
            {
                if (fee == null) {
                    fee = new Objects.Fee();
                }

                return fee;
            }
            set
            {
                fee = value;
                Button_Apply.Content = "Apply";
                OnPropertyChanged();
            }
        }

        public Window_AddFee()
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
