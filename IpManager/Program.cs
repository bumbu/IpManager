using Parse;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace IpConfig
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ParseClient.Initialize("YOUR APPLICATION ID", "YOUR WINDOWS KEY");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow());
        }
    }
}
