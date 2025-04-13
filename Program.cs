using System;
using System.Text.RegularExpressions;
using Dapper;
using BCrypt.Net;
using MySql.Data.MySqlClient;

namespace LoginSignupSystem
{
    // Database connection class
    class DatabaseConnection
    {
        private string connectionString = $"server=127.0.0.1;user=root;database=LoginSignupSystem;port=3306;password={Environment.GetEnvironmentVariable("DB_PASSWORD")};SslMode=Preferred";

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }

        public MySqlConnection OpenConnection()
        {
            var connection = GetConnection();
            connection.Open();
            return connection;
        }
    }

    // User model
    class User
    {
        public int ID { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    // SignUp class
    class SignUp
    {
        private DatabaseConnection db = new DatabaseConnection();

        public void RegisterUser(string username, string email, string password)
        {
            using (var connection = db.OpenConnection())
            {
                string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username OR Email = @Email";
                int userExists = connection.ExecuteScalar<int>(checkQuery, new { Username = username, Email = email });

                if (userExists > 0)
                {
                    Console.WriteLine("\nUsername or Email already taken.");
                    return;
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                string insertQuery = "INSERT INTO Users (Username, Email, Password) VALUES (@Username, @Email, @Password)";
                connection.Execute(insertQuery, new
                {
                    Username = username,
                    Email = email,
                    Password = hashedPassword
                });

                Console.WriteLine("\nUser registered successfully!");
            }
        }
    }

    static class ValidationHelper
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, emailPattern);
        }

        public static bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;
            string usernamePattern = @"^[a-zA-Z0-9_]+$";
            return Regex.IsMatch(username, usernamePattern);
        }

        public static bool IsValidPassword(string password)
        {
            return !string.IsNullOrWhiteSpace(password) && password.Length >= 8 && password.Length <= 255;
        }
    }

    // Login class
    class Login
    {
        private DatabaseConnection db = new DatabaseConnection();

        public bool Authenticate(string identifier, string password)
        {
            using (var connection = db.OpenConnection())
            {
                string query = "SELECT * FROM Users WHERE Username = @Identifier OR Email = @Identifier";
                var user = connection.QueryFirstOrDefault<User>(query, new { Identifier = identifier });

                if (user == null)
                {
                    Console.WriteLine("\nUser not found!");
                    return false;
                }

                if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
                {
                    Console.WriteLine("\nIncorrect password!");
                    return false;
                }

                Console.WriteLine($"\nWelcome back, {user.Username}!");
                return true;
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the Login/Signup System");

            while (true)
            {
                Console.Write("\nType 'login', 'signup', or 'exit': ");
                string action = Console.ReadLine()?.Trim().ToLower();

                switch (action)
                {
                    case "signup":
                        HandleSignup();
                        break;

                    case "login":
                        if (HandleLogin())
                            return; // Exit after successful login
                        break;

                    case "exit":
                        Console.WriteLine("\nGoodbye!");
                        return;

                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
        }

        static void HandleSignup()
        {
            var signup = new SignUp();

            string username = GetValidInput("Enter username: ",
                ValidationHelper.IsValidUsername,
                "Username can only contain letters, numbers and underscores");

            string email = GetValidInput("Enter email: ",
                ValidationHelper.IsValidEmail,
                "Please enter a valid email address");

            string password = GetValidInput("Enter password: ",
                ValidationHelper.IsValidPassword,
                "Password must be 8-255 characters");

            signup.RegisterUser(username, email, password);
            Console.WriteLine($"\nWelcome, {username}! Registration successful.");
        }

        static bool HandleLogin()
        {
            var login = new Login();

            Console.Write("\nEnter username or email: ");
            string identifier = Console.ReadLine();

            string password = GetValidInput("Enter password: ",
                ValidationHelper.IsValidPassword,
                "Invalid password");

            return login.Authenticate(identifier, password);
        }

        static string GetValidInput(string prompt, Func<string, bool> validator, string errorMessage)
        {
            string input;
            do
            {
                Console.Write(prompt);
                input = Console.ReadLine();
                if (!validator(input))
                {
                    Console.WriteLine(errorMessage);
                }
            } while (!validator(input));

            return input;
        }
        
        Console.ReadLine();
    }
}