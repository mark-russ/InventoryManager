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
    /// Interaction logic for Window_ProfileSelection.xaml
    /// </summary>
    public partial class Window_ProfileSelection : Window
    {
        public Boolean IsStartup = false;

        public Window_ProfileSelection()
        {
            InitializeComponent();
            DropDown_Profiles.SelectedIndex = App.Settings.SelectedProfileIndex;
        }
        
        private void Button_Start_Click(object sender, RoutedEventArgs e)
        {
            Button_Start.IsEnabled = false;

            if (IsStartup)
            {
                App.Settings.SelectedProfileIndex = DropDown_Profiles.SelectedIndex;
                DialogResult = true;
                Close();
            }
            else
            {
                var selectedProfile = DropDown_Profiles.SelectedIndex;
                new Task(() =>
                {
                    var result = Objects.Database.Initialize(App.Settings.Profiles[selectedProfile]);

                    Dispatcher.Invoke(() =>
                    {
                        if (result != null)
                        {
                            Button_Start.IsEnabled = true;
                            Adericium.DialogBox.Show(result.Message, "Connection failed", Adericium.Win32.Imaging.SystemIcons.Error);
                        }
                        else
                        {
                            App.Settings.SelectedProfileIndex = selectedProfile;
                            DialogResult = true;
                            Close();
                        }
                    });
                }).Start();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsStartup == false) {
                Button_Start.Content = "Apply";
            }
        }
    }
}
