using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server;

class Server
{
    static void Main(string[] args)
    {
        IPAddress serverIp = IPAddress.Loopback;
        int serverPort = 1234;
        TcpListener server = new TcpListener(serverIp, serverPort);
        
        server.Start();

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            NetworkStream ns = client.GetStream();
            
            byte[] greeting = new byte[100];
            greeting = Encoding.Default.GetBytes("Welcome to the server!");

            ns.Write(greeting, 0, greeting.Length);

            while (client.Connected)
            {
                byte[] msg = new byte[1024];
                ns.Read(msg, 0, msg.Length);
                Console.WriteLine(Encoding.UTF8.GetString(msg));
            }
        }
    }
}