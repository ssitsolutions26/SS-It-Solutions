using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Data.SqlClient;
using QuestPDF.Fluent;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SolarQuotationBillingSystem.Helpers;
using SolarQuotationBillingSystem.Models;

namespace SolarQuotationBillingSystem.ViewModels
{
    public partial class InvoiceViewModel : ObservableObject
    {
        private readonly MainViewModel? _mainViewModel;
        private int _quotationId;

        // Customer Details
        [ObservableProperty] private string customerName = string.Empty;
        [ObservableProperty] private string mobile = string.Empty;
        [ObservableProperty] private string address = string.Empty;
        [ObservableProperty] private string city = string.Empty;
        [ObservableProperty] private string gstNumber = string.Empty;
        
        // Company Details
        [ObservableProperty] private string myCompanyName = "ADISH ENTERPRISES";
        [ObservableProperty] private string myCompanyPAN = string.Empty;
        [ObservableProperty] private string companyGstNumber = string.Empty;

        // Invoice Details
        [ObservableProperty] private string invoiceNo = "ADP-01";
        [ObservableProperty] private DateTime invoiceDate = DateTime.Now;
        [ObservableProperty] private string referenceQuotation = string.Empty;

        // System Specs
        [ObservableProperty] private decimal systemCapacityKW;
        [ObservableProperty] private decimal totalSystemCost;
        [ObservableProperty] private decimal subsidy;
        [ObservableProperty] private decimal netPayable;
        [ObservableProperty] private decimal grossTotal;
        
        // GST Properties
        [ObservableProperty] private decimal totalTaxableAmount;
        [ObservableProperty] private decimal totalCGST;
        [ObservableProperty] private decimal totalSGST;
        [ObservableProperty] private decimal totalIGST;
        [ObservableProperty] private decimal roundOff;

        [ObservableProperty] private string amountInWords = string.Empty;

        // Payment Mode Details
        public ObservableCollection<string> PaymentModes { get; } = new() { "Cash", "UPI", "Bank Transfer", "Cheque" };
        [ObservableProperty] private string selectedPaymentMode = "Cash";
        [ObservableProperty] private string paymentRefNo = string.Empty;
        [ObservableProperty] private string paymentRefLabel = string.Empty;
        [ObservableProperty] private bool isRefNoVisible = false;

        partial void OnSelectedPaymentModeChanged(string value)
        {
            switch (value)
            {
                case "UPI":
                    PaymentRefLabel = "UPI Transaction ID / Ref No:";
                    IsRefNoVisible = true;
                    break;
                case "Bank Transfer":
                    PaymentRefLabel = "Bank Transaction / UTR No:";
                    IsRefNoVisible = true;
                    break;
                case "Cheque":
                    PaymentRefLabel = "Cheque Number:";
                    IsRefNoVisible = true;
                    break;
                default: // Cash
                    PaymentRefLabel = string.Empty;
                    PaymentRefNo = string.Empty;
                    IsRefNoVisible = false;
                    break;
            }
        }

        public string GetPaymentRefHeader()
        {
            return SelectedPaymentMode switch
            {
                "UPI" => "UPI Ref No",
                "Bank Transfer" => "Transaction/UTR No",
                "Cheque" => "Cheque No",
                _ => "Ref No"
            };
        }
        
        public ObservableCollection<QuotationItemModel> InvoiceItems { get; set; }

        public InvoiceViewModel()
        {
            InvoiceItems = new ObservableCollection<QuotationItemModel>();
        }

        public InvoiceViewModel(MainViewModel mainViewModel, int quotationId)
        {
            _mainViewModel = mainViewModel;
            _quotationId = quotationId;
            InvoiceItems = new ObservableCollection<QuotationItemModel>();
            
            _ = LoadDataAsync();
        }

