using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;

namespace Client;

class Client
{
    private const string Manual = "\nPlease, enter the command from the list below: \n" +
                                  "1. GET <filename>\n" + 
                                  "2. LIST\n" + 
                                  "3. PUT  <filepath>\n" + 
                                  "4. DELETE <filepath>\n" + 
                                  "5. INFO <filepath>\n" +
                                  "Or type 'exit' to terminate.";
    static void Main(string[] args)
    {
        try
        {
            TcpClient client = new TcpClient(IPAddress.Loopback.ToString(), 1234);
            NetworkStream ns = client.GetStream();
            BinaryWriter writer = new BinaryWriter(ns);
            BinaryReader reader = new BinaryReader(ns);

            reader.ReadString();
            
            while (true)
            {
                Thread.Sleep(2000);
                Console.WriteLine(Manual);
                
                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;
                string[] cmd = input.Split(" ");

                byte cmdId;
                switch (cmd[0].ToUpper())
                {
                    case "GET":
                        string localPath = Path.Combine("Downloads", cmd[1]);
                        if (File.Exists(localPath))
                        {
                            Console.Write($"[WARNING] File '{cmd[1]}' already exist. Overwrite? (y/n): ");
                            string answer = Console.ReadLine();

                            if (answer?.ToUpper() != "Y")
                            {
                                Console.WriteLine("[INFO] GET Cancelled.");
                                break; 
                            }
                        }
                        
                        cmdId = 1;
                        writer.Write(cmdId);
                        if (!string.IsNullOrEmpty(cmd[1])) writer.Write(cmd[1]);
                        else {
                            Console.WriteLine("[WARNING] Path cannot be empty!");
                            break;
                        }
                        
                        HandleGet(reader, cmd);
                        break;
                    case "LIST":
                        cmdId = 2;
                        writer.Write(cmdId);
                        
                        HandleList(reader);
                        break;
                    case "PUT":
                        Console.WriteLine("Ooops. Is not supported yet 🏗️");
                        break;
                    case "DELETE":
                        cmdId = 4;
                        writer.Write(cmdId);
                        if (!string.IsNullOrEmpty(cmd[1])) writer.Write(cmd[1]);
                        else {
                            Console.WriteLine("[WARNING] Path cannot be empty!");
                            break;
                        }
                        HandleDelete(reader);
                        break;
                    case "INFO":
                        cmdId = 5;
                        writer.Write(cmdId);
                        if (!string.IsNullOrEmpty(cmd[1])) writer.Write(cmd[1]);
                        else {
                            Console.WriteLine("[WARNING] Path cannot be empty!");
                            break;
                        }
                        HandleInfo(cmd[1], reader);
                        break;
                    case "EXIT":
                        Console.WriteLine("Closing the connection...");
                        client.Close();
                        return;
                    default:
                        Console.WriteLine("[INFO] This command doesn't exist :(");
                        break;
                }
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Connection failed: {e.Message}");
        }
    }

    private static void HandleGet(BinaryReader reader, string[] cmd)
    {
        if (reader.ReadBoolean())
        {
            string path = Path.Combine("Downloads", cmd[1]);
            
            long fileSize = reader.ReadInt64();
            Directory.CreateDirectory("Downloads");
            Console.WriteLine($"[INFO] Downloading file '{cmd[1]}' ({fileSize} bytes)...");

            using (FileStream fileStream = File.Create(path))
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

    private static void HandleList(BinaryReader reader)
    {
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

    private static void HandleDelete(BinaryReader reader)
    {
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

    private static void HandleInfo(string filename, BinaryReader reader)
    {
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