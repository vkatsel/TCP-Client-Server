using System.Net;
using System.Net.Sockets;
namespace Server;

class Server
{
    private const string Greeting = "Welcome to the server! ✪ ω ✪" +
                                    "Connected successfully.";
    private static readonly string StoragePath = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
    static void Main(string[] args)
    {
        Directory.CreateDirectory(StoragePath);
        
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
                string filename;
                switch (commandId)
                {
                    case 1:
                        Console.WriteLine($"[INFO] GET command received. ");
                        filename = reader.ReadString();
                        HandleGet(filename, writer);
                        break;
                    case 2:
                        Console.WriteLine($"[INFO] LIST command recieved");
                        HandleList(writer);
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

    private static void HandleGet(string filename, BinaryWriter writer)
    {
        string path = Path.Combine(StoragePath, filename);
        if (!File.Exists(path))
        {
            writer.Write(false);
            writer.Write("[ERROR] The specified file does not exist. Error code 100.");
            return;
        }
        
        writer.Write(true);

        long fileSize = new FileInfo(path).Length;
        writer.Write(fileSize);

        using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
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

    private static void HandleList(BinaryWriter writer)
    {
        writer.Write(true);

        string[] files = Directory.GetFiles(StoragePath);
        writer.Write(files.Length);
        foreach (string file in files)
        {
            writer.Write(Path.GetFileName(file));
        }
        Console.WriteLine("[INFO] LIST operation executed");
    }
}