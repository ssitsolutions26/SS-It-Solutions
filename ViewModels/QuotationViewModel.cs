using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Data.SqlClient;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuestPDF.Fluent;
using SolarQuotationBillingSystem.Helpers;
using SolarQuotationBillingSystem.Models;

namespace SolarQuotationBillingSystem.ViewModels
{
    public partial class QuotationViewModel : ObservableObject
    {
        private int? _editingQuotationId = null;

        // -----------------------------------------------------------
        // MY COMPANY DETAILS
        // -----------------------------------------------------------
        [ObservableProperty]
        private string myCompanyName = "ADISH ENTERPRISES";
        
        [ObservableProperty]
        private string myCompanyPAN = string.Empty;
        
        [ObservableProperty]
        private string myCompanyGstNumber = string.Empty;

        // -----------------------------------------------------------
        // CUSTOMER DETAILS
        // -----------------------------------------------------------
        [ObservableProperty]
        private string customerName = string.Empty;
        
        [ObservableProperty]
        private string companyName = string.Empty;

        [ObservableProperty]
        private string customerStateCode = string.Empty;

        [ObservableProperty]
        private string companyStateCode = string.Empty;

        [ObservableProperty]
        private string contactPerson = string.Empty;

        [ObservableProperty]
        private string mobile = string.Empty;

        [ObservableProperty]
        private string alternateMobile = string.Empty;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string gstNumber = string.Empty;

        [ObservableProperty]
        private string address = string.Empty;

        [ObservableProperty]
        private string city = string.Empty;

        [ObservableProperty]
        private string district = string.Empty;

        [ObservableProperty]
        private string state = string.Empty;

        [ObservableProperty]
        private string pinCode = string.Empty;

        // -----------------------------------------------------------
        // QUOTATION DETAILS & SOLAR SPECS
        // -----------------------------------------------------------
        [ObservableProperty]
        private string saveButtonText = "Generate Quotation";
        
        private Action _onClose;

        [ObservableProperty]
        private string quotationNo = string.Empty;

        [ObservableProperty]
        private DateTime quotationDate = DateTime.Now;

        [ObservableProperty]
        private DateTime validUntil = DateTime.Now.AddDays(15);

        [ObservableProperty]
        private string salesExecutive = string.Empty;

        [ObservableProperty]
        private string reference = string.Empty;

        [ObservableProperty]
        private string installationType = "On-Grid";

        [ObservableProperty]
        private string roofType = "RCC Flat Roof";

        [ObservableProperty]
        private string subsidyEligible = "No";

        [ObservableProperty]
        private decimal systemCapacityKW = 3;
        partial void OnSystemCapacityKWChanged(decimal value) => CalculatePMYojanaSubsidy();

        [ObservableProperty]
        private string solarPanelBrand = string.Empty;

        [ObservableProperty]
        private int panelWatt;
        partial void OnPanelWattChanged(int value) => CalculateTotals();

        [ObservableProperty]
        private int noOfPanels;
        partial void OnNoOfPanelsChanged(int value) => CalculateTotals();

        [ObservableProperty]
        private string inverterBrand = string.Empty;

        [ObservableProperty]
        private decimal inverterCapacity;
        partial void OnInverterCapacityChanged(decimal value) => CalculateTotals();

        [ObservableProperty]
        private string battery = string.Empty;

        [ObservableProperty]
        private string mountingStructure = "GI Structure";

        [ObservableProperty]
        private string earthingKit = "3 Pits";

        [ObservableProperty]
        private string lightningArrestor = "Conventional";

        [ObservableProperty]
        private string mC4Connector = "Standard";

        [ObservableProperty]
        private string dCCable = "4 sqmm";

        [ObservableProperty]
        private string aCCable = "4 Core";

        // -----------------------------------------------------------
        // PRICING / COSTS
        // -----------------------------------------------------------
        [ObservableProperty]
        private decimal panelCostPerWatt = 25; // Example base price
        partial void OnPanelCostPerWattChanged(decimal value) => CalculateTotals();

        [ObservableProperty]
        private decimal _totalSystemCost = 200000;
        partial void OnTotalSystemCostChanged(decimal value) => CalculateTotals();

        [ObservableProperty]
        private decimal _subsidy = 78000;
        partial void OnSubsidyChanged(decimal value) => CalculateTotals();

        [ObservableProperty]
        private decimal _netPayable;

        [ObservableProperty]
        private decimal _grossTotal;

        [ObservableProperty]
        private decimal _totalTaxableAmount = 0;

        [ObservableProperty]
        private decimal _overallGstPercentage = 9;
        partial void OnOverallGstPercentageChanged(decimal value) => CalculateTotals();

        [ObservableProperty]
        private decimal _totalCGST = 0;

        [ObservableProperty]
        private decimal _totalSGST = 0;

        [ObservableProperty]
        private decimal _totalIGST = 0;

        [ObservableProperty]
        private decimal _roundOff = 0;

        [ObservableProperty]
        private decimal inverterCost = 0;
        partial void OnInverterCostChanged(decimal value) => CalculateTotals();

        [ObservableProperty]
        private decimal batteryCost = 0;
        partial void OnBatteryCostChanged(decimal value) => CalculateTotals();

        [ObservableProperty]
        private decimal accessoriesCost = 0;
        partial void OnAccessoriesCostChanged(decimal value) => CalculateTotals();

        [ObservableProperty]
        private decimal installationCharges = 0;
        partial void OnInstallationChargesChanged(decimal value) => CalculateTotals();

        [ObservableProperty]
        private decimal transportation = 0;
        partial void OnTransportationChanged(decimal value) => CalculateTotals();

        [ObservableProperty]
        private decimal otherCharges = 0;
        partial void OnOtherChargesChanged(decimal value) => CalculateTotals();
        // -----------------------------------------------------------
        // CALCULATED TOTALS
        // -----------------------------------------------------------
        [ObservableProperty]
        private string amountInWords = string.Empty;

        // -----------------------------------------------------------
        // COLLECTIONS & DATA GRIDS
        // -----------------------------------------------------------
        public ObservableCollection<QuotationItemModel> QuotationItems { get; set; }

        public QuotationViewModel()
        {
            QuotationItems = new ObservableCollection<QuotationItemModel>();
            QuotationItems.CollectionChanged += (s, e) => 
            {
                if (e.NewItems != null)
                {
                    foreach (QuotationItemModel item in e.NewItems)
                        item.PropertyChanged += Item_PropertyChanged;
                }
                if (e.OldItems != null)
                {
                    foreach (QuotationItemModel item in e.OldItems)
                        item.PropertyChanged -= Item_PropertyChanged;
                }
                CalculateTotals();
            };

            LoadCompanySettings();
            
            // Default Items from the PDF
            QuotationItems.Add(new QuotationItemModel { Component = "Solar Panels", Description = "High-efficiency Mono PERC Half-Cut Panels (3kW Total)", Quantity = 1, Brand = "Havells" });
            QuotationItems.Add(new QuotationItemModel { Component = "Solar Inverter", Description = "3kW On-Grid High-Efficiency Solar Inverter", Quantity = 1, Brand = "Havells" });
            QuotationItems.Add(new QuotationItemModel { Component = "Structure", Description = "Heavy-duty Hot Dip Galvanized (GI) Structure", Quantity = 1, Brand = "Premium" });
            QuotationItems.Add(new QuotationItemModel { Component = "DC/AC Cables", Description = "Solar Specific Copper Cables (UV Protected)", Quantity = 1, Brand = "Havells" });
            QuotationItems.Add(new QuotationItemModel { Component = "Safety & Earthing", Description = "DCDB/ACDB Boxes, Lightning Arrester (LA) & Earthing Kit", Quantity = 1, Brand = "Standard" });

            // Generate next serial QuotationNo: ADP-01
            GenerateNextQuotationNo();

            // Initial calculations
            CalculateTotals();
        }

