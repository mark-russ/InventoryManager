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
    public partial class Window_QuickList : Window, System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] String propertyName = "") {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }


        public Objects.eBayListingSchema ListingSchema;

        public eBay.Service.Core.Soap.SuggestedCategoryTypeCollection suggestedCategories { get; set; }
        public eBay.Service.Core.Soap.SuggestedCategoryTypeCollection SuggestedCategories  { get { return suggestedCategories; } set { suggestedCategories = value; OnPropertyChanged(); } }

        private eBay.Service.Core.Soap.SuggestedCategoryType selectedSuggestion;
        public eBay.Service.Core.Soap.SuggestedCategoryType SelectedSuggestion { get { return selectedSuggestion; }
            set
            {
                selectedSuggestion = value;

                if (selectedSuggestion == null) return;

                ListingSchema = Objects.eBayListingSchema.GetSchema(selectedSuggestion.Category.CategoryID);
                OnPropertyChanged();
            }
        }
        
        private eBay.Service.Core.Soap.CategoryFeatureType selectedCategoryFeatures;
        public eBay.Service.Core.Soap.CategoryFeatureType SelectedCategoryFeatures {
            get
            {
                return selectedCategoryFeatures;
            }
            set
            {
                selectedCategoryFeatures = value;
                OnPropertyChanged();
            }
        }

        public eBay.Service.Core.Soap.PaymentDetailsType CategoryPaymentMethods { get; set; }

        private Objects.eBayListingItem item;
        public Objects.eBayListingItem Item { get { return item; } set { item = value; OnPropertyChanged(); } }

        public Window_QuickList()
        {
            InitializeComponent();
        }
        
        private void Button_ListItem_Click(object sender, RoutedEventArgs e)
        {
            var apiAddItem = new eBay.Service.Call.AddItemCall(App.eBayContextPRD);
            var ebayItem = new eBay.Service.Core.Soap.ItemType();

            ebayItem.Title = Box_Title.Text;
            ebayItem.Description = Box_Description.Text;
            ebayItem.PaymentMethods = new eBay.Service.Core.Soap.BuyerPaymentMethodCodeTypeCollection()
            {

            };
            ebayItem.PayPalEmailAddress = Box_PayPalEmail.Text;
            ebayItem.PrimaryCategory = SelectedSuggestion.Category;
            ebayItem.Quantity = (Int32)Box_Quantity.Value;
            //ebayItem.ShippingDetails = 
            ebayItem.IsSecureDescription = true;
            ebayItem.ListingType = eBay.Service.Core.Soap.ListingTypeCodeType.FixedPriceItem;
        }

        private void Button_Picture_Remove_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            Item.Pictures.Remove((Objects.PictureItem)btn.DataContext);
        }
         
        private void Button_AddPictures_Click(object sender, RoutedEventArgs e)
        {
            using (var openFileDialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog())
            {
                openFileDialog.AllowNonFileSystemItems = true;
                openFileDialog.Multiselect = true;
                openFileDialog.DefaultDirectory = App.Settings.Profile.ImageFolder;

                String allExtensions = null;
                foreach (String extension in Objects.eBayListingUtil.SupportedExtensions)
                {
                    allExtensions += $"*{extension};";
                    openFileDialog.Filters.Add(new Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogFilter($"{extension.Substring(1).ToUpper()} Files", $"*{extension}"));
                }
                allExtensions = allExtensions.Substring(0, allExtensions.Length - 1);

                openFileDialog.Filters.Insert(0, new Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogFilter("All Supported Files", $"{allExtensions}"));

                if (openFileDialog.ShowDialog(this) == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
                {
                    if (Item.Pictures.Count + openFileDialog.FileNames.Count() > 12) {
                        Adericium.DialogBox.Show("You're adding more pictures than eBay allows.", null, Adericium.Win32.Imaging.SystemIcons.Asterisk);
                        return;
                    }

                    foreach (var file in openFileDialog.FileNames)
                    {
                        Item.Pictures.Add(new Objects.PictureItem() {
                            ImageSrc = new BitmapImage(new Uri(file))
                        });
                    }
                }
            }
        }

        private void Button_ClearPictures_Click(object sender, RoutedEventArgs e)
        {
            Item.Pictures.Clear();
        }

        private void Button_Category_Search_Click(object sender, RoutedEventArgs e)
        {
            GetSuggestedCategories(Box_Search.Text);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GetSuggestedCategories(Item.Title);
        }

        private void GetSuggestedCategories(String keywords) {
            var apiSearchCategory = new eBay.Service.Call.GetSuggestedCategoriesCall(App.eBayContextPRD);
            SuggestedCategories = apiSearchCategory.GetSuggestedCategories(keywords);
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            var obj = (eBay.Service.Core.Soap.ListingDurationReferenceType)e.Item;
            e.Accepted = (obj.type == Item.ListingType);

            if (e.Accepted == false)
            {
                Console.WriteLine("SKIPPED " + obj.type + " with time " + obj.Value);
            }
        }
        
        private void DropDown_ListingFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded == false) return;

            if (ListingSchema.ListingDurations.ContainsKey(Item.ListingType) == false)
                DropDown_ListingDurations.ItemsSource = null;
            else
                DropDown_ListingDurations.ItemsSource = ListingSchema.ListingDurations[Item.ListingType];
        }
        
        private void CVS_ListingTypes_Filter(object sender, FilterEventArgs e)
        {
            var typeCode = ((eBay.Service.Core.Soap.ListingTypeCodeType)((Adericium.EnumerationExtension.EnumerationMember)e.Item).Value);
            e.Accepted = (typeCode == eBay.Service.Core.Soap.ListingTypeCodeType.Auction || typeCode == eBay.Service.Core.Soap.ListingTypeCodeType.FixedPriceItem);
        }
    }
}
