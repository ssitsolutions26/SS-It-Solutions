using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Data.SqlClient;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SolarQuotationBillingSystem.Helpers;
using SolarQuotationBillingSystem.Models;
using System.Linq;

namespace SolarQuotationBillingSystem.ViewModels
{
    public partial class PaymentsViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<PaymentDashboardModel> invoices;

        [ObservableProperty] private DateTime? filterDateFrom = DateTime.Today;
        [ObservableProperty] private DateTime? filterDateTo = DateTime.Today;
        [ObservableProperty] private string filterSearchText = string.Empty;
        [ObservableProperty] private string filterStatus = "All";

        public ObservableCollection<string> StatusFilters { get; } = new() { "All", "Pending", "Partial", "Paid" };

        [ObservableProperty] private int totalBillsCount;
        [ObservableProperty] private decimal totalPaidAmount;
        [ObservableProperty] private decimal totalPendingAmount;

        public PaymentsViewModel()
        {
            Invoices = new ObservableCollection<PaymentDashboardModel>();
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
                    WHERE (@DateFrom IS NULL OR CAST(i.InvoiceDate AS DATE) >= CAST(@DateFrom AS DATE))
                      AND (@DateTo IS NULL OR CAST(i.InvoiceDate AS DATE) <= CAST(@DateTo AS DATE))
                      AND (@Status = 'All' OR i.PaymentStatus = @Status)
                      AND (@Search = '' OR i.InvoiceNo LIKE '%' + @Search + '%' OR c.CustomerName LIKE '%' + @Search + '%' OR c.Mobile LIKE '%' + @Search + '%' OR (SELECT TOP 1 QuotationNo FROM Quotation WHERE InvoiceNo = i.InvoiceNo) LIKE '%' + @Search + '%')
                    ORDER BY i.InvoiceDate DESC
                ";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@DateFrom", FilterDateFrom ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@DateTo", FilterDateTo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Status", FilterStatus);
                cmd.Parameters.AddWithValue("@Search", FilterSearchText ?? string.Empty);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Invoices.Add(new PaymentDashboardModel
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

                CalculateSummaries();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading payments: {ex.Message}");
            }
        }

        private void CalculateSummaries()
        {
            TotalBillsCount = Invoices.Count;
            TotalPaidAmount = Invoices.Sum(x => x.PaidAmount);
            TotalPendingAmount = Invoices.Sum(x => x.BalanceAmount);
        }

        [RelayCommand]
        private async Task Search()
        {
            await LoadInvoicesAsync();
        }

        [RelayCommand]
        private async Task Clear()
        {
            FilterDateFrom = DateTime.Today;
            FilterDateTo = DateTime.Today;
            FilterSearchText = string.Empty;
            FilterStatus = "All";
            await LoadInvoicesAsync();
        }

        [RelayCommand]
        private void CollectPayment(PaymentDashboardModel invoice)
        {
            if (invoice == null) return;
            var vm = new PaymentCollectionViewModel(invoice);
            var win = new Views.PaymentCollectionWindow(vm);
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();

            if (vm.PaymentSaved)
            {
                _ = LoadInvoicesAsync(); // Reload
            }
        }

        [RelayCommand]
        private async Task ReprintBill(PaymentDashboardModel invoice)
        {
            if (invoice == null) return;
            
            try
            {
                // We need to fetch QuotationID since InvoiceViewModel currently expects it
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
                    MessageBox.Show("Could not find the original Quotation to reprint the bill.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reprinting bill: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task WhatsAppBill(PaymentDashboardModel invoice)
        {
            if (invoice == null) return;

            if (string.IsNullOrWhiteSpace(invoice.Mobile))
            {
                MessageBox.Show("Customer mobile number not found.", "WhatsApp Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT QuotationID FROM Quotation WHERE InvoiceNo = @invNo", conn);
                cmd.Parameters.AddWithValue("@invNo", invoice.InvoiceNo);
                var qIdObj = await cmd.ExecuteScalarAsync();

                string pdfPath = string.Empty;
                if (qIdObj != null)
                {
                    int qId = Convert.ToInt32(qIdObj);
                    var ivm = new InvoiceViewModel();
                    await ivm.LoadInvoiceDataAsync(qId);
                    pdfPath = ivm.GeneratePdfFile();
                }
                else
                {
                    MessageBox.Show("Unable to generate bill.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string message = $@"🌞 *ADISH ENTERPRISES*

Dear {invoice.CustomerName},

Thank you for choosing us.

Please find your Solar System Bill attached.

🧾 Invoice No : {invoice.InvoiceNo}
💰 Amount : ₹{invoice.TotalAmount}
📅 Date : {invoice.InvoiceDate:dd MMM yyyy}

If you have any questions, please contact us.

Thank you.
ADISH ENTERPRISES";

                var previewVm = new WhatsAppPreviewViewModel
                {
                    MobileNumber = invoice.Mobile,
                    MessageText = message,
                    CustomerName = invoice.CustomerName,
                    DocumentType = "Invoice",
                    PdfPath = pdfPath
                };

                var window = new Views.WhatsAppPreviewWindow(previewVm);
                window.Owner = Application.Current.MainWindow;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open WhatsApp.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ExportPdf()
        {
            MessageBox.Show("Export PDF functionality will be implemented soon.", "Information");
        }

        [RelayCommand]
        private void ExportExcel()
        {
            MessageBox.Show("Export Excel functionality will be implemented soon.", "Information");
        }
    }
}
