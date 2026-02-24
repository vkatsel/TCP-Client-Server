using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;

namespace MultiClientTest;

class MultiClient
{
    static void Main(string[] args)
    {
        IPAddress ipAddress = IPAddress.Loopback;
        int port = 1500;

        string[] users = ["Vadym", "Alice", "John"];
        List<Thread> testThreads = new List<Thread>();
        
        foreach (var user in users) 
        {
            Thread thread = new Thread(() => LaunchClient(ipAddress, port, user));
            testThreads.Add(thread);
            thread.Start();
        }
        
        foreach (var thread in testThreads) thread.Join();
        
        Console.WriteLine("All clients finished!");
    }

    static void LaunchClient(IPAddress ipAddress, int port, string username)
    {
        TcpClient client = new TcpClient(ipAddress.ToString(), port);
        NetworkStream ns = client.GetStream();
        BinaryWriter writer = new BinaryWriter(ns);
        BinaryReader reader = new BinaryReader(ns);

        reader.ReadString();
        writer.Write(username);
        
        if (!Directory.Exists(username)) Directory.CreateDirectory(username);
        var localPath = Path.Combine(Directory.GetCurrentDirectory(), username);
        
        TestPut(writer, reader, localPath);
        Thread.Sleep(1000);
        
        TestGet(writer, reader, localPath);
        Thread.Sleep(1000);
        
        TestList(writer, reader);
        Thread.Sleep(1000);
        
        TestDelete(writer, reader);
        Thread.Sleep(1000);
        
        TestPut(writer, reader, localPath, "ServerFile.txt");
        Thread.Sleep(1000);
        
        TestInfo(writer, reader);
        Thread.Sleep(1000);
        
        writer.Write((byte)0);
    }

    static void TestPut(BinaryWriter writer, BinaryReader reader, string path, string filename="test.txt")
    {
        var filePath = Path.Combine(path, filename);
        
        writer.Write((byte)3);
        writer.Write(filename);
        
        FileInfo fileInfo = new FileInfo(filePath);
        writer.Write(fileInfo.Length);
        
        Console.WriteLine("[INFO] Uploading...");
        using (FileStream fileStream = File.OpenRead(filePath))
        {
            byte[] buffer = new byte[4096];
            int bytesRead;

            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                writer.Write(buffer, 0, bytesRead);
                writer.Flush();
            }
        }

        if (reader.ReadBoolean())
        {
            Console.WriteLine($"[SUCCESS] {reader.ReadString()}");
        }
        else
        {
            Console.WriteLine($"[ERROR] {reader.ReadString()}");
        }
    }
    
    static void TestGet(BinaryWriter writer, BinaryReader reader, string path)
    {
        var filename = "ServerFile.txt";
        var filePath = Path.Combine(path, filename);
        
        writer.Write((byte)1);
        writer.Write(filename);
        
        if (reader.ReadBoolean())
        {
            long fileSize = reader.ReadInt64();
            Console.WriteLine($"[INFO] Downloading file '{filename}' ({fileSize} bytes)...");

            using (FileStream fileStream = File.Create(filePath))
            {
                byte[] buffer = new byte[4096];
                long totalBytesRead = 0;

                while (totalBytesRead < fileSize)
                {
                    int bytesToRead = (int)Math.Min(buffer.Length, fileSize - totalBytesRead);
                    int bytesReceived = reader.Read(buffer, 0, bytesToRead);

                    if (bytesToRead == 0 || bytesReceived == 0) 
                        break;
                                    
                    fileStream.Write(buffer, 0, bytesReceived);
                    totalBytesRead += bytesReceived;
                    Console.Write($"\rProgress: {totalBytesRead}/{fileSize}");
                }
                Console.WriteLine("\n[SUCCESS] File downloaded!");
            }
        } else {
            string errorMsg = reader.ReadString();
            Console.WriteLine($"[SERVER] {errorMsg}");
        }
    }
    
    static void TestList(BinaryWriter writer, BinaryReader reader)
    {
        writer.Write((byte)2);
        
        if (reader.ReadBoolean())
        {
            Console.WriteLine($"=======FILES IN STORAGE=======");
            int fileCount = reader.ReadInt32();
            for (int i = 0; i < fileCount; i++)
            {
                string filename = reader.ReadString();
                Console.WriteLine($"{i}. {filename}");
            }   
        }
        else
        {
            string errorMsg = reader.ReadString();
            Console.WriteLine($"[SERVER] {errorMsg}");
        }
            
    }
    
    static void TestDelete(BinaryWriter writer, BinaryReader reader)
    {
        writer.Write((byte)4);
        writer.Write("ServerFile.txt");
        if (reader.ReadBoolean())
        {
            string successMsg = reader.ReadString();
            Console.WriteLine(successMsg);
        }
        else
        {
            string errorMsg = reader.ReadString();
            Console.WriteLine($"[SERVER] {errorMsg}");
        }
    }
    
    static void TestInfo(BinaryWriter writer, BinaryReader reader)
    {
        var filename = "ServerFile.txt";
        
        writer.Write((byte)5);
        writer.Write(filename);
        if (reader.ReadBoolean())
        {
             Console.WriteLine($"=== {filename} metadata:");
             string json = reader.ReadString();
             try
             {
                 JsonNode data = JsonNode.Parse(json);
                 Console.WriteLine($"- Name: {data["Name"]}");
                 Console.WriteLine($"- Size: {data["Size"]}");
                 Console.WriteLine($"- Extension: {data["Extension"]}");
                 Console.WriteLine($"- Modified: {data["Modified"]}");
                 Console.WriteLine($"- Created: {data["Created"]}");
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"[ERROR] JSON Parsing failed: {ex.Message}");
                 Console.WriteLine($"Raw data: {json}");
             }
        }
        else
        {
            string errorMsg = reader.ReadString();
            Console.WriteLine($"[SERVER] {errorMsg}");
        }        
    }
}