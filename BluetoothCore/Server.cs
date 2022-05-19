using Fleck;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using vestervang.DotNetBlueZ;
using Websocket.Client;

namespace BluetoothCore
{
    public class Server
    {
        private static int clients = 0;
        private static int _timeout = 30;
        static Thread clientTcpThread, clientBlThread1, clientBlThread2;
        private readonly static object _lock = new object();
        private readonly static Queue<string> _queue = new Queue<string>();
        private readonly static AutoResetEvent _signal = new AutoResetEvent(true);
        private static bool _disconnected = false;


        private readonly static object _lock2 = new object();
        private readonly static Queue<string> _queue2 = new Queue<string>();
        private readonly static AutoResetEvent _signal2 = new AutoResetEvent(true);
        private static bool _disconnected2 = false;

        private static IReadOnlyList<Adapter?> _adapters;
        private static Adapter _adapter;
        private static Bluetooth _b;
        private static Bluetooth _b2;
        private static IReadOnlyList<Device?> _list;

        const string RASPY_WS_SERVERURL = "ws://192.168.1.105:8765";
        const string RASPY_WS_SERVERURL2 = "ws://192.168.1.105:8766";

        public static async Task Main(bool isdebug = false)
        {

            IConfiguration config = new ConfigurationBuilder()
                        .AddJsonFile("settings.json")
                        .Build();

            String ip = config.GetSection("ip").Value;
            int port = Convert.ToInt32(config.GetSection("port").Value);
            _timeout = Convert.ToInt32(config.GetSection("timeout").Value);
            string ws = "wss";

            if (isdebug)
                ws = "ws";


            new Thread(() => ClientPythonThread()).Start();
            new Thread(() => ClientPythonThread2()).Start();


            // Set the TcpListener on port 
            IPAddress localAddr = IPAddress.Parse(ip);
            FleckLog.Level = LogLevel.Debug;
            var server1 = new WebSocketServer($"{ws}://{ip}:{port}/");
            var server2 = new WebSocketServer($"{ws}://{ip}:{port + 1}/");

            if (!isdebug)
            {

                server1.EnabledSslProtocols = SslProtocols.Tls13;
                server1.Certificate = new X509Certificate2("realarenabe_ddns_net.pfx");
                server2.EnabledSslProtocols = SslProtocols.Tls13;
                server2.Certificate = new X509Certificate2("realarenabe_ddns_net.pfx");
            }
           

            server1.Start(conn =>
            {
                conn.OnOpen = () =>
                {
                    // Metodo eseguito all'apertura della connessione
                    // Stampa il messaggio
                    Console.WriteLine($"open1");

                };
                conn.OnMessage = message =>
                {
                    Console.WriteLine($"sendin {message}");
                    lock (_lock)
                    {
                        _queue.Enqueue(message);
                        _signal.Set();
                    }
                };
                conn.OnClose = () =>
                {
                    // Metodo eseguito alla chiusura della connessione
                };
            });
            ;
            server2.Start(conn =>
            {
                conn.OnOpen = () =>
                {
                    Console.WriteLine($"open1");
                    // Metodo eseguito all'apertura della connessione
                    // Stampa il messaggio

                };
                conn.OnMessage = message =>
                {
                    Console.WriteLine($"sendin {message}");
                    lock (_lock2)
                    {
                        _queue2.Enqueue(message);
                        _signal2.Set();
                    }
                };
                conn.OnClose = () =>
                {
                    // Metodo eseguito alla chiusura della connessione
                };
            });

            Console.WriteLine($"Server started on {ip} {port}");
            Console.WriteLine($"Server2 started on {ip} {port + 1}");

        }


        private static async void ClientPythonThread()
        {
            var exitEvent = new ManualResetEvent(false);
            var url = new Uri(RASPY_WS_SERVERURL);
            using (var client = new WebsocketClient(url))
            {
                client.ReconnectTimeout = TimeSpan.FromSeconds(30);
                client.ReconnectionHappened.Subscribe(info =>

                client.MessageReceived.Subscribe());


                await client.Start();

                while (true)
                {
                    // wait to be notified
                    _signal.WaitOne();
                    string item = null;

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

                            Task.Run(() => client.Send(item));

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Can't senddata bl: {ex.Message}");

                        }
                    }
                } 


                exitEvent.WaitOne();
            }
        }


        private static async void ClientPythonThread2()
        {
            var exitEvent = new ManualResetEvent(false);
            var url = new Uri(RASPY_WS_SERVERURL2);
            using (var client = new WebsocketClient(url))
            {
                client.ReconnectTimeout = TimeSpan.FromSeconds(30);
                client.ReconnectionHappened.Subscribe(info =>

                client.MessageReceived.Subscribe());


                await client.Start();

                while (true)
                {
                    // wait to be notified
                    _signal2.WaitOne();
                    string item = null;

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

                            Task.Run(() => client.Send(item));

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Can't senddata bl: {ex.Message}");

                        }
                    }
                }


                exitEvent.WaitOne();
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
                            if (b == null || await b.IsConnected(device))
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
            //Console.WriteLine($"Getting bluetooth adapters");


            //Console.WriteLine($"Bluetooth adapters list: {_adapters.Count}");

            if (_adapters == null)
            {
                _adapters = await BlueZManager.GetAdaptersAsync();
            }
            if (_adapters == null)
            {
                //_adapter = await BlueZManager.GetAdapterAsync("hci0");
            }
            //var name = await _adapters.FirstOrDefault().GetNameAsync();
            //Console.WriteLine($"Adapter name {name} adapters count {_adapters.Count}");
            _b = new Bluetooth(_adapters.FirstOrDefault());
            Console.WriteLine($"Bluetooth istantiated");
            _list = await _b.ScanAsync();
            Console.WriteLine($"Bluetooth scanned list of devices: {_list.Count}");

            var deviceProperties = await _list.First().GetAllAsync();
            var deviceProperties2 = await _list.Last().GetAllAsync();
            string jsonDeviceProperties = JsonConvert.SerializeObject(deviceProperties);
            Console.WriteLine($"properties first device: {jsonDeviceProperties}");
            await _b.Connect(number == 1 ? _list.First() : _list.Last());
            Console.WriteLine($"Bluetooth connected number {number}");


            var device = number == 1 ? _list.First() : _list.Last();
            Console.WriteLine($"Reading device properties number: {number} {device.ObjectPath}");

            //var deviceProperties = await device.GetAllAsync();
            //string jsonDeviceProperties = JsonConvert.SerializeObject(deviceProperties);
            //Console.WriteLine("Trying to get again adapters");
            //var adapter = await BlueZManager.GetAdapterAsync("raspberrypi");


            //Console.WriteLine("Istantiating a new bluetooth class");

            //var b = new Bluetooth(_adapter);
            //Console.WriteLine("After istantiate a new bluetooth class try to rescan");

            //var res = await b.ScanAsync();

            //Console.WriteLine($"Result of second scan: {res.Count}");
            
            ////device.Dispose();

            //Console.WriteLine($"Trying to connect bl class just created {device.ObjectPath}");
            //await b.Connect(res.First());

            //Console.WriteLine($"Trying to connect bl class aldready created {device.ObjectPath}");

            //await _b.Connect(device);


            return new CustomTuple(_b, device);
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
