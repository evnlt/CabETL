using System.Data;
using System.Globalization;
using CabETL.CLI.Enums;
using CabETL.CLI.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CabETL.CLI.Services;

// bad service name, have to think of something else
public class CsvToDbService
{
    private readonly TimeZoneInfo estTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
    private readonly string _connectionString;

    public CsvToDbService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public int ImportCsvToDatabase(string csvFilePath, int batchSize = 10000)
    {
        if (!File.Exists(csvFilePath))
        {
            throw new FileNotFoundException($"CSV file not found: {csvFilePath}");
        }

        using var reader = new StreamReader(csvFilePath);
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            BadDataFound = null,
            MissingFieldFound = null,
            ReadingExceptionOccurred = ex => false
        };

        using var csv = new CsvReader(reader, csvConfig);
        csv.Context.RegisterClassMap<CabDataModelMap>();

        int totalRowsInserted = 0;
        var batch = new List<CabDataEntity>(batchSize);

        while (csv.Read())
        {
            var record = csv.GetRecord<CabDataModel?>();

            if (record == null)
            {
                continue;
            }

            CabDataEntity? entity = ValidateAndProcess(record);

            if (entity != null)
            {
                batch.Add(entity.Value);
            }

            if (batch.Count >= batchSize)
            {
                totalRowsInserted += batch.Count;
                BulkInsert(batch);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            totalRowsInserted += batch.Count;
            BulkInsert(batch);
        }

        return totalRowsInserted;
    }

    private void BulkInsert(IEnumerable<CabDataEntity> batch)
    {
        using var table = new DataTable();
        table.Columns.Add("PickupDatetime", typeof(DateTime));
        table.Columns.Add("DropoffDatetime", typeof(DateTime));
        table.Columns.Add("PassengerCount", typeof(int));
        table.Columns.Add("TripDistance", typeof(float));
        table.Columns.Add("StoreAndFwd", typeof(string));
        table.Columns.Add("PickupLocationId", typeof(int));
        table.Columns.Add("DropoffLocationId", typeof(int));
        table.Columns.Add("FareAmount", typeof(decimal));
        table.Columns.Add("TipAmount", typeof(decimal));

        foreach (var row in batch)
        {
            table.Rows.Add(
                row.PickupDatetimeUTC,
                row.DropoffDatetimeUTC,
                row.PassengerCount,
                row.TripDistance,
                row.StoreAndFwd,
                row.PickupLocationId,
                row.DropoffLocationId,
                row.FareAmount,
                row.TipAmount
            );
        }

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using var bulkCopy = new SqlBulkCopy(connection)
        {
            DestinationTableName = "dbo.CabData", // should be a constant
            BatchSize = table.Rows.Count
        };

        bulkCopy.ColumnMappings.Add("PickupDatetime", "PickupDatetime");
        bulkCopy.ColumnMappings.Add("DropoffDatetime", "DropoffDatetime");
        bulkCopy.ColumnMappings.Add("PassengerCount", "PassengerCount");
        bulkCopy.ColumnMappings.Add("TripDistance", "TripDistance");
        bulkCopy.ColumnMappings.Add("StoreAndFwd", "StoreAndFwd");
        bulkCopy.ColumnMappings.Add("PickupLocationId", "PickupLocationId");
        bulkCopy.ColumnMappings.Add("DropoffLocationId", "DropoffLocationId");
        bulkCopy.ColumnMappings.Add("FareAmount", "FareAmount");
        bulkCopy.ColumnMappings.Add("TipAmount", "TipAmount");
        
        bulkCopy.WriteToServer(table);
    }
    
    public async Task<int> DeleteDuplicates(string csvFilePath)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var duplicatesQuery = @"
            WITH Duplicates AS (
                SELECT *, COUNT(*) OVER(PARTITION BY DropoffDatetime, PickupLocationId, PassengerCount) AS DupCount
                FROM dbo.CabData
            )
            SELECT *
            FROM Duplicates
            WHERE DupCount > 1;";

        // should be loaded into memory in batches
        var duplicates = (await connection.QueryAsync<CabDataEntity>(duplicatesQuery)).AsList();
        var duplicatesCount = duplicates.Count;

        if (!duplicates.Any())
        {
            return 0;
        }

        await WriteToCsv(duplicates, csvFilePath);

        var deleteQuery = @"
            WITH ToDelete AS (
                SELECT *,
                       ROW_NUMBER() OVER(PARTITION BY DropoffDatetime, PickupLocationId, PassengerCount ORDER BY (SELECT 0)) AS rn
                FROM dbo.CabData
            )
            DELETE FROM ToDelete
            WHERE rn > 1;";

        await connection.ExecuteAsync(deleteQuery);

        return duplicatesCount;
    }

    private async Task WriteToCsv(IEnumerable<CabDataEntity> duplicates, string csvFilePath)
    {
        await using var writer = new StreamWriter(csvFilePath);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(duplicates);
        await writer.FlushAsync();
    }

    private CabDataEntity? ValidateAndProcess(CabDataModel model)
    {
        model.StoreAndFwd = model.StoreAndFwd?.Trim();
        model.StoreAndFwd = model.StoreAndFwd switch
        {
            "N" => nameof(StoreAndFwdOptions.No),
            "Y" => nameof(StoreAndFwdOptions.Yes),
            _ => null
        };
        
        if (model.StoreAndFwd == null)
        {
            return null;
        }

        if (model.PassengerCount is null or <= 0)
        {
            return null;
        }
        
        if (model.PickupDatetimeEST > model.DropoffDatetimeEST)
        {
            return null;
        }

        if (model.FareAmount <= 0)
        {
            return null;
        }
        
        if (model.TipAmount < 0)
        {
            return null;
        }
        
        if (model.TripDistance <= 0)
        {
            return null;
        }

        return new CabDataEntity
        {
            PickupDatetimeUTC = TimeZoneInfo.ConvertTimeToUtc(model.PickupDatetimeEST, estTimeZone),
            DropoffDatetimeUTC = TimeZoneInfo.ConvertTimeToUtc(model.DropoffDatetimeEST, estTimeZone),
            PassengerCount = model.PassengerCount.Value,
            TripDistance = model.TripDistance,
            StoreAndFwd = model.StoreAndFwd,
            PickupLocationId = model.PickupLocationId,
            DropoffLocationId = model.DropoffLocationId,
            FareAmount = model.FareAmount,
            TipAmount = model.TipAmount
        };
    }
}