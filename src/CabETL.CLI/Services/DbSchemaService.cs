using Dapper;
using Microsoft.Data.SqlClient;

namespace CabETL.CLI.Services;

public class DbSchemaService
{
    public static async Task EnsureDbSchemaExists(string connectionString)
    {
        var masterConnectionString = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        }.ToString();

        await using var masterConnection = new SqlConnection(masterConnectionString);
        await masterConnection.OpenAsync();

        var createDb = @"IF DB_ID('CabDataDb') IS NULL CREATE DATABASE CabDataDb;";
        await masterConnection.ExecuteAsync(createDb);

        var dbConnectionString = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "CabDataDb"
        }.ToString();

        await using var dbConnection = new SqlConnection(dbConnectionString);
        await dbConnection.OpenAsync();

        // TODO - add performance configuration

        var createTable = @"
            IF OBJECT_ID('dbo.CabData', 'U') IS NULL
            CREATE TABLE dbo.CabData (
                PickupDatetime DATETIME NOT NULL,
                DropoffDatetime DATETIME NOT NULL,
                PassengerCount INT NOT NULL,
                TripDistance DECIMAL(6,2) NOT NULL,
                StoreAndFwd CHAR(3) NOT NULL,
                PickupLocationId INT NOT NULL,
                DropoffLocationId INT NOT NULL,
                FareAmount DECIMAL(6,2) NOT NULL,
                TipAmount DECIMAL(6,2) NOT NULL
            );";

        await dbConnection.ExecuteAsync(createTable);
    }
    
    public static async Task CreateIndexes(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var query1 = @"
            CREATE INDEX IX_CabData_PickupLocationId
            ON dbo.CabData (PickupLocationId);";

        var query2 = @"
            CREATE VIEW dbo.v_CabData_TipStatsByPickupLocation
            WITH SCHEMABINDING
            AS
            SELECT
                PickupLocationId,
                SUM(CAST(TipAmount AS DECIMAL(10,2))) AS TotalTipAmount,
                COUNT_BIG(*) AS TripCount
            FROM dbo.CabData
            GROUP BY PickupLocationId;";
        
        var query3 = @"
            CREATE UNIQUE CLUSTERED INDEX IX_v_CabData_TipStatsByPickupLocation
            ON v_CabData_TipStatsByPickupLocation (PickupLocationId);";
        
        var query4 = @"
            CREATE INDEX IX_CabData_TripDistance_FareAmount
            ON dbo.CabData (TripDistance DESC)
            INCLUDE (FareAmount);";
        
        var query5 = @"
            ALTER TABLE dbo.CabData
            ADD TripDurationInMinutes AS DATEDIFF(MINUTE, PickupDatetime, DropoffDatetime) PERSISTED;

            CREATE INDEX IX_CabData_TripDurationInMinutes
            ON dbo.CabData (TripDurationInMinutes DESC);";

        await connection.ExecuteAsync(query1);
        await connection.ExecuteAsync(query2);
        await connection.ExecuteAsync(query3);
        await connection.ExecuteAsync(query4);
        await connection.ExecuteAsync(query5);
    }

    public static async Task DeleteDatabase()
    {
        var sql = @"
            DROP DATABASE [CabDataDb];
        ";

        await using var connection = new SqlConnection("Data Source=(local);Database=master;User Id=sa;Password=Qwerty123$%;TrustServerCertificate=true");
        await connection.OpenAsync();
        await connection.ExecuteAsync(sql, new { DbName = "CabDataDb" });
    }
}