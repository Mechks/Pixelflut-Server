using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
            bg3 = new Bitmap(panel3.Width, panel3.Height);
            panel3.BackgroundImage = (Image)bg3;
            bg4 = new Bitmap(panel4.Width, panel4.Height);
            panel4.BackgroundImage = (Image)bg3;

            timer1.Interval = 1000 / 2; // fps
            timer1.Tick += new EventHandler(calcFrame);
            timer1.Start();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            // Benchmark: 50k/s
            Bitmap bmp = new Bitmap(panel1.Width, panel1.Height);
            panel1.BackgroundImage = (Image)bmp;
            button1.Enabled = false;
            await Task.Run(() =>
           {
               Console.WriteLine(DateTime.Now);
               int c = 0;
               while (c < 1000000) {
                   lock (bmp)
                   bmp.SetPixel(10, 10, Color.Red);
                   if (c % 100 == 0) // Sleep or it will break some times
                       Thread.Sleep(1);
                   c++;
               }
               Console.WriteLine(DateTime.Now);

           });
            Console.WriteLine("Reach enable");
            button1.Enabled = true;
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            Bitmap b = await Task.Run(() =>
            {
                return createBitmap();
            });
            panel2.BackgroundImage = (Image)b;
            Console.WriteLine("Reach enable");
            button2.Enabled = true;
        }

        private Bitmap createBitmap()
        {
            Console.WriteLine(DateTime.Now);
            Bitmap bmp = new Bitmap(panel1.Width, panel1.Height);
            int c = 0;
            while (c < 1000000)
            {
                lock (bmp)
                    bmp.SetPixel(10, 10, Color.Red);
                //if (c % 100 == 0) // Sleep or it will break some times
                    //Thread.Sleep(1);
                c++;
            }
            Console.WriteLine(DateTime.Now);
            return bmp;
        }

        Bitmap bg3 = null;
        private async void button3_Click(object sender, EventArgs e)
        {
            button3.Enabled = false;

            Bitmap bin = bg3.Clone(new Rectangle(0, 0, bg3.Width, bg3.Height), bg3.PixelFormat);
            Bitmap b = await Task.Run(() =>
            {
                return updateBitmap(bin);
            });
            panel3.BackgroundImage = (Image)b;
            Console.WriteLine("Reach enable {0:B}", bin == b);
            button3.Enabled = true;

            //B.Clone(new Rectangle(0, 0, B.Width, B.Height), B.PixelFormat)
        }

        private Bitmap updateBitmap(Bitmap bmp)
        {
            Console.WriteLine(DateTime.Now);
            int x_max = bmp.Width;
            int y_max = bmp.Height;
            int c = 0;
            int c_max = Math.Min(500000, Server.balancedPoints.Count); // Queue may only increase
            Console.WriteLine(c_max);
            while (c < c_max)
            {
                int[] element = Server.balancedPoints.Dequeue();
                if (element[0] < x_max && element[1] < y_max)
                    bmp.SetPixel(element[0], element[1], Color.FromArgb(element[2]));
                c++;
            }
            Console.WriteLine(DateTime.Now);
            return bmp;
        }

        Bitmap bg4 = null;
        CancellationTokenSource lastSource = new CancellationTokenSource();
        Task<Bitmap> lastUpdate = null;
        private async void calcFrame(Object myObject, EventArgs myEventArgs)
        {
            //Console.WriteLine("Tick");
            if (Server.balancedPoints.Count == 0)
            {
                return;
            }
            var tokensource = new CancellationTokenSource();

            lock (lastSource)
            {
                lastSource.Cancel();
            }
            if (lastUpdate != null)
            {
                await lastUpdate;
                lastSource.Dispose();
                lastUpdate.Dispose();
            }

            // last update task done
            Bitmap bin = bg4.Clone(new Rectangle(0, 0, bg4.Width, bg4.Height), bg4.PixelFormat);
            Task<Bitmap> update = new Task<Bitmap>(() => { return updateBitmapCancellable(bin, tokensource.Token); });
            lock (lastSource)
            {
                lastUpdate = update;
                lastSource = tokensource;
            }
            update.Start();
            Bitmap b = await update;
            panel4.BackgroundImage = b;
            bg4 = b;
        }


        private Bitmap updateBitmapCancellable(Bitmap bmp, CancellationToken t)
        {
            int x_max = bmp.Width;
            int y_max = bmp.Height;
            int c = 0;
            int c_max = Server.balancedPoints.Count; // Queue may only increase
            while (!t.IsCancellationRequested && c < c_max)
            {
                int[] element;
                lock (Server.balancedPoints) // thread safe access, otherwise null elements appear
                    element = Server.balancedPoints.Dequeue();
                if (element[0] < x_max && element[1] < y_max)
                    bmp.SetPixel(element[0], element[1], Color.FromArgb(element[2]));
                c++;
            }
            return bmp;
        }
    }
}
