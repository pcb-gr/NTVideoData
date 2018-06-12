using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NTVideoData.Util
{
    class UnicodeUtil
    {
        public static string toTitleCase(string str)
        {
            str = str.Trim();
            return str != "" ? str[0].ToString().ToUpper() + str.Substring(1) : str;
        }

        public static string replaceSpecialCharacter(string str)
        {
            str = str.Replace("  ", " ").Replace("Quôc", "Quốc").Trim();
            if (str.ToLower().IndexOf("phim lẻ") != -1 || str.ToLower().IndexOf("phim lẽ") != -1 || str.ToLower().IndexOf("phim bộ") != -1)
            {
                return toTitleCase(str.Replace('ẽ', 'ẻ'));
            }
            else
            {
                return toTitleCase(str.Replace("Phim", "").Replace("phim", "").Replace("PHIM", ""));
            }
        }

        public static string stripInjectionString(string str)
        {
            return str.Replace("'", "");
        }

        public static string removeVietnameseUnicode(string str)
        {
            str = stripInjectionString(str);
            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = str.Normalize(NormalizationForm.FormD);
            str = regex.Replace(temp, String.Empty)
                        .Replace('\u0111', 'd').Replace('\u0110', 'D');
            return str;
        }

        public static string uniformCatName(string str)
        {
            string[] catNames = 
                {
                "Tâm lý",
                "Viễn tưởng",
                "Hài hước",
                "Hành động",
                "Kinh dị",
                "Phiêu lưu",
                "Hoạt hình",
                "TV Show",
                "Anime",
                "Thần thoại",
                "Cổ trang",
                "Thuyết minh",
                "Hình sự",
                "Võ thuật",
                "Chiến tranh",
                "Movie Trailers",
                "Âm nhạc",
                "Chiếu rạp",
                "Tài liệu"
            };
            foreach (string catName in catNames)
            {
                if (str.ToLower().IndexOf(catName.ToLower()) != -1)
                {
                    str = catName;
                }
            }
            return str;
        }

        public static string uniformCountryName(string str)
        {
            string[] countryNames =
            {
                "Mỹ",
                "Khác"
            };
            foreach (string countryName in countryNames)
            {
                if (str.ToLower().IndexOf(countryName.ToLower()) != -1)
                {
                    str = countryName;
                }
            }
            return str;
        }

        public static string replaceSpecialCharThatFolderNameNotAllow(string str)
        {
            string[] specialChar = { "\\", "/", ":", "*", "?", "\"", "<", ">", "|" };
            foreach (string c in specialChar)
            {
                if (str.IndexOf(c) != -1)
                {
                    str = str.Replace(c, "-");
                }
            }
            return str;
        }

        public static string removeSpecialChar(string str)
        {
            string[] specialChar = { "!", "(", ")", "~", "'", "♥" };
            foreach (string c in specialChar)
            {
                if (str.IndexOf(c) != -1)
                {
                    str = str.Replace(c, "");
                }
            }
            return str;
        }

        public static string convertToAlias(string str)
        {
            str = removeSpecialChar(replaceSpecialCharThatFolderNameNotAllow(removeVietnameseUnicode(str.Trim().Replace(" ", "-")).ToLower()));
            while (str.IndexOf("--") != -1) str = str.Replace("--", "-");
            return str;
        }

        public static string replaceWithRegex(string pattern, string input, string replacement)
        {
            Regex rgx = new Regex(pattern);
            string result = rgx.Replace(input, replacement);
            return result;
        }
    }
}
