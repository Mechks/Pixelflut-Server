using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    static class Program
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Thread serverT = new Thread(() => Server.handleConnections());
            Thread statT = new Thread(() => Server.statistics());
            Thread balanceT = new Thread(() => Server.balanceInput());
            serverT.Start();
            statT.Start();
            balanceT.Start();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow());

            Server.shutdown = true;
            serverT.Join();
            statT.Join();
            balanceT.Join();
        }
    }
}
