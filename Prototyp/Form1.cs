using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        Queue<String> inputQ = null;
        Bitmap bmp = null;


        public Form1(Queue<String> q)
        {
            InitializeComponent();
            inputQ = q;
            updateThread = new Thread(updateUILoop);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Console.WriteLine(DateTime.Now);

            Graphics g = panel1.CreateGraphics();
            
            int c = 0;
            Pen pRed = new Pen(Color.FromArgb(200, 30, 30));
            while (c < 100000)
            {
                
                g.DrawRectangle(pRed, 10, 10, 1, 1);
                
                c++;
            }
            pRed.Dispose();
            Console.WriteLine(DateTime.Now);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Console.WriteLine(DateTime.Now);

            Graphics g = panel1.CreateGraphics();

            Bitmap bmp1 = new Bitmap(panel1.Width, panel1.Height);
            panel1.BackgroundImage = (Image)bmp1;
            panel1.BackgroundImageLayout = ImageLayout.None;
            bmp = bmp1;
            Thread t = new Thread(randomNoise);
            t.Start();
            t.Join();
            //randomNoise();
            /* int c = 0;
            int d = panel1.Width;
            Random r = new Random();
            while (c < 10000000)
            {

                bmp1.SetPixel(r.Next(0,d), 15, Color.Red);
                //bmp.SetPixel(15, 15, Color.Red);

                c++;
            }*/
            //panel1.BackgroundImage = (Image)bmp;
            Console.WriteLine(DateTime.Now);
        }

        void randomNoise()
        {
            bmp = new Bitmap(panel1.Width, panel1.Height);

            int c = 0;
            int d = panel1.Width;
            Random r = new Random();
            while (c < 10000000)
            {

                    bmp.SetPixel(r.Next(0, d), 15, Color.Red);
                
                //bmp.SetPixel(15, 15, Color.Red);

                c++;
            }
            panel1.BackgroundImage = (Image)bmp;

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (updating)
            {
                updating = false;
                updateThread.Suspend();
            } else
            {
                updating = true;
                if (updateThread.ThreadState.Equals(ThreadState.Unstarted))
                {
                    Graphics g = panel1.CreateGraphics();
                    bmp = new Bitmap(panel1.Width, panel1.Height);
                    panel1.BackgroundImage = (Image)bmp;
                    panel1.BackgroundImageLayout = ImageLayout.None;
                    updateThread.Start();
                } else
                {
                    updateThread.Resume();
                }
            }
        }

        volatile Thread updateThread = null;
        volatile bool updating = false;

        private void updateUILoop()
        {

            String colorcode = "";
            int x, y = 0;
            while (true)
            {
                if (inputQ.Count == 0)
                {
                    Thread.Sleep(10);
                    continue;
                }
                String request = inputQ.Dequeue();
                String[] split = request.Split(' ');
                if (split.Length != 3)
                {
                    Console.WriteLine("Not enough input");
                    continue;
                }
                try
                {   

                    if (split[2].Length == 8)
                    {
                        colorcode = split[2].Substring(6) + split[2].Substring(0, 5);
                    } else
                    {
                        colorcode = split[2];
                    }
                    x = Int32.Parse(split[0]);
                    y = Int32.Parse(split[1]);
                    Console.WriteLine("{0:D}, {1:D}, {2:D}", x, y, colorcode);

                    if (x >= bmp.Width || y >= bmp.Height)
                    {
                        continue;
                    }
                    Console.WriteLine("%d, %d, %d", x, y, colorcode);
                    //bmp.SetPixel(x,y, Color.FromArgb(Int32.Parse(colorcode, NumberStyles.HexNumber)));
                    bmp.SetPixel(x, y, Color.Red);
                }
                catch (Exception)
                {
                    //skip line
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int c = 0;
            int d = panel1.Width;
            Random r = new Random();
            while (c < 10000000)
            {

                bmp.SetPixel(r.Next(0, d), 15, Color.Red);
                //bmp.SetPixel(15, 15, Color.Red);

                c++;
            }
        }
    }
}
