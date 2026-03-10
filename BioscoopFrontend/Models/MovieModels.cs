namespace BioscoopFrontend.Models;

public class UpcomingMovieDto
{
    public Guid MovieId { get; set; }
    public string Title { get; set; } = "";
    public string? ImageUrl { get; set; }
    public DateTimeOffset FirstScreeningAtUtc { get; set; }
    public List<UpcomingScreeningDto> Screenings { get; set; } = new();
}

public class UpcomingScreeningDto
{
    public Guid ScreeningId { get; set; }
    public DateTimeOffset StartTimeUtc { get; set; }
    public Guid HallId { get; set; }
    public int HallNumber { get; set; }
}

public class MovieDetailsDto
{
    public Guid MovieId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int DurationMinutes { get; set; }
    public int Age { get; set; }
    public string? ImageUrl { get; set; }
    public List<ScreeningWithHallDto> Screenings { get; set; } = new();
}

public class ScreeningWithHallDto
{
    public Guid ScreeningId { get; set; }
    public DateTimeOffset StartTimeUtc { get; set; }
    public Guid HallId { get; set; }
    public int HallNumber { get; set; }
    public string HallName { get; set; } = "";
    public int AvailableSeats { get; set; }
    public int TotalSeats { get; set; }
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
