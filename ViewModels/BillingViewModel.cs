using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using SolarQuotationBillingSystem.Helpers;
using SolarQuotationBillingSystem.Models;

namespace SolarQuotationBillingSystem.ViewModels
{
    public partial class BillingViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;

        [ObservableProperty]
        private ObservableCollection<QuotationListModel> _quotations;

        [ObservableProperty]
        private bool _isLoading;

        public BillingViewModel(MainViewModel mainViewModel = null)
        {
            _mainViewModel = mainViewModel;
            Quotations = new ObservableCollection<QuotationListModel>();
            _ = LoadQuotationsAsync();
        }

        private async Task LoadQuotationsAsync()
        {
            IsLoading = true;
            Quotations.Clear();
            try
            {
                using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                await conn.OpenAsync();

                var query = @"
                    SELECT 
                        q.QuotationID, 
                        q.QuotationNo, 
                        q.QuotationDate, 
                        q.Status,
                        c.CustomerName, 
                        c.Mobile, 
                        q.GrandTotal 
                    FROM Quotation q
                    INNER JOIN Customers c ON q.CustomerID = c.CustomerID
                    ORDER BY q.QuotationDate DESC
                ";

                using var cmd = new SqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Quotations.Add(new QuotationListModel
                    {
                        QuotationID = Convert.ToInt32(reader["QuotationID"]),
                        QuotationNo = reader["QuotationNo"]?.ToString() ?? "",
                        QuotationDate = reader["QuotationDate"] != DBNull.Value ? Convert.ToDateTime(reader["QuotationDate"]) : DateTime.Now,
                        CustomerName = reader["CustomerName"]?.ToString() ?? "",
                        Mobile = reader["Mobile"]?.ToString() ?? "",
                        GrandTotal = reader["GrandTotal"] != DBNull.Value ? Convert.ToDecimal(reader["GrandTotal"]) : 0m,
                        Status = reader["Status"]?.ToString() ?? "Pending"
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading quotations: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void EditQuotation(QuotationListModel selectedQuotation)
        {
            if (selectedQuotation == null || _mainViewModel == null) return;
            
            if (selectedQuotation.Status != "Pending")
            {
                MessageBox.Show("Only pending quotations can be edited.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _mainViewModel.NavigateTo(new QuotationViewModel(selectedQuotation.QuotationID, true, () => 
            {
                _mainViewModel.NavigateTo(new QuotationBillingModuleViewModel(_mainViewModel));
            }));
        }

        [RelayCommand]
        private void CreateFinalBill(QuotationListModel selectedQuotation)
        {
            if (selectedQuotation == null || _mainViewModel == null) return;
            
            if (selectedQuotation.Status == "Paid")
            {
                MessageBox.Show("Bill is already created for this quotation. You can Reprint it.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Navigate to InvoiceViewModel and pass the QuotationID (to CREATE bill)
            _mainViewModel.NavigateTo(new InvoiceViewModel(_mainViewModel, selectedQuotation.QuotationID));
        }

        [RelayCommand]
        private async Task PrintQuotation(QuotationListModel selectedQuotation)
        {
            if (selectedQuotation == null) return;

            var qvm = new QuotationViewModel();
            await qvm.LoadQuotationDataAsync(selectedQuotation.QuotationID);
            qvm.ExportPdf();
        }

        [RelayCommand]
        private async Task ShareWhatsApp(QuotationListModel selectedQuotation)
        {
            if (selectedQuotation == null) return;
            
            try
            {
                var qvm = new QuotationViewModel();
                await qvm.LoadQuotationDataAsync(selectedQuotation.QuotationID);
                string pdfPath = qvm.GeneratePdfFile();
                
                string message = $@"Dear {selectedQuotation.CustomerName},

Thank you for your interest in ADISH ENTERPRISES.

Please find your Quotation attached.

📄 Quotation No : {selectedQuotation.QuotationNo}
📅 Date : {selectedQuotation.QuotationDate:dd MMM yyyy}
💰 Quotation Value : ₹{selectedQuotation.GrandTotal}

This quotation is valid for 15 days.

If you need any changes or have any questions, please contact us.

We look forward to serving you.

Regards,
ADISH ENTERPRISES
Complete Solar Solution

📞 +91-9407299837
📧 adishenterprises09@gmail.com";

                var previewVm = new WhatsAppPreviewViewModel
                {
                    MobileNumber = selectedQuotation.Mobile,
                    MessageText = message,
                    CustomerName = selectedQuotation.CustomerName,
                    DocumentType = "Quotation",
                    PdfPath = pdfPath
                };

                var window = new Views.WhatsAppPreviewWindow(previewVm);
                window.Owner = Application.Current.MainWindow;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open WhatsApp sharing: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ShareEmail(QuotationListModel selectedQuotation)
        {
            if (selectedQuotation == null) return;

            string subject = $"Your Document: {selectedQuotation.QuotationNo}";
            string body = $"Hello {selectedQuotation.CustomerName},\n\nPlease find attached your document ({selectedQuotation.QuotationNo}).\nTotal Amount: Rs. {selectedQuotation.GrandTotal}\n\nThank you.";
            string url = $"mailto:?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not launch Email Client: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
