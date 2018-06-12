using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTVideoData_v1.Utils
{
    class NumberUtil
    {
        public static double GetDouble(string value)
        {
            //value = value.Replace(".", ",");
            return double.Parse(value, CultureInfo.InvariantCulture);
        }
    }
}