        private void Item_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(QuotationItemModel.Total) ||
                e.PropertyName == nameof(QuotationItemModel.TaxableAmount) ||
                e.PropertyName == nameof(QuotationItemModel.CGST) ||
                e.PropertyName == nameof(QuotationItemModel.SGST) ||
                e.PropertyName == nameof(QuotationItemModel.IGST))
            {
                CalculateTotals();
            }
        }

        private void LoadCompanySettings()
        {
            try
            {
                using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();
                var cmd = new SqlCommand("SELECT TOP 1 CompanyName, GSTNumber, CompanyPAN, StateCode FROM Settings", conn);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string cName = reader["CompanyName"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(cName)) MyCompanyName = cName;
                    MyCompanyGstNumber = reader["GSTNumber"]?.ToString() ?? "";
                    MyCompanyPAN = reader["CompanyPAN"]?.ToString() ?? "";
                    CompanyStateCode = reader["StateCode"]?.ToString() ?? "";
                }
            }
            catch { }
        }

        private void GenerateNextQuotationNo()
        {
            try
            {
                using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();
                string prefix = "ADP-";
                var cmd = new SqlCommand(@"
                    SELECT TOP 1 QuotationNo FROM Quotation 
                    WHERE QuotationNo LIKE @prefix + '%' 
                    ORDER BY QuotationID DESC
                ", conn);
                cmd.Parameters.AddWithValue("@prefix", prefix);
                var lastNo = cmd.ExecuteScalar();

                int nextSerial = 1;
                if (lastNo != null)
                {
                    string lastStr = lastNo.ToString()!;
                    string[] parts = lastStr.Split('-');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int lastSerial))
                    {
                        nextSerial = lastSerial + 1;
                    }
                }
                QuotationNo = $"ADP-{nextSerial:D2}";
            }
            catch
            {
                // Fallback if DB not ready
                QuotationNo = $"ADP-01";
            }
        }

        public QuotationViewModel(int customerId) : this()
        {
            LoadCustomerData(customerId);
        }

        public QuotationViewModel(int quotationId, bool isEditMode, Action onClose = null) : this()
        {
            _onClose = onClose;
            if (isEditMode)
            {
                SaveButtonText = "Update Quotation";
                _editingQuotationId = quotationId;
                LoadQuotationDataAsync(quotationId);
            }
        }

        [RelayCommand]
        private void Close()
        {
            _onClose?.Invoke();
        }

        private void LoadCustomerData(int customerId)
        {
            try
            {
                using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();
                var cmd = new SqlCommand("SELECT * FROM Customers WHERE CustomerID = @id", conn);
                cmd.Parameters.AddWithValue("@id", customerId);
                
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    CustomerName = reader["CustomerName"]?.ToString() ?? "";
                    CompanyName = reader["CompanyName"]?.ToString() ?? "";
                    ContactPerson = reader["ContactPerson"]?.ToString() ?? "";
                    Mobile = reader["Mobile"]?.ToString() ?? "";
                    AlternateMobile = reader["AlternateMobile"]?.ToString() ?? "";
                    Email = reader["Email"]?.ToString() ?? "";
                    GstNumber = reader["GSTNumber"]?.ToString() ?? "";
                    Address = reader["Address"]?.ToString() ?? "";
                    City = reader["City"]?.ToString() ?? "";
                    District = reader["District"]?.ToString() ?? "";
                    State = reader["State"]?.ToString() ?? "";
                    CustomerStateCode = reader["StateCode"]?.ToString() ?? "";
                    PinCode = reader["PINCode"]?.ToString() ?? "";
                }
                
                CalculateTotals(); // Recalculate based on state code
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customer data: {ex.Message}");
            }
        }

        public async Task LoadQuotationDataAsync(int quotationId)
        {
            try
            {
                using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"
                    SELECT q.*, c.* 
                    FROM Quotation q
                    INNER JOIN Customers c ON q.CustomerID = c.CustomerID
                    WHERE q.QuotationID = @id
                ", conn);
                cmd.Parameters.AddWithValue("@id", quotationId);
                
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    // Customer Details
                    CustomerName = reader["CustomerName"]?.ToString() ?? "";
                    CompanyName = reader["CompanyName"]?.ToString() ?? "";
                    ContactPerson = reader["ContactPerson"]?.ToString() ?? "";
                    Mobile = reader["Mobile"]?.ToString() ?? "";
                    AlternateMobile = reader["AlternateMobile"]?.ToString() ?? "";
                    Email = reader["Email"]?.ToString() ?? "";
                    GstNumber = reader["GSTNumber"]?.ToString() ?? "";
                    Address = reader["Address"]?.ToString() ?? "";
                    City = reader["City"]?.ToString() ?? "";
                    District = reader["District"]?.ToString() ?? "";
                    State = reader["State"]?.ToString() ?? "";
                    CustomerStateCode = reader["StateCode"]?.ToString() ?? "";
                    PinCode = reader["PINCode"]?.ToString() ?? "";

                    // Quotation Details
                    QuotationNo = reader["QuotationNo"]?.ToString() ?? "";
                    if (reader["QuotationDate"] != DBNull.Value) QuotationDate = Convert.ToDateTime(reader["QuotationDate"]);
                    if (reader["ValidUntil"] != DBNull.Value) ValidUntil = Convert.ToDateTime(reader["ValidUntil"]);
                    SalesExecutive = reader["SalesExecutive"]?.ToString() ?? "";
                    Reference = reader["Reference"]?.ToString() ?? "";
                    InstallationType = reader["InstallationType"]?.ToString() ?? "";
                    RoofType = reader["RoofType"]?.ToString() ?? "";
                    SubsidyEligible = reader["SubsidyEligible"]?.ToString() ?? "";
                    SystemCapacityKW = reader["SystemCapacityKW"] != DBNull.Value ? Convert.ToDecimal(reader["SystemCapacityKW"]) : 0;
                    SolarPanelBrand = reader["SolarPanelBrand"]?.ToString() ?? "";
                    PanelWatt = reader["PanelWatt"] != DBNull.Value ? Convert.ToInt32(reader["PanelWatt"]) : 0;
                    NoOfPanels = reader["NoOfPanels"] != DBNull.Value ? Convert.ToInt32(reader["NoOfPanels"]) : 0;
                    InverterBrand = reader["InverterBrand"]?.ToString() ?? "";
                    InverterCapacity = reader["InverterCapacity"] != DBNull.Value ? Convert.ToDecimal(reader["InverterCapacity"]) : 0;
                    Battery = reader["Battery"]?.ToString() ?? "";
                    MountingStructure = reader["MountingStructure"]?.ToString() ?? "";
                    EarthingKit = reader["EarthingKit"]?.ToString() ?? "";
                    LightningArrestor = reader["LightningArrestor"]?.ToString() ?? "";
                    MC4Connector = reader["MC4Connector"]?.ToString() ?? "";
                    DCCable = reader["DCCable"]?.ToString() ?? "";
                    ACCable = reader["ACCable"]?.ToString() ?? "";
                    InstallationCharges = reader["InstallationCharges"] != DBNull.Value ? Convert.ToDecimal(reader["InstallationCharges"]) : 0;
                    Transportation = reader["Transportation"] != DBNull.Value ? Convert.ToDecimal(reader["Transportation"]) : 0;
                    OtherCharges = reader["OtherCharges"] != DBNull.Value ? Convert.ToDecimal(reader["OtherCharges"]) : 0;
                    TotalSystemCost = reader["GrandTotal"] != DBNull.Value ? Convert.ToDecimal(reader["GrandTotal"]) : 0;
                    Subsidy = reader["Subsidy"] != DBNull.Value ? Convert.ToDecimal(reader["Subsidy"]) : 0;
                    NetPayable = reader["NetPayable"] != DBNull.Value ? Convert.ToDecimal(reader["NetPayable"]) : 0;
                    TotalTaxableAmount = reader["Subtotal"] != DBNull.Value ? Convert.ToDecimal(reader["Subtotal"]) : 0;
                    TotalCGST = reader["TotalCGST"] != DBNull.Value ? Convert.ToDecimal(reader["TotalCGST"]) : 0;
                    TotalSGST = reader["TotalSGST"] != DBNull.Value ? Convert.ToDecimal(reader["TotalSGST"]) : 0;
                    TotalIGST = reader["TotalIGST"] != DBNull.Value ? Convert.ToDecimal(reader["TotalIGST"]) : 0;
                    RoundOff = reader["RoundOff"] != DBNull.Value ? Convert.ToDecimal(reader["RoundOff"]) : 0;
                    AmountInWords = reader["AmountInWords"]?.ToString() ?? "";
                }
                await reader.CloseAsync();

                // Load QuotationItems from database
                var itemsCmd = new SqlCommand(@"
                    SELECT ProductName, Description, Brand, Qty, HSNCode, Unit, Rate, GSTPercentage, TaxableAmount, CGST, SGST, IGST, Amount 
                    FROM QuotationItems 
                    WHERE QuotationID = @id
                    ORDER BY QuotationItemID
                ", conn);
                itemsCmd.Parameters.AddWithValue("@id", quotationId);

                using var itemsReader = await itemsCmd.ExecuteReaderAsync();
                var loadedItems = new System.Collections.Generic.List<QuotationItemModel>();
                while (await itemsReader.ReadAsync())
                {
                    decimal qty = 1;
                    if (itemsReader["Qty"] != DBNull.Value && decimal.TryParse(itemsReader["Qty"].ToString(), out decimal q)) qty = q;

                    var item = new QuotationItemModel
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
                    };
                    item.PropertyChanged += Item_PropertyChanged;
                    loadedItems.Add(item);
                }

                // If items found in DB, replace default items
                if (loadedItems.Count > 0)
                {
                    QuotationItems.Clear();
                    foreach (var item in loadedItems)
                    {
                        QuotationItems.Add(item);
                    }
                }
                
                // Reverse calculate overall GST % based on loaded totals to restore UI state
                if (NetPayable > 0)
                {
                    decimal totalTax = TotalCGST + TotalSGST + TotalIGST;
                    OverallGstPercentage = Math.Round((totalTax / NetPayable) * 100);
                }
                else
                {
                    OverallGstPercentage = 0;
                }
                
                CalculateTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading quotation data: {ex.Message}");
            }
        }

        private void CalculatePMYojanaSubsidy()
        {
            // PM Surya Ghar Yojana Subsidy Calculation:
            // Up to 2 kW: 30,000 per kW
            // Additional up to 3 kW: 18,000 per kW
            // Above 3 kW: fixed at 78,000
            
            decimal capacity = SystemCapacityKW;
            decimal calculatedSubsidy = 0;
            
            if (capacity > 0)
            {
                if (capacity <= 2)
                {
                    calculatedSubsidy = capacity * 30000m;
                }
                else if (capacity <= 3)
                {
                    calculatedSubsidy = 60000m + ((capacity - 2) * 18000m);
                }
                else
                {
                    calculatedSubsidy = 78000m;
                }
            }
            
            Subsidy = calculatedSubsidy;
            CalculateTotals();
        }

        private void CalculateTotals()
        {
            bool isIgst = !string.IsNullOrWhiteSpace(CompanyStateCode) && 
                          !string.IsNullOrWhiteSpace(CustomerStateCode) && 
                          CompanyStateCode != CustomerStateCode;

            decimal tempItemTotal = 0;

            foreach (var item in QuotationItems)
            {
                item.Total = item.Quantity * item.Rate; // Simple total if they want to use rates at item level, else we just ignore.
                tempItemTotal += item.Total;
            }

            // In Option 1, we calculate GST on TotalSystemCost manually
            TotalTaxableAmount = TotalSystemCost;
            
            decimal additionalCharges = InstallationCharges + Transportation + OtherCharges;
            
            decimal exactTotal = TotalTaxableAmount + additionalCharges - Subsidy; // Subsidy deducted from total
            RoundOff = Math.Round(exactTotal) - exactTotal;
            NetPayable = Math.Round(exactTotal);
            
            decimal totalGstAmount = NetPayable * (OverallGstPercentage / 100m);
            
            if (isIgst)
            {
                TotalIGST = totalGstAmount;
                TotalCGST = 0;
                TotalSGST = 0;
            }
            else
            {
                TotalCGST = totalGstAmount / 2m;
                TotalSGST = totalGstAmount / 2m;
                TotalIGST = 0;
            }
            
            GrossTotal = Math.Round(NetPayable + totalGstAmount);
            
            AmountInWords = $"Rupees {NetPayable:N2} Only";
        }

        [RelayCommand]
        private void AddProduct()
        {
            var newItem = new QuotationItemModel { Component = "New Component", Description = "", Quantity = 1, Brand = "" };
            newItem.PropertyChanged += Item_PropertyChanged;
            QuotationItems.Add(newItem);
        }

        // -----------------------------------------------------------
        // COMMANDS
        // -----------------------------------------------------------
        [RelayCommand]
        private async Task SaveCustomer()
        {
            if (string.IsNullOrWhiteSpace(CustomerName) || string.IsNullOrWhiteSpace(Mobile))
            {
                MessageBox.Show("Customer Name and Mobile are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"
                    INSERT INTO Customers (CustomerName, CompanyName, ContactPerson, Mobile, AlternateMobile, Email, GSTNumber, Address, City, District, State, PINCode)
                    VALUES (@CustomerName, @CompanyName, @ContactPerson, @Mobile, @AlternateMobile, @Email, @GSTNumber, @Address, @City, @District, @State, @PINCode);
                    SELECT SCOPE_IDENTITY();
                ", conn);

                cmd.Parameters.AddWithValue("@CustomerName", CustomerName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@CompanyName", CompanyName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ContactPerson", ContactPerson ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Mobile", Mobile ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@AlternateMobile", AlternateMobile ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", Email ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@GSTNumber", GstNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Address", Address ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@City", City ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@District", District ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@State", State ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PINCode", PinCode ?? (object)DBNull.Value);

                var customerId = await cmd.ExecuteScalarAsync();
                MessageBox.Show($"Customer saved successfully with ID: {customerId}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving customer: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task GenerateQuotation()
        {
            if (string.IsNullOrWhiteSpace(CustomerName))
            {
                MessageBox.Show("Please fill out and Save Customer Details first so we have a Customer ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                await conn.OpenAsync();
                
                // For demonstration, fetch top 1 customer ID if none selected (Ideally, should track CustomerID in ViewModel)
                var cmdCust = new SqlCommand("SELECT TOP 1 CustomerID FROM Customers WHERE Mobile = @Mobile", conn);
                cmdCust.Parameters.AddWithValue("@Mobile", Mobile);
                var cidObj = await cmdCust.ExecuteScalarAsync();
                if (cidObj == null)
                {
                    MessageBox.Show("Could not find Customer in DB. Please save the customer first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                int customerId = Convert.ToInt32(cidObj);

                int newQuotationId = 0;
                SqlCommand cmd;
                
                if (_editingQuotationId.HasValue)
                {
                    var cmdUpdate = new SqlCommand(@"
                        UPDATE Quotation SET
                            QuotationDate = @QuotationDate, ValidUntil = @ValidUntil, CustomerID = @CustomerID, SalesExecutive = @SalesExecutive,
                            Reference = @Reference, InstallationType = @InstallationType, RoofType = @RoofType, SubsidyEligible = @SubsidyEligible,
                            SystemCapacityKW = @SystemCapacityKW, SolarPanelBrand = @SolarPanelBrand, PanelWatt = @PanelWatt, NoOfPanels = @NoOfPanels,
                            InverterBrand = @InverterBrand, InverterCapacity = @InverterCapacity, Battery = @Battery, MountingStructure = @MountingStructure,
                            EarthingKit = @EarthingKit, LightningArrestor = @LightningArrestor, MC4Connector = @MC4Connector, DCCable = @DCCable,
                            ACCable = @ACCable, InstallationCharges = @InstallationCharges, Transportation = @Transportation, OtherCharges = @OtherCharges,
                            Subtotal = @Subtotal, TotalGST = @TotalGST, TotalCGST = @TotalCGST, TotalSGST = @TotalSGST, TotalIGST = @TotalIGST, RoundOff = @RoundOff,
                            Subsidy = @Subsidy, Discount = @Discount, GrandTotal = @GrandTotal, NetPayable = @NetPayable, AmountInWords = @AmountInWords
                        WHERE QuotationID = @QuotationID
                    ", conn);
                    
                    cmdUpdate.Parameters.AddWithValue("@QuotationID", _editingQuotationId.Value);
                    // Add parameters (shared below)
                    cmd = cmdUpdate;
                    
                    // Delete old items
                    var cmdDeleteItems = new SqlCommand("DELETE FROM QuotationItems WHERE QuotationID = @QuotationID", conn);
                    cmdDeleteItems.Parameters.AddWithValue("@QuotationID", _editingQuotationId.Value);
                    await cmdDeleteItems.ExecuteNonQueryAsync();
                    
                    newQuotationId = _editingQuotationId.Value;
                }
                else
                {
                    cmd = new SqlCommand(@"
                        INSERT INTO Quotation (
                            QuotationNo, QuotationDate, ValidUntil, CustomerID, SalesExecutive, Reference, InstallationType,
                            RoofType, SubsidyEligible, SystemCapacityKW, SolarPanelBrand, PanelWatt, NoOfPanels, InverterBrand,
                            InverterCapacity, Battery, MountingStructure, EarthingKit, LightningArrestor, MC4Connector,
                            DCCable, ACCable, InstallationCharges, Transportation, OtherCharges, Subtotal, TotalGST,
                            TotalCGST, TotalSGST, TotalIGST, RoundOff,
                            Subsidy, Discount, GrandTotal, NetPayable, AmountInWords
                        ) VALUES (
                            @QuotationNo, @QuotationDate, @ValidUntil, @CustomerID, @SalesExecutive, @Reference, @InstallationType,
                            @RoofType, @SubsidyEligible, @SystemCapacityKW, @SolarPanelBrand, @PanelWatt, @NoOfPanels, @InverterBrand,
                            @InverterCapacity, @Battery, @MountingStructure, @EarthingKit, @LightningArrestor, @MC4Connector,
                            @DCCable, @ACCable, @InstallationCharges, @Transportation, @OtherCharges, @Subtotal, @TotalGST,
                            @TotalCGST, @TotalSGST, @TotalIGST, @RoundOff,
                            @Subsidy, @Discount, @GrandTotal, @NetPayable, @AmountInWords
                        );
                        SELECT SCOPE_IDENTITY();
                    ", conn);
                }


                cmd.Parameters.AddWithValue("@QuotationNo", QuotationNo);
                cmd.Parameters.AddWithValue("@QuotationDate", QuotationDate);
                cmd.Parameters.AddWithValue("@ValidUntil", ValidUntil);
                cmd.Parameters.AddWithValue("@CustomerID", customerId);
                cmd.Parameters.AddWithValue("@SalesExecutive", SalesExecutive ?? string.Empty);
                cmd.Parameters.AddWithValue("@Reference", Reference ?? string.Empty);
                cmd.Parameters.AddWithValue("@InstallationType", InstallationType ?? string.Empty);
                cmd.Parameters.AddWithValue("@RoofType", RoofType ?? string.Empty);
                cmd.Parameters.AddWithValue("@SubsidyEligible", SubsidyEligible ?? string.Empty);
                cmd.Parameters.AddWithValue("@SystemCapacityKW", SystemCapacityKW);
                cmd.Parameters.AddWithValue("@SolarPanelBrand", SolarPanelBrand ?? string.Empty);
                cmd.Parameters.AddWithValue("@PanelWatt", PanelWatt);
                cmd.Parameters.AddWithValue("@NoOfPanels", NoOfPanels);
                cmd.Parameters.AddWithValue("@InverterBrand", InverterBrand ?? string.Empty);
                cmd.Parameters.AddWithValue("@InverterCapacity", InverterCapacity);
                cmd.Parameters.AddWithValue("@Battery", Battery ?? string.Empty);
                cmd.Parameters.AddWithValue("@MountingStructure", MountingStructure ?? string.Empty);
                cmd.Parameters.AddWithValue("@EarthingKit", EarthingKit ?? string.Empty);
                cmd.Parameters.AddWithValue("@LightningArrestor", LightningArrestor ?? string.Empty);
                cmd.Parameters.AddWithValue("@MC4Connector", MC4Connector ?? string.Empty);
                cmd.Parameters.AddWithValue("@DCCable", DCCable ?? string.Empty);
                cmd.Parameters.AddWithValue("@ACCable", ACCable ?? string.Empty);
                cmd.Parameters.AddWithValue("@InstallationCharges", InstallationCharges);
                cmd.Parameters.AddWithValue("@Transportation", Transportation);
                cmd.Parameters.AddWithValue("@OtherCharges", OtherCharges);
                cmd.Parameters.AddWithValue("@Subtotal", TotalTaxableAmount);
                cmd.Parameters.AddWithValue("@TotalGST", TotalCGST + TotalSGST + TotalIGST);
                cmd.Parameters.AddWithValue("@TotalCGST", TotalCGST);
                cmd.Parameters.AddWithValue("@TotalSGST", TotalSGST);
                cmd.Parameters.AddWithValue("@TotalIGST", TotalIGST);
                cmd.Parameters.AddWithValue("@RoundOff", RoundOff);
                cmd.Parameters.AddWithValue("@Subsidy", Subsidy);
                cmd.Parameters.AddWithValue("@Discount", 0);
                cmd.Parameters.AddWithValue("@GrandTotal", TotalSystemCost);
                cmd.Parameters.AddWithValue("@NetPayable", NetPayable);
                cmd.Parameters.AddWithValue("@AmountInWords", AmountInWords ?? string.Empty);

                if (!_editingQuotationId.HasValue)
                {
                    var result = await cmd.ExecuteScalarAsync();
                    newQuotationId = Convert.ToInt32(result);
                }
                else
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // Save all QuotationItems to database
                foreach (var item in QuotationItems)
                {
                    var itemCmd = new SqlCommand(@"
                        INSERT INTO QuotationItems (QuotationID, ProductID, ProductName, Description, Brand, Qty, HSNCode, Unit, Price, Rate, GSTPercentage, GSTAmount, TaxableAmount, CGST, SGST, IGST, Amount)
                        VALUES (@QuotationID, 0, @ProductName, @Description, @Brand, @Qty, @HSNCode, @Unit, @Rate, @Rate, @GSTPercentage, (@CGST + @SGST + @IGST), @TaxableAmount, @CGST, @SGST, @IGST, @Amount)
                    ", conn);
                    itemCmd.Parameters.AddWithValue("@QuotationID", newQuotationId);
                    itemCmd.Parameters.AddWithValue("@ProductName", item.Component ?? string.Empty);
                    itemCmd.Parameters.AddWithValue("@Description", item.Description ?? string.Empty);
                    itemCmd.Parameters.AddWithValue("@Brand", item.Brand ?? string.Empty);
                    itemCmd.Parameters.AddWithValue("@Qty", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@HSNCode", item.HSNCode ?? string.Empty);
                    itemCmd.Parameters.AddWithValue("@Unit", item.Unit ?? string.Empty);
                    itemCmd.Parameters.AddWithValue("@Rate", item.Rate);
                    itemCmd.Parameters.AddWithValue("@GSTPercentage", item.GSTPercentage);
                    itemCmd.Parameters.AddWithValue("@TaxableAmount", item.TaxableAmount);
                    itemCmd.Parameters.AddWithValue("@CGST", item.CGST);
                    itemCmd.Parameters.AddWithValue("@SGST", item.SGST);
                    itemCmd.Parameters.AddWithValue("@IGST", item.IGST);
                    itemCmd.Parameters.AddWithValue("@Amount", item.Total);
                    await itemCmd.ExecuteNonQueryAsync();
                }

                if (_editingQuotationId.HasValue)
                {
                    MessageBox.Show($"Quotation {QuotationNo} has been successfully updated!", "Updated", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Quotation {QuotationNo} generated and fully saved to the database!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving quotation: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public void ExportPdf()
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
                MessageBox.Show($"Error generating PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string GeneratePdfFile()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            var document = new Helpers.QuotationDocument(this);
            string filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"Quotation_{CustomerName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            document.GeneratePdf(filePath);
            return filePath;
        }

        [RelayCommand]
        private void ExportExcel()
        {
            MessageBox.Show("Exporting data to Excel format... (Excel module integration)", "Export Excel", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void SendEmail()
        {
            MessageBox.Show("Preparing email with Quotation PDF attachment...", "Email", MessageBoxButton.OK, MessageBoxImage.Information);
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

Thank you for your interest in ADISH ENTERPRISES.

Please find your Quotation attached.

📄 Quotation No : {QuotationNo}
📅 Date : {QuotationDate:dd MMM yyyy}
💰 Quotation Value : ₹{NetPayable}

This quotation is valid for {(ValidUntil - QuotationDate).Days} days.

If you need any changes or have any questions, please contact us.

We look forward to serving you.

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
    }
}