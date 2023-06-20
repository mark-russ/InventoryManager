using System;
using System.Windows.Media;

namespace Inventory_Manager.Objects
{
    public class PictureItem : Adericium.ObservableObject
    {
        private ImageSource image;
        public ImageSource ImageSrc { get { return image; } set { image = value; NotifyPropertyChanged(); } }
    }
}
