using System;
using System.Windows.Forms;

namespace TradeXD
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            OverlayForm form = new OverlayForm();
            Application.Run();
        }
    }
}