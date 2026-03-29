namespace BioscoopFrontend.Models;

public class UpcomingMovieDto
{
    public Guid MovieId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Genre { get; set; } = "";
    public int Age { get; set; }
    public int DurationMinutes { get; set; }
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

public class MovieDetailDto
{
    public Guid MovieId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int DurationMinutes { get; set; }
    public int Age { get; set; }
    public string Genre { get; set; } = "";
    public string? ImageUrl { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public class ScreeningResponseDto
{
    public Guid ScreeningId { get; set; }
    public Guid MovieId { get; set; }
    public Guid HallId { get; set; }
    public DateTimeOffset StartTimeUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public MovieDetailDto? Movie { get; set; }
    public HallDto? Hall { get; set; }
}

public class HallDto
{
    public Guid HallId { get; set; }
    public int Number { get; set; }
    public List<SeatDto> Seats { get; set; } = new();
}

public class SeatDto
{
    public Guid SeatId { get; set; }
    public Guid HallId { get; set; }
    public string RowLabel { get; set; } = "";
    public int SeatNumber { get; set; }
}

public class TariffDto
{
    public Guid TariffId { get; set; }
    public string TariffType { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public decimal Price { get; set; }
    public int SortOrder { get; set; }
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

public class ReservationGroupResponseDto
{
    public string PrintCode { get; set; } = "";
    public string MovieTitle { get; set; } = "";
    public int HallNumber { get; set; }
    public string Status { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public decimal TicketAmount { get; set; }
    public decimal ArrangementAmount { get; set; }
    public List<ReservationSeatDto> Seats { get; set; } = new();
    public List<ArrangementItemDto> Arrangements { get; set; } = new();
}

public class ReservationSeatDto
{
    public Guid OrderId { get; set; }
    public string RowLabel { get; set; } = "";
    public int SeatNumber { get; set; }
}

// Arrangement DTOs voor horeca
public class ArrangementDto
{
    public Guid ArrangementId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class ArrangementItemDto
{
    public Guid ArrangementId { get; set; }
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
