using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data.SqlClient;
using System.IO;

namespace DriveClient
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB; Initial Catalog=MiniGoogleDriveDB; Integrated Security=True";

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void GoToRegister_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RegisterWindow register = new RegisterWindow();
            register.Show();
            this.Close();
        }

        private void ShowPassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Show password feature coming soon!");
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = UsernameBox.Text.Trim();
                string password = PasswordBox.Password.Trim();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Please fill all fields");
                    return;
                }

                string connectionString =
                    @"Data Source=(LocalDB)\MSSQLLocalDB; Initial Catalog=MiniGoogleDriveDB; Integrated Security=True";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    string query =
                        "SELECT COUNT(*) FROM Users WHERE Username=@u AND Password=@p";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@p", password);

                    int result = (int)cmd.ExecuteScalar();

                    // MessageBox.Show("DB Result: " + result); // DEBUG STEP

                    if (result > 0)
                    {
                        MessageBox.Show("Login Successful!");

                        MainWindow main = new MainWindow(username);
                        main.Show();

                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Invalid username or password");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR: " + ex.Message);
            }
        }
    }
}
