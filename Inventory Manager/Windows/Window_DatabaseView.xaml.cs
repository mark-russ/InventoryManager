using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Inventory_Manager.Windows
{
    /// <summary>
    /// Interaction logic for Window_DatabaseView.xaml
    /// </summary>
    public partial class Window_DatabaseView : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] String propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        MySql.Data.MySqlClient.MySqlConnection Connection = Objects.Database.GetConnection();
        MySql.Data.MySqlClient.MySqlTransaction Transaction;

        ObservableCollection<String> queryHistory = new ObservableCollection<String>();
        public ObservableCollection<String> QueryHistory {
            get { return queryHistory; }
            set { queryHistory = value; OnPropertyChanged(); }
        }

        public Window_DatabaseView()
        {
            InitializeComponent();
            Transaction = Connection.BeginTransaction();
        }

        private void Button_Execute_Click(object sender, RoutedEventArgs e)
        {
            Object result = Objects.Database.ExecuteQuery(Box_Query.Text, Connection, Transaction);

            if (result is String)
            {
                QueryHistory.Add(Box_Query.Text);
                Adericium.DialogBox.Show(result as String, "Query Succeeded", Adericium.Win32.Imaging.SystemIcons.Information, "Ok");
            }
            else
            {
                try
                {
                    DataGrid_Results.DataContext = result as System.Data.DataTable;
                }
                catch (Exception ex)
                {
                    Adericium.DialogBox.Show(ex.Message, "Query Failed", Adericium.Win32.Imaging.SystemIcons.Error, "Ok");
                }
            }
        }

        private void Button_Commit_Click(object sender, RoutedEventArgs e)
        {
            var response = Adericium.InputBox.Show("Are you sure you want to commit these changes?" + Environment.NewLine + "Type \"commit\" below to confirm.", "Commit Changes?", Adericium.Win32.Imaging.SystemIcons.Exclamation, "No", "Yes");

            if (response.Button == "Yes" && response.Input.ToLower() == "commit") {
                Transaction.Commit(); Transaction.Dispose();
                Transaction = Connection.BeginTransaction();
                QueryHistory.Clear();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e) {
            Transaction?.Rollback(); Transaction?.Dispose();
        }
    }
}
