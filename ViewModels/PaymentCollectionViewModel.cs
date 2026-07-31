using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Data.SqlClient;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SolarQuotationBillingSystem.Helpers;
using SolarQuotationBillingSystem.Models;

namespace SolarQuotationBillingSystem.ViewModels
{
    public partial class PaymentCollectionViewModel : ObservableObject
    {
        public Action OnClose;

        [ObservableProperty] private int invoiceID;
        [ObservableProperty] private string invoiceNo = string.Empty;
        [ObservableProperty] private string customerName = string.Empty;
        [ObservableProperty] private decimal billAmount;
        [ObservableProperty] private decimal remainingBalance;
        [ObservableProperty] private decimal paidAmount;
        [ObservableProperty] private string paymentMode = "Cash";
        [ObservableProperty] private string referenceNo = string.Empty;
        [ObservableProperty] private DateTime paymentDate = DateTime.Now;
        [ObservableProperty] private string remarks = string.Empty;

        public ObservableCollection<string> PaymentModes { get; } = new() { "Cash", "UPI", "Bank Transfer", "Cheque", "Card" };

        public bool PaymentSaved { get; private set; } = false;

        public PaymentCollectionViewModel(PaymentDashboardModel invoice)
        {
            InvoiceID = invoice.InvoiceID;
            InvoiceNo = invoice.InvoiceNo;
            CustomerName = invoice.CustomerName;
            BillAmount = invoice.TotalAmount;
            RemainingBalance = invoice.BalanceAmount;
            PaidAmount = RemainingBalance; // Default to full remaining balance
        }

        [RelayCommand]
        private async Task SavePayment()
        {
            if (PaidAmount <= 0)
            {
                MessageBox.Show("Please enter a valid paid amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (PaidAmount > RemainingBalance)
            {
                MessageBox.Show("Paid amount cannot exceed the remaining balance.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                await conn.OpenAsync();

                // Start Transaction
                using var transaction = conn.BeginTransaction();

                try
                {
                    // 1. Insert into PaymentHistory
                    var insertCmd = new SqlCommand(@"
                        INSERT INTO PaymentHistory (InvoiceNo, PaymentDate, PaidAmount, PaymentMode, ReferenceNo, Remarks)
                        VALUES (@invNo, @date, @amount, @mode, @refNo, @remarks)
                    ", conn, transaction);
                    insertCmd.Parameters.AddWithValue("@invNo", InvoiceNo);
                    insertCmd.Parameters.AddWithValue("@date", PaymentDate);
                    insertCmd.Parameters.AddWithValue("@amount", PaidAmount);
                    insertCmd.Parameters.AddWithValue("@mode", PaymentMode);
                    insertCmd.Parameters.AddWithValue("@refNo", string.IsNullOrWhiteSpace(ReferenceNo) ? (object)DBNull.Value : ReferenceNo);
                    insertCmd.Parameters.AddWithValue("@remarks", string.IsNullOrWhiteSpace(Remarks) ? (object)DBNull.Value : Remarks);
                    await insertCmd.ExecuteNonQueryAsync();

                    // 2. Update Invoice Paid, Balance, PaymentStatus
                    decimal newPaid = (BillAmount - RemainingBalance) + PaidAmount;
                    decimal newBalance = BillAmount - newPaid;
                    string newStatus = newBalance <= 0 ? "Paid" : (newPaid > 0 ? "Partial" : "Pending");

                    var updateCmd = new SqlCommand(@"
                        UPDATE Invoice 
                        SET Paid = @paid, Balance = @balance, PaymentStatus = @status
                        WHERE InvoiceID = @id
                    ", conn, transaction);
                    updateCmd.Parameters.AddWithValue("@paid", newPaid);
                    updateCmd.Parameters.AddWithValue("@balance", newBalance);
                    updateCmd.Parameters.AddWithValue("@status", newStatus);
                    updateCmd.Parameters.AddWithValue("@id", InvoiceID);
                    await updateCmd.ExecuteNonQueryAsync();

                    transaction.Commit();

                    PaymentSaved = true;
                    MessageBox.Show("Payment saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    OnClose?.Invoke();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving payment: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Close()
        {
            OnClose?.Invoke();
        }
    }
}
