using System;

namespace YachtSharing.CatalogService.DataTable;

public class BookingProjection
{
    public int Id { get; set; }

    public int YachtId { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }
}