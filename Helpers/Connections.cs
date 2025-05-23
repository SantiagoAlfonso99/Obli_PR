using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    public class Connections
    {
        public static async Task SendMessage(string message, TcpClient socketClient)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            int datalength = data.Length;
            byte[] dataLength = BitConverter.GetBytes(datalength);
            Send(dataLength,socketClient);
            Send(data,socketClient);
        }

        public static async Task<string> ReceiveMessage(TcpClient socketClient)
        {
            byte[] dataLength = await Receive(4, socketClient);
            byte[] data = await Receive(BitConverter.ToInt32(dataLength), socketClient);
            string message = Encoding.UTF8.GetString(data);
            return message;
        }
        
        public static async Task Send(byte[] data, TcpClient socketClient)
        {
            NetworkStream networkStream = socketClient.GetStream();
            await networkStream.WriteAsync(data,0, data.Length);
        }

        public static async Task<byte[]> Receive(int length, TcpClient socketClient)
        {
            NetworkStream networkStream = socketClient.GetStream();
            int offset = 0;
            var data = new byte[length];
            while (offset < length)
            {
                var received = await networkStream.ReadAsync(
                    data,
                    offset,
                    length - offset);
                if (received == 0)
                    throw new Exception("Connection lost");
                offset += received;
            }
            return data;
        }
    }
}
