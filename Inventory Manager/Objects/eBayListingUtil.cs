using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory_Manager.Objects
{
    public static class eBayListingUtil
    {
        public static String[] FindImages(String name)
        {
            if (App.Settings.Profile.ImageFolder == null) return null;
            return System.IO.Directory.EnumerateFiles(App.Settings.Profile.ImageFolder).Where(f => FileMatches(f, name))?.ToArray();
        }
        

        public static String[] SupportedExtensions = new[] { ".jpg", ".png", ".jpeg", ".gif", ".tif", ".tiff", ".bmp" };

        private static Boolean FileMatches(String file, String searchName)
        {
            String fileName = System.IO.Path.GetFileNameWithoutExtension(file);

            if (fileName != searchName)
            {
                Int32 dashPos = fileName.LastIndexOf('-');

                if (dashPos > 0)
                {
                    fileName = fileName.Substring(0, dashPos);
                    String afterDash = fileName.Substring(dashPos);

                    if (fileName != searchName)
                        return false;
                }
                else
                    return false;
            }

            return SupportedExtensions.Contains(System.IO.Path.GetExtension(file));
        }
    }
}
