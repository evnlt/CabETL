using CabETL.CLI.Models;
using CsvHelper.Configuration;

namespace CabETL.CLI;

public sealed class CabDataModelMap : ClassMap<CabDataModel>
{
    public CabDataModelMap()
    {
        // TODO - column names should be in constants file
        Map(m => m.PickupDatetimeEST).Name("tpep_pickup_datetime");
        Map(m => m.DropoffDatetimeEST).Name("tpep_dropoff_datetime");
        Map(m => m.PassengerCount).Name("passenger_count");
        Map(m => m.TripDistance).Name("trip_distance");
        Map(m => m.StoreAndFwd).Name("store_and_fwd_flag");
        Map(m => m.PickupLocationId).Name("PULocationID");
        Map(m => m.DropoffLocationId).Name("DOLocationID");
        Map(m => m.FareAmount).Name("fare_amount");
        Map(m => m.TipAmount).Name("tip_amount");
    }
}