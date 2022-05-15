using HashtagChris.DotNetBlueZ;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BluetoothCore
{
    public class Server
    {
        private static int clients = 0;
        private static int _timeout = 30;
        static Thread clientTcpThread, clientBlThread;
        private readonly static object _lock = new object();
        private readonly static Queue<string> _queue = new Queue<string>();
        private readonly static AutoResetEvent _signal = new AutoResetEvent(true);
        private static bool _disconnected = false;


        private readonly static object _lock2 = new object();
        private readonly static Queue<string> _queue2 = new Queue<string>();
        private readonly static AutoResetEvent _signal2 = new AutoResetEvent(true);
        private static bool _disconnected2 = false;

        public static async Task Main()
        {
            TcpListener server = null;
            TcpListener server2 = null;

            IConfiguration config = new ConfigurationBuilder()
                        .AddJsonFile("settings.json")
                        .Build();

            String ip = config.GetSection("ip").Value;
            int port = Convert.ToInt32(config.GetSection("port").Value);
            _timeout = Convert.ToInt32(config.GetSection("timeout").Value);

            // Set the TcpListener on port 
            IPAddress localAddr = IPAddress.Parse(ip);

            // TcpListener server = new TcpListener(port);
            server = new TcpListener(localAddr, port);
            server2 = new TcpListener(localAddr, port + 1);

            // Start listening for client requests.
            server.Start();
            server2.Start();
            Console.WriteLine($"Server started on {ip} {port}");
            Console.WriteLine($"Server2 started on {ip} {port + 1}");

            Task.Run(() => Robot1(server));
            Task.Run(() => Robot2(server2));

            while (true)
            {

            }
        }

        private async static Task Robot1(TcpListener server)
        {
            // we set up max client number
            while (true)
            {
                if (clients >= 2)
                    continue;

                TcpClient clientTCP = server.AcceptTcpClient();


                Console.WriteLine("A client connected.");
                clientTcpThread = new Thread(new ParameterizedThreadStart(HandleTcpComm));
                clientBlThread = new Thread(new ParameterizedThreadStart(HandleBlcomm1));

                clients += 1;

                clientTcpThread.Start(clientTCP);
                clientBlThread.Start(clientBlThread);

            }
        }

        private async static Task Robot2(TcpListener server2)
        {
            // we set up max client number
            while (true)
            {
                if (clients >= 2)
                    continue;

                TcpClient clientTCP = server2.AcceptTcpClient();


                Console.WriteLine("A client connected.");
                clientTcpThread = new Thread(new ParameterizedThreadStart(HandleTcpComm));
                clientBlThread = new Thread(new ParameterizedThreadStart(HandleBlcomm2));

                clients += 1;

                clientTcpThread.Start(clientTCP);
                clientBlThread.Start(clientBlThread);

            }
        }

        private static void HandleTcpComm(object client)
        {

            DateTime lastActiveConnection = DateTime.Now;


            TcpClient tcpClient = (TcpClient)client;
            //clientBlThread.Join(2000);

            //_ = b.Connect(device);

            //Console.WriteLine($"Bluetooth connected");

            while (true)
            {
                Console.WriteLine(clients);

                NetworkStream stream = tcpClient.GetStream();

                //bluetooth connection at startup

                while (!stream.DataAvailable)
                {
                    //if idle for more then 30 sec, close the connection
                    if ((DateTime.Now - lastActiveConnection).Seconds > _timeout)
                    {
                        tcpClient.Close();
                        Console.WriteLine("Closing thread for inactivity");
                        Thread.CurrentThread.DisableComObjectEagerCleanup();
                        clients -= 1;
                        _signal.Set();
                        _disconnected = true;
                        return;
                    }
                };
                while (tcpClient.Available < 3) ; // match against "get"

                lastActiveConnection = DateTime.Now;
                byte[] bytes = new byte[tcpClient.Available];
                try
                {
                    stream.Read(bytes, 0, tcpClient.Available);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                string s = Encoding.UTF8.GetString(bytes);

                if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
                {
                    Console.WriteLine("=====Handshaking from client=====\n{0}", s);

                    // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                    // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                    // 3. Compute SHA-1 and Base64 hash of the new value
                    // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                    string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                    string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                    byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                    string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                    // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                    byte[] response = Encoding.UTF8.GetBytes(
                        "HTTP/1.1 101 Switching Protocols\r\n" +
                        "Connection: Upgrade\r\n" +
                        "Upgrade: websocket\r\n" +
                        "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                    stream.Write(response, 0, response.Length);
                }
                else
                {
                    bool fin = (bytes[0] & 0b10000000) != 0,
                        mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"

                    int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                        msglen = bytes[1] - 128, // & 0111 1111
                        offset = 2;

                    if (msglen == 126)
                    {
                        // was ToUInt16(bytes, offset) but the result is incorrect
                        msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                        offset = 4;
                    }
                    else if (msglen == 127)
                    {
                        Console.WriteLine("TODO: msglen == 127, needs qword to store msglen");
                        // i don't really know the byte order, please edit this
                        // msglen = BitConverter.ToUInt64(new byte[] { bytes[5], bytes[4], bytes[3], bytes[2], bytes[9], bytes[8], bytes[7], bytes[6] }, 0);
                        // offset = 10;
                    }

                    if (msglen == 0)
                        Console.WriteLine("msglen == 0");
                    else if (mask)
                    {
                        byte[] decoded = new byte[msglen];
                        byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                        offset += 4;

                        for (int i = 0; i < msglen; ++i)
                            decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

                        string text = Encoding.UTF8.GetString(decoded);
                        lock (_lock)
                        {
                            if(text.StartsWith("{") && text.EndsWith("|"))
                            _queue.Enqueue(text);
                        }

                        // notify the waiting thread
                        _signal.Set();
                        Console.WriteLine("{0}", text);
                    }
                    else
                        Console.WriteLine("mask bit not set");

                    Console.WriteLine();
                }
            }




        }

        private static async void HandleBlcomm1(object client)
        {
            Bluetooth b = null;
            Device device = null;
            // try to connect
            try
            {
                var tuple = await Connect();
                b = tuple.Bluetooth;
                device = tuple.Device;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Can't connect bl: {ex.Message}");
            }

            while (true)
            {
                // wait to be notified
                _signal.WaitOne();
                string item = null;

                if (_disconnected)
                {
                    await b.Disconnect(device);
                    _disconnected = false;
                }

                do
                {
                    item = null;

                    // fetch the item,
                    // but only lock shortly
                    lock (_lock)
                    {
                        if (_queue.Count > 0)
                        {
                            item = _queue.Peek();

                            _queue.Clear();
                        }
                    }

                    if (item != null)
                    {
                        // do stuff
                        Console.WriteLine($"item: {item}");

                        try
                        {
                            // if null try to connect again
                            if(b == null || await b.IsConnected(device))
                            {
                                var tuple = await Connect();
                                b = tuple.Bluetooth;
                                device = tuple.Device;

                            }
                            await b.SendData(device, item);

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Can't senddata bl: {ex.Message}");

                        }
                    }
                }
                while (item != null); // loop until there are items to collect
            }
        }

        private static async void HandleBlcomm2(object client)
        {
            Bluetooth b = null;
            Device device = null;
            // try to connect
            try
            {
                var tuple = await Connect(2);
                b = tuple.Bluetooth;
                device = tuple.Device;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Can't connect bl: {ex.Message}");
            }

            while (true)
            {
                // wait to be notified
                _signal2.WaitOne();
                string item = null;

                if (_disconnected2)
                {
                    await b.Disconnect(device);
                    _disconnected2 = false;
                }

                do
                {
                    item = null;

                    // fetch the item,
                    // but only lock shortly
                    lock (_lock2)
                    {
                        if (_queue2.Count > 0)
                        {
                            item = _queue2.Peek();

                            _queue2.Clear();
                        }
                    }

                    if (item != null)
                    {
                        // do stuff
                        Console.WriteLine($"item: {item}");

                        try
                        {
                            // if null try to connect again
                            if (b == null || await b.IsConnected(device))
                            {
                                var tuple = await Connect(2);
                                b = tuple.Bluetooth;
                                device = tuple.Device;

                            }
                            await b.SendData(device, item);

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Can't senddata bl: {ex.Message}");

                        }
                    }
                }
                while (item != null); // loop until there are items to collect
            }
        }
        private static async Task<CustomTuple> Connect(int number = 1)
        {
            Console.WriteLine($"Getting bluetooth adapters");

            var adapters = await BlueZManager.GetAdaptersAsync();
            Console.WriteLine($"Bluetooth adapters list: {adapters.Count}");

            var b = new Bluetooth(adapters.FirstOrDefault());
            Console.WriteLine($"Bluetooth istantiated");
            var list = await b.ScanAsync();
            Console.WriteLine($"Bluetooth scanned list of devices: {list.Count}");


            var device = number == 1 ? list.First() : list.Last();
            var deviceProperties = await device.GetAllAsync();
            string jsonDeviceProperties = JsonConvert.SerializeObject(deviceProperties);
            Console.WriteLine($"Trying to connect bl {jsonDeviceProperties}");

            await b.Connect(device);


            Console.WriteLine("Bluetooth connected");
            return new CustomTuple(b, device);
        }

        public class CustomTuple
        {
            public CustomTuple(Bluetooth bluetooth, Device device)
            {
                Bluetooth = bluetooth;
                Device = device;
            }

            public Bluetooth Bluetooth { get; set; }
            public Device Device { get; set; }
        }
    }
}
