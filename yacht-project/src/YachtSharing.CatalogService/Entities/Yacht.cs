namespace YachtSharing.CatalogService.Entities;

public class Yacht
{
    public Yacht()
    {
        Model = string.Empty;
    }

    public Yacht(int id, string model, decimal pricePerHour)
    {
        Id = id;
        Model = model;
        PricePerHour = pricePerHour;
    }

    public int Id { get; set; }

    public string Model { get; set; }

    public decimal PricePerHour { get; set; }
}