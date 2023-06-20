using System;
using Adericium;
using System.Windows;
using System.Threading;
using Inventory_Manager.Objects;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Media;
using System.Windows.Media;

namespace Inventory_Manager.Windows
{
    /// <summary>
    /// Interaction logic for Window_Settings.xaml
    /// </summary>
    public partial class Window_Settings : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] String propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool SelectDatabaseTab;

        private Settings temporarySettings;
        public Settings TemporarySettings
        {
            get
            {
                return temporarySettings;
            }

            set
            {
                temporarySettings = value;
                OnPropertyChanged();
            }
        }

        public eBay.Service.Core.Sdk.ApiContext eBayAPIContext;

        private CancellationTokenSource CancelTokenSource = null;

        private Boolean IsBusy = false;

        private DispatcherTimer Timer_LoadingIndicator;
        private Int32 DotCount = 1;

        public Boolean NeedsChecked
        {
            get
            {
                if (App.Settings.Profile.DatabaseHostname != TemporarySettings.Profile.DatabaseHostname)
                    return false;

                if (App.Settings.Profile.DatabasePort != TemporarySettings.Profile.DatabasePort)
                    return false;

                if (App.Settings.Profile.DatabaseName != TemporarySettings.Profile.DatabaseName)
                    return false;

                if (App.Settings.Profile.DatabaseUsername != TemporarySettings.Profile.DatabaseUsername)
                    return false;

                if (App.Settings.Profile.DatabasePassword != TemporarySettings.Profile.DatabasePassword)
                    return false;
                
                if (App.Settings.Profile.DatabaseSSLMode != TemporarySettings.Profile.DatabaseSSLMode)
                    return false;

                return true;
            }
        }

        public Window_Settings()
        {
            TemporarySettings = new Settings();

            InitializeComponent();
            Box_ConnectionPassword.Password = Adericium.Utility.DPAPI_Unprotect(TemporarySettings.Profile.DatabasePassword, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            
            if (SelectDatabaseTab == true) {
                Tab_Database.IsSelected = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (eBayAPIContext != null && temporarySettings.Profile.EnableQuicklist == true)
                LoadEBayAccount(eBayAPIContext);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (IsBusy == true) {
                CancelTokenSource.Cancel();
            }
        }

        private void Timer_LoadingIndicator_Tick(object sender, EventArgs e)
        {
            if (DotCount == 4) {
                Label_Status.Content = "Working.";
                DotCount = 0;
            } 
            else {
                Label_Status.Content = (string)Label_Status.Content + '.';
            }

            DotCount++;
        }

        private void List_ItemFees_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Window_AddFee window_fee = new Window_AddFee();

            if (window_fee.ShowDialog(this) == true)
            {
                TemporarySettings.Profile.Fees.Add(window_fee.Fee);
            }
        }

        private void List_ItemFees_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != System.Windows.Input.MouseButton.Left) {
                return;
            }


            if (((FrameworkElement)e.OriginalSource).DataContext is Fee item)
            {
                Window_AddFee window_fee = new Window_AddFee()
                {
                    Fee = item.Copy()
                };

                if (window_fee.ShowDialog(this) == true)
                {
                    TemporarySettings.Profile.Fees[TemporarySettings.Profile.Fees.IndexOf(item)] = window_fee.Fee;
                }
            }
        }

        private void List_ItemFees_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Delete || List_ItemFees.SelectedIndex == -1)
                return;

            if (DialogBox.Show("Are you sure you want to delete the selection?", "Delete Confirmation", Win32.Imaging.SystemIcons.Error, "Yes", "No") == "Yes")
            {
                while (List_ItemFees.SelectedIndex != -1)
                {
                    TemporarySettings.Profile.Fees.RemoveAt(List_ItemFees.SelectedIndex);
                }
            }
        }

        private void List_ShippingMethods_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Window_AddShippingMethod window_sm = new Window_AddShippingMethod();

            if (window_sm.ShowDialog(this) == true) {
                TemporarySettings.Profile.ShippingMethods.Add(window_sm.ShippingMethod);
            }
        }

        private void List_ShippingMethods_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != System.Windows.Input.MouseButton.Left) {
                return;
            }
            
            if (((FrameworkElement)e.OriginalSource).DataContext is ShippingMethod item)
            {
                Window_AddShippingMethod window_sm = new Window_AddShippingMethod
                {
                    ShippingMethod = item.Copy()
                };

                if (window_sm.ShowDialog(this) == true)
                {
                    TemporarySettings.Profile.ShippingMethods[TemporarySettings.Profile.ShippingMethods.IndexOf(item)] = window_sm.ShippingMethod;
                }
            }
        }

        private void List_ShippingMethods_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Delete || List_ShippingMethods.SelectedIndex == -1)
                return;

            if (DialogBox.Show("Are you sure you want to delete the selection?", "Delete Confirmation", Win32.Imaging.SystemIcons.Error, "Yes", "No") == "Yes")
            {
                while (List_ShippingMethods.SelectedIndex != -1)
                {
                    TemporarySettings.Profile.ShippingMethods.RemoveAt(List_ShippingMethods.SelectedIndex);
                }
            }
        }

        private void Button_Connect_Click(object sender, RoutedEventArgs e)
        {
            Button_Save.IsEnabled = false;
            Button_Connect.Content = "Cancel";

            if (IsBusy == true)
            {
                CancelTokenSource.Cancel();
                return;
            }

            CancelTokenSource?.Dispose();
            CancelTokenSource = new CancellationTokenSource();
            IsBusy = true;

            DotCount = 1;
            Label_Status.Content = "Working.";
            Timer_LoadingIndicator = new DispatcherTimer();
            Timer_LoadingIndicator.Tick += Timer_LoadingIndicator_Tick;
            Timer_LoadingIndicator.Interval = TimeSpan.FromMilliseconds(500);
            Timer_LoadingIndicator.Start();

            Task.Factory.StartNew(TryConnecting);
        }

        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            if (TemporarySettings.Profile.EnableQuicklist == false) {
                if (System.IO.File.Exists("prd.lastlogin")) System.IO.File.Delete("prd.lastlogin");
            }
            else if (eBayAPIContext.ApiCredential.eBayToken != App.eBayContextPRD.ApiCredential.eBayToken) {
                System.IO.File.WriteAllText("prd.lastlogin", eBayAPIContext.ApiCredential.eBayToken);
            }

            App.eBayContextPRD = eBayAPIContext;

            if (App.eBayContextPRD != null && App.eBayDetails == null) {
                eBayAPIUtil.GetDetails(App.eBayContextPRD);
            }

            App.Settings = TemporarySettings;
            App.Settings.Save();
            DialogResult = true;
            Close();
        }

        private void Button_Backup_Click(object sender, RoutedEventArgs e)
        {
            String response = DialogBox.Show("Leave me alone before I crash the program!", "Stop that!", Win32.Imaging.SystemIcons.Warning, "See if I care", "Sorry");

            if (response == "Sorry") {
                DialogBox.Show("That's what I thought.");
            }
            else if (response == "See if I care") {
                throw new Exception("I did it. I told you I would.");
            }
        }

        private void Button_Create_Profile_Click(object sender, RoutedEventArgs e)
        {
            var window_CreateProfile = new Window_CreateProfile() {
                Settings = TemporarySettings
            };

            if (window_CreateProfile.ShowDialog(this) == true) {
                
                if (window_CreateProfile.Profile.ItemCategories == null) {
                    window_CreateProfile.Profile.UseDefaultCategory();
                }

                TemporarySettings.Profiles.Add(window_CreateProfile.Profile);
            }
        }

        private void Button_Delete_Profile_Click(object sender, RoutedEventArgs e)
        {
            if (DialogBox.Show("Are you sure you want to delete this profile?", null, Win32.Imaging.SystemIcons.Question, "Yes", "Cancel") != "Yes")
                return;

            if (TemporarySettings.Profiles.Count == 1)
            {
                if (DialogBox.Show($"This is the last profile!{Environment.NewLine}Are you sure you want to fully reset this application?", null, Win32.Imaging.SystemIcons.Warning, "Yes", "Cancel") == "Yes")
                {
                    App.Settings.Reset();
                    Application.Current.Restart();
                }
            }
            else
            {
                TemporarySettings.Profiles.RemoveAt(TemporarySettings.SelectedProfileIndex);
            }
        }
        
        private void Box_ConnectionPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            if (Box_ConnectionPassword.Password == null) {
                TemporarySettings.Profile.DatabasePassword = null;
            }
            else {
                TemporarySettings.Profile.DatabasePassword = Adericium.Utility.DPAPI_Protect(Box_ConnectionPassword.Password, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            }
        }
        
        private void Button_Rename_Profile_Click(object sender, RoutedEventArgs e)
        {
            var response = InputBox.Show($"What do you want to rename \"{TemporarySettings.Profile.Label}\" to?", null, Win32.Imaging.SystemIcons.Question, "Apply");

            if (response.Button != "Apply") return;

            TemporarySettings.Profile.Label = response.Input;
        }

        private void Button_Create_ItemCategory_Click(object sender, RoutedEventArgs e)
        {
            var window = new Window_ItemCategoryEditor() {
                IsEditMode = false,
                ItemCategory = new ItemCategory()
            };

            if (window.ShowDialog(this) == true) {
                TemporarySettings.Profile.ItemCategories.Add(window.ItemCategory);
                TemporarySettings.Profile.CurrentItemCategoryIndex = TemporarySettings.Profile.ItemCategories.Count - 1;
            }
        }

        private void Button_Rename_ItemCategory_Click(object sender, RoutedEventArgs e)
        {
            var window = new Window_ItemCategoryEditor() {
                IsEditMode = true,
                ItemCategory = TemporarySettings.Profile.CurrentItemCategory.Copy()
            };

            if (window.ShowDialog(this) == true) {
                Int32 oldIndex = TemporarySettings.Profile.CurrentItemCategoryIndex;
                TemporarySettings.Profile.ItemCategories[TemporarySettings.Profile.CurrentItemCategoryIndex] = window.ItemCategory;
                TemporarySettings.Profile.CurrentItemCategoryIndex = oldIndex;
            }
        }

        private void Button_Delete_ItemCategory_Click(object sender, RoutedEventArgs e)
        {
            if (DialogBox.Show("Are you sure you want to delete this category?", null, Win32.Imaging.SystemIcons.Question, "Yes", "Cancel") != "Yes")
                return;
            
            TemporarySettings.Profile.ItemCategories.RemoveAt(TemporarySettings.Profile.CurrentItemCategoryIndex);
        }

        private void DropDown_ProfileSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (IsLoaded == false) return;
            Box_ConnectionPassword.Password = Adericium.Utility.DPAPI_Unprotect(TemporarySettings.Profile.DatabasePassword, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        }
        
        private void CheckBox_QuickList_Checked(object sender, RoutedEventArgs e) {
            if (IsLoaded == false) return;

            eBayAPIContext = eBayAPIUtil.CreateContext(
                App.eBayAPIVersion, 
                App.eBayContextPRD.Site, 
                App.eBayContextPRD.RuName,
                App.eBayContextPRD.ApiCredential.ApiAccount.Developer, App.eBayContextPRD.ApiCredential.ApiAccount.Application, App.eBayContextPRD.ApiCredential.ApiAccount.Certificate
            );
            
            if (eBayAPIUtil.GetAuthorization(eBayAPIContext) == true) {
                LoadEBayAccount(eBayAPIContext);
            }
            else {
                (sender as System.Windows.Controls.CheckBox).IsChecked = false;
                e.Handled = true;
            }
        }

        private void CheckBox_QuickList_Unchecked(object sender, RoutedEventArgs e)
        {
            Label_UserID.Text = "";
            Label_Standing.Text = "";
            Label_Feedback.Text = "";

            eBayAPIContext.ApiCredential.eBayToken = null;
        }

        private void Button_ChangeAccount_Click(object sender, RoutedEventArgs e)
        {
            if (eBayAPIUtil.GetAuthorization(eBayAPIContext) == true) {
                LoadEBayAccount(eBayAPIContext);
                DialogBox.Show("Authorization was successfully received!");
            }
        }
        
        private void Button_ImageFolder_Browse(object sender, RoutedEventArgs e)
        {
            using (var folderBrowser = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog())
            {
                folderBrowser.AllowNonFileSystemItems = true;
                folderBrowser.IsFolderPicker = true;
                folderBrowser.Multiselect = false;
                folderBrowser.InitialDirectory = TemporarySettings.Profile.ImageFolder;
                
                if (folderBrowser.ShowDialog(this) == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok) {
                    TemporarySettings.Profile.ImageFolder = folderBrowser.FileName;
                }
            }
        }
        
        private eBay.Service.Core.Soap.ShippingServiceDetailsType GetShippingDetails(String shippingMethod)
        {
            foreach (eBay.Service.Core.Soap.ShippingServiceDetailsType details in App.eBayDetails.ShippingServiceDetailList)
            {
                if (details.ShippingService == shippingMethod)
                    return details;
            }

            return null;
        }

        private void LoadEBayAccount(eBay.Service.Core.Sdk.ApiContext context)
        {
            if (IsLoaded == false) return;

            Label_UserID.Text = "Fetching...";
            Label_Standing.Text = "Fetching...";
            Label_Feedback.Text = "Fetching...";

            Task.Factory.StartNew(() => {
                var user = new eBay.Service.Call.GetUserCall(context).GetUser();

                Dispatcher.Invoke(() => {
                    Label_UserID.Text = user.UserID.ToString();
                    Label_Standing.Text = user.Status + ", " + (user.eBayGoodStanding ? "Good Standing" : "Negative Standing");
                    Label_Feedback.Text = String.Format("{0:N0} ({1:P1} positive)", user.FeedbackScore, user.PositiveFeedbackPercent / 100);
                });
            });
        }

        private async void TryConnecting()
        {
            String status = null;
            String errorMsg = null;

            try
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                using (var connection = Database.GetConnection(TemporarySettings.Profile))
                {
                    await connection.OpenAsync(CancelTokenSource.Token).ConfigureAwait(false);
                }

                sw.Stop();
                status = String.Format("Success! Connection established in {0:N0}ms.", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                if (CancelTokenSource.IsCancellationRequested) {
                    status = "Cancelled.";
                }
                else {
                    status = "Connection failed.";
                    errorMsg = ex.Message;
                }
            }
            finally
            {
                Dispatcher.Invoke(() => {
                    IsBusy = false;
                    Timer_LoadingIndicator.Stop();
                    Timer_LoadingIndicator = null;
                    Label_Status.Content = status;
                    Button_Save.IsEnabled = true;
                    Button_Connect.Content = "Run Test";

                    if (errorMsg != null)
                    {
                        DialogBox.Show(errorMsg, "Connection failure", Win32.Imaging.SystemIcons.Error);
                    }
                });
            }
        }
    }
}

