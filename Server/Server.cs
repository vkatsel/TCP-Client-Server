using System.Globalization;
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
                        Console.WriteLine($"[INFO] LIST command received");
                        HandleList(writer);
                        break;
                    case 3:
                        Console.WriteLine($"[INFO] PUT command received");
                        filename = reader.ReadString();
                        long filesize = reader.ReadInt64();
                        HandlePut(filename, filesize, reader, writer);
                        break;
                    case 4:
                        Console.WriteLine($"[INFO] DELETE command received");
                        filename = reader.ReadString();
                        HandleDelete(filename, writer);
                        break;
                    case 5:
                        Console.WriteLine($"[INFO] INFO command received");
                        filename = reader.ReadString();
                        HandleInfo(filename, writer);
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

    private static void HandlePut(string filename, long filesize, BinaryReader reader, BinaryWriter writer)
    {
        string path = Path.Combine(StoragePath, filename);

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
                writer.Write("\n[SERVER] File uploaded!");
                Console.WriteLine("\n[SUCCESS] File uploaded!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Something went wrong: {ex.Message}");
            writer.Write(false);
            writer.Write($"[Server] Something went wrong: {ex.Message}");
            File.Delete(path);
        }
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

    private static void HandleDelete(string filename, BinaryWriter writer)
    {
        string path = Path.Combine(StoragePath, filename);
        if (!File.Exists(path))
        {
            writer.Write(false);
            writer.Write("[ERROR] The specified file does not exist. Error code 100.");
            return;
        }
        
        try
        {
            File.Delete(path);
            Console.WriteLine($"[INFO] {Path.GetFileName(path)} deleted successfully");
            writer.Write(true);
            writer.Write($"File {Path.GetFileName(path)} deleted successfully");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[ERROR] File is accessed by another process: {ex.Message}");
            writer.Write(false);
            writer.Write($"[ERROR] File is accessed by another process: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"[ERROR] No rights to access this file: {ex.Message}");
            writer.Write(false);
            writer.Write($"[ERROR] No rights to access this file: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Something went wrong: {ex.Message}");
            writer.Write(false);
            writer.Write($"[ERROR] Something went wrong: {ex.Message}");
        }
    }

    private static void HandleInfo(string filename, BinaryWriter writer)
    {
        string path = Path.Combine(StoragePath, filename);
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
                Name = fileInfo.Name,
                Size = fileInfo.Length,
                Created = fileInfo.CreationTimeUtc,
                Modified = fileInfo.LastWriteTimeUtc,
                Extension = fileInfo.Extension
            };
            string json = System.Text.Json.JsonSerializer.Serialize(metaData);
            writer.Write(true);
            writer.Write(json);
        }
        else
        {
            writer.Write(false);
            writer.Write($"[ERROR] File {filename} metadata is not available. Error code 101");
        }
    }
}