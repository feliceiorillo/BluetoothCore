using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vestervang.DotNetBlueZ;

namespace BluetoothCore
{
    public class Bluetooth
    {
        private readonly IAdapter1 _adapter1;
        private readonly TimeSpan timeout = TimeSpan.FromSeconds(15);
        public Bluetooth(IAdapter1 adapter1)
        {
            _adapter1 = adapter1;
        }

        public async Task<IReadOnlyList<Device>> ScanAsync()
        {
            return await _adapter1.GetDevicesAsync();
        }


        public async Task<IReadOnlyList<IGattCharacteristic1>> RetrieveGattServiceCharacteristics(Device device, string serviceUUID)
        {
            IGattService1 service = await device.GetServiceAsync(serviceUUID);
            if (service == null)
                Console.WriteLine($"Service for uuid {serviceUUID} is null");
            else
                Console.WriteLine($"Service {serviceUUID} NOT null");

            return await service.GetCharacteristicsAsync();
        }

        public async Task<string> ReadGattCharacteristicValue(Device device, IGattCharacteristic1 characteristic)
        {
            byte[] value = await characteristic.ReadValueAsync(timeout);

            return Encoding.UTF8.GetString(value);
        }

        public async Task Connect(Device device)
        {

            await device.ConnectAsync();
            //_ = device.ConnectAsync();
            Console.WriteLine("I am inside Connect");

            //await device.WaitForPropertyValueAsync("Connected", true, timeout);
            //await device.WaitForPropertyValueAsync("ServicesResolved", true, timeout);
            //Console.WriteLine("I am inside Connect after waitforproperty");

        }

        public async Task Disconnect(Device device)
        {
            await device.DisconnectAsync();
        }

        public async Task<bool> IsConnected(Device device)
        {
            return await device.GetConnectedAsync();
        }

        public async Task SendData(Device device, string data)
        {
            string serviceUUID = "0000ffe0-0000-1000-8000-00805f9b34fb";
            string characteristicUUID = "0000ffe1-0000-1000-8000-00805f9b34fb";

            IGattService1 service = await device.GetServiceAsync(serviceUUID);
            Console.WriteLine("After serivceGat");
            if (service == null)
                Console.WriteLine("service is null");

            IGattCharacteristic1 characteristic = await service.GetCharacteristicAsync(characteristicUUID);
            if (characteristic == null)
            {
                Console.WriteLine("characteristic is null");
                return;
            }
            Console.WriteLine("I am inside senddata");
            await characteristic.WriteValueAsync(Encoding.UTF8.GetBytes(data), new Dictionary<string, object>());
        }

    }
}
