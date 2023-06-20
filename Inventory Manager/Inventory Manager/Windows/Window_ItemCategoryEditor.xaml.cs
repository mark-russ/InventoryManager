using Adericium;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for Window_CreateItemBehavior.xaml
    /// </summary>
    public partial class Window_ItemCategoryEditor : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] String propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Boolean IsEditMode { get; set; }
        
        private Objects.ItemCategory itemCategory;
        public Objects.ItemCategory ItemCategory
        {
            get
            {
                return itemCategory;
            }
            set
            {
                itemCategory = value;
                OnPropertyChanged();
            }
        }

        public Window_ItemCategoryEditor()
        {
            InitializeComponent();
        }

        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Button_Preview_Click(object sender, RoutedEventArgs e)
        {
            String description = ItemCategory.DescriptionTemplate;

            List<String> problems = new List<String>();

            if (description.Contains("{TITLE}") == false)
            {
                problems.Add("Missing {TITLE} placeholder.");
            }
            else
            {
                description = description.Replace("{TITLE}", "Rite Aid First Aid Ultra Absorbent Dressing Large 5ct");
            }

            if (description.Contains("{UPC}") == false)
            {
                problems.Add("Missing {UPC} placeholder.");
            }
            else
            {
                description = description.Replace("{UPC}", "011822543125");
            }

            if (description.Contains("{EXTRA}") == false)
            {
                problems.Add("Missing {EXTRA} placeholder.");
            }
            else
            {
                description = description.Replace("{EXTRA}", "04/2017-09/2017");
            }

            if (description.Contains("{PRICE}") == false)
            {
                problems.Add("Missing {PRICE} placeholder.");
            }
            else
            {
                description = description.Replace("{PRICE}", "$5.50 - $7.50");
            }

            if (problems.Count != 0)
            {
                String message = "The description may have issues:";

                foreach (String problem in problems)
                {
                    message += Environment.NewLine + problem;
                }

                if (DialogBox.Show(message, "Potential issues", Win32.Imaging.SystemIcons.Warning, "Continue", "Cancel") == "Cancel")
                {
                    return;
                }
            }

            String fileName = System.IO.Path.GetTempPath() + "Inventory Manager - Description Preview.html";

            if (System.IO.File.Exists(fileName))
            {
                System.IO.File.Delete(fileName);
            }

            using (var file = System.IO.File.CreateText(fileName))
            {
                file.Write(description);

                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo = new System.Diagnostics.ProcessStartInfo(fileName);
                    process.Start();
                }
            }
        }
    }
}