/*
        private void Button_TEST_LISTING_Click(object sender, RoutedEventArgs e)
        {
            var apiCall = new eBay.Service.Call.GetSuggestedCategoriesCall(App.eBayContextPRD);
            var categorySuggestions = apiCall.GetSuggestedCategories("multivitamin");

            var selectedShippingService = GetShippingDetails(App.Settings.Profile.ShippingService);


            var apiCallVerifyItem = new eBay.Service.Call.VerifyAddItemCall(App.eBayContextPRD);
            var item = new eBay.Service.Core.Soap.ItemType()
            {
                AutoPay = TemporarySettings.Profile.RequireImmediatePayment,
                PaymentMethods = new eBay.Service.Core.Soap.BuyerPaymentMethodCodeTypeCollection() {
                    TemporarySettings.Profile.PaymentMethod
                },
                PayPalEmailAddress = "mark.f.russ@outlook.com",
                Title = "Test Listing",
                Description = "This is a test listing!",
                StartPrice = new eBay.Service.Core.Soap.AmountType()
                {
                    currencyID = eBay.Service.Core.Soap.CurrencyCodeType.USD,
                    Value = 7.99
                },
                Quantity = 70,
                Location = "Groveland, FL",
                PostalCode = "34736",
                ListingType = eBay.Service.Core.Soap.ListingTypeCodeType.FixedPriceItem,
                Country = eBay.Service.Core.Soap.CountryCodeType.US,
                Currency = eBay.Service.Core.Soap.CurrencyCodeType.USD,
                BestOfferEnabled = TemporarySettings.Profile.AllowOffers,
                ListingDuration = "Days_30",
                ConditionID = 1000,
                PrimaryCategory = categorySuggestions[0].Category,
                ShippingDetails = new eBay.Service.Core.Soap.ShippingDetailsType()
                {
                    ShippingServiceOptions = new eBay.Service.Core.Soap.ShippingServiceOptionsTypeCollection()
                    {
                        new eBay.Service.Core.Soap.ShippingServiceOptionsType() {
                            FreeShipping = true,
                            ShippingService = selectedShippingService.ShippingService,
                        }
                    },
                    GlobalShipping = false
                },
                DispatchTimeMax = 1, // Handling time.
                ReturnPolicy = new eBay.Service.Core.Soap.ReturnPolicyType()
                {
                    InternationalReturnsAcceptedOption = "ReturnsNotAccepted",
                    RefundOption = "MoneyBackOrReplacement",
                    ReturnsAcceptedOption = "ReturnsAccepted",
                    ReturnsWithinOption = "Days_30"
                }
            };

            var apiPictureServiceCall = new eBay.Service.EPS.eBayPictureService(App.eBayContextPRD);

            String[] foundImages = eBayListingUtil.FindImages("31254742264");
            item.PictureDetails = new eBay.Service.Core.Soap.PictureDetailsType()
            {
                PictureURL = new eBay.Service.Core.Soap.StringCollection(apiPictureServiceCall.UpLoadPictureFiles(eBay.Service.Core.Soap.PhotoDisplayCodeType.None, foundImages))
            };

            eBay.Service.Core.Soap.FeeTypeCollection response = apiCallVerifyItem.VerifyAddItem(item);

            foreach (eBay.Service.Core.Soap.FeeType fee in response)
            {
                Console.WriteLine("Fee " + fee.Name + " is " + (fee.Fee.Value - (fee.PromotionalDiscount == null ? 0 : fee.PromotionalDiscount.Value)));
            }
        }
*/