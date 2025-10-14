using System.Data;
using System.Globalization;
using CabETL.CLI.Entities;
using CabETL.CLI.Entities.Enums;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;

namespace CabETL.CLI.Services;

// bad service name, have to think of something else
public class CsvToDbService
{
    private readonly string _connectionString;

    public CsvToDbService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void ImportCsvToDatabase(string csvFilePath, int batchSize = 10000)
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
            MissingFieldFound = null
        };

        using var csv = new CsvReader(reader, csvConfig);
        csv.Context.RegisterClassMap<CabDataModelMap>();

        var batch = new List<CabDataEntity>(batchSize);

        while (csv.Read())
        {
            var record = csv.GetRecord<CabDataModel>();

            // TODO - data processing / validation 
            CabDataEntity? entity = Process(record);

            if (entity != null)
            {
                batch.Add(entity.Value);
            }

            if (batch.Count >= batchSize)
            {
                BulkInsert(batch);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            BulkInsert(batch);
        }

        // TODO - return total rows inserted
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
                row.PickupDatetime,
                row.DropoffDatetime,
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
        Console.WriteLine($"Inserted batch of {table.Rows.Count} rows");
    }

    // TODO - move to validation class
    public static CabDataEntity? Process(CabDataModel model)
    {
        model.StoreAndFwd = model.StoreAndFwd switch
        {
            "N" => nameof(StoreAndFwdEntityOptions.No),
            "Y" => nameof(StoreAndFwdEntityOptions.Yes),
            _ => null
        };
        
        if (model.StoreAndFwd == null)
        {
            return null;
        }

        if (model.PassengerCount == null)
        {
            return null;
        }

        return new CabDataEntity
        {
            PickupDatetime = model.PickupDatetime,
            DropoffDatetime = model.DropoffDatetime,
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

public sealed class CabDataModelMap : ClassMap<CabDataModel>
{
    public CabDataModelMap()
    {
        // TODO - column names should be in constants file
        Map(m => m.PickupDatetime).Name("tpep_pickup_datetime");
        Map(m => m.DropoffDatetime).Name("tpep_dropoff_datetime");
        Map(m => m.PassengerCount).Name("passenger_count");
        Map(m => m.TripDistance).Name("trip_distance");
        Map(m => m.StoreAndFwd).Name("store_and_fwd_flag");
        Map(m => m.PickupLocationId).Name("PULocationID");
        Map(m => m.DropoffLocationId).Name("DOLocationID");
        Map(m => m.FareAmount).Name("fare_amount");
        Map(m => m.TipAmount).Name("tip_amount");
    }
}