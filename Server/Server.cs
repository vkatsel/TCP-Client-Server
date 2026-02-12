using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server;

class Server
{
    private const string Greeting = "Welcome to the server! ✪ ω ✪" +
                                    "Connected successfully.";
    static void Main(string[] args)
    {
        IPAddress serverIp = IPAddress.Loopback;
        int serverPort = 1234;
        TcpListener server = new TcpListener(serverIp, serverPort);
        
        server.Start();

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine($"[INFO] Client connected!");

            HandleClient(client);
        }
    }

    static void HandleClient(TcpClient client)
    {
        NetworkStream ns = client.GetStream();
        var isConnected = client.Connected;
        BinaryReader reader = new BinaryReader(ns);
        BinaryWriter writer = new BinaryWriter(ns);
        
        writer.Write(Greeting);
        
        while (isConnected)
        {
            try
            {
                byte commandId = reader.ReadByte();
                switch (commandId)
                {
                    case 1:
                        Console.WriteLine($"[INFO] Get command received. ");
                        string filepath = reader.ReadString();
                        HandleGet(filepath, writer);
                        break;
                    case 2:
                        break;
                    case 3:
                        break;
                    case 4:
                        break;
                    case 5:
                        break;
                    case 0:
                        isConnected = false;
                        Console.WriteLine($"[INFO] Closing the connection. Command id {commandId}");
                        break;
                }
            }
            catch (EndOfStreamException e)
            {
                Console.WriteLine($"[ERROR] Stream Error: {e.Message}");
                isConnected = false;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Error arose: {e.Message}");
                isConnected = false;
            }
        } client.Close();
    }

    static void HandleGet(string filepath, BinaryWriter writer)
    {
        if (!File.Exists(filepath))
        {
            writer.Write(false);
            writer.Write("[ERROR] The specified file does not exist. Error code 100.");
            return;
        }
        
        writer.Write(true);

        long fileSize = new FileInfo(filepath).Length;
        writer.Write(fileSize);

        using (FileStream fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
        {
            byte[] buffer = new byte[4096];
            int bytesRead;

            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                writer.Write(buffer, 0, bytesRead);
                writer.Flush();
            }
        }
        Console.WriteLine("[INFO] File transmission complete");
    }
}