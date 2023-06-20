using System;
using System.Text.RegularExpressions;

namespace Inventory_Manager
{
    public static class Utility
    {
        private static Regex regexUPC = new Regex("^(\\d{8}|\\d{12,14})$");

        public static Boolean IsValidUPC(String upc)
        {
            if (regexUPC.IsMatch(upc) == false)
                return false;

            Int32 checkDigit = Convert.ToInt32(Char.GetNumericValue(upc[upc.Length - 1]));

            if (upc.Length < 14) {
                upc = upc.PadLeft(14, '0');
            }
            
            Int32[] sums = new Int32[2];

            for (Int32 i = 0; i < upc.Length - 1; i++)
            {
                if (i % 2 == 1) {
                    sums[1] += Convert.ToInt32(Char.GetNumericValue(upc[i]));
                }
                else {
                    sums[0] += Convert.ToInt32(Char.GetNumericValue(upc[i]));
                }
            }

            Int32 result = ((sums[0] * 3) + sums[1]) % 10;

            if (result != 0) {
                result = 10 - result;
            }
            
            return result == checkDigit;
        }
    }
}
