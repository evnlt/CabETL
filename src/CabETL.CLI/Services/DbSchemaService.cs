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
}