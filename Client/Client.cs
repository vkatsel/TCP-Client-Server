using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;

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
                        writer.Write(cmd[1]);

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
                        Console.WriteLine("Ooops. Is not supported yet 🏗️");
                        break;
                    case "INFO":
                        Console.WriteLine("Ooops. Is not supported yet 🏗️");
                        break;
                    case "EXIT":
                        Console.WriteLine("Closing the connection...");
                        client.Close();
                        return;
                    default:
                        Console.WriteLine("[INFO] Command doesn't exist :(");
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
                int bytesRead;

                while (totalBytesRead < fileSize)
                {
                    int bytesToRead = (int)Math.Min(buffer.Length, fileSize - totalBytesRead);
                    reader.Read(buffer, 0, bytesToRead);
                    if (bytesToRead == 0) 
                        break;
                                    
                    fileStream.Write(buffer, 0, bytesToRead);
                    totalBytesRead += bytesToRead;
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
}