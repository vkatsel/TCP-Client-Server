using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client;

class Client
{
    static void Main(string[] args)
    {
        try
        {
            TcpClient client = new TcpClient(IPAddress.Loopback.ToString(), 1234);
            NetworkStream ns = client.GetStream();
            
            Console.WriteLine("Connected to server!");

            byte[] buffer = new byte[1024];
            int bytesRead = ns.Read(buffer, 0, buffer.Length);
            Console.WriteLine($"Server: {Encoding.UTF8.GetString(buffer, 0, bytesRead)}");

            while (true)
            {
                Console.WriteLine("Enter message or 'exit': ");
                string message = Console.ReadLine();
                if (string.IsNullOrEmpty(message) || message == "exit") break;

                byte[] data = Encoding.UTF8.GetBytes(message);
                ns.Write(data, 0, data.Length);
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Connection failed: {e.Message}");
        }
    }
}