
using BluetoothCore;
using HashtagChris.DotNetBlueZ;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;


await Server.Main();

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



