using System;
using System.ComponentModel;
using System.Windows;

namespace Inventory_Manager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string eBayAPIVersion { get; } = "1065";
        public static eBay.Service.Core.Sdk.ApiContext eBayContextPRD { get; set; }
        public static eBay.Service.Core.Sdk.ApiContext eBayContextSBX { get; set; }
        public static System.Collections.Generic.List<Objects.eBayShipping> eBayShippingMethods { get; set; }

        private static eBay.Service.Call.GeteBayDetailsCall ebayDetails;
        public static eBay.Service.Call.GeteBayDetailsCall eBayDetails {
            get { return ebayDetails; }
            set {
                ebayDetails = value;
                RaiseStaticPropertyChanged();
            }
        }

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;
        public static void RaiseStaticPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] String propName = "") {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propName));
        }

        private static Objects.Settings settings = new Objects.Settings();
        public static Objects.Settings Settings
        {
            get {
                return settings;
            }
            set {
                settings = value;
                RaiseStaticPropertyChanged();
            }
        }

        public static String Name
        {
            get {
                return System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
            }
        }
        
        public static String Version
        {
            get {
                var fv = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
                return String.Format("v{0}.{1}.{2}", fv.FileMajorPart, fv.FileMinorPart, fv.FileBuildPart);
            }
        }
        
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Resources.Add("INVALID DATE GEN", $"A variation of this item (or this item itself) has an invalid date.{Environment.NewLine}A date range could not be generated for this item.");
            Resources.Add("NAME TOO LONG", $"The generated name's length is longer than eBay allows.{Environment.NewLine}If you paste this in, the end of the name will be cut off.");

            settings.InvokeLoad();

            if (Settings.FirstStart)
            {
                try
                {
                    Version current = new Version(App.Version.ToString().Substring(1));
                    Version previous = new Version(App.Settings.GetPreviousVersion("Version").ToString().Substring(1));

                    if (current > previous)
                    {
                        App.Settings.Upgrade();
                        App.Settings.Save();
                        App.Settings.FirstStart = (App.Settings.Profiles == null || App.Settings.Profiles.Count == 0);
                        settings.InvokeLoad();
                    }
                }
                catch (Exception) { }
            }
            
            if (Settings.FirstStart && OpenSettings() != true) {
                Current?.Shutdown(0);
                return;
            }
            
            // Open the profile selector if needed.
            if (Settings.RememberProfile == false && Settings.Profiles.Count > 1)
            {
                var window_ProfileSelection = new Windows.Window_ProfileSelection();
                window_ProfileSelection.IsStartup = true;

                if (window_ProfileSelection.ShowDialog() != true) {
                    Current.Shutdown(0);
                }
            }

            eBayContextPRD = Objects.eBayAPIUtil.CreateContext(
                eBayAPIVersion,
                Settings.Profile.SiteCode,
                "Mark_Russ-MarkRuss-EasyLi-qqbbiz",
                "c265f9f3-d1a7-4bee-99a8-d0e38f3920cb",
                "MarkRuss-EasyList-PRD-781f16f17-f0d779bf",
                "PRD-81f16f173ac1-1ab8-4554-adb1-b294"
            );

            eBayContextSBX = Objects.eBayAPIUtil.CreateContext(
                eBayAPIVersion,
                Settings.Profile.SiteCode,
                "Mark_Russ-MarkRuss-EasyLi-oboda",
                "c265f9f3-d1a7-4bee-99a8-d0e38f3920cb",
                "MarkRuss-EasyList-SBX-c91c6ad5c-6423843e",
                "SBX-91c6ad5c796f-0c25-482a-a173-f17f"
            );
            
            new Windows.Window_Splash().ShowDialog();
        }

        public static Boolean? OpenSettings()
        {
            var window_Settings = new Windows.Window_Settings();
            window_Settings.SelectDatabaseTab = true;
            return window_Settings.ShowDialog();
        }
    }
}
