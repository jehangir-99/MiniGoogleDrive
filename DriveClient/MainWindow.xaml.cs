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
using Microsoft.Win32;
using System.Windows.Media.Animation;

namespace DriveClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string currentUser;
        string selectedFilePath = "";

        public class FileItem
        {
            public string FileName { get; set; }
            public string Icon { get; set; }
        }

        public MainWindow(string username)
        {
            InitializeComponent();

            currentUser = username;

            UserLabel.Text = "Logged in as: " + currentUser;

            SelectedFileText.Text = "No file selected";

            Loaded += AnimateIn;

            LoadUserFiles();
        }

        private void AnimateIn(object sender, RoutedEventArgs e)
        {
            DoubleAnimation fade = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400)
            };

            this.BeginAnimation(OpacityProperty, fade);
        }

        private void SendUser(NetworkStream stream)
        {
            if (string.IsNullOrEmpty(currentUser))
                throw new Exception("User not logged in");

            byte[] userBytes = Encoding.UTF8.GetBytes(currentUser);
            stream.Write(BitConverter.GetBytes(userBytes.Length), 0, 4);
            stream.Write(userBytes, 0, userBytes.Length);
        }


        private void LoadUserFiles()
        {
            GetFiles_Click(null, null);
        }

        private void UpdateProgress(int value)
        {
            Dispatcher.Invoke(() =>
            {
                TransferProgressBar.Value = value;
            });
        }

        static void WriteInt32(NetworkStream stream, int value)
        {
            byte[] data = BitConverter.GetBytes(value);
            stream.Write(data, 0, data.Length);
        }

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            if (dialog.ShowDialog() == true)
            {
                selectedFilePath = dialog.FileName;
                SelectedFileText.Text = "Selected: " + System.IO.Path.GetFileName(selectedFilePath);
            }
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedFilePath))
                {
                    MessageBox.Show("Select a file first!");
                    return;
                }

                string fileName = System.IO.Path.GetFileName(selectedFilePath);
                byte[] fileBytes = File.ReadAllBytes(selectedFilePath);
                int total = fileBytes.Length;

                TcpClient client = new TcpClient("127.0.0.1", 5000);
                NetworkStream stream = client.GetStream();

                SendUser(stream);

                string command = "UPLOAD";
                byte[] cmdBytes = Encoding.UTF8.GetBytes(command);

                stream.Write(BitConverter.GetBytes(cmdBytes.Length), 0, 4);
                stream.Write(cmdBytes, 0, cmdBytes.Length);

                byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
                stream.Write(BitConverter.GetBytes(nameBytes.Length), 0, 4);
                stream.Write(nameBytes, 0, nameBytes.Length);

                stream.Write(BitConverter.GetBytes(total), 0, 4);

                int sent = 0;
                int chunk = 4096;

                while (sent < total)
                {
                    int size = Math.Min(chunk, total - sent);
                    stream.Write(fileBytes, sent, size);

                    sent += size;

                    UpdateProgress((int)((sent / (double)total) * 100));
                }

                UpdateProgress(100);

                client.Close();

                MessageBox.Show("Upload successful!");

                selectedFilePath = "";
                SelectedFileText.Text = "No file selected";

                LoadUserFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Upload Error: " + ex.Message);
            }
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FileItem selected = FilesListBox.SelectedItem as FileItem;

                if (selected == null)
                {
                    MessageBox.Show("Select a file first!");
                    return;
                }

                string fileName = selected.FileName;

                TcpClient client = new TcpClient("127.0.0.1", 5000);
                NetworkStream stream = client.GetStream();

                SendUser(stream);

                string command = "DOWNLOAD";
                byte[] cmdBytes = Encoding.UTF8.GetBytes(command);

                stream.Write(BitConverter.GetBytes(cmdBytes.Length), 0, 4);
                stream.Write(cmdBytes, 0, cmdBytes.Length);

                byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
                stream.Write(BitConverter.GetBytes(nameBytes.Length), 0, 4);
                stream.Write(nameBytes, 0, nameBytes.Length);

                int fileSize = BitConverter.ToInt32(ReadExact(stream, 4), 0);
                byte[] fileData = ReadExact(stream, fileSize);

                client.Close();

                SaveFileDialog save = new SaveFileDialog
                {
                    FileName = fileName
                };

                if (save.ShowDialog() == true)
                {
                    File.WriteAllBytes(save.FileName, fileData);
                    MessageBox.Show("Download complete!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Download Error: " + ex.Message);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FileItem selected = FilesListBox.SelectedItem as FileItem;

                if (selected == null)
                {
                    MessageBox.Show("Select a file first!");
                    return;
                }

                string fileName = selected.FileName;

                TcpClient client = new TcpClient("127.0.0.1", 5000);
                NetworkStream stream = client.GetStream();

                SendUser(stream);

                string command = "DELETE";
                byte[] cmdBytes = Encoding.UTF8.GetBytes(command);

                stream.Write(BitConverter.GetBytes(cmdBytes.Length), 0, 4);
                stream.Write(cmdBytes, 0, cmdBytes.Length);

                byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
                stream.Write(BitConverter.GetBytes(nameBytes.Length), 0, 4);
                stream.Write(nameBytes, 0, nameBytes.Length);

                client.Close();

                MessageBox.Show("File deleted!");

                LoadUserFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete Error: " + ex.Message);
            }
        }

        private void GetFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TcpClient client = new TcpClient("127.0.0.1", 5000);
                NetworkStream stream = client.GetStream();

                SendUser(stream);

                string command = "LIST";
                byte[] cmdBytes = Encoding.UTF8.GetBytes(command);

                stream.Write(BitConverter.GetBytes(cmdBytes.Length), 0, 4);
                stream.Write(cmdBytes, 0, cmdBytes.Length);

                int length = BitConverter.ToInt32(ReadExact(stream, 4), 0);
                string data = Encoding.UTF8.GetString(ReadExact(stream, length));

                string[] files = data.Split('|');

                List<FileItem> items = new List<FileItem>();

                foreach (var file in files)
                {
                    if (string.IsNullOrWhiteSpace(file)) continue;

                    items.Add(new FileItem
                    {
                        FileName = file,
                        Icon = GetFileIcon(file)
                    });
                }

                FilesListBox.ItemsSource = items;

                client.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("LIST Error: " + ex.Message);
            }
        }

        private string GetFileIcon(string fileName)
        {
            string ext = System.IO.Path.GetExtension(fileName).ToLower();

            switch (ext)
            {
                case ".txt":
                    return "📄";
                case ".doc":
                case ".docx":
                    return "📘";
                case ".pdf":
                    return "📕";
                case ".png":
                case ".jpg":
                case ".jpeg":
                    return "🖼";
                case ".zip":
                    return "🗜";
                default:
                    return "📁";
            }
        }

        private void FilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FileItem selected = FilesListBox.SelectedItem as FileItem;

            if (selected != null)
            {
                Console.WriteLine("Selected: " + selected.FileName);
            }
        }

        static byte[] ReadExact(NetworkStream stream, int size)
        {
            byte[] buffer = new byte[size];
            int total = 0;

            while (total < size)
            {
                int read = stream.Read(buffer, total, size - total);

                if (read == 0)
                    throw new Exception("Disconnected");

                total += read;
            }

            return buffer;
        }
    }
}
