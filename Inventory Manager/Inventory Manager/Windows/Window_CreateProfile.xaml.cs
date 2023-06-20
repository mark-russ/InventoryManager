using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Inventory_Manager.Windows
{
    /// <summary>
    /// Interaction logic for Window_CreateProfile.xaml
    /// </summary>
    public partial class Window_CreateProfile : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] String propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Objects.Settings settings;
        public Objects.Settings Settings {
            get
            {
                return settings;
            }
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }
        public Objects.Profile Profile { get; set; }

        public Window_CreateProfile()
        {
            InitializeComponent();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_Create_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBox_CopySettings.IsChecked == true) {
                Objects.Profile currentProfile = ((Objects.Profile)DropDown_CopyTarget.SelectedItem);
                Profile = currentProfile.Clone();

                Profile.Fees = new System.Collections.ObjectModel.ObservableCollection<Objects.Fee>();
                foreach (Objects.Fee fee in currentProfile.Fees)
                {
                    Profile.Fees.Add(fee.Clone());
                }

                Profile.ShippingMethods = new System.Collections.ObjectModel.ObservableCollection<Objects.ShippingMethod>();
                foreach (Objects.ShippingMethod shippingMethod in currentProfile.ShippingMethods)
                {
                    Profile.ShippingMethods.Add(shippingMethod.Clone());
                }
            }
            else {
                Profile = new Objects.Profile();
            }

            Profile.Label = Box_ProfileName.Text;
            DialogResult = true;
            Close();
        }
    }
}
