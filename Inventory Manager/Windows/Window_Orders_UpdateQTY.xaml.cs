using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for Window_Orders_UpdateQTY.xaml
    /// </summary>
    public partial class Window_Orders_UpdateQTY : Window
    {
        private int responseValue;
        public int ResponseValue
        {
            get
            {
                return responseValue;
            }
            set
            {
                responseValue = value;
            }
        }

        private Boolean? isGood;

        public Window_Orders_UpdateQTY()
        {
            InitializeComponent();
        }

        private void Button_Update_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = isGood;
        }

        private void Box_Input_TextChanged(object sender, TextChangedEventArgs e)
        {
            isGood = int.TryParse(Box_Input.Text, out responseValue);
        }

        private void Box_Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                DialogResult = isGood;
        }
    }
}
