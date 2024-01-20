using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace BleGattApp
{

    /// <summary>
    /// TcpGameClient
    /// </summary>
    public class TcpGameClient
    {
        /// <summary>
        /// socketConnection
        /// </summary>
        private TcpClient socketConnection;

        private Thread tcpListenerThread;

        private BleGattManager bleGattManager;

        /// <summary>
        /// BleGattManager set
        /// </summary>
        public BleGattManager BLEGattManager
        {
            set { bleGattManager = value; }
        }

        public TcpGameClient()
        {

        }

        /// <summary>
        /// Connect
        /// </summary>
        /// <returns></returns>
        public bool Connect()
        {
            socketConnection = null;
            try
            {
                socketConnection = new TcpClient("localhost", 9999);
            }
            catch (SocketException socketExpection)
            {
                return false;
                Console.WriteLine("TcpGameClient - Connect() --> Error " + socketExpection.Message);
            }

            tcpListenerThread = new Thread(new ThreadStart(ListenData));
            tcpListenerThread.IsBackground = true;
            tcpListenerThread.Start(); 

            // TODO : Return false if socketConnectio is null, if socketconnection is not null then return true
            /*if(socketConnection.Connected)*/
            return true;
        }

        
        /// <summary>
        /// ListenData, listen data from Unity
        /// </summary>
        private void ListenData()
        {
            using (NetworkStream stream = socketConnection.GetStream())
            {
                Byte[] bytes = new Byte[4];
                int length;
                // Read incoming stream into byte array
                while((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    if(bleGattManager.WriteCharacteristic != null)
                    { 
                        Debug.WriteLine("TcpGameClient -> ListenData() --> Unity sending reading from player" + BitConverter.ToInt32(bytes, 0));
                        Task.Run(async () => await bleGattManager.WriteCharacteristic.WriteValueAsync(bytes.AsBuffer(), GattWriteOption.WriteWithoutResponse));

                    }
                    //stream.Write(bytes, 0, length);
                   
                }
            }
        }
        

        /// <summary>
        /// SendMessage
        /// </summary>
        public void SendMessage(byte[] command)
        {
            if (socketConnection == null && !Connect())
            {
                return;
            }

            // TODO: Check if socketConnection is null then return, do not try to send message to unity server
            try
            {
                // Get a stream object for writing.
                if (socketConnection == null) return;
                NetworkStream stream = socketConnection.GetStream();
                if (stream.CanWrite)
                {
                    // Write byte array to socketConnection stream.                 
                    stream.Write(command, 0, command.Length);
                    int value = BitConverter.ToInt32(command, 0);

                    //Console.WriteLine("TcpGameClient - SendMessage() --> Send" + value);
                }
            }
            catch (SocketException socketException)
            {
                Console.WriteLine("TcpGameClient - SendMessage() --> Error " + socketException);
                Connect();
            }
            catch (System.IO.IOException)
            {
                socketConnection.Close();
                Connect();
            }
        }
    }
}
