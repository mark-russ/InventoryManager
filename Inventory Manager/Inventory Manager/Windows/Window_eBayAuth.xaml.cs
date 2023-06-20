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
    /// Interaction logic for Window_eBayAuth.xaml
    /// </summary>
    public partial class Window_eBayAuth : Window
    {
        public String Homepage { get; set; }
        public Window_eBayAuth()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Browser.Navigate(Homepage);
        }
        
        private void Browser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (e.Uri.OriginalString.Contains("?"))
            {
                System.Collections.Specialized.NameValueCollection parameters = System.Web.HttpUtility.ParseQueryString(e.Uri.OriginalString.Substring(e.Uri.OriginalString.IndexOf('?')));

                if (parameters["isAuthSuccessful"] != null)
                {
                    e.Cancel = true;
                    DialogResult = true;
                    Close();
                }
                else if (parameters["thirdpartyreject"] != null)
                {
                    e.Cancel = true;
                    DialogResult = false;
                    Close();
                }
            }

            Title = "Loading Webpage...";
        }

        private void Browser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            dynamic document = Browser.Document;

            try
            {
                Title = document.Title;
            }
            catch (Exception) { }
        }
    }
}
