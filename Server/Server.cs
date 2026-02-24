using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
namespace Server;

class Server
{
    private const string Greeting = "Welcome to the server! ✪ ω ✪" +
                                    "Please, enter your name to continue: ";
    private static readonly string Root = Directory.GetCurrentDirectory();
    private static ConcurrentDictionary<string, int> _serverStats = new ConcurrentDictionary<string, int>();
    static void Main()
    {
        IPAddress serverIp = IPAddress.Loopback;
        int serverPort = 1500;
        TcpListener server = new TcpListener(serverIp, serverPort);
        
        server.Start();
        
        IPEndPoint localEndPoint = (IPEndPoint)server.LocalEndpoint;
        Console.WriteLine("[INFO] The server is started!");
        Console.WriteLine($"[INFO] Listening on {localEndPoint.Address}:{localEndPoint.Port}");
        Console.WriteLine("Available Admin Commands:" +
                          "1. stop - stops the server" +
                          "2. info - prints out the statistics");
        
        Thread readingThread = new Thread(() => HandleAdminCommands(server));
        readingThread.Start();

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine($"[INFO] Client connected!");

            Thread thread = new Thread(() => HandleClient(client));
            thread.Start();
        }
    }

    static void HandleAdminCommands(TcpListener server)
    {
        while (true)
        {
            string input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;

            switch (input.Trim().ToLower())
            {
                case "stats":
                    Console.WriteLine("\n=== SERVER STATISTICS ===");
                    foreach (var stat in _serverStats)
                    {
                        Console.WriteLine($"- {stat.Key}: {stat.Value} calls");
                    }
                    Console.WriteLine("=========================\n");
                    break;
            
                case "stop":
                    Console.WriteLine("[INFO] Shutting down the server...");
                    server.Stop(); 
                    Environment.Exit(0); 
                    break;
                default:
                    Console.WriteLine("[INFO] Unknown admin command.");
                    break;
            }
        }
    }

    static void HandleClient(TcpClient client)
    {
        NetworkStream ns = client.GetStream();
        var isConnected = client.Connected;
        BinaryReader reader = new BinaryReader(ns);
        BinaryWriter writer = new BinaryWriter(ns);
        
        writer.Write(Greeting);
        
        var clientName = reader.ReadString();
        var localPath = Path.Combine(Root, clientName);

        if (!Directory.Exists(localPath)) Directory.CreateDirectory(localPath);
        
        client.ReceiveTimeout = 60000;
        while (isConnected)
        {
            try
            {
                byte commandId = reader.ReadByte();
                string fullPath;
                switch (commandId)
                {
                    case 1:
                        Console.WriteLine($"[INFO][{clientName}] GET command received. ");
                        _serverStats.AddOrUpdate("GET", 1, (_, oldValue) => oldValue + 1);
                        
                        fullPath = Path.Combine(localPath, Path.GetFileName(reader.ReadString()));
                        HandleGet(fullPath, writer, clientName);
                        break;
                    case 2:
                        Console.WriteLine($"[INFO][{clientName}] LIST command received");
                        _serverStats.AddOrUpdate("LIST", 1, (_, oldValue) => oldValue + 1);
                        
                        HandleList(writer, clientName);
                        break;
                    case 3:
                        Console.WriteLine($"[INFO][{clientName}] PUT command received");
                        _serverStats.AddOrUpdate("PUT", 1, (_, oldValue) => oldValue + 1);
                        
                        fullPath = Path.Combine(localPath, Path.GetFileName(reader.ReadString()));
                        long filesize = reader.ReadInt64();
                        HandlePut(fullPath, filesize, reader, writer, clientName);
                        break;
                    case 4:
                        Console.WriteLine($"[INFO][{clientName}] DELETE command received");
                        _serverStats.AddOrUpdate("DELETE", 1, (_, oldValue) => oldValue + 1);
                        
                        fullPath = Path.Combine(localPath, Path.GetFileName(reader.ReadString()));
                        HandleDelete(fullPath, writer, clientName);
                        break;
                    case 5:
                        Console.WriteLine($"[INFO][{clientName}] INFO command received");
                        _serverStats.AddOrUpdate("INFO", 1, (_, oldValue) => oldValue + 1);
                        
                        fullPath = Path.Combine(localPath, Path.GetFileName(reader.ReadString()));
                        HandleInfo(fullPath, writer);
                        break;
                    case 0:
                        isConnected = false;
                        Console.WriteLine($"[INFO][{clientName}] Closing the connection. Command id {commandId}");
                        break;
                    default:
                        Console.WriteLine($"Unknown command");
                        break;
                }
            }
            catch (EndOfStreamException e)
            {
                Console.WriteLine($"[ERROR][{clientName}] Stream Error: {e.Message}");
                isConnected = false;
                writer.Close();
                reader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR][{clientName}] Error arose: {e.Message}");
                isConnected = false;
                writer.Close();
                reader.Close();
            }
        } 
        client.Close();
        writer.Close();
        reader.Close();
    }

    private static void HandlePut(string path, long filesize, BinaryReader reader, BinaryWriter writer, string client)
    {
        try
        {
            using (FileStream fs = File.Create(path))
            {
                byte[] buffer = new byte[4096];
                long totalBytesRead = 0;
                
                while (totalBytesRead < filesize)
                {
                    int bytesToRead = (int)Math.Min(buffer.Length, filesize - totalBytesRead);
                    int bytesReceived = reader.Read(buffer, 0, bytesToRead);

                    if (bytesToRead == 0 || bytesReceived == 0) 
                        throw new Exception("Transmission failed: Connection error");
                                    
                    fs.Write(buffer, 0, bytesReceived);
                    totalBytesRead += bytesReceived;
                    Console.Write($"\rProgress: {totalBytesRead}/{filesize}");
                }
                writer.Write(true);
                writer.Write($"\n[SERVER] File uploaded!");
                Console.WriteLine($"\n[SUCCESS][{client}] File uploaded!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR][{client}] Something went wrong: {ex.Message}");
            writer.Write(false);
            writer.Write($"[Server] Something went wrong: {ex.Message}");
            File.Delete(path);
        }
    }

    private static void HandleGet(string path, BinaryWriter writer, string client)
    {
        if (!File.Exists(path))
        {
            writer.Write(false);
            writer.Write($"[ERROR] The specified file does not exist. Error code 100.");
            return;
        }
        
        writer.Write(true);

        long fileSize = new FileInfo(path).Length;
        writer.Write(fileSize);

        using (FileStream fileStream = File.OpenRead(path))
        {
            byte[] buffer = new byte[4096];
            int bytesRead;

            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                writer.Write(buffer, 0, bytesRead);
                writer.Flush();
            }
        }
        Console.WriteLine($"[INFO][{Path.GetFileName(client)}] File transmission complete");
    }

    private static void HandleList(BinaryWriter writer, string clientName)
    {
        writer.Write(true);

        string[] files = Directory.GetFiles(Path.Combine(Root, clientName));
        writer.Write(files.Length);
        foreach (string file in files)
        {
            writer.Write(Path.GetFileName(file));
        }
        Console.WriteLine($"[INFO][{clientName}] LIST operation executed");
    }

    private static void HandleDelete(string path, BinaryWriter writer, string client)
    {
        if (!File.Exists(path))
        {
            writer.Write(false);
            writer.Write($"[ERROR] The specified file does not exist. Error code 100.");
            return;
        }
        
        try
        {
            File.Delete(path);
            Console.WriteLine($"[INFO][{Path.GetFileName(client)}] {Path.GetFileName(path)} deleted successfully");
            writer.Write(true);
            writer.Write($"File {Path.GetFileName(path)} deleted successfully");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[ERROR][{Path.GetFileName(client)}] File is accessed by another process: {ex.Message}");
            writer.Write(false);
            writer.Write($"[ERROR] File is accessed by another process: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"[ERROR][{Path.GetFileName(client)}] No rights to access this file: {ex.Message}");
            writer.Write(false);
            writer.Write($"[ERROR] No rights to access this file: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR][{Path.GetFileName(client)}] Something went wrong: {ex.Message}");
            writer.Write(false);
            writer.Write($"[ERROR] Something went wrong: {ex.Message}");
        }
    }

    private static void HandleInfo(string path, BinaryWriter writer)
    {
        if (!File.Exists(path))
        {
            writer.Write(false);
            writer.Write("[ERROR] The specified file does not exist. Error code 100.");
            return;
        }
        
        FileInfo fileInfo = new FileInfo(path);
        if (fileInfo.Exists)
        {
            var metaData = new 
            {
                fileInfo.Name,
                Size = fileInfo.Length,
                Created = fileInfo.CreationTimeUtc,
                Modified = fileInfo.LastWriteTimeUtc,
                fileInfo.Extension
            };
            string json = System.Text.Json.JsonSerializer.Serialize(metaData);
            writer.Write(true);
            writer.Write(json);
        }
        else
        {
            writer.Write(false);
            writer.Write($"[ERROR] File {Path.GetFileName(path)} metadata is not available. Error code 101");
        }
    }
}