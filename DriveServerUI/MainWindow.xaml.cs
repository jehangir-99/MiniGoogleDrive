using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DriveServerUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpListener server;
        public MainWindow()
        {
            InitializeComponent();
            StartServer();
        }

        void StartServer()
        {
            server = new TcpListener(IPAddress.Any, 5000);
            server.Start();

            Log("Server started...");

            Thread t = new Thread(Listen);
            t.IsBackground = true;
            t.Start();
        }

        void Listen()
        {
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Log("Client connected!");

                Thread t = new Thread(() => HandleClient(client));
                t.IsBackground = true;
                t.Start();
            }
        }

        void HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();

                // =========================
                // 1. READ USERNAME FIRST
                // =========================
                int userLen = BitConverter.ToInt32(ReadExact(stream, 4), 0);
                string username = Encoding.UTF8.GetString(ReadExact(stream, userLen));

                string userFolder = System.IO.Path.Combine("ServerFiles", username);
                EnsureFolder(userFolder);

                // =========================
                // 2. READ COMMAND
                // =========================
                int cmdLen = BitConverter.ToInt32(ReadExact(stream, 4), 0);
                string command = Encoding.UTF8.GetString(ReadExact(stream, cmdLen));

                Log($"User: {username} | Command: {command}");

                // =========================
                // UPLOAD
                // =========================
                if (command == "UPLOAD")
                {
                    int nameLen = BitConverter.ToInt32(ReadExact(stream, 4), 0);
                    string fileName = Encoding.UTF8.GetString(ReadExact(stream, nameLen));

                    int fileSize = BitConverter.ToInt32(ReadExact(stream, 4), 0);

                    byte[] fileData = new byte[fileSize];
                    int totalRead = 0;

                    while (totalRead < fileSize)
                    {
                        int read = stream.Read(fileData, totalRead, fileSize - totalRead);
                        if (read == 0) throw new Exception("Disconnected");
                        totalRead += read;
                    }

                    string path = System.IO.Path.Combine(userFolder, fileName);

                    File.WriteAllBytes(path, fileData);

                    Log($"Saved: {username}/{fileName}");
                }

                // =========================
                // DELETE
                // =========================
                else if (command == "DELETE")
                {
                    int len = BitConverter.ToInt32(ReadExact(stream, 4), 0);
                    string fileName = Encoding.UTF8.GetString(ReadExact(stream, len));

                    string path = System.IO.Path.Combine(userFolder, fileName);

                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        Log($"Deleted: {username}/{fileName}");
                    }
                }

                // =========================
                // DOWNLOAD
                // =========================
                else if (command == "DOWNLOAD")
                {
                    int len = BitConverter.ToInt32(ReadExact(stream, 4), 0);
                    string fileName = Encoding.UTF8.GetString(ReadExact(stream, len));

                    string path = System.IO.Path.Combine(userFolder, fileName);

                    if (!File.Exists(path))
                        throw new Exception("File not found");

                    byte[] data = File.ReadAllBytes(path);

                    stream.Write(BitConverter.GetBytes(data.Length), 0, 4);
                    stream.Write(data, 0, data.Length);

                    Log($"Sent: {username}/{fileName}");
                }

                // =========================
                // LIST
                // =========================
                else if (command == "LIST")
                {
                    var files = Directory.GetFiles(userFolder)
                        .Select(System.IO.Path.GetFileName);

                    string data = string.Join("|", files);
                    byte[] bytes = Encoding.UTF8.GetBytes(data);

                    stream.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
                    stream.Write(bytes, 0, bytes.Length);

                    Log($"File list sent for {username}");
                }
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.Message);
            }
            finally
            {
                client.Close();
                Log("Client disconnected");
            }
        }

        void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogBox.Items.Add($"[{DateTime.Now:T}] {message}");
            });
        }

        static byte[] ReadExact(NetworkStream stream, int size)
        {
            byte[] buffer = new byte[size];
            int totalRead = 0;

            while (totalRead < size)
            {
                int read = stream.Read(buffer, totalRead, size - totalRead);

                if (read == 0)
                    throw new Exception("Client disconnected early");

                totalRead += read;
            }

            return buffer;
        }

        static void WriteInt32(NetworkStream stream, int value)
        {
            byte[] data = BitConverter.GetBytes(value);
            stream.Write(data, 0, data.Length);
        }

        static int ReadInt32(NetworkStream stream)
        {
            return BitConverter.ToInt32(ReadExact(stream, 4), 0);
        }

        static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        static string[] GetFiles(string folder)
        {
            EnsureFolder(folder);
            return Directory.GetFiles(folder)
                            .Select(System.IO.Path.GetFileName)
                            .ToArray();
        }

        static void SendString(NetworkStream stream, string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            WriteInt32(stream, bytes.Length);
            stream.Write(bytes, 0, bytes.Length);
        }

        static string ReceiveString(NetworkStream stream)
        {
            int len = ReadInt32(stream);
            byte[] data = ReadExact(stream, len);
            return Encoding.UTF8.GetString(data);
        }
    }
}
