using System;
using System.Diagnostics;

namespace NTVideoData.Util
{
    class CryptoUtil
    {
        static string password = "1624558d5f8c89aadb89ae0b0fddb5f7";

        public static string encrypt(string plainText)
        {
            return Base64Encode(Base64Encode(plainText) + password);
        }

        public static string decrypt(string base64EncodedData)
        {
            return Base64Decode(Base64Decode(base64EncodedData).Replace(password, ""));
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