        public async Task LoadInvoiceDataAsync(int quotationId)
        {
            _quotationId = quotationId;
            InvoiceItems ??= new ObservableCollection<QuotationItemModel>();
            InvoiceItems.Clear();
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                await conn.OpenAsync();

                // Load Settings
                using (var cmdSettings = new SqlCommand("SELECT TOP 1 CompanyName, GSTNumber, CompanyPAN FROM Settings", conn))
                using (var readerSettings = await cmdSettings.ExecuteReaderAsync())
                {
                    if (await readerSettings.ReadAsync())
                    {
                        string cName = readerSettings["CompanyName"]?.ToString() ?? "";
                        if (!string.IsNullOrWhiteSpace(cName)) MyCompanyName = cName;
                        CompanyGstNumber = readerSettings["GSTNumber"]?.ToString() ?? "";
                        MyCompanyPAN = readerSettings["CompanyPAN"]?.ToString() ?? "";
                    }
                }

                // Load Invoice Header
                var query = @"
                    SELECT q.*, c.CustomerName, c.Mobile, c.Address, c.City, c.GSTNumber 
                    FROM Quotation q
                    INNER JOIN Customers c ON q.CustomerID = c.CustomerID
                    WHERE q.QuotationID = @id;
                ";
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", _quotationId);
                
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    CustomerName = reader["CustomerName"]?.ToString() ?? "";
                    Mobile = reader["Mobile"]?.ToString() ?? "";
                    Address = reader["Address"]?.ToString() ?? "";
                    City = reader["City"]?.ToString() ?? "";
                    GstNumber = reader["GSTNumber"]?.ToString() ?? "";

                    ReferenceQuotation = reader["QuotationNo"]?.ToString() ?? "";
                    SystemCapacityKW = reader["SystemCapacityKW"] != DBNull.Value ? Convert.ToDecimal(reader["SystemCapacityKW"]) : 0;
                    TotalSystemCost = reader["GrandTotal"] != DBNull.Value ? Convert.ToDecimal(reader["GrandTotal"]) : 0;
                    Subsidy = reader["Subsidy"] != DBNull.Value ? Convert.ToDecimal(reader["Subsidy"]) : 0;
                    NetPayable = reader["NetPayable"] != DBNull.Value ? Convert.ToDecimal(reader["NetPayable"]) : TotalSystemCost + Subsidy;
                    
                    TotalTaxableAmount = reader["Subtotal"] != DBNull.Value ? Convert.ToDecimal(reader["Subtotal"]) : 0;
                    TotalCGST = reader["TotalCGST"] != DBNull.Value ? Convert.ToDecimal(reader["TotalCGST"]) : 0;
                    TotalSGST = reader["TotalSGST"] != DBNull.Value ? Convert.ToDecimal(reader["TotalSGST"]) : 0;
                    TotalIGST = reader["TotalIGST"] != DBNull.Value ? Convert.ToDecimal(reader["TotalIGST"]) : 0;
                    RoundOff = reader["RoundOff"] != DBNull.Value ? Convert.ToDecimal(reader["RoundOff"]) : 0;
                    
                    GrossTotal = Math.Round(TotalTaxableAmount + TotalCGST + TotalSGST);

                    AmountInWords = reader["AmountInWords"]?.ToString() ?? $"Rupees {NetPayable:N2} Only"; 

                    bool needsInvoiceNoSave = false;
                    if (reader["InvoiceNo"] != DBNull.Value && !string.IsNullOrWhiteSpace(reader["InvoiceNo"]?.ToString()))
                    {
                        InvoiceNo = reader["InvoiceNo"]!.ToString()!;
                    }
                    else
                    {
                        needsInvoiceNoSave = true;
                    }

                    if (reader["PaymentMode"] != DBNull.Value && !string.IsNullOrWhiteSpace(reader["PaymentMode"]?.ToString()))
                    {
                        SelectedPaymentMode = reader["PaymentMode"]!.ToString()!;
                    }
                    if (reader["PaymentRefNo"] != DBNull.Value)
                    {
                        PaymentRefNo = reader["PaymentRefNo"]!.ToString()!;
                    }
                    
                    await reader.CloseAsync();

                    // Load Items from QuotationItems table
                    var itemsCmd = new SqlCommand(@"
                        SELECT ProductName, Description, Brand, Qty, HSNCode, Unit, Rate, GSTPercentage, TaxableAmount, CGST, SGST, IGST, Amount 
                        FROM QuotationItems 
                        WHERE QuotationID = @id
                        ORDER BY QuotationItemID
                    ", conn);
                    itemsCmd.Parameters.AddWithValue("@id", _quotationId);


                    using var itemsReader = await itemsCmd.ExecuteReaderAsync();
                    while (await itemsReader.ReadAsync())
                    {
                        decimal qty = 1;
                        if (itemsReader["Qty"] != DBNull.Value && decimal.TryParse(itemsReader["Qty"].ToString(), out decimal q)) qty = q;

                        InvoiceItems.Add(new QuotationItemModel
                        {
                            Component = itemsReader["ProductName"]?.ToString() ?? "",
                            Description = itemsReader["Description"]?.ToString() ?? "",
                            Brand = itemsReader["Brand"]?.ToString() ?? "",
                            Quantity = qty,
                            HSNCode = itemsReader["HSNCode"]?.ToString() ?? "",
                            Unit = itemsReader["Unit"]?.ToString() ?? "",
                            Rate = itemsReader["Rate"] != DBNull.Value ? Convert.ToDecimal(itemsReader["Rate"]) : 0,
                            GSTPercentage = itemsReader["GSTPercentage"] != DBNull.Value ? Convert.ToDecimal(itemsReader["GSTPercentage"]) : 0,
                            TaxableAmount = itemsReader["TaxableAmount"] != DBNull.Value ? Convert.ToDecimal(itemsReader["TaxableAmount"]) : 0,
                            CGST = itemsReader["CGST"] != DBNull.Value ? Convert.ToDecimal(itemsReader["CGST"]) : 0,
                            SGST = itemsReader["SGST"] != DBNull.Value ? Convert.ToDecimal(itemsReader["SGST"]) : 0,
                            IGST = itemsReader["IGST"] != DBNull.Value ? Convert.ToDecimal(itemsReader["IGST"]) : 0,
                            Total = itemsReader["Amount"] != DBNull.Value ? Convert.ToDecimal(itemsReader["Amount"]) : 0
                        });
                    }

                    if (needsInvoiceNoSave)
                    {
                        await reader.CloseAsync();
                        InvoiceNo = await GenerateNextInvoiceNoAsync(conn);
                        using var saveCmd = new SqlCommand("UPDATE Quotation SET InvoiceNo = @invNo WHERE QuotationID = @id", conn);
                        saveCmd.Parameters.AddWithValue("@invNo", InvoiceNo);
                        saveCmd.Parameters.AddWithValue("@id", _quotationId);
                        await saveCmd.ExecuteNonQueryAsync();
                    }

                    using var settingsCmd = new SqlCommand("SELECT TOP 1 GSTNumber FROM Settings", conn);
                    using var settingsReader = await settingsCmd.ExecuteReaderAsync();
                    if (await settingsReader.ReadAsync())
                    {
                        CompanyGstNumber = settingsReader["GSTNumber"]?.ToString() ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading quotation data: {ex.Message}");
            }
        }

        private async Task<string> GenerateNextInvoiceNoAsync(SqlConnection conn)
        {
            try
            {
                var cmd = new SqlCommand(@"
                    SELECT ISNULL(MAX(
                        CAST(
                            CASE 
                                WHEN CHARINDEX('-', InvoiceNo) > 0 
                                THEN SUBSTRING(InvoiceNo, CHARINDEX('-', InvoiceNo) + 1, 10)
                                ELSE '0'
                            END AS INT)
                    ), 0) 
                    FROM Quotation 
                    WHERE InvoiceNo IS NOT NULL AND InvoiceNo LIKE 'ADP-%'", conn);
                var res = await cmd.ExecuteScalarAsync();
                int maxVal = res != DBNull.Value ? Convert.ToInt32(res) : 0;
                return $"ADP-{(maxVal + 1):D2}";
            }
            catch
            {
                return "ADP-01";
            }
        }

        [RelayCommand]
        private void PrintFinalBill()
        {
            try
            {
                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

                var document = new Helpers.InvoiceDocument(this);
                string filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"Invoice_{CustomerName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                
                document.GeneratePdf(filePath);
                
                var previewWindow = new Views.InvoicePreviewWindow(filePath);
                previewWindow.Owner = Application.Current.MainWindow;
                previewWindow.ShowDialog();

                // Update Quotation Status to Paid and save Payment Details & InvoiceNo in Database
                using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();
                using var cmd = new SqlCommand(@"
                    UPDATE Quotation 
                    SET Status = 'Paid', 
                        InvoiceNo = @invNo,
                        PaymentMode = @mode, 
                        PaymentRefNo = @refNo 
                    WHERE QuotationID = @id;
                    
                    IF NOT EXISTS (SELECT 1 FROM Invoice WHERE InvoiceNo = @invNo)
                    BEGIN
                        INSERT INTO Invoice (InvoiceNo, InvoiceDate, CustomerID, Subtotal, TotalGST, TotalCGST, TotalSGST, TotalIGST, RoundOff, Discount, GrandTotal, Paid, Balance, PaymentMode, AmountInWords, PaymentStatus)
                        VALUES (@invNo, GETDATE(), (SELECT CustomerID FROM Quotation WHERE QuotationID = @id), @subtotal, @totalGst, @cgst, @sgst, @igst, @roundOff, 0, @grandTotal, 0, @grandTotal, @mode, @words, 'Pending');
                    END
                ", conn);
                cmd.Parameters.AddWithValue("@invNo", InvoiceNo);
                cmd.Parameters.AddWithValue("@mode", SelectedPaymentMode ?? "Cash");
                cmd.Parameters.AddWithValue("@refNo", string.IsNullOrWhiteSpace(PaymentRefNo) ? (object)DBNull.Value : PaymentRefNo);
                cmd.Parameters.AddWithValue("@id", _quotationId);
                
                cmd.Parameters.AddWithValue("@subtotal", TotalTaxableAmount);
                cmd.Parameters.AddWithValue("@totalGst", TotalCGST + TotalSGST + TotalIGST);
                cmd.Parameters.AddWithValue("@cgst", TotalCGST);
                cmd.Parameters.AddWithValue("@sgst", TotalSGST);
                cmd.Parameters.AddWithValue("@igst", TotalIGST);
                cmd.Parameters.AddWithValue("@roundOff", RoundOff);
                cmd.Parameters.AddWithValue("@grandTotal", NetPayable);
                cmd.Parameters.AddWithValue("@words", AmountInWords ?? "");
                cmd.ExecuteNonQuery();

                MessageBox.Show($"Final Bill {InvoiceNo} generated successfully! Status updated to Paid.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating bill: {ex.Message}");
            }
        }

        public void ExportPdfDirect()
        {
            try
            {
                string filePath = GeneratePdfFile();
                
                var previewWindow = new Views.InvoicePreviewWindow(filePath);
                previewWindow.Owner = Application.Current.MainWindow;
                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating bill PDF: {ex.Message}");
            }
        }

        public string GeneratePdfFile()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            var document = new Helpers.InvoiceDocument(this);
            string filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"Invoice_{CustomerName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            document.GeneratePdf(filePath);
            return filePath;
        }

        [RelayCommand]
        private void SendWhatsApp()
        {
            if (string.IsNullOrWhiteSpace(Mobile))
            {
                MessageBox.Show("Please enter a valid mobile number for the customer.", "WhatsApp Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                string pdfPath = GeneratePdfFile();
                string message = $@"Dear {CustomerName},

🙏 Thank you for choosing ADISH ENTERPRISES.

Please find your Tax Invoice attached.

📄 Invoice No : {InvoiceNo}
📅 Date : {InvoiceDate:dd MMM yyyy}
💰 Total Amount : ₹{GrossTotal}

If you have any questions regarding this invoice, please feel free to contact us.

Thank you for your trust and business.

Regards,
ADISH ENTERPRISES
Complete Solar Solution

📞 +91-9407299837
📧 adishenterprises09@gmail.com";

                var previewVm = new WhatsAppPreviewViewModel
                {
                    MobileNumber = Mobile,
                    MessageText = message,
                    CustomerName = CustomerName,
                    DocumentType = "Invoice",
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
        private void GoBack()
        {
            if (_mainViewModel != null)
            {
                var module = new QuotationBillingModuleViewModel(_mainViewModel);
                module.NavBilling(); // Go straight to the Billing tab
                _mainViewModel.NavigateTo(module);
            }
        }
    }
}