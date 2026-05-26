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

namespace DriveClient
{
    /// <summary>
    /// Interaction logic for RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
    {
        string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB; Initial Catalog=MiniGoogleDriveDB; Integrated Security=True";

        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = UsernameBox.Text.Trim();
                string password = PasswordBox.Password.Trim();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    StatusText.Text = "Please fill all fields.";
                    return;
                }

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // Check if user already exists
                    string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username=@u";
                    SqlCommand checkCmd = new SqlCommand(checkQuery, con);
                    checkCmd.Parameters.AddWithValue("@u", username);

                    int exists = (int)checkCmd.ExecuteScalar();

                    if (exists > 0)
                    {
                        StatusText.Text = "Username already exists!";
                        return;
                    }

                    // Insert user
                    string query = "INSERT INTO Users (Username, Password) VALUES (@u, @p)";
                    SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@p", password);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Account created successfully!");

                // Go back to login
                LoginWindow login = new LoginWindow();
                login.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error: " + ex.Message;
            }
        }

        private void GoToLogin_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}
