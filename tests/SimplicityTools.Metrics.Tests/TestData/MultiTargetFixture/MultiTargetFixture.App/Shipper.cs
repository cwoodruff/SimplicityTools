namespace MultiTargetFixture.App;

public sealed class Shipper : IShipper
{
    public string Ship(int parcels)
    {
        if (parcels <= 0)
        {
            return "nothing to ship";
        }

        if (parcels > 100)
        {
            return "bulk shipment";
        }

        return "standard shipment";
    }
}
