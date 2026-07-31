using System;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SolarQuotationBillingSystem.Helpers;
using Microsoft.Data.SqlClient;

namespace SolarQuotationBillingSystem.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _rememberMe;

        public LoginViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        [RelayCommand]
        private void Exit()
        {
            Application.Current.Shutdown();
        }

        [RelayCommand]
        private void CheckUpdate()
        {
            try
            {
                // This link opens the Website's Updates section
                string downloadUrl = "https://ssitsolutions26.github.io/SS-It-Solutions/#updates";
                
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = downloadUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Login()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter username and password.";
                return;
            }

            try
            {
                using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT Role FROM Users WHERE Username=@u AND PasswordHash=@p", conn))
                    {
                        cmd.Parameters.AddWithValue("@u", Username);
                        // In a real app, hash the password. Here using plain text for demo based on seed data.
                        cmd.Parameters.AddWithValue("@p", Password);

                        var role = cmd.ExecuteScalar()?.ToString();
                        
                        if (!string.IsNullOrEmpty(role))
                        {
                            _mainViewModel.CurrentUserRole = role;
                            _mainViewModel.CurrentUsername = Username;
                            _mainViewModel.IsLoggedIn = true;
                            _mainViewModel.NavigateTo(new DashboardViewModel(_mainViewModel));
                        }
                        else
                        {
                            ErrorMessage = "Invalid username or password.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Database error: {ex.Message}";
            }
        }
    }
}
