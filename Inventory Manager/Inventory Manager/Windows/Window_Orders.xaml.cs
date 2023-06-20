using Adericium;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for Window_Orders.xaml
    /// </summary>
    public partial class Window_Orders : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] String propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<Objects.InventoryOrder> obs = new ObservableCollection<Objects.InventoryOrder>();
        public ObservableCollection<Objects.InventoryOrder> Orders
        {
            get
            {
                return obs;
            }
            set
            {
                obs = value;
                OnPropertyChanged();
            }
        }
        
        Boolean isShowingError = false;
        Boolean SubmitAfterSearch = false;

        private string status;
        public String Status { get { return status; } set { status = value; OnPropertyChanged(); } }

        public Window_Orders()
        {
            InitializeComponent();

            Loaded += Window_Orders_Loaded;
            Objects.Database.DatabaseQueryError += Database_QueryError;
            Objects.Database.SearchResultsReceived += Database_SearchResultsReceived;
            DataObject.AddPastingHandler(DropDown_Search, OnPaste);
        }
        
        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true) == false)
                return;


            DataObject d = new DataObject();
            d.SetData(DataFormats.Text, (e.SourceDataObject.GetData(DataFormats.UnicodeText) as string).Trim());
            e.DataObject = d;
        }

        private void Window_Orders_Loaded(object sender, RoutedEventArgs e)
        {
            var cvs_Orders = (ListCollectionView)((CollectionViewSource)Resources["CVS_Orders"]).View;
            cvs_Orders.CustomSort = Comparer<Objects.InventoryOrder>.Create((x, y) => Win32.SafeNativeMethods.StrCmpLogicalW(x.Box, y.Box));
        }

        private void Database_SearchResultsReceived(object sender, Objects.SearchEventArgs e)
        {
            Dispatcher.Invoke(() => {
                if (DropDown_Search.Text != String.Empty)
                {
                    DropDown_Search.ItemsSource = e.Results;
                    Status = String.Format("Found {0:N0} items", DropDown_Search.Items.Count);
                }

                if (SubmitAfterSearch == true)
                {
                    SelectItem();
                    SubmitAfterSearch = false;
                }
            });
        }
        
        private void Database_QueryError(object sender, Objects.DatabaseQueryErrorEventArgs error)
        {
            Dispatcher.Invoke(() => {
                if (isShowingError) return;
                Status = "Error: " + error.Exception.Message;
                isShowingError = true;
                DialogBox.Show(error.Exception.Message);
                isShowingError = false;
            });
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

        private void SelectItem()
        {
            if (String.IsNullOrWhiteSpace(DropDown_Search.Text))
            {
                return;
            }

            Objects.InventoryItem matchingItem = null;

            foreach (Objects.InventoryItem item in DropDown_Search.Items)
            {
                if (item.UPC == DropDown_Search.Text || item.ListingId == DropDown_Search.Text || item.Name == DropDown_Search.Text)
                {
                    if (matchingItem != null)
                    {
                        System.Media.SystemSounds.Beep.Play();
                        return;
                    }

                    matchingItem = item;
                }
            }

            if (matchingItem != null)
            {
                DropDown_Search.SetChosenItem(matchingItem);
            }
            else
            {
                DropDown_Search.Text = String.Empty;
            }

            DropDown_Search.IsOpen = false;
        }

        private void DropDown_Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DropDown_Search.Text != String.Empty) {
                Status = "Searching...";
                Objects.Database.StartSearching(DropDown_Search.Text, App.Settings.Profile.MaxSearchResults);
            }
            else {
                Status = String.Empty;
                DropDown_Search.IsOpen = false;
            }
        }

        private void DropDown_Search_ItemChosen(object sender, Adericium.Controls.DropDown.ItemChosenEventArgs e)
        {
            List_Orders.Focus();

            if (DropDown_Search.SelectedItem == null)
                return;
           
            e.Handled = true;

            var newOrder = Objects.InventoryOrder.FromItem((Objects.InventoryItem)DropDown_Search.SelectedItem);

            if (FocusListItem(newOrder.Id) == false) {
                Orders.Add(newOrder);
            }
            else
            {
                Window_Orders_UpdateQTY window_UpdateOrder = new Window_Orders_UpdateQTY();

                if (window_UpdateOrder.ShowDialog(this) == true) {
                    foreach (Objects.InventoryOrder order in Orders)
                    {
                        if (newOrder.Id == order.Id)
                        {
                            order.QuantityOrdered += window_UpdateOrder.ResponseValue;
                            DropDown_Search.Focus();
                            break;
                        }
                    }
                }
            }
            
            DropDown_Search.Text = String.Empty;
        }
       
        /// <summary>
        /// Focuses the list item quantity textbox by the inventory ID.
        /// </summary>
        /// <param name="inventoryId">The ID to search for.</param>
        /// <returns>Returns TRUE if the textbox was found and focused.</returns>
        private Boolean FocusListItem(uint? inventoryId)
        {
            foreach (var child in Adericium.Utility.FindVisualChildren<TextBox>(List_Orders))
            {
                if (inventoryId == ((Objects.InventoryOrder)child.DataContext).Id)
                {
                    child.Focus();
                    return true;
                }
            }

            return false;
        }

        private void Box_QuantityOrdered_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            DropDown_Search.Focus();
        }

        private void Button_Finalize_Click(object sender, RoutedEventArgs e)
        {
            foreach (Objects.InventoryOrder order in Orders)
            {
                if (order.QuantityOrdered < 1) {
                    DialogBox.Show($"\"{order.Label}\" has an invalid ordered quantity. Enter an amount that is greater than zero.");
                    return;
                }
            }

            if (DialogBox.Show("Are you sure you want to commit these changes?", null, Win32.Imaging.SystemIcons.Question, "Yes", "Cancel") != "Yes")
                return;

            if (HasPrinted == false && DialogBox.Show("Are you really sure? You have not printed this list.", null, Win32.Imaging.SystemIcons.Exclamation, "Yes", "Cancel") != "Yes") {
                return;
            }
            
            try
            {
                String result = Objects.Database.UpdateStockFromOrders(Orders);

                if (result != "SUCCESS") {
                    DialogBox.Show(result, null, Win32.Imaging.SystemIcons.Error, "Ok");
                    return;
                }

            }
            catch (Exception ex)
            {
                DialogBox.Show($"Update failed with reason:{Environment.NewLine}{ex.Message}{Environment.NewLine}The inventory update was rolled back.", null, Win32.Imaging.SystemIcons.Error, "Ok");
                return;
            }

            String folderName = "Finalized";

            if (System.IO.Directory.Exists(folderName) == false) {
                System.IO.Directory.CreateDirectory(folderName);
            }


            using (var file = new System.IO.StreamWriter(System.IO.Path.Combine(folderName, DateTime.UtcNow.TimeOfDay.Ticks + ".txt")))
            {
                file.WriteLine("This order list was produced: " + DateTime.Now.ToString(@"MM/dd/yy   h:mmtt"));

                Int32[] lengths = new Int32[3];

                foreach (Objects.InventoryOrder order in Orders)
                {
                    if (order.Label.Length >= lengths[0])
                        lengths[0] = order.Label.Length + 1;
                    
                    if (order.Extra?.Length >= lengths[1])
                        lengths[1] = order.Extra.Length;

                    if (order.UPC.Length >= lengths[2])
                        lengths[2] = order.UPC.Length;

                }
                
                String lastBox = null;
                foreach (Objects.InventoryOrder order in Orders)
                {
                    if (lastBox != order.Box) {
                        lastBox = order.Box;
                        file.WriteLine($"{Environment.NewLine}{lastBox}");
                    }

                    file.Write($"   {order.Label.PadRight(lengths[0])}");

                    if (String.IsNullOrWhiteSpace(order.Extra) == false)
                        file.Write($"{order.Extra.PadRight(lengths[1])},  ");
                    else
                        file.Write(new String(' ', lengths[1]) + "   ");

                    if (String.IsNullOrWhiteSpace(order.UPC) == false)
                        file.Write($"UPC: {order.UPC.PadRight(lengths[2])},  ");
                    else
                        file.Write($"UPC: {"N/A".PadRight(lengths[2])}   ");

                    file.WriteLine($"QTY: {order.QuantityOrdered.ToString().PadRight(4)}");
                }
            }

            Close();
        }

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.IsNumeric();
        }

        private void NumericOnly_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = e.Key == Key.Space;
        }

        private void Button_RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (Objects.InventoryOrder)((Button)sender).DataContext;

            if (DialogBox.Show($"Are you sure you want to remove: {Environment.NewLine}{item.Label}?", null, Win32.Imaging.SystemIcons.Question, "Yes", "No", "Cancel") == "Yes")
                Orders.Remove(item);
        }
        
        private Boolean HasPrinted = false;

        private void Button_Print_Click(object sender, RoutedEventArgs e)
        {
            String docTitle = "Orders: " + DateTime.Now.ToString(@"MM/dd/yy   h:mmtt");
            var document = GetPrintable(docTitle);

            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                HasPrinted = true;
                pd.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, docTitle);
            }
        }

        private FlowDocument GetPrintable(String title = null)
        {
            if (title == null) {
                title = "Orders: " + DateTime.Now.ToString(@"MM/dd/yy   h:mmtt");
            }

            var cvs_Orders = ((CollectionViewSource)Resources["CVS_Orders"]).View;

            var document = new FlowDocument();
            document.PagePadding = new Thickness(32);
            document.IsColumnWidthFlexible = true;
            document.ColumnWidth = Double.PositiveInfinity;
            document.FontFamily = new FontFamily("Calibri");

            document.Blocks.Add(new Paragraph(new Run(title))
            {
                Padding = new Thickness(0, 32, 0, 32),
                TextAlignment = TextAlignment.Center
            });

            var table = new Table();
            table.Columns.Add(new TableColumn() { Width = new GridLength(1.65, GridUnitType.Star) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(5, GridUnitType.Star) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(2, GridUnitType.Star) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(0.5, GridUnitType.Star) });
            table.RowGroups.Add(new TableRowGroup());

            var heading = new TableRow();
            heading.Cells.Add(new TableCell(new Paragraph(new Run("UPC"))));
            heading.Cells.Add(new TableCell(new Paragraph(new Run("Name"))));
            heading.Cells.Add(new TableCell(new Paragraph(new Run("Extra"))));
            heading.Cells.Add(new TableCell(new Paragraph(new Run("Location"))));
            heading.Cells.Add(new TableCell(new Paragraph(new Run("Qty"))));
            table.RowGroups[0].Rows.Add(heading);

            Int32 itemIndex = 0;
            foreach (CollectionViewGroup group in cvs_Orders.Groups)
            {
                foreach (Objects.InventoryOrder order in group.Items)
                {
                    itemIndex++;
                    var row = new TableRow();
                    row.Background = (itemIndex % 2 == 1 ? (SolidColorBrush)new BrushConverter().ConvertFromString("#CACACA") : Brushes.White);
                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.UPC))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.Label))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.Extra))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.Box))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.QuantityOrdered.ToString()))));
                    table.RowGroups[0].Rows.Add(row);
                }
            }

            document.Blocks.Add(table);
            return document;
        }
    }
}
