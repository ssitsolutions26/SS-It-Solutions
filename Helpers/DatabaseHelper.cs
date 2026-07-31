using System;
using System.IO;
using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace SolarQuotationBillingSystem.Helpers
{
    public static class DatabaseHelper
    {
        // Using LocalDB for easy setup without needing a full SQL Server instance
        private static readonly string _dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SolarQuotationBillingSystem");
        private static readonly string _dbPath = Path.Combine(_dbFolder, "SolarDb.mdf");

        // The logical database name in LocalDB
        private const string DatabaseName = "SolarDb";

        // IMPORTANT: We use Initial Catalog because we register the DB dynamically using CREATE DATABASE.
        // Using TrustServerCertificate=True helps avoid SSL chain issues on newer SqlClient versions.
        public static string ConnectionString => $@"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog={DatabaseName};Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;";

        // Connection string for master database operations (used to create our DB)
        private static string MasterConnectionString => @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;";

        public static void InitializeDatabase()
        {
            try
            {
                if (!Directory.Exists(_dbFolder))
                {
                    Directory.CreateDirectory(_dbFolder);
                    Debug.WriteLine($"[DB INIT] Created DB folder at: {_dbFolder}");
                }

                // If file doesn't exist, we create it.
                if (!File.Exists(_dbPath))
                {
                    Debug.WriteLine($"[DB INIT] Database file missing at {_dbPath}. Creating...");
                    CreateDatabaseFile();
                }

                // We ALWAYS attempt to create tables and seed data to ensure the schema is up to date.
                // The IF NOT EXISTS clauses protect us from overwriting existing data.
                CreateTables();
                SeedData();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CRITICAL DB INIT ERROR] {ex.Message}");
                Debug.WriteLine($"[STACK TRACE] {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"[INNER EXCEPTION] {ex.InnerException.Message}");
                }
                throw; // Rethrow to let the UI/App crash handler catch it, so the user knows.
            }
        }

        private static void CreateDatabaseFile()
        {
            try
            {
                // Connect to master to create the database
                using var conn = new SqlConnection(MasterConnectionString);
                conn.Open();

                using var cmd = conn.CreateCommand();

                // CRITICAL FIX: If the MDF file was manually deleted, LocalDB still thinks the DB exists but fails to open it.
                // We MUST drop the logical database if it exists but the physical file is gone, before creating it.
                cmd.CommandText = $@"
                    IF DB_ID('{DatabaseName}') IS NOT NULL
                    BEGIN
                        ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE [{DatabaseName}];
                    END

                    CREATE DATABASE [{DatabaseName}] ON PRIMARY (NAME=[{DatabaseName}], FILENAME='{_dbPath}')
                ";

                cmd.ExecuteNonQuery();
                Debug.WriteLine($"[DB INIT] Successfully created LocalDB database at {_dbPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CreateDatabaseFile ERROR] {ex.Message}");
                throw;
            }
        }

        private static void CreateTables()
        {
            // Added IF NOT EXISTS to all tables to prevent crashing on subsequent runs
            string sql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' and xtype='U')
                BEGIN
                    CREATE TABLE Users (
                        Id INT PRIMARY KEY IDENTITY(1,1),
                        Username NVARCHAR(50) NOT NULL UNIQUE,
                        PasswordHash NVARCHAR(256) NOT NULL,
                        Role NVARCHAR(20) NOT NULL
                    );
                END

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Customers' and xtype='U')
                BEGIN
                    CREATE TABLE Customers (
                        CustomerID INT PRIMARY KEY IDENTITY(1,1),
                        CustomerName NVARCHAR(100) NOT NULL,
                        CompanyName NVARCHAR(100),
                        ContactPerson NVARCHAR(100),
                        FatherName NVARCHAR(100),
                        Mobile NVARCHAR(15) NOT NULL,
                        AlternateMobile NVARCHAR(15),
                        Email NVARCHAR(100),
                        Address NVARCHAR(500),
                        City NVARCHAR(50),
                        District NVARCHAR(50),
                        State NVARCHAR(50),
                        PINCode NVARCHAR(10),
                        Aadhar NVARCHAR(20),
                        PAN NVARCHAR(20),
                        GSTNumber NVARCHAR(20),
                        InstallationAddress NVARCHAR(500),
                        Remarks NVARCHAR(MAX)
                    );
                END

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Products' and xtype='U')
                BEGIN
                    CREATE TABLE Products (
                        ProductID INT PRIMARY KEY IDENTITY(1,1),
                        ProductName NVARCHAR(100) NOT NULL,
                        Category NVARCHAR(50),
                        Brand NVARCHAR(50),
                        Model NVARCHAR(50),
                        Unit NVARCHAR(20),
                        Price DECIMAL(18,2) NOT NULL,
                        GST DECIMAL(5,2) NOT NULL,
                        Stock INT NOT NULL,
                        Description NVARCHAR(MAX)
                    );
                END

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Quotation' and xtype='U')
                BEGIN
                    CREATE TABLE Quotation (
                        QuotationID INT PRIMARY KEY IDENTITY(1,1),
                        QuotationNo NVARCHAR(50) NOT NULL UNIQUE,
                        QuotationDate DATETIME NOT NULL,
                        ValidUntil DATETIME NOT NULL,
                        CustomerID INT NOT NULL,
                        SalesExecutive NVARCHAR(100),
                        Reference NVARCHAR(100),
                        InstallationType NVARCHAR(50),
                        RoofType NVARCHAR(50),
                        SubsidyEligible NVARCHAR(10),
                        SystemCapacityKW DECIMAL(18,2),
                        SolarPanelBrand NVARCHAR(100),
                        PanelWatt INT,
                        NoOfPanels INT,
                        InverterBrand NVARCHAR(100),
                        InverterCapacity DECIMAL(18,2),
                        Battery NVARCHAR(100),
                        MountingStructure NVARCHAR(100),
                        EarthingKit NVARCHAR(100),
                        LightningArrestor NVARCHAR(100),
                        MC4Connector NVARCHAR(50),
                        DCCable NVARCHAR(50),
                        ACCable NVARCHAR(50),
                        InstallationCharges DECIMAL(18,2) NOT NULL,
                        Transportation DECIMAL(18,2) NOT NULL,
                        OtherCharges DECIMAL(18,2) NOT NULL,
                        Subtotal DECIMAL(18,2) NOT NULL,
                        TotalGST DECIMAL(18,2) NOT NULL,
                        Subsidy DECIMAL(18,2) NOT NULL,
                        Discount DECIMAL(18,2) NOT NULL,
                        GrandTotal DECIMAL(18,2) NOT NULL,
                        NetPayable DECIMAL(18,2) NOT NULL,
                        AmountInWords NVARCHAR(500) NOT NULL,
                        Status NVARCHAR(20) DEFAULT 'Pending',
                        PaymentMode NVARCHAR(50) DEFAULT 'Cash',
                        PaymentRefNo NVARCHAR(100) NULL,
                        InvoiceNo NVARCHAR(50) NULL,
                        FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID)
                    );
                END
                ELSE
                BEGIN
                    IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'Status' AND Object_ID = Object_ID(N'Quotation'))
                    BEGIN
                        ALTER TABLE Quotation ADD Status NVARCHAR(20) DEFAULT 'Pending';
                        EXEC('UPDATE Quotation SET Status = ''Pending'' WHERE Status IS NULL');
                    END
                    IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PaymentMode' AND Object_ID = Object_ID(N'Quotation'))
                    BEGIN
                        ALTER TABLE Quotation ADD PaymentMode NVARCHAR(50) DEFAULT 'Cash';
                    END
                    IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PaymentRefNo' AND Object_ID = Object_ID(N'Quotation'))
                    BEGIN
                        ALTER TABLE Quotation ADD PaymentRefNo NVARCHAR(100) NULL;
                    END
                    IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'InvoiceNo' AND Object_ID = Object_ID(N'Quotation'))
                    BEGIN
                        ALTER TABLE Quotation ADD InvoiceNo NVARCHAR(50) NULL;
                    END

                    -- Auto-assign InvoiceNo to existing Paid quotations if missing
                    EXEC('
                        WITH PaidQuotes AS (
                            SELECT QuotationID, ROW_NUMBER() OVER (ORDER BY QuotationID) as RowNum
                            FROM Quotation
                            WHERE Status = ''Paid'' AND (InvoiceNo IS NULL OR InvoiceNo = '''')
                        )
                        UPDATE q
                        SET q.InvoiceNo = ''ADP-'' + RIGHT(''0'' + CAST(pq.RowNum AS VARCHAR), 2)
                        FROM Quotation q
                        INNER JOIN PaidQuotes pq ON q.QuotationID = pq.QuotationID;
                    ');
                END

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='QuotationItems' and xtype='U')
                BEGIN
                    CREATE TABLE QuotationItems (
                        QuotationItemID INT PRIMARY KEY IDENTITY(1,1),
                        QuotationID INT NOT NULL,
                        ProductID INT NOT NULL DEFAULT 0,
                        ProductName NVARCHAR(100) NOT NULL,
                        Description NVARCHAR(MAX),
                        Brand NVARCHAR(50),
                        Qty NVARCHAR(50) NOT NULL DEFAULT '1',
                        Price DECIMAL(18,2) NOT NULL DEFAULT 0,
                        GSTPercentage DECIMAL(5,2) NOT NULL DEFAULT 0,
                        GSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                        Amount DECIMAL(18,2) NOT NULL DEFAULT 0,
                        FOREIGN KEY (QuotationID) REFERENCES Quotation(QuotationID)
                    );
                END
                ELSE
                BEGIN
                    -- If table exists but Qty is INT, alter it to NVARCHAR
                    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'QuotationItems' AND COLUMN_NAME = 'Qty' AND DATA_TYPE = 'int')
                    BEGIN
                        ALTER TABLE QuotationItems ALTER COLUMN Qty NVARCHAR(50) NOT NULL;
                    END

                    -- Drop FK constraint on ProductID if it exists
                    DECLARE @fkName NVARCHAR(200);
                    SELECT @fkName = fk.name 
                    FROM sys.foreign_keys fk
                    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                    INNER JOIN sys.columns c ON fkc.parent_column_id = c.column_id AND fkc.parent_object_id = c.object_id
                    WHERE OBJECT_NAME(fk.parent_object_id) = 'QuotationItems' AND c.name = 'ProductID';

                    IF @fkName IS NOT NULL
                    BEGIN
                        DECLARE @sql NVARCHAR(500) = 'ALTER TABLE QuotationItems DROP CONSTRAINT ' + @fkName;
                        EXEC sp_executesql @sql;
                    END
                END

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Invoice' and xtype='U')
                BEGIN
                    CREATE TABLE Invoice (
                        InvoiceID INT PRIMARY KEY IDENTITY(1,1),
                        InvoiceNo NVARCHAR(50) NOT NULL UNIQUE,
                        InvoiceDate DATETIME NOT NULL,
                        CustomerID INT NOT NULL,
                        Subtotal DECIMAL(18,2) NOT NULL,
                        TotalGST DECIMAL(18,2) NOT NULL,
                        Discount DECIMAL(18,2) NOT NULL,
                        GrandTotal DECIMAL(18,2) NOT NULL,
                        Paid DECIMAL(18,2) NOT NULL,
                        Balance DECIMAL(18,2) NOT NULL,
                        PaymentMode NVARCHAR(50) NOT NULL,
                        AmountInWords NVARCHAR(500) NOT NULL,
                        PaymentStatus NVARCHAR(50) DEFAULT 'Pending',
                        FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID)
                    );
                END
                ELSE
                BEGIN
                    IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PaymentStatus' AND Object_ID = Object_ID(N'Invoice'))
                    BEGIN
                        ALTER TABLE Invoice ADD PaymentStatus NVARCHAR(50) DEFAULT 'Pending';
                        EXEC('UPDATE Invoice SET PaymentStatus = ''Paid'' WHERE Balance <= 0');
                        EXEC('UPDATE Invoice SET PaymentStatus = ''Partial'' WHERE Balance > 0 AND Paid > 0');
                    END
                END

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PaymentHistory' and xtype='U')
                BEGIN
                    CREATE TABLE PaymentHistory (
                        PaymentID INT PRIMARY KEY IDENTITY(1,1),
                        InvoiceNo NVARCHAR(50) NOT NULL,
                        PaymentDate DATETIME NOT NULL,
                        PaidAmount DECIMAL(18,2) NOT NULL,
                        PaymentMode NVARCHAR(50) NOT NULL,
                        ReferenceNo NVARCHAR(100),
                        Remarks NVARCHAR(MAX)
                    );
                END

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='BillDeleteLog' and xtype='U')
                BEGIN
                    CREATE TABLE BillDeleteLog (
                        LogID INT PRIMARY KEY IDENTITY(1,1),
                        InvoiceNo NVARCHAR(50) NOT NULL,
                        QuotationNo NVARCHAR(50),
                        CustomerName NVARCHAR(100),
                        DeletedBy NVARCHAR(50) NOT NULL,
                        DeletedDate DATETIME NOT NULL DEFAULT GETDATE(),
                        Reason NVARCHAR(MAX) NOT NULL,
                        MachineName NVARCHAR(100) NOT NULL
                    );
                END
                ELSE
                BEGIN
                    IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'CustomerName' AND Object_ID = Object_ID(N'BillDeleteLog'))
                    BEGIN
                        ALTER TABLE BillDeleteLog ADD CustomerName NVARCHAR(100) NULL;
                        ALTER TABLE BillDeleteLog ALTER COLUMN Reason NVARCHAR(MAX) NOT NULL;
                    END
                END

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='InvoiceItems' and xtype='U')
                BEGIN
                    CREATE TABLE InvoiceItems (
                        InvoiceItemID INT PRIMARY KEY IDENTITY(1,1),
                        InvoiceID INT NOT NULL,
                        ProductID INT NOT NULL,
                        ProductName NVARCHAR(100) NOT NULL,
                        Description NVARCHAR(MAX),
                        Brand NVARCHAR(50),
                        Qty INT NOT NULL,
                        Price DECIMAL(18,2) NOT NULL,
                        GSTPercentage DECIMAL(5,2) NOT NULL,
                        GSTAmount DECIMAL(18,2) NOT NULL,
                        Amount DECIMAL(18,2) NOT NULL,
                        FOREIGN KEY (InvoiceID) REFERENCES Invoice(InvoiceID),
                        FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
                    );
                END

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Settings' and xtype='U')
                BEGIN
                    CREATE TABLE Settings (
                        ID INT PRIMARY KEY IDENTITY(1,1),
                        CompanyName NVARCHAR(100),
                        CompanyLogo VARBINARY(MAX),
                        GSTNumber NVARCHAR(50),
                        BankDetails NVARCHAR(MAX),
                        UPIQRCode VARBINARY(MAX),
                        TermsAndConditions NVARCHAR(MAX),
                        QuotationPrefix NVARCHAR(20) DEFAULT 'QT-',
                        QuotationStartingNumber INT DEFAULT 1000
                    );
                END
                
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WhatsAppHistory' and xtype='U')
                BEGIN
                    CREATE TABLE WhatsAppHistory (
                        ID INT PRIMARY KEY IDENTITY(1,1),
                        SentDate DATETIME NOT NULL,
                        CustomerName NVARCHAR(100),
                        MobileNumber NVARCHAR(20),
                        DocumentType NVARCHAR(50),
                        Status NVARCHAR(50)
                    );
                END
                IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'CompanyPAN' AND Object_ID = Object_ID(N'Settings'))
                BEGIN
                    ALTER TABLE Settings ADD CompanyPAN NVARCHAR(20) NULL;
                    ALTER TABLE Settings ADD State NVARCHAR(50) NULL;
                    ALTER TABLE Settings ADD StateCode NVARCHAR(10) NULL;
                END

                IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'HSNCode' AND Object_ID = Object_ID(N'Products'))
                BEGIN
                    ALTER TABLE Products ADD HSNCode NVARCHAR(50) NULL;
                    ALTER TABLE Products ADD TaxType NVARCHAR(20) DEFAULT 'Exclusive';
                    ALTER TABLE Products ADD TaxablePrice DECIMAL(18,2) DEFAULT 0;
                END

                IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'StateCode' AND Object_ID = Object_ID(N'Customers'))
                BEGIN
                    ALTER TABLE Customers ADD StateCode NVARCHAR(10) NULL;
                END

                IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'HSNCode' AND Object_ID = Object_ID(N'QuotationItems'))
                BEGIN
                    ALTER TABLE QuotationItems ADD HSNCode NVARCHAR(50) NULL;
                    ALTER TABLE QuotationItems ADD Unit NVARCHAR(20) NULL;
                    ALTER TABLE QuotationItems ADD Rate DECIMAL(18,2) DEFAULT 0;
                    ALTER TABLE QuotationItems ADD CGST DECIMAL(18,2) DEFAULT 0;
                    ALTER TABLE QuotationItems ADD SGST DECIMAL(18,2) DEFAULT 0;
                    ALTER TABLE QuotationItems ADD IGST DECIMAL(18,2) DEFAULT 0;
                    ALTER TABLE QuotationItems ADD TaxableAmount DECIMAL(18,2) DEFAULT 0;
                    ALTER TABLE QuotationItems ADD Total DECIMAL(18,2) DEFAULT 0;
                END

                IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'HSNCode' AND Object_ID = Object_ID(N'InvoiceItems'))
                BEGIN
                    ALTER TABLE InvoiceItems ADD HSNCode NVARCHAR(50) NULL;
                    ALTER TABLE InvoiceItems ADD Unit NVARCHAR(20) NULL;
                    ALTER TABLE InvoiceItems ADD Rate DECIMAL(18,2) DEFAULT 0;
                    ALTER TABLE InvoiceItems ADD CGST DECIMAL(18,2) DEFAULT 0;
                    ALTER TABLE InvoiceItems ADD SGST DECIMAL(18,2) DEFAULT 0;
                    ALTER TABLE InvoiceItems ADD IGST DECIMAL(18,2) DEFAULT 0;
                    ALTER TABLE InvoiceItems ADD TaxableAmount DECIMAL(18,2) DEFAULT 0;
                    ALTER TABLE InvoiceItems ADD Total DECIMAL(18,2) DEFAULT 0;
                END

                IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'TotalCGST' AND Object_ID = Object_ID(N'Quotation'))
                BEGIN
                    ALTER TABLE Quotation ADD TotalCGST DECIMAL(18,2) DEFAULT 0;
                    ALTER TABLE Quotation ADD TotalSGST DECIMAL(18,2) DEFAULT 0;
                    ALTER TABLE Quotation ADD TotalIGST DECIMAL(18,2) DEFAULT 0;
                    ALTER TABLE Quotation ADD TaxableAmount DECIMAL(18,2) DEFAULT 0;
                    ALTER TABLE Quotation ADD RoundOff DECIMAL(18,2) DEFAULT 0;
                END

                IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'TotalCGST' AND Object_ID = Object_ID(N'Invoice'))
                BEGIN
                    ALTER TABLE Invoice ADD TotalCGST DECIMAL(18,2) DEFAULT 0;
                    ALTER TABLE Invoice ADD TotalSGST DECIMAL(18,2) DEFAULT 0;
                    ALTER TABLE Invoice ADD TotalIGST DECIMAL(18,2) DEFAULT 0;
                    ALTER TABLE Invoice ADD TaxableAmount DECIMAL(18,2) DEFAULT 0;
                    ALTER TABLE Invoice ADD RoundOff DECIMAL(18,2) DEFAULT 0;
                END
            ";

            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
                Debug.WriteLine("[DB INIT] Tables verified/created successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CreateTables ERROR] {ex.Message}");
                throw;
            }
        }

        private static void SeedData()
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();
                
                using var cmd = conn.CreateCommand();
                
                // Seed default admin user safely (and reset password to 123)
                cmd.CommandText = @"
                    IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
                    BEGIN
                        INSERT INTO Users (Username, PasswordHash, Role)
                        VALUES ('admin', '123', 'Admin');
                    END
                    ELSE
                    BEGIN
                        UPDATE Users SET PasswordHash = '123' WHERE Username = 'admin';
                    END
                ";
                cmd.ExecuteNonQuery();

                // Seed default settings safely
                cmd.CommandText = @"
                    IF NOT EXISTS (SELECT 1 FROM Settings)
                    BEGIN
                        INSERT INTO Settings (CompanyName, GSTNumber, QuotationPrefix, QuotationStartingNumber)
                        VALUES ('Solar Power Co.', 'GST123456789', 'QT-', 1000);
                    END
                ";
                cmd.ExecuteNonQuery();
                
                Debug.WriteLine("[DB INIT] Seed data verified/inserted successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SeedData ERROR] {ex.Message}");
                throw;
            }
        }
    }
}
