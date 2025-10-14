namespace CabETL.CLI.Models;

public struct CabDataEntity
{
    public DateTime PickupDatetimeUTC { get; set; }
    public DateTime DropoffDatetimeUTC { get; set; }
    public int PassengerCount { get; set; }
    public float TripDistance { get; set; }
    public string StoreAndFwd { get; set; } // no idea how to name it
    public int PickupLocationId { get; set; }
    public int DropoffLocationId { get; set; }
    public decimal FareAmount { get; set; }
    public decimal TipAmount { get; set; }
}