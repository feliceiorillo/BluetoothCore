
using BluetoothCore;
using Fleck;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Resources;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.WebSockets;
using Websocket.Client;


//var exitEvent = new ManualResetEvent(false);
//var url = new Uri("ws://192.168.1.105:8765");

//using (var client = new WebsocketClient(url))
//{
//    client.ReconnectTimeout = TimeSpan.FromSeconds(30);
//    client.ReconnectionHappened.Subscribe(info =>

//    client.MessageReceived.Subscribe());


//    client.Start();

//    Task.Run(() => client.Send("{ message }"));


//    exitEvent.WaitOne();
//}

await Server.Main(args.Length > 0 ? true : false);


//FleckLog.Level = LogLevel.Debug;
//var socket = new WebSocketServer("wss://0.0.0.0:2800/");
//socket.EnabledSslProtocols = SslProtocols.Tls13;
//socket.Certificate = new X509Certificate2("realarenabe_ddns_net.pfx");
//socket.Start(conn =>
//{
//    conn.OnOpen = () =>
//    {
//        // Metodo eseguito all'apertura della connessione
//         // Stampa il messaggio

//    };
//    conn.OnMessage = message =>
//    {
//        // Metodo eseguito alla ricezione di un messaggio
//        // La stringa 'message' rappresenta il messaggio
//        Console.WriteLine(message); // Stampa il messaggio
//    };
//    conn.OnClose = () =>
//    {
//        // Metodo eseguito alla chiusura della connessione
//    };
//});
while (true) { }

//var list = await b.ScanAsync();
//var firstDevice = list.First();

//string[] uuids = await firstDevice.GetUUIDsAsync();
//Console.WriteLine(await firstDevice.GetNameAsync());
//uuids = await firstDevice.GetUUIDsAsync();
//await b.Connect(firstDevice);
//while(true)
//{
//    var input = Console.ReadLine();
//    if (input == "0")
//        break;

//    await b.SendData(firstDevice, input?.ToString() ?? "");

//}
//await b.Disconnect(firstDevice);
//Console.WriteLine("Disconnected");

//foreach (var uuid in uuids)
//{
//    Console.WriteLine(uuid);
//    var characteristics = await b.RetrieveGattServiceCharacteristics(firstDevice, uuid);

//    if (characteristics == null)
//        continue;

//    foreach (var characteristic in characteristics)
//    {

//        var chrUUID = await characteristic.GetUUIDAsync();
//        var srvUUID = await characteristic.GetServiceAsync();
//        var chrFlags = await characteristic.GetFlagsAsync();
//        Console.WriteLine("service: " + srvUUID);
//        Console.WriteLine("characteristic: " + chrUUID);
//        Console.WriteLine("characteristic flags: " + String.Join(";", chrFlags));
//        try
//        {
//            var gatCharacteristic = await b.ReadGattCharacteristicValue(firstDevice, characteristic);

//            if (gatCharacteristic != null)
//                Console.WriteLine("gatCharacteristic" + gatCharacteristic);
//            else
//                Console.WriteLine("gatCharacteristic is null");
//        }
//        catch (Exception ex)
//        {

//        }

//    }





//}



