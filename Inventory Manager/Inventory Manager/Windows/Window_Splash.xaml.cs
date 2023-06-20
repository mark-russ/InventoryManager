using System;
using System.Windows;
using System.Threading.Tasks;
using Inventory_Manager.Objects;
using System.ComponentModel;

namespace Inventory_Manager.Windows
{
    /// <summary>
    /// Interaction logic for Window_Splash.xaml
    /// </summary>
    public partial class Window_Splash : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private String status = "Initializing application";
        public String Status
        {
            get { return status; }
            set
            {
                status = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Status"));
            }
        }

        public Window_Splash()
        {
            InitializeComponent();
            MouseLeftButtonDown += Window_Splash_MouseLeftButtonDown;
            del = new DragMoveDelegate(() => DragMove());
        }
        
        delegate void DragMoveDelegate();
        DragMoveDelegate del;

        private void Window_Splash_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Dispatcher.BeginInvoke(del);
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Int32 dotCount = 1;

            var ticker = new System.Windows.Threading.DispatcherTimer() {
                Interval = new TimeSpan(2000000)
            };
            ticker.Tick += (senderr, er) => {
                Label_LoadingDots.Text += '.';

                if (dotCount == 4)
                {
                    Label_LoadingDots.Text = ".";
                    dotCount = 0;
                }

                dotCount++;
            };

            ticker.Start();

            Task.Factory.StartNew(() =>
            {
                if (App.Settings.Profile.EnableQuicklist == true)
                {
                    Dispatcher.Invoke(() => { Status = "Verifying eBay authorization"; });

                    if (eBayAPIUtil.GetAuthorization(App.eBayContextPRD, "prd.lastlogin") == true)
                    {
                        eBayAPIUtil.GetDetails(App.eBayContextPRD);
                    }
                    else
                    {
                        Dispatcher.Invoke(() => {
                            Adericium.DialogBox.Show("Your eBay authorization was revoked or has expired, quicklisting functionality has been disabled. You can go to your settings to re-enable and reauthorize this application.", "Connection Error", Adericium.Win32.Imaging.SystemIcons.Error);
                            System.IO.File.Delete("prd.lastlogin");
                        });
                    }
                }
                
                Dispatcher.Invoke(() => { Status = "Establishing database connection"; });
                Exception error = Database.Initialize(App.Settings.Profile);
                Boolean close = false;

                while (error != null && close == false)
                {
                    Dispatcher.Invoke(() => {
                        Hide();

                        if (error != null)
                        {
                            var response = Adericium.DialogBox.Show(error.Message, "Connection Error", Adericium.Win32.Imaging.SystemIcons.Error, "Open Settings", "Retry", "Exit Application");

                            if (response == "Open Settings")
                                close = (App.OpenSettings() == false);
                            else if (response == "Exit Application" || response == null)
                                close = true;
                        }
                    });

                    error = Database.Initialize(App.Settings.Profile);
                }

                Dispatcher.Invoke(() =>
                {
                    ticker.Stop();

                    if (error == null)
                    {
                        new MainWindow().Show();
                        Close();
                    }
                    else {
                        Application.Current.Shutdown(0);
                    }
                });
            });
        }

        private void Button_Hide_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
