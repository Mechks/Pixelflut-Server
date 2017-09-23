using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApp2
{
    static class Server 
    {
        volatile public static bool shutdown = false;
        const int PORT = 1234;

        // Opens a server socket and waits for incoming requests
        public static void handleConnections()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 1234);
            listener.Start();
            while (!shutdown)
            {
                if (!listener.Pending())
                {
                    Thread.Sleep(100);
                    continue;
                }
                TcpClient s = listener.AcceptTcpClient(); 
                Thread clientThread = new Thread(() => handleClient(s));
                clientThread.Name = ((IPEndPoint)s.Client.RemoteEndPoint).Address.ToString();
                clientThread.Start();
                Console.WriteLine("Client connected from {0:S}", clientThread.Name);
            }
            listener.Stop();
        }

        static void handleClient(TcpClient client)
        {
            // typical message PX 20 30 ff8800\n or PX 20 30 00ff8800\n
            String clientName = getTcpClientName(client);
            addClient(clientName);
            StreamReader read = new StreamReader(client.GetStream(), Encoding.UTF8);
            try
            {
                while (true)
                {
                    String line = read.ReadLine();

                    if (line == null)
                    {
                        break;
                    }
                    if (line.StartsWith("PX ") && line.Length > 3)
                    {
                        try
                        {
                            int[] point = handleMessage(line.Substring(3));
                            //Task.Run(() =>
                            //{
                                try
                                {
                                    if (calls.ContainsKey(clientName))
                                        calls[clientName]++; // not threadsafe but not relevant for a counter
                                    if (points.ContainsKey(clientName))
                                        points[clientName].Enqueue(point);
                                }
                                catch (KeyNotFoundException) {
                                    // client will close after that
                                }
                            //});
                        }
                        catch (ArgumentException)
                        {
                            // skip
                            continue;
                        }
                    }
                }
                read.Close();
                client.Close();
                removeClient(clientName);
            }
            catch (Exception)
            {
                read.Close();
                client.Close();
                removeClient(clientName);
            }
            Console.WriteLine("Client removed: {0:S}", clientName);
        }

        // Returns int[] { X, Y, Colorcode as ARGB}, ArgumentException if parsing failed
        private static int[] handleMessage(String msg)
        {
            if (msg == null || msg == "")
            {
                throw new ArgumentException("Message has to contain information.");
            }
            String[] msgParts = msg.Trim().Split(' ');
            if (msgParts.Length != 3)
            {
                throw new ArgumentException("Not enough input");
            }

            try
            {
                int[] values = new int[3];
                values[0] = Int32.Parse(msgParts[0]);
                values[1] = Int32.Parse(msgParts[1]);

                String colorcode = msgParts[2];
                if (colorcode.Length == 6)
                {
                    values[2] = Int32.Parse("ff" + colorcode, NumberStyles.HexNumber);
                } else if (colorcode.Length == 8)
                {
                    // reorder from RGBA to ARGB for Color class
                    colorcode = colorcode.Substring(6) + colorcode.Substring(0, 5);
                    values[2] = Int32.Parse(colorcode, NumberStyles.HexNumber);
                }
                return values;
            }
            catch (Exception)
            {
                throw new ArgumentException("Parsing failed of message parts.");
            }
        }

        private static Dictionary<String, int> calls = new Dictionary<string, int>();
        private static Dictionary<String, ConcurrentQueue<int[]>> points = new Dictionary<string, ConcurrentQueue<int[]>>();
        private static List<String> tNames = new List<string>(); // list of keys to query calls thread unsafe
        public static Queue<int[]> balancedPoints = new Queue<int[]>();

        private static void addClient(String client)
        {
            if (client == null)
                return;
            lock (tNames)
            {
                if (!tNames.Contains(client))
                {
                    calls.Add(client, 0);
                    points.Add(client, new ConcurrentQueue<int[]>());
                    tNames.Add(client);
                }
            }
        }

        private static void removeClient(String client)
        {
            if (client == null)
                return;
            lock (tNames)
            {
                calls.Remove(client);
                points.Remove(client);
                tNames.Remove(client);
            }
        }

        private static String getTcpClientName(TcpClient client)
        {
            return ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
        }

        public static void statistics()
        {
            while (!shutdown)
            {
                Thread.Sleep(1000);
                if (calls.Count == 0)
                    continue;
                String text = "";
                lock (tNames)
                {
                    foreach (String key in tNames)
                    {
                        text += key + " : " + calls[key].ToString() + "\n";
                        calls[key] = 0;
                        calls[key] = 0;
                    }
                }
                Console.WriteLine(text);

            }
        }

        /// <summary>
        /// maximal queue size for buffering queue to ui (10M = ~120MB RAM)
        /// </summary>
        const int UI_WAITING_MAX = 10000000; 
        public static void balanceInput()
        {
            while (!shutdown)
            {
                if (tNames.Count == 0)
                {
                    Thread.Sleep(100);
                    continue;
                }

                // clear queues if ui can not handle at the moment
                bool queueFull = balancedPoints.Count > UI_WAITING_MAX;

                ConcurrentQueue<int[]> singleQueue = null;
                List<ConcurrentQueue<int[]>> multiQueue = null;
                lock (tNames)
                {
                    if (tNames.Count == 1)
                    {
                        if (queueFull)
                            points[tNames.First()] = new ConcurrentQueue<int[]>();
                        else
                            singleQueue = points.Values.First();
                    } else if (tNames.Count >= 1)
                    {
                        if (queueFull)
                            tNames.ForEach(n => points[n] = new ConcurrentQueue<int[]>());
                        else
                            multiQueue = new List<ConcurrentQueue<int[]>>(points.Values);
                    }
                }

                

                if (singleQueue != null)
                {
                    int size = singleQueue.Count;

                    for (int i = 0; i < size; i++)
                    {
                        int[] element = null;
                        if (singleQueue.TryDequeue(out element))
                            lock(balancedPoints)
                                balancedPoints.Enqueue(element);
                    }
                }

                if (multiQueue != null)
                {
                    bool queueEmpty = false;
                    while (!shutdown && !queueEmpty)
                    {
                        multiQueue.ForEach(q =>
                        {
                            int[] element = null;
                            if (q.TryDequeue(out element))
                            {
                                lock (balancedPoints)
                                    balancedPoints.Enqueue(element);
                            } else
                            {
                                queueEmpty = true;
                            }
                        });
                    }
                }
            }
        }
    }
    
}
