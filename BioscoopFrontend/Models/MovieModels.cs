namespace BioscoopFrontend.Models;

public class UpcomingMovieDto
{
    public Guid MovieId { get; set; }
    public string Title { get; set; } = "";
    public DateTimeOffset FirstScreeningAtUtc { get; set; }
    public List<UpcomingScreeningDto> Screenings { get; set; } = new();
}

public class UpcomingScreeningDto
{
    public Guid ScreeningId { get; set; }
    public DateTimeOffset StartTimeUtc { get; set; }
    public Guid HallId { get; set; }
}

public class OrderDto
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public string PaymentMethod { get; set; } = "Pin";
    public decimal TotalAmount { get; set; }
    public string PrintCode { get; set; } = "";
}
