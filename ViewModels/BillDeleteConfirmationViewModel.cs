using System;
using System.Windows;
using Microsoft.Data.SqlClient;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SolarQuotationBillingSystem.Helpers;
using SolarQuotationBillingSystem.Models;

namespace SolarQuotationBillingSystem.ViewModels
{
    public partial class BillDeleteConfirmationViewModel : ObservableObject
    {
        public Action<bool> OnClose;

        [ObservableProperty] private string invoiceNo = string.Empty;
        [ObservableProperty] private DateTime billDate;
        [ObservableProperty] private string customerName = string.Empty;
        [ObservableProperty] private decimal totalAmount;

        [ObservableProperty] private string deleteRemark = string.Empty;
        [ObservableProperty] private string adminPassword = string.Empty;

        private readonly string _currentUsername;

        public BillDeleteConfirmationViewModel(string currentUsername, BillDeleteModel invoice)
        {
            _currentUsername = string.IsNullOrEmpty(currentUsername) ? "Admin" : currentUsername;
            if (invoice != null)
            {
                InvoiceNo = invoice.InvoiceNo;
                BillDate = invoice.InvoiceDate;
                CustomerName = invoice.CustomerName;
                TotalAmount = invoice.TotalAmount;
            }
        }

        [RelayCommand]
        private void Delete()
        {
            if (string.IsNullOrWhiteSpace(DeleteRemark))
            {
                MessageBox.Show("Please enter delete remark.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(AdminPassword))
            {
                MessageBox.Show("Please enter the Admin Password.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Verify Admin Password
            bool isValid = false;
            try
            {
                using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();
                using var cmd = new SqlCommand("SELECT COUNT(1) FROM Users WHERE Username = @u AND PasswordHash = @p AND Role = 'Admin'", conn);
                cmd.Parameters.AddWithValue("@u", _currentUsername);
                cmd.Parameters.AddWithValue("@p", AdminPassword);
                isValid = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database error: {ex.Message}");
                return;
            }

            if (!isValid)
            {
                // Try fallback logic if currentUsername isn't populated or differs
                try
                {
                    using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                    conn.Open();
                    using var cmd = new SqlCommand("SELECT COUNT(1) FROM Users WHERE PasswordHash = @p AND Role = 'Admin'", conn);
                    cmd.Parameters.AddWithValue("@p", AdminPassword);
                    isValid = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
                catch { }

                if (!isValid)
                {
                    MessageBox.Show("Invalid Admin Password.", "Unauthorized", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            OnClose?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel()
        {
            OnClose?.Invoke(false);
        }
    }
}
