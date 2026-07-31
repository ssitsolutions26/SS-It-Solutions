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
    public partial class BillDeleteViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<BillDeleteModel> invoices;
        [ObservableProperty] private string filterSearchText = string.Empty;

        public BillDeleteViewModel()
        {
            Invoices = new ObservableCollection<BillDeleteModel>();
            _ = LoadInvoicesAsync();
        }

        private async Task LoadInvoicesAsync()
        {
            Invoices.Clear();
            try
            {
                using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                await conn.OpenAsync();

                var query = @"
                    SELECT 
                        i.InvoiceID, 
                        i.InvoiceNo, 
                        i.InvoiceDate, 
                        i.GrandTotal, 
                        i.Paid, 
                        i.Balance, 
                        i.PaymentStatus,
                        c.CustomerName, 
                        c.Mobile,
                        (SELECT TOP 1 QuotationNo FROM Quotation WHERE InvoiceNo = i.InvoiceNo) as QuotationNo
                    FROM Invoice i
                    INNER JOIN Customers c ON i.CustomerID = c.CustomerID
                    WHERE (@Search = '' OR i.InvoiceNo = @Search)
                    ORDER BY i.InvoiceDate DESC
                ";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Search", FilterSearchText?.Trim() ?? string.Empty);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Invoices.Add(new BillDeleteModel
                    {
                        InvoiceID = Convert.ToInt32(reader["InvoiceID"]),
                        InvoiceNo = reader["InvoiceNo"]?.ToString() ?? "",
                        QuotationNo = reader["QuotationNo"]?.ToString() ?? "",
                        InvoiceDate = reader["InvoiceDate"] != DBNull.Value ? Convert.ToDateTime(reader["InvoiceDate"]) : DateTime.Now,
                        CustomerName = reader["CustomerName"]?.ToString() ?? "",
                        Mobile = reader["Mobile"]?.ToString() ?? "",
                        TotalAmount = reader["GrandTotal"] != DBNull.Value ? Convert.ToDecimal(reader["GrandTotal"]) : 0,
                        PaidAmount = reader["Paid"] != DBNull.Value ? Convert.ToDecimal(reader["Paid"]) : 0,
                        BalanceAmount = reader["Balance"] != DBNull.Value ? Convert.ToDecimal(reader["Balance"]) : 0,
                        PaymentStatus = reader["PaymentStatus"]?.ToString() ?? "Pending"
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading bills: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task Search()
        {
            await LoadInvoicesAsync();
        }

        [RelayCommand]
        private async Task Clear()
        {
            FilterSearchText = string.Empty;
            await LoadInvoicesAsync();
        }

        [RelayCommand]
        private async Task DeleteBill(BillDeleteModel invoice)
        {
            if (invoice == null) return;

            // Open Confirmation Dialog
            var appMain = Application.Current.MainWindow?.DataContext as MainViewModel;
            string currentUser = appMain?.CurrentUsername ?? "Admin";

            var confirmVm = new BillDeleteConfirmationViewModel(currentUser, invoice);
            var confirmWin = new Views.BillDeleteConfirmationWindow(confirmVm);
            confirmWin.Owner = Application.Current.MainWindow;
            var result = confirmWin.ShowDialog();

            if (result == true)
            {
                try
                {
                    using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                    await conn.OpenAsync();
                    
                    string reason = confirmVm.DeleteRemark;
                    string deletedBy = currentUser;

                    using var transaction = conn.BeginTransaction();
                    try
                    {
                        // 1. Log the deletion
                        var logCmd = new SqlCommand("INSERT INTO BillDeleteLog (InvoiceNo, QuotationNo, CustomerName, DeletedBy, Reason, MachineName) VALUES (@invNo, @quotNo, @customer, @user, @reason, @machine)", conn, transaction);
                        logCmd.Parameters.AddWithValue("@invNo", invoice.InvoiceNo);
                        logCmd.Parameters.AddWithValue("@quotNo", invoice.QuotationNo);
                        logCmd.Parameters.AddWithValue("@customer", invoice.CustomerName);
                        logCmd.Parameters.AddWithValue("@user", deletedBy);
                        logCmd.Parameters.AddWithValue("@reason", reason);
                        logCmd.Parameters.AddWithValue("@machine", Environment.MachineName);
                        await logCmd.ExecuteNonQueryAsync();

                        // 2. Revert Quotation status to 'Pending'
                        var updQuotCmd = new SqlCommand("UPDATE Quotation SET Status = 'Pending', InvoiceNo = NULL, PaymentMode = NULL, PaymentRefNo = NULL WHERE InvoiceNo = @invNo", conn, transaction);
                        updQuotCmd.Parameters.AddWithValue("@invNo", invoice.InvoiceNo);
                        await updQuotCmd.ExecuteNonQueryAsync();

                        // 3. Restore Inventory Stock
                        var updStockCmd = new SqlCommand(@"
                            UPDATE p
                            SET p.Stock = p.Stock + ii.Qty
                            FROM Products p
                            INNER JOIN InvoiceItems ii ON p.ProductID = ii.ProductID
                            WHERE ii.InvoiceID = @id", conn, transaction);
                        updStockCmd.Parameters.AddWithValue("@id", invoice.InvoiceID);
                        await updStockCmd.ExecuteNonQueryAsync();

                        // 4. Delete from PaymentHistory
                        var delPayCmd = new SqlCommand("DELETE FROM PaymentHistory WHERE InvoiceNo = @invNo", conn, transaction);
                        delPayCmd.Parameters.AddWithValue("@invNo", invoice.InvoiceNo);
                        await delPayCmd.ExecuteNonQueryAsync();

                        // 5. Delete Invoice Items
                        var delInvItemsCmd = new SqlCommand("DELETE FROM InvoiceItems WHERE InvoiceID = @id", conn, transaction);
                        delInvItemsCmd.Parameters.AddWithValue("@id", invoice.InvoiceID);
                        await delInvItemsCmd.ExecuteNonQueryAsync();

                        // 6. Delete Invoice
                        var delInvCmd = new SqlCommand("DELETE FROM Invoice WHERE InvoiceID = @id", conn, transaction);
                        delInvCmd.Parameters.AddWithValue("@id", invoice.InvoiceID);
                        await delInvCmd.ExecuteNonQueryAsync();

                        transaction.Commit();
                        MessageBox.Show("Bill deleted successfully.\nInventory updated successfully.\nDelete log saved successfully.", "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        FilterSearchText = string.Empty;
                        await LoadInvoicesAsync();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting bill: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task PrintBill(BillDeleteModel invoice)
        {
             if (invoice == null) return;
             try
             {
                 using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                 await conn.OpenAsync();
                 var cmd = new SqlCommand("SELECT QuotationID FROM Quotation WHERE InvoiceNo = @invNo", conn);
                 cmd.Parameters.AddWithValue("@invNo", invoice.InvoiceNo);
                 var qIdObj = await cmd.ExecuteScalarAsync();
                 
                 if (qIdObj != null)
                 {
                     int qId = Convert.ToInt32(qIdObj);
                     var ivm = new InvoiceViewModel();
                     await ivm.LoadInvoiceDataAsync(qId);
                     ivm.ExportPdfDirect();
                 }
                 else
                 {
                     MessageBox.Show("Could not find the original Quotation to print the bill.");
                 }
             }
             catch (Exception ex)
             {
                 MessageBox.Show($"Error printing bill: {ex.Message}");
             }
        }
    }
}
