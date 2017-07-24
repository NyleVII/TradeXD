using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using TradeXD.Properties;

namespace TradeXD
{
    internal static class PoEHelper
    {
        internal static double XMult = 0.01011904761904761904761904761905;
        internal static double YMult = 0.14952380952380952380952380952381;
        internal static double SizeMult = 0.5847619047619047619047619047619;
        internal static string Uri = @"https://www.pathofexile.com/";

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, ref RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        internal static Rectangle GetClientSize()
        {
            RECT rect = new RECT();
            Point point = new Point();
            if (GetClientSizeByName("PathOfExile", ref rect, ref point))
                return new Rectangle(point.X, point.Y, rect.Width, rect.Height);
            if (GetClientSizeByName("PathOfExile_x64", ref rect, ref point))
                return new Rectangle(point.X, point.Y, rect.Width, rect.Height);
            if (GetClientSizeByName("PathOfExileSteam", ref rect, ref point))
                return new Rectangle(point.X, point.Y, rect.Width, rect.Height);
            if (GetClientSizeByName("PathOfExileSteam_x64", ref rect, ref point))
                return new Rectangle(point.X, point.Y, rect.Width, rect.Height);
            return new Rectangle(0, 0, 0, 0);
        }

        internal static bool GetClientSizeByName(string name, ref RECT rect, ref Point point)
        {
            var processes = Process.GetProcessesByName(name);
            foreach (Process p in processes)
            {
                IntPtr handle = p.MainWindowHandle;
                GetClientRect(handle, ref rect);
                ClientToScreen(handle, ref point);
                return true;
            }
            return false;
        }

        internal static (string Cipher, string Salt) Encrypt(string password)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            password = "";

            var saltBytes = new byte[64];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }

            string cipher = Convert.ToBase64String(
                ProtectedData.Protect(passwordBytes, saltBytes, DataProtectionScope.CurrentUser));
            string salt = Convert.ToBase64String(saltBytes);

            return (cipher, salt);
        }

        internal static string Decrypt(string cipher, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);
            var cipherBytes = Convert.FromBase64String(cipher);

            return Encoding.UTF8.GetString(
                ProtectedData.Unprotect(cipherBytes, saltBytes, DataProtectionScope.CurrentUser));
        }

        public static void SaveCredentials(string email, string password)
        {
            (string cipher, string salt) = Encrypt(password);
            Settings.Default.Email = email;
            Settings.Default.Cipher = cipher;
            Settings.Default.Salt = salt;
            Settings.Default.Save();
        }

        public static (bool successful, string email, string cipher, string salt) LoadCredentials()
        {
            string email = Settings.Default.Email;
            string cipher = Settings.Default.Cipher;
            string salt = Settings.Default.Salt;
            return (email != "" && cipher != "" && salt != "", email, cipher, salt);
        }
    }
}