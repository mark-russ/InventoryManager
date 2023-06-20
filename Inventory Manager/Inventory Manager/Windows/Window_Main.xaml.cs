using System;
using Adericium;
using Adericium.ArrayExtensions;

using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Interop;
using System.Windows.Data;

namespace Inventory_Manager.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Boolean ItemIsLoading = false;
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] String propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<Objects.InventoryItem> History { get; set; } = new ObservableCollection<Objects.InventoryItem>();
        
        private Objects.InventoryItem item;
        public Objects.InventoryItem Item {
            get {
                return item;
            }
            set {
                item = value;

                if (item?.Id == null)
                {
                    Button_Update.Visibility = Visibility.Collapsed;
                }
                else
                {
                    Button_Update.Visibility = Visibility.Visible;
                }


                OnPropertyChanged();
            }
        }

        Boolean SubmitAfterSearch = false;

        public MainWindow()
        {
            InitializeComponent();
            
            Resources.Add("NO DATE GIVEN",              $"No date was given, was this intentional?");
            Resources.Add("INVALID DATE GEN",           $"A variation of this item (or this item itself) has an invalid date.{Environment.NewLine}A date range could not be generated for this item.");
            Resources.Add("INVALID DATE RANGE",         $"The date you entered doesn't appear to be in the correct format.{Environment.NewLine}Examples of acceptable dates include:{Environment.NewLine}   01/2017-11/2017 (January 2017 to November 2017){Environment.NewLine}   03/2018 (March 2018)");
            Resources.Add("NAME TOO LONG",              $"The generated name's length is longer than eBay allows.{Environment.NewLine}If you paste this in, the end of the name will be cut off.");
            Resources.Add("GENERATING NAME",            $"Generating name...");
            Resources.Add("NAME COPIED",                $"Name copied.");
            Resources.Add("GENERATING DESCRIPTION",     $"Generating description...");
            Resources.Add("DESCRIPTION COPIED",         $"Description copied.");
            Resources.Add("ITEM WAS DELETED",           $"Item was deleted.");
            Resources.Add("DELETE CONFIRMATION",        $"Are you sure you want to delete this item?");
            Resources.Add("REQUIRES NAME",              $"The item must have a name.");

            if (App.Settings.WindowPositionRemember == true && App.Settings.WindowPosition != null) {
                Left = ((Point)App.Settings.WindowPosition).X;
                Top = ((Point)App.Settings.WindowPosition).Y;
                this.FixPosition();
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            //Label_ProfileLabel.Content = App.Settings.Profile.Label;
            ResetItem();
            Objects.Database.DatabaseQueryError += Database_QueryError;
            Objects.Database.SearchResultsReceived += Database_SearchResultsReceived;
            DataObject.AddPastingHandler(Box_UPC, NumericOnlyPaste);
            DataObject.AddPastingHandler(Box_Name, TrimmedPaste);
            DataObject.AddPastingHandler(DropDown_Search, TrimmedPaste);

            DropDown_Search.Focus();
        }
        
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (App.Settings.WindowPositionRemember)
            {
                App.Settings.WindowPosition = new Point(Left, Top);
                App.Settings.Save();
            }
        }

        private void NumericOnlyPaste(object sender, DataObjectPastingEventArgs e)
        {
            var originalPaste = ((String)e.DataObject.GetData(typeof(String))).Trim();

            if (!originalPaste.IsNumeric()) {
                e.CancelCommand();
                return;
            }
            
            // Replace with filtered data.
            DataObject data = new DataObject();
            data.SetText(originalPaste);
            e.DataObject = data;
        }

        private void TrimmedPaste(Object sender, DataObjectPastingEventArgs e)
        {
            var originalPaste = ((String)e.DataObject.GetData(typeof(String)));

            // Replace with filtered data.
            DataObject data = new DataObject();
            data.SetText(originalPaste.Trim());
            e.DataObject = data;
        }

        private void NumericOnly_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = (e.Key == Key.Space);
        }

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.IsNumeric();
        }
        
        private void Button_ClearStock_Click(object sender, RoutedEventArgs e)
        {
            Item.State = Objects.ItemStates.Sold;
            Box_Stock.Text = "0";
            ClearStock();
        }

        private void Button_Update_Click(object sender, RoutedEventArgs e)
        {
            if (NoIssues() == false) return;
            
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var result = Objects.Database.UpdateInventory(Item);

            if (result == Objects.Database.UpdateActions.Updated)
            {
                Label_Status.Text = String.Format("Updated \"{0}\" in {1:N0}ms.", Item.ToString(), sw.ElapsedMilliseconds);
            }
            else if (result == Objects.Database.UpdateActions.Deleted)
            {
                Label_Status.Text = String.Format("Deleted variation \"{0}\" in {1:N0}ms.", Item.ToString(), sw.ElapsedMilliseconds);
            }
            else if (result == Objects.Database.UpdateActions.None)
            {
                Label_Status.Text = String.Format("No changes made: \"{0}\" in {1:N0}ms.", Item.ToString(), sw.ElapsedMilliseconds);
            }
            else
            {
                sw.Stop();
                return;
            }

            ResetItem();
            DropDown_Search.Focus();
        }

        private void Button_Create_Click(object sender, RoutedEventArgs e)
        {
            if (NoIssues() == false) return;
            Objects.Database.AddInventory(Item);
            Label_Status.Text = String.Format("Added ", Item.ToString());
            ResetItem();
        }
        
        private void Box_Price_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ItemIsLoading == true) return;

            UpdateProfit();
        }

        private void Box_Weight_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ItemIsLoading == true) return;

            UpdateProfit();
        }

        private void Button_Config_Click(object sender, RoutedEventArgs e)
        {
            Window_Settings settingsWindow = new Window_Settings();
            settingsWindow.ShowDialog(this);

            if (App.Current == null)
                return;
            
            RefreshProfile(false);
        }

        private void Button_Name_Copy_Click(object sender, RoutedEventArgs e)
        {
            Label_Status.Text = (String)Resources["GENERATING NAME"];
            String generatedName = Item.GenerateName();

            if (generatedName != null)
            {
                Clipboard.SetText(generatedName);
                Label_Status.Text = (String)Resources["NAME COPIED"];
            }
        }

        private void Button_Description_Copy_Click(object sender, RoutedEventArgs e)
        {
            Label_Status.Text = (String)Resources["GENERATING DESCRIPTION"];
            String generatedDescription = Item.GenerateDescription();

            if (generatedDescription != null)
            {
                Clipboard.SetText(generatedDescription);
                Label_Status.Text = (String)Resources["DESCRIPTION COPIED"];
            }
        }
        
        private void DropDown_Search_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            if (Objects.Database.IsSearching) {
                SubmitAfterSearch = true;
                return;
            }

            SelectItem();
        }

        private void DropDown_Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DropDown_Search.Text != String.Empty) {
                Label_Status.Text = "Searching...";
                Objects.Database.StartSearching(DropDown_Search.Text, App.Settings.Profile.MaxSearchResults);
            }
            else 
            {
                Label_Status.Text = String.Empty;

                if (History.Count == 0) {
                    DropDown_Search.IsOpen = false;
                }

                DropDown_Search.ItemsSource = History;
            }
        }

        private void DropDown_Search_ItemChosen(object sender, Adericium.Controls.DropDown.ItemChosenEventArgs e)
        {
            if (DropDown_Search.SelectedItem == null) {
                return;
            }

            ItemIsLoading = true;
            e.Handled = true;
            Item = (Objects.InventoryItem)DropDown_Search.SelectedItem;

            History_RemoveById(Item.Id);
            History.Insert(0, Item);

            UpdateProfit();
            Box_Stock.Focus();
            Box_Stock.MoveCaretToRight();

            if (InHistoryMode == false) {
                Label_Status.Text = String.Format("Selected {0} from DB.", Item);
                DropDown_Search.Text = String.Empty;
            }
            else {
                Label_Status.Text = String.Format("Selected {0} from history.", Item);
            }
            ItemIsLoading = false;
        }

        private void Database_SearchResultsReceived(object sender, Objects.SearchEventArgs e)
        {
            Dispatcher.Invoke(() => {
                if (DropDown_Search.Text != String.Empty) {
                    DropDown_Search.ItemsSource = e.Results;
                    Label_Status.Text = String.Format("Found {0:N0} items", DropDown_Search.Items.Count);
                }

                if (SubmitAfterSearch == true) {
                    SelectItem();
                    SubmitAfterSearch = false;
                }
            });
        }

        Boolean isShowingError = false;

        private void Database_QueryError(object sender, Objects.DatabaseQueryErrorEventArgs error)
        {
            Dispatcher.Invoke(() => {
                if (isShowingError) return;
                Label_Status.Text = "Error: " + error.Exception.Message;
                isShowingError = true;
                DialogBox.Show(error.Exception.Message);
                isShowingError = false;
            });
        }
        
        private void History_RemoveById(UInt64? id)
        {
            for (Int32 i = 0; i < History.Count; i++)
            {
                if (History[i].Id == id)
                {
                    History.RemoveAt(i);
                    return;
                }
            }
        }

        private void History_RemoveByUPCName(Objects.InventoryItem item)
        {
            for (Int32 i = 0; i < History.Count; i++)
            {
                if (History[i].UPC == item.UPC && History[i].Name == item.Name && History[i].State != Objects.ItemStates.Listed)
                {
                    History.RemoveAt(i);
                    return;
                }
            }
        }

        public Boolean InHistoryMode
        {
            get
            {
                return DropDown_Search.ItemsSource == History;
            }
        }
        
        private Boolean IsExpired(String date)
        {
            if (date.Contains("-")) {
                String[] dates = date.Split('-');
                return IsExpired(dates[0]) && IsExpired(dates[1]);
            }
            else if (date.Contains("/"))
            {
                String[] parts = date.Split('/');

                if (Int32.TryParse(parts[0], out Int32 month) == false | Int32.TryParse(parts[1], out Int32 year) == false) {
                    DialogBox.Show((String)Resources["INVALID DATE RANGE"], null, Win32.Imaging.SystemIcons.Error);
                    return false;
                }
                
                if (year < DateTime.Now.Year)
                {
                    return true;
                }
                else if (year > DateTime.Now.Year)
                {
                    return false;
                }

                return (month < DateTime.Now.Month);
            }

            return false;
        }

        private DateTime?[] ExpirationStringToDate(String expiration)
        {
            List<DateTime?> times = new List<DateTime?>();

            String[] range = expiration.Split('-');

            for (Int32 i = 0; i < range.Length; i++)
            {
                String[] parts = range[i].Split('/');
                if (Int32.TryParse(parts[0], out Int32 month) == false | Int32.TryParse(parts[1], out Int32 year) == false)
                {
                    times.Add(null);
                }

                times.Add(new DateTime(year, month, 1));
            }

            // Duplicate.
            if (range.Length == 1)
            {
                times.Add(times[0]);
            }

            return times.ToArray();
        }

        private void SelectItem()
        {
            if (String.IsNullOrWhiteSpace(DropDown_Search.Text)) {
                return;
            }

            Objects.InventoryItem matchingItem = null;

            foreach (Objects.InventoryItem item in DropDown_Search.Items)
            {
                if (item.UPC == DropDown_Search.Text || item.ListingId == DropDown_Search.Text || item.Name == DropDown_Search.Text)
                {
                    if (matchingItem != null) {
                        System.Media.SystemSounds.Beep.Play();
                        return;
                    }

                    matchingItem = item;
                }
            }

            if (matchingItem != null)
            {
                DropDown_Search.SetChosenItem(matchingItem);
                Box_Stock.Focus();
            }
            else
            {
                // Ready the UI for a new item.
                ResetItem();

                if (DropDown_Search.Text.IsNumeric())
                {
                    Box_UPC.Text = DropDown_Search.Text;
                    Box_Name.Focus();
                }
                else
                {
                    Box_Name.Text = DropDown_Search.Text;
                    Box_UPC.Focus();
                }

                DropDown_Search.Text = String.Empty;
            }

            DropDown_Search.IsOpen = false;
        }

        /// <summary>
        /// Returns the decimal cost of shipping.
        /// </summary>
        /// <param name="weight"></param>
        /// <returns></returns>
        private Decimal GetShippingCost(Decimal weight)
        {
            Decimal ebayDiscountMultiplier = 1.0m;
            Decimal roundedDownWeight = Math.Floor(weight);
            Decimal cost = 4.06M;

            if (roundedDownWeight <= 4) {
                ebayDiscountMultiplier = 0.77m; // 23% discount
            }

            else if (roundedDownWeight <= 8) {
                cost += 0.75m;
                ebayDiscountMultiplier = 0.76m; // 24% discount
            }

            else if (roundedDownWeight <= 12) {
                cost += 1.60m;
                ebayDiscountMultiplier = 0.77m; // 23% discount
            }

            else if (roundedDownWeight == 13) {
                cost += 2.21m;
                ebayDiscountMultiplier = 0.89m; // 11% discount
            }

            else if (roundedDownWeight <= 16)
            {
                cost += 5.24m;
                ebayDiscountMultiplier = 0.60m; // 40% discount
            }

            /*
            if (roundedDownWeight > 15)
            {
                roundedDownWeight = 15;
            }

            if (roundedDownWeight < 5)
            {// 24% savings
                ebayDiscountMultiplier = 0.76m;
            }
            else if (roundedDownWeight == 5)
            {// 25% savings
                ebayDiscountMultiplier = 0.75m;
            }
            else if (roundedDownWeight == 6)
            {// 22% savings
                ebayDiscountMultiplier = 0.78m;
            }
            else if (roundedDownWeight == 7 || roundedDownWeight == 9)
            {// 18% savings
                ebayDiscountMultiplier = 0.82m;
            }
            else if (roundedDownWeight == 8)
            {// 15% savings
                ebayDiscountMultiplier = 0.85m;
            }
            else if (roundedDownWeight == 10)
            {// 21% savings
                ebayDiscountMultiplier = 0.79m;
            }
            else if (roundedDownWeight == 11)
            {// 23% savings
                ebayDiscountMultiplier = 0.77m;
            }
            else if (roundedDownWeight == 12 || roundedDownWeight == 13)
            {// 25% savings
                ebayDiscountMultiplier = 0.75m;
            }
            else if (roundedDownWeight == 14)
            {// 34% savings
                ebayDiscountMultiplier = 0.66m;
            }
            else if (roundedDownWeight == 15)
            {// 30% savings
                ebayDiscountMultiplier = 0.70m;
            }
            else if (roundedDownWeight == 16)
            {// 26% savings
                ebayDiscountMultiplier = 0.74m;
            }

            if (roundedDownWeight > 4 && roundedDownWeight <= 8)
            {
                cost += 0.73m;
            }
            else if (roundedDownWeight > 8 && roundedDownWeight <= 12)
            {
                cost += 0.73m + 0.8m;
            }
            else if (roundedDownWeight == 12)
            {
                cost += 0.73m + 0.8m + 0.52m;
            }
            */

            return cost*ebayDiscountMultiplier + App.Settings.Profile.PackingCost;
        }

        /// <summary>
        /// Resets the item optionally clearing box number if the setting KeepBoxNumber says to do this.
        /// </summary>
        private void ResetItem()
        {
            if (item != null && App.Settings.Profile.KeepBoxNumber == true)
            {
                String oldBox = Item.Box;
                Item = new Objects.InventoryItem {
                    Box = oldBox
                };
            }
            else
            {
                Item = new Objects.InventoryItem();
            }
        }

        /// <summary>
        /// Clears the stock, box, price, state and extra field if the item group is drugs.
        /// </summary>
        private void ClearStock()
        {
            Item.Stock = 0;
            Item.Box = null;
            Item.Price = null;
            Item.ListingId = null;

            if (App.Settings.Profile.CurrentItemCategory.SoldOutClearExtra) {
                Item.Extra = String.Empty;
            }
        }

        /// <summary>
        /// Updates the profit shipping cost.
        /// </summary>
        private void UpdateProfit()
        {
            if (String.IsNullOrEmpty(Box_Price.Text) || String.IsNullOrEmpty(Box_Weight.Text) || !Decimal.TryParse(Box_Weight.Text, out decimal weight) || !Decimal.TryParse(Box_Price.Text, out decimal price) || weight == 0 || price == 0)
            {
                Label_ShippingCost.Text = String.Empty;
                return;
            }


            Decimal profitAfterFees = price - GetFees(price);

            Decimal netWeight = weight + App.Settings.Profile.PackingWeight;
            Decimal profit = profitAfterFees - GetShippingCost(netWeight);

            String shippingCostText = String.Empty;

            if (netWeight >= 17)
            {
                foreach (Objects.ShippingMethod method in App.Settings.Profile.ShippingMethods)
                {
                    if (shippingCostText != String.Empty)
                    {
                        shippingCostText += "    ";
                    }

                    shippingCostText += String.Format("{0} {1:C}", method.Name, profitAfterFees - method.Cost);
                }
            }
            else
            {
                if (profit < 0)
                {
                    profit = -profit;
                    shippingCostText = String.Format("Loss: {0:C}.", profit);
                }
                else
                {
                    shippingCostText = String.Format("Profit: {0:C}.", profit);
                }
            }

            Label_ShippingCost.Text = shippingCostText;
        }

        private Decimal GetFees(Decimal price)
        {
            Decimal percentages = 0;
            Decimal fixedfees = 0;

            foreach (Objects.Fee fee in App.Settings.Profile.Fees)
            {
                if (fee.Type == Objects.FeeType.Percentage)
                {
                    percentages += fee.Amount;
                }
                else
                {
                    fixedfees += fee.Amount;
                }
            }

            return (price * percentages / 100) + fixedfees;
        }

        private void Button_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (Item.Id == null) return;

            if (DialogBox.Show((String)Resources["DELETE CONFIRMATION"], null, Win32.Imaging.SystemIcons.Question, "Yes", "Cancel") != "Yes")
                return;
            
            Objects.Database.RemoveInventory((UInt32)Item.Id);
            History_RemoveById((UInt32)Item.Id);
            Label_Status.Text = (String)Resources["ITEM WAS DELETED"];
            ResetItem();
        }
        
        private void Button_Database_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            new Window_DatabaseView().ShowDialog(this);
            Show();
        }

        private void Button_ViewOnEbay_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.ebay.com/itm/" + Item.ListingId).Dispose();
        }
        
        private void Box_ListingId_TextChangedSourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            if (Box_ListingId.Text != String.Empty)
            {
                Item.State = Objects.ItemStates.Listed;
            }
            else
            {
                Item.State = Objects.ItemStates.None;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Button_ProfileSwitcher_Click(object sender, RoutedEventArgs e)
        {
            if ((new Window_ProfileSelection()).ShowDialog(this) == true) {
                RefreshProfile(true);
            }
        }

        private void RefreshProfile(Boolean clearHistory)
        {
            if (clearHistory) {
                History.Clear();
                DropDown_Search.Text = String.Empty;
                DropDown_Search.IsOpen = false;
            }
        }

        private void DropDown_State_SelectionChangedSourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            if (Item.State == Objects.ItemStates.Sold) {
                Box_Stock.Focus();
                ClearStock();
            }

            if (Item.State != Objects.ItemStates.Listed) {
                Item.ListingId = string.Empty;
            }
        }

        private DateTime[] StringToDateRange(String dateRange)
        {
            if (String.IsNullOrWhiteSpace(dateRange))
                return null;

            var dtRange = new DateTime[2];

            if (dateRange.Contains("-")) {
                String[] parts = dateRange.Split('-');
                
                if (!DateTime.TryParse(ExpandDateString(parts[0]), out dtRange[0]) || !DateTime.TryParse(ExpandDateString(parts[parts.Length-1]), out dtRange[1]))
                    return null;
            }
            else {
                if (!DateTime.TryParse(ExpandDateString(dateRange), out dtRange[0]))
                    return null;

                dtRange[1] = dtRange[0];
            }

            return dtRange;
        }
        
        private String GetDateOfRange(String range, Boolean getLast)
        {
            if (range == null || range.Contains("-") == false)
                return range;

            return range.Split('-')[getLast ? 1 : 0];
        }

        private String ExpandDateString(String shorthand)
        {
            String yearPart = shorthand.Substring(shorthand.IndexOf('/') + 1);

            if (yearPart.Length == 2)
                shorthand = shorthand.Substring(0, shorthand.IndexOf('/')) + $"/20{yearPart}";
            else if (yearPart.Length != 4)
                return null;

            return shorthand;
        }

        private String StringDateToDateTime(String date)
        {
            return DateTime.Parse(date).ToString("M/yy");
        }

        public class DateStringComparer : IComparer<string>
        {
            public int Compare(String x, String y)
            {
                var xParts = x.Split('/');
                Int32 xVal = Int32.Parse(xParts[1]) * 12 + Int32.Parse(xParts[0]);

                var yParts = y.Split('/');
                Int32 yVal = Int32.Parse(yParts[1]) * 12 + Int32.Parse(yParts[0]);

                return xVal.CompareTo(yVal);
            }
        }

        private String SortCSV(String csv, Boolean isDates)
        {
            if (isDates == false) {
                return csv.Alphabetize(',');
            }
            
            List<String> blah = new List<String>(csv.Split(','));
            blah.Sort(new DateStringComparer());
            return blah.ToCSV();
        }
        
        public Boolean NoIssues()
        {
            if (App.Settings.Profile.CurrentItemCategory.ExtraBehavior == Objects.ExtraBehaviors.Dates)
            {
                if (String.IsNullOrEmpty(Item.Extra))
                {
                    if (Item.State == Objects.ItemStates.Listed)
                    {
                        if (DialogBox.Show((String)Resources["NO DATE GIVEN"], null, Win32.Imaging.SystemIcons.Error, "Yes", "No") == "No")
                            return false;
                    }
                }
                else if (Item.Extra != String.Empty && StringToDateRange(Item.Extra) == null)
                {
                    DialogBox.Show((String)Resources["INVALID DATE RANGE"], null, Win32.Imaging.SystemIcons.Error, "Ok");
                    return false;
                }
            }

            if (String.IsNullOrEmpty(Item.Name))
            {
                DialogBox.Show((String)Resources["REQUIRES NAME"], null, Win32.Imaging.SystemIcons.Error, "Ok");
                return false;
            }

            if (Item.State == Objects.ItemStates.Listed) {
                if (String.IsNullOrEmpty(Item.ListingId) && DialogBox.Show("This item was marked as listed but does not have a listing ID. Continue anyway?", null, Win32.Imaging.SystemIcons.Exclamation, "Yes", "Cancel") != "Yes")
                    return false;

                if (String.IsNullOrEmpty(Item.Box) && DialogBox.Show("This item was marked as listed but has no box. Continue anyway?", null, Win32.Imaging.SystemIcons.Exclamation, "Yes", "Cancel") != "Yes")
                    return false;
            }
            else {
                if (String.IsNullOrEmpty(Item.ListingId) == false && DialogBox.Show("This item has a listing ID but isn't marked as listed. Continue anyway?", null, Win32.Imaging.SystemIcons.Exclamation, "Yes", "Cancel") != "Yes")
                    return false;
            }

            if (Item.State == Objects.ItemStates.Sold && Item.Stock > 0 && DialogBox.Show("This item was marked as sold out, but the in-stock quantity is not zero. Continue anyway?", null, Win32.Imaging.SystemIcons.Exclamation, "Yes", "Cancel") != "Yes")
                return false;

            return true;
        }

        private void Button_QuickList_Click(object sender, RoutedEventArgs e)
        {
            var listingItem = Objects.eBayListingItem.GenerateListing(Item);
            new Window_QuickList() {
                Item = listingItem,
            }.ShowDialog(this);
        }

        private void Button_Orders_Click(object sender, RoutedEventArgs e)
        {
            Objects.Database.DatabaseQueryError -= Database_QueryError;
            Objects.Database.SearchResultsReceived -= Database_SearchResultsReceived;
            Hide();

            new Window_Orders() { Owner = this }.ShowDialog();

            Objects.Database.DatabaseQueryError += Database_QueryError;
            Objects.Database.SearchResultsReceived += Database_SearchResultsReceived;
            Show();
        }
    }
}
