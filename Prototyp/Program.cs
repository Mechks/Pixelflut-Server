using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    static class Program
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Queue<String> uiQueue = new Queue<String>();
            RequestHandler rq = new RequestHandler(uiQueue);
            Thread serverThread = new Thread(rq.handle);
            serverThread.Start();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(uiQueue));
        }
    }


    class RequestHandler
    {
        public void handle()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 1234);
            listener.Start();
            while (!isInterrupted)
            {
                TcpClient s = listener.AcceptTcpClient(); // may deadlock here, timeout somehow needed
                ClientHandler c = new ClientHandler(s, uiQueue);
                Thread clientThread = new Thread(c.handleClient);
                clientThread.Start();
            }
            listener.Stop();
        }

        void requestStop()
        {
            isInterrupted = true;
        }
        volatile bool isInterrupted = false;
        private Queue<string> uiQueue;

        public RequestHandler(Queue<string> uiQueue)
        {
            this.uiQueue = uiQueue;
        }
    }


    class ClientHandler
    {
        private Queue<String> q = null;
        private TcpClient s = null;
        public ClientHandler(TcpClient s, Queue<String> q)
        {
            this.s = s;
            this.q = q;
            Console.WriteLine("Client at " + s.ToString());
        }

        public void handleClient()
        {
            // typical message PX 20 30 ff8800\n or PX 20 30 00ff8800\n

            StreamReader read = new StreamReader(s.GetStream(), Encoding.UTF8);
            try
            {
                while (true)
                {
                    String line = read.ReadLine();
                    
                    if (line == null)
                    {
                        throw new IOException();
                    }
                    if (line == "")
                    {
                        continue;
                    }
                    if (line.StartsWith("PX ") && line.Length > 3)
                    {
                        q.Enqueue(line.Substring(3).Trim());
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("End of input");
                read.Close();
                s.Close();
            }
        }
    }

    public class UIUpdater {

        public UIUpdater(Queue<String> q, System.Drawing.Bitmap bmp)
        {

        }

       }
}
