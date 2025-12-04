#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net10.0

#:package CsvHelper@30.0.1
#:package Microsoft.Data.SqlClient@5.2.0
#:package Microsoft.Extensions.Logging@8.0.0
#:package Microsoft.Extensions.Logging.Console@8.0.0
#:package Microsoft.Extensions.Configuration@8.0.0
#:package Microsoft.Extensions.Configuration.Abstractions@8.0.0
#:package Microsoft.Extensions.Configuration.EnvironmentVariables@8.0.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CsvHelper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger("CsvLoader");
var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

logger.LogInformation("Starting CSV loader job...");

var connectionString = config.GetConnectionString("whatcardb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    logger.LogError($"{nameof(connectionString)} environment variable is not set. Cannot connect to SQL Server.");
    return;
}

var csvPath = "../data/df_VEH0120_GB.csv";
if (!File.Exists(csvPath))
{
    logger.LogError($"CSV file not found: {csvPath}");
    return;
}

string fileHash = ComputeFileHash(csvPath);

using var conn = new SqlConnection(connectionString);
conn.Open();

// 1. Create schema if not exists
CreateSchema(conn, logger);

// 2. Idempotency check
if (AlreadyImported(conn, fileHash))
{
    logger.LogInformation("File already imported. Skipping.");
    return;
}

// 3. Parse CSV
var records = LoadCsv(csvPath, logger);

// 4. Insert Vehicles + SalesData
foreach (var record in records)
{
    int vehicleId = UpsertVehicle(conn, record, logger);

    foreach (var kvp in record.QuarterlySales)
    {
        if (!kvp.Value.HasValue || kvp.Value.Value == 0) continue;

        string quarterLabel = kvp.Key.Replace(" ", ""); // e.g. "2025Q2"
        int year = int.Parse(quarterLabel.Substring(0, 4));
        int quarter = int.Parse(quarterLabel.Substring(5, 1));

        UpsertSalesData(conn, vehicleId, year, quarter, kvp.Value.Value, logger);
    }
}

// 5. Record import
using (var cmd = new SqlCommand("INSERT INTO DataImports (FileName, FileHash, Status) VALUES (@FileName,@FileHash,'Completed')", conn))
{
    cmd.Parameters.AddWithValue("@FileName", Path.GetFileName(csvPath));
    cmd.Parameters.AddWithValue("@FileHash", fileHash);
    cmd.ExecuteNonQuery();
}

logger.LogInformation("Import completed.");

// --- Helper methods ---
void CreateSchema(SqlConnection conn, ILogger logger)
{
    // 1. Create tables and indexes
    string schemaSql = @"
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Vehicles' AND xtype='U')
        BEGIN
            CREATE TABLE Vehicles (
                VehicleId INT IDENTITY PRIMARY KEY,
                BodyType NVARCHAR(50) NOT NULL,
                Make NVARCHAR(50) NOT NULL,
                GenModel NVARCHAR(100) NOT NULL,
                Model NVARCHAR(100) NOT NULL,
                Fuel NVARCHAR(50) NOT NULL,
                LicenceStatus NVARCHAR(50) NOT NULL,
                VehicleHash NVARCHAR(64) NOT NULL,
                CONSTRAINT UX_Vehicles_Hash UNIQUE (VehicleHash)
            );
            CREATE INDEX IX_Vehicles_Make_Model_Fuel ON Vehicles(Make, Model, Fuel);
            CREATE INDEX IX_Vehicles_Fuel ON Vehicles(Fuel);
            CREATE INDEX IX_Vehicles_BodyType ON Vehicles(BodyType);
        END;

        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SalesData' AND xtype='U')
        BEGIN
            CREATE TABLE SalesData (
                SalesId INT IDENTITY PRIMARY KEY,
                VehicleId INT NOT NULL FOREIGN KEY REFERENCES Vehicles(VehicleId),
                Year INT NOT NULL,
                Quarter INT NOT NULL,
                UnitsSold INT NOT NULL,
                CONSTRAINT UX_SalesData_Vehicle_Period UNIQUE (VehicleId, Year, Quarter)
            );
            CREATE INDEX IX_SalesData_Year ON SalesData(Year);
            CREATE INDEX IX_SalesData_VehicleId_Year ON SalesData(VehicleId, Year);
            CREATE INDEX IX_SalesData_Fuel_Year ON SalesData(Year, VehicleId);
        END;

        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DataImports' AND xtype='U')
        BEGIN
            CREATE TABLE DataImports (
                ImportId INT IDENTITY PRIMARY KEY,
                FileName NVARCHAR(255) NOT NULL,
                FileHash NVARCHAR(64) NOT NULL,
                ImportedAt DATETIME DEFAULT GETDATE(),
                Status NVARCHAR(50) NOT NULL
            );
            CREATE UNIQUE INDEX UX_DataImports_FileHash ON DataImports(FileHash);
        END;
    ";
    using var cmd = new SqlCommand(schemaSql, conn);
    try
    {
        cmd.ExecuteNonQuery();
        logger.LogInformation("Schema ensured.");
    }
    catch (Exception ex)
    {
        logger.LogError($"Error creating schema: {ex.Message}");
        throw;
    }

    // 2. Drop function if exists
    string dropFnSql = @"IF OBJECT_ID('dbo.GetSchemaSummary', 'FN') IS NOT NULL DROP FUNCTION dbo.GetSchemaSummary;";
    using (var dropCmd = new SqlCommand(dropFnSql, conn))
    {
        try { dropCmd.ExecuteNonQuery(); } catch { /* ignore if not exists */ }
    }

    // 3. Create function in its own batch
    string createFnSql = @"
        CREATE FUNCTION dbo.GetSchemaSummary()
        RETURNS NVARCHAR(MAX)
        AS
        BEGIN
            DECLARE @summary NVARCHAR(MAX) = '';

            -- Entities
            SELECT @summary = @summary + 'Entities: ' + STRING_AGG(TABLE_NAME, ', ') WITHIN GROUP (ORDER BY TABLE_NAME) + CHAR(13) + CHAR(10)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE';

            -- Relationships (foreign keys)
            SELECT @summary = @summary + 'Relationship: ' + fk.name + ' (' + OBJECT_NAME(fk.parent_object_id) + ' → ' + OBJECT_NAME(fk.referenced_object_id) + ')' + CHAR(13) + CHAR(10)
            FROM sys.foreign_keys fk;

            -- Sample Values (first 5 for context)
            SET @summary = @summary + 'Sample Values:' + CHAR(13) + CHAR(10);

            -- Fuel types (sample)
            SELECT @summary = @summary + '- Fuel types (sample): ' + STRING_AGG(Fuel, ', ') WITHIN GROUP (ORDER BY Fuel) + CHAR(13) + CHAR(10)
            FROM (SELECT DISTINCT TOP 5 Fuel FROM Vehicles ORDER BY Fuel) AS Fuels;

            -- Makes (sample)
            SELECT @summary = @summary + '- Makes (sample): ' + STRING_AGG(Make, ', ') WITHIN GROUP (ORDER BY Make) + CHAR(13) + CHAR(10)
            FROM (SELECT DISTINCT TOP 5 Make FROM Vehicles ORDER BY Make) AS Makes;

            -- Models (sample)
            SELECT @summary = @summary + '- Models (sample): ' + STRING_AGG(Model, ', ') WITHIN GROUP (ORDER BY Model) + CHAR(13) + CHAR(10)
            FROM (SELECT DISTINCT TOP 5 Model FROM Vehicles ORDER BY Model) AS Models;

            -- Years (sample)
            SELECT @summary = @summary + '- Years (sample): ' + STRING_AGG(CAST(Year AS NVARCHAR(10)), ', ') WITHIN GROUP (ORDER BY Year DESC) + CHAR(13) + CHAR(10)
            FROM (SELECT DISTINCT TOP 5 Year FROM SalesData ORDER BY Year DESC) AS Years;

            -- Complete Lists (for prompt context)
            SET @summary = @summary + CHAR(13) + CHAR(10) + 'Complete Lists (use EXACT strings):' + CHAR(13) + CHAR(10);

            -- ALL Fuel Types
            SELECT @summary = @summary + '- Fuel types (all): ' + STRING_AGG(QUOTENAME(Fuel, ''''), ', ') WITHIN GROUP (ORDER BY Fuel) + CHAR(13) + CHAR(10)
            FROM (SELECT DISTINCT Fuel FROM Vehicles) AS AllFuels;

            -- Year Range
            DECLARE @minYear INT, @maxYear INT;
            SELECT @minYear = MIN(Year), @maxYear = MAX(Year) FROM SalesData;
            SET @summary = @summary + '- Data coverage: Years ' + CAST(@minYear AS NVARCHAR(10)) + ' to ' + CAST(@maxYear AS NVARCHAR(10)) + CHAR(13) + CHAR(10);

            -- Total counts for context
            DECLARE @vehicleCount INT, @salesCount INT;
            SELECT @vehicleCount = COUNT(*) FROM Vehicles;
            SELECT @salesCount = COUNT(*) FROM SalesData;
            SET @summary = @summary + '- Total vehicles: ' + CAST(@vehicleCount AS NVARCHAR(10)) + CHAR(13) + CHAR(10);
            SET @summary = @summary + '- Total sales records: ' + CAST(@salesCount AS NVARCHAR(10)) + CHAR(13) + CHAR(10);

            RETURN @summary;
        END
    ";
    using var createCmd = new SqlCommand(createFnSql, conn);
    try
    {
        createCmd.ExecuteNonQuery();
    }
    catch (Exception ex)
    {
        logger.LogError($"Error creating function: {ex.Message}");
    }
}

bool AlreadyImported(SqlConnection conn, string fileHash)
{
    using var cmd = new SqlCommand("SELECT COUNT(*) FROM DataImports WHERE FileHash=@hash AND Status='Completed'", conn);
    cmd.Parameters.AddWithValue("@hash", fileHash);
    return (int)cmd.ExecuteScalar() > 0;
}

static string ComputeVehicleHash(VehicleSalesRaw record)
{
    string composite = $"{record.BodyType}|{record.Make}|{record.GenModel}|{record.Model}|{record.Fuel}|{record.LicenceStatus}";
    using var sha = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(composite);
    var hash = sha.ComputeHash(bytes);
    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
}

static int UpsertVehicle(SqlConnection conn, VehicleSalesRaw record, ILogger logger)
{
    string hash = ComputeVehicleHash(record);

    // Check if vehicle already exists
    using (var checkCmd = new SqlCommand("SELECT VehicleId FROM Vehicles WHERE VehicleHash=@hash", conn))
    {
        checkCmd.Parameters.AddWithValue("@hash", hash);
        var existing = checkCmd.ExecuteScalar();
        if (existing != null) return (int)existing;
    }

    // Insert new vehicle
    using (var insertCmd = new SqlCommand(@"
        INSERT INTO Vehicles (BodyType, Make, GenModel, Model, Fuel, LicenceStatus, VehicleHash)
        OUTPUT INSERTED.VehicleId
        VALUES (@BodyType, @Make, @GenModel, @Model, @Fuel, @LicenceStatus, @VehicleHash);", conn))
    {
        insertCmd.Parameters.AddWithValue("@BodyType", record.BodyType);
        insertCmd.Parameters.AddWithValue("@Make", record.Make);
        insertCmd.Parameters.AddWithValue("@GenModel", record.GenModel);
        insertCmd.Parameters.AddWithValue("@Model", record.Model);
        insertCmd.Parameters.AddWithValue("@Fuel", record.Fuel);
        insertCmd.Parameters.AddWithValue("@LicenceStatus", record.LicenceStatus);
        insertCmd.Parameters.AddWithValue("@VehicleHash", hash);

        try
        {
            return (int)insertCmd.ExecuteScalar();
        }
        catch (Exception ex)
        {
            logger.LogError($"Error upserting vehicle: {ex.Message}");
            throw;
        }
    }
}

static void UpsertSalesData(SqlConnection conn, int vehicleId, int year, int quarter, int unitsSold, ILogger logger)
{
    using var cmd = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM SalesData WHERE VehicleId=@VehicleId AND Year=@Year AND Quarter=@Quarter)
    INSERT INTO SalesData (VehicleId, Year, Quarter, UnitsSold)
    VALUES (@VehicleId, @Year, @Quarter, @UnitsSold);", conn);

    cmd.Parameters.AddWithValue("@VehicleId", vehicleId);
    cmd.Parameters.AddWithValue("@Year", year);
    cmd.Parameters.AddWithValue("@Quarter", quarter);
    cmd.Parameters.AddWithValue("@UnitsSold", unitsSold);

    try
    {
        cmd.ExecuteNonQuery();
    }
    catch (Exception ex)
    {
        logger.LogError($"Error upserting sales data: {ex.Message}");
        throw;
    }
}

string ComputeFileHash(string path)
{
    using var sha = SHA256.Create();
    using var stream = File.OpenRead(path);
    var hash = sha.ComputeHash(stream);
    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
}

List<VehicleSalesRaw> LoadCsv(string path, ILogger logger)
{
    using var reader = new StreamReader(path);
    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

    csv.Read();
    csv.ReadHeader();
    var headers = csv.HeaderRecord;

    var records = new List<VehicleSalesRaw>();
    while (csv.Read())
    {
        var record = new VehicleSalesRaw
        {
            BodyType = csv.GetField("BodyType"),
            Make = csv.GetField("Make"),
            GenModel = csv.GetField("GenModel"),
            Model = csv.GetField("Model"),
            Fuel = csv.GetField("Fuel"),
            LicenceStatus = csv.GetField("LicenceStatus")
        };

        foreach (var h in headers.Skip(6)) // after LicenceStatus
        {
            var val = csv.GetField(h);
            record.QuarterlySales[h.Replace(" ", "")] = string.IsNullOrWhiteSpace(val) ? null : int.Parse(val);
        }

        records.Add(record);
    }
    logger.LogInformation($"Loaded {records.Count} records from CSV.");
    return records;
}

public class VehicleSalesRaw
{
    public required string BodyType { get; set; }
    public required string Make { get; set; }
    public required string GenModel { get; set; }
    public required string Model { get; set; }
    public required string Fuel { get; set; }
    public required string LicenceStatus { get; set; }
    public Dictionary<string, int?> QuarterlySales { get; set; } = new();
}
