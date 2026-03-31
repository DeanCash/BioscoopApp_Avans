using API.Services;
using BackendAPI.Controllers;
using BackendAPI.DTOs.Orders;
using BackendAPI.Models.Arrangement;
using BackendAPI.Models.Hall;
using BackendAPI.Models.Movie;
using BackendAPI.Models.Order;
using BackendAPI.Models.Reservation;
using BackendAPI.Models.Screening;
using BackendAPI.Models.Seat;
using BackendAPI.Models.Tariff;
using BackendAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Tests.Integration;

/// <summary>
/// Integration tests voor de complete ticket afdruk workflow.
/// Test het volledige proces: bioscoopbezoeker reserveert -> betaalt -> drukt ticket af -> komt zaal binnen.
/// </summary>
public class TicketPrintWorkflowTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ReservationService _reservationService;
    private readonly OrdersController _ordersController;
    private readonly ReservationsController _reservationsController;
    private Guid _screeningId;
    private Guid _hallId;
    private Guid _movieId;
    private Guid _adultTariffId;
    private Guid _childTariffId;
    private Guid _seniorTariffId;
    private Guid _popcornArrangementId;
    private Guid _drinkArrangementId;

    public TicketPrintWorkflowTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _reservationService = new ReservationService(_db);
        _ordersController = new OrdersController(_db);
        _reservationsController = new ReservationsController(_reservationService);

        SeedTestData().Wait();
    }

    private async Task SeedTestData()
    {
        // Maak zaal aan met stoelen
        _hallId = Guid.NewGuid();
        var hall = new HallModel
        {
            HallId = _hallId,
            Number = 1,
            LayoutType = LayoutType.Standard,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        _db.Halls.Add(hall);

        // Maak 5 rijen van 10 stoelen aan (50 stoelen totaal)
        var rows = new[] { "A", "B", "C", "D", "E" };
        foreach (var row in rows)
        {
            for (int seatNum = 1; seatNum <= 10; seatNum++)
            {
                _db.Seats.Add(new SeatModel
                {
                    SeatId = Guid.NewGuid(),
                    HallId = _hallId,
                    RowLabel = row,
                    SeatNumber = seatNum
                });
            }
        }

        // Maak film aan
        _movieId = Guid.NewGuid();
        var movie = new MovieModel
        {
            MovieId = _movieId,
            Title = "Inception",
            DurationMinutes = 148,
            Genre = "Sci-Fi",
            Age = 13,
            Description = "A mind-bending thriller",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        _db.Movies.Add(movie);

        // Maak screening aan
        _screeningId = Guid.NewGuid();
        var screening = new ScreeningModel
        {
            ScreeningId = _screeningId,
            MovieId = _movieId,
            HallId = _hallId,
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(3),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        _db.Screenings.Add(screening);

        // Maak tarieven aan
        _adultTariffId = Guid.NewGuid();
        _db.Tariffs.Add(new TariffModel
        {
            TariffId = _adultTariffId,
            TariffType = "Adult",
            DisplayName = "Volwassene",
            Price = 12.50m,
            SortOrder = 1
        });

        _childTariffId = Guid.NewGuid();
        _db.Tariffs.Add(new TariffModel
        {
            TariffId = _childTariffId,
            TariffType = "Child",
            DisplayName = "Kind",
            Price = 8.00m,
            SortOrder = 2
        });

        _seniorTariffId = Guid.NewGuid();
        _db.Tariffs.Add(new TariffModel
        {
            TariffId = _seniorTariffId,
            TariffType = "Senior",
            DisplayName = "65+",
            Price = 10.00m,
            SortOrder = 3
        });

        // Maak horeca arrangements aan
        _popcornArrangementId = Guid.NewGuid();
        _db.Arrangements.Add(new ArrangementModel
        {
            ArrangementId = _popcornArrangementId,
            Name = "Popcorn Large",
            Description = "Large bucket of popcorn",
            Category = ArrangementCategory.Popcorn,
            Price = 6.50m,
            SortOrder = 1,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        _drinkArrangementId = Guid.NewGuid();
        _db.Arrangements.Add(new ArrangementModel
        {
            ArrangementId = _drinkArrangementId,
            Name = "Cola Medium",
            Description = "Medium Coca-Cola",
            Category = ArrangementCategory.Drank,
            Price = 4.50m,
            SortOrder = 2,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task CompleteWorkflow_EnkelBezoeker_ReserverenBetalenAfdrukkenZaalIn()
    {
        // SCENARIO: Enkele bioscoopbezoeker boekt ticket, betaalt, en komt met PrintCode de zaal in

        // STAP 1: Bezoeker reserveert 1 ticket via app/website
        var reservationRequest = new ReservationRequestDto
        {
            ScreeningId = _screeningId,
            Tickets = new List<TicketLineDto>
            {
                new() { TariffId = _adultTariffId, Count = 1 }
            }
        };

        var reservationResult = await _reservationsController.Reserve(reservationRequest);
        var reservationResponse = (reservationResult as OkObjectResult)?.Value as ReservationGroupResponseDto;

        Assert.NotNull(reservationResponse);
        Assert.NotNull(reservationResponse.PrintCode);
        Assert.Equal("reserved", reservationResponse.Status);
        Assert.Equal(12.50m, reservationResponse.TotalAmount);

        var printCode = reservationResponse.PrintCode;
        var orderId = reservationResponse.Seats[0].OrderId;

        // STAP 2: Bezoeker betaalt aan kassa of online
        var paymentResult = await _ordersController.ConfirmPayment(orderId);
        var paymentResponse = (paymentResult.Result as OkObjectResult)?.Value as OrderResponse;

        Assert.NotNull(paymentResponse);
        Assert.Equal("Confirmed", paymentResponse.Status);
        Assert.Equal("Paid", paymentResponse.PaymentStatus);
        Assert.Equal(printCode, paymentResponse.PrintCode);

        // STAP 3: Bezoeker drukt ticket af (kan aan automaat of per mail ontvangen)
        var order = await _db.Orders.FindAsync(orderId);
        Assert.NotNull(order);
        order.PrintedAtUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        // STAP 4: Bezoeker komt bij zaalingang - medewerker scant PrintCode
        var verifyOrder = await _db.Orders
            .Include(o => o.Screening)
            .ThenInclude(s => s.Movie)
            .Include(o => o.Seat)
            .FirstOrDefaultAsync(o => o.PrintCode == printCode);

        Assert.NotNull(verifyOrder);
        Assert.Equal("Confirmed", verifyOrder.Status);
        Assert.Equal("Paid", verifyOrder.PaymentStatus);
        Assert.NotNull(verifyOrder.PrintedAtUtc);
        Assert.Equal("Inception", verifyOrder.Screening.Movie.Title);
        Assert.NotNull(verifyOrder.Seat);

        // SUCCES: Bezoeker mag de zaal in!
    }

    [Fact]
    public async Task CompleteWorkflow_Groepsreservering_DriePersonenMetGedeeldePrintCode()
    {
        // SCENARIO: Gezin van 3 (2 volwassenen + 1 kind) boekt samen, allen krijgen dezelfde PrintCode

        // STAP 1: Gezin reserveert 3 tickets tegelijk
        var reservationRequest = new ReservationRequestDto
        {
            ScreeningId = _screeningId,
            Tickets = new List<TicketLineDto>
            {
                new() { TariffId = _adultTariffId, Count = 2 },
                new() { TariffId = _childTariffId, Count = 1 }
            },
            Arrangements = new List<ArrangementLineDto>
            {
                new() { ArrangementId = _popcornArrangementId, Quantity = 2 },
                new() { ArrangementId = _drinkArrangementId, Quantity = 3 }
            }
        };

        var reservationResult = await _reservationsController.Reserve(reservationRequest);
        var reservationResponse = (reservationResult as OkObjectResult)?.Value as ReservationGroupResponseDto;

        Assert.NotNull(reservationResponse);
        Assert.Equal(3, reservationResponse.Seats.Count);
        Assert.Equal(2, reservationResponse.Arrangements.Count);

        var sharedPrintCode = reservationResponse.PrintCode;

        // Totaal: 2×€12.50 + 1×€8.00 + 2×€6.50 + 3×€4.50 = €59.50
        Assert.Equal(59.50m, reservationResponse.TotalAmount);

        // STAP 2: Betaal alle 3 tickets (elk order apart bevestigen)
        foreach (var seat in reservationResponse.Seats)
        {
            await _ordersController.ConfirmPayment(seat.OrderId);
        }

        // STAP 3: Controleer dat alle 3 orders dezelfde PrintCode hebben
        var groupOrders = await _db.Orders
            .Where(o => o.PrintCode == sharedPrintCode)
            .ToListAsync();

        Assert.Equal(3, groupOrders.Count);
        Assert.All(groupOrders, o =>
        {
            Assert.Equal("Confirmed", o.Status);
            Assert.Equal("Paid", o.PaymentStatus);
            Assert.Equal(sharedPrintCode, o.PrintCode);
        });

        // STAP 4: Bij zaalingang - medewerker scant 1× de PrintCode en ziet alle 3 tickets
        var entranceCheck = await _db.Orders
            .Include(o => o.Seat)
            .Where(o => o.PrintCode == sharedPrintCode && o.Status == "Confirmed")
            .ToListAsync();

        Assert.Equal(3, entranceCheck.Count);

        // Stoelen zitten naast elkaar
        var seats = entranceCheck.Select(o => o.Seat!).OrderBy(s => s.SeatNumber).ToList();
        Assert.Equal(seats[0].RowLabel, seats[1].RowLabel);
        Assert.Equal(seats[1].RowLabel, seats[2].RowLabel);
        Assert.Equal(seats[0].SeatNumber + 1, seats[1].SeatNumber);
        Assert.Equal(seats[1].SeatNumber + 1, seats[2].SeatNumber);

        // SUCCES: Alle 3 gezinsleden mogen de zaal in met 1 PrintCode!
    }

    [Fact]
    public async Task CompleteWorkflow_SeniorTicket_KortingToegepast()
    {
        // SCENARIO: Senior burger (65+) boekt ticket met korting

        // STAP 1: Reserveer senior ticket
        var reservationRequest = new ReservationRequestDto
        {
            ScreeningId = _screeningId,
            Tickets = new List<TicketLineDto>
            {
                new() { TariffId = _seniorTariffId, Count = 1 }
            }
        };

        var reservationResult = await _reservationsController.Reserve(reservationRequest);
        var reservationResponse = (reservationResult as OkObjectResult)?.Value as ReservationGroupResponseDto;

        Assert.NotNull(reservationResponse);
        Assert.Equal(10.00m, reservationResponse.TotalAmount); // Senior korting: €10 i.p.v. €12.50

        var printCode = reservationResponse.PrintCode;
        var orderId = reservationResponse.Seats[0].OrderId;

        // STAP 2: Betalen
        await _ordersController.ConfirmPayment(orderId);

        // STAP 3: Bij ingang - medewerker controleert leeftijd + PrintCode
        var verifyOrder = await _db.Orders
            .Include(o => o.Tariff)
            .FirstOrDefaultAsync(o => o.PrintCode == printCode);

        Assert.NotNull(verifyOrder);
        Assert.NotNull(verifyOrder.Tariff);
        Assert.Equal("Senior", verifyOrder.Tariff.TariffType);
        Assert.Equal(10.00m, verifyOrder.TotalAmount);

        // SUCCES: Senior mag de zaal in met kortingstarief!
    }

    [Fact]
    public async Task CompleteWorkflow_MetHoreca_ArrangementsOpTicketZichtbaar()
    {
        // SCENARIO: Bezoeker bestelt ticket + popcorn & drank, medewerker ziet dit bij afdrukken

        // STAP 1: Reserveer ticket met horeca
        var reservationRequest = new ReservationRequestDto
        {
            ScreeningId = _screeningId,
            Tickets = new List<TicketLineDto>
            {
                new() { TariffId = _adultTariffId, Count = 1 }
            },
            Arrangements = new List<ArrangementLineDto>
            {
                new() { ArrangementId = _popcornArrangementId, Quantity = 1 },
                new() { ArrangementId = _drinkArrangementId, Quantity = 1 }
            }
        };

        var reservationResult = await _reservationsController.Reserve(reservationRequest);
        var reservationResponse = (reservationResult as OkObjectResult)?.Value as ReservationGroupResponseDto;

        Assert.NotNull(reservationResponse);
        Assert.Equal(2, reservationResponse.Arrangements.Count);
        Assert.Equal(12.50m + 6.50m + 4.50m, reservationResponse.TotalAmount); // €23.50 totaal

        var printCode = reservationResponse.PrintCode;
        var orderId = reservationResponse.Seats[0].OrderId;

        // STAP 2: Betalen
        await _ordersController.ConfirmPayment(orderId);

        // STAP 3: Bij horeca balie - medewerker ziet via PrintCode wat besteld is
        var orderWithArrangements = await _db.Orders
            .Include(o => o.OrderArrangements)
            .ThenInclude(oa => oa.Arrangement)
            .FirstOrDefaultAsync(o => o.PrintCode == printCode);

        Assert.NotNull(orderWithArrangements);
        Assert.Equal(2, orderWithArrangements.OrderArrangements.Count);

        var arrangements = orderWithArrangements.OrderArrangements
            .Select(oa => oa.Arrangement.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.Contains("Cola Medium", arrangements);
        Assert.Contains("Popcorn Large", arrangements);

        // SUCCES: Bezoeker krijgt horeca bij balie en kan met PrintCode de zaal in!
    }

    [Fact]
    public async Task CompleteWorkflow_NietBetaald_ToeganggWeigerd()
    {
        // SCENARIO: Bezoeker reserveert maar betaalt niet - mag zaal niet in

        // STAP 1: Reserveer ticket maar betaal niet
        var reservationRequest = new ReservationRequestDto
        {
            ScreeningId = _screeningId,
            Tickets = new List<TicketLineDto>
            {
                new() { TariffId = _adultTariffId, Count = 1 }
            }
        };

        var reservationResult = await _reservationsController.Reserve(reservationRequest);
        var reservationResponse = (reservationResult as OkObjectResult)?.Value as ReservationGroupResponseDto;

        Assert.NotNull(reservationResponse);
        var printCode = reservationResponse.PrintCode;

        // STAP 2: Bezoeker probeert zaal in te gaan zonder te betalen
        var verifyOrder = await _db.Orders
            .FirstOrDefaultAsync(o => o.PrintCode == printCode);

        Assert.NotNull(verifyOrder);
        Assert.Equal("reserved", verifyOrder.Status);
        Assert.Equal("pending", verifyOrder.PaymentStatus);

        // ASSERT: Toegang geweigerd - PaymentStatus is niet "Paid"
        Assert.NotEqual("Paid", verifyOrder.PaymentStatus);
        Assert.NotEqual("Confirmed", verifyOrder.Status);

        // Bezoeker moet eerst betalen voordat hij de zaal in mag!
    }

    [Fact]
    public async Task CompleteWorkflow_VolledigeZaal_NieuweBezoekersKunnenNietReserveren()
    {
        // SCENARIO: Populaire film - alle 50 stoelen bezet, nieuwe bezoeker kan niet boeken

        // STAP 1: Reserveer alle 50 stoelen (50 enkelstoels reserveringen om zeker te zijn)
        var allSeats = await _db.Seats.Where(s => s.HallId == _hallId).ToListAsync();
        Assert.Equal(50, allSeats.Count); // Verify we hebben 50 stoelen

        // Reserveer alle stoelen door direct orders aan te maken
        foreach (var seat in allSeats)
        {
            _db.Orders.Add(new OrderModel
            {
                OrderId = Guid.NewGuid(),
                ScreeningId = _screeningId,
                SeatId = seat.SeatId,
                TariffId = _adultTariffId,
                Status = "reserved",
                PaymentStatus = "pending",
                TotalAmount = 12.50m,
                PrintCode = Guid.NewGuid().ToString("N")[..6].ToUpper(),
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        // Verify alle stoelen zijn bezet
        var reservedCount = await _db.Orders.CountAsync(o => o.ScreeningId == _screeningId);
        Assert.Equal(50, reservedCount);

        // STAP 2: Nieuwe bezoeker probeert te reserveren
        var lateRequest = new ReservationRequestDto
        {
            ScreeningId = _screeningId,
            Tickets = new List<TicketLineDto>
            {
                new() { TariffId = _adultTariffId, Count = 1 }
            }
        };

        // ASSERT: Reservering mislukt - SOLD_OUT
        var result = await _reservationsController.Reserve(lateRequest);
        Assert.IsType<ConflictObjectResult>(result);

        var conflictResult = result as ConflictObjectResult;
        Assert.Equal("All seats for this screening are sold out", conflictResult?.Value);
    }

    [Fact]
    public async Task CompleteWorkflow_TweeBezoekers_ElkMetEigenPrintCode()
    {
        // SCENARIO: Twee aparte bezoekers boeken onafhankelijk - elk krijgt eigen PrintCode

        // STAP 1: Eerste bezoeker reserveert
        var request1 = new ReservationRequestDto
        {
            ScreeningId = _screeningId,
            Tickets = new List<TicketLineDto>
            {
                new() { TariffId = _adultTariffId, Count = 2 }
            }
        };
        var result1 = await _reservationsController.Reserve(request1);
        var response1 = (result1 as OkObjectResult)?.Value as ReservationGroupResponseDto;

        // STAP 2: Tweede bezoeker reserveert
        var request2 = new ReservationRequestDto
        {
            ScreeningId = _screeningId,
            Tickets = new List<TicketLineDto>
            {
                new() { TariffId = _childTariffId, Count = 1 }
            }
        };
        var result2 = await _reservationsController.Reserve(request2);
        var response2 = (result2 as OkObjectResult)?.Value as ReservationGroupResponseDto;

        Assert.NotNull(response1);
        Assert.NotNull(response2);

        // ASSERT: Elke bezoeker heeft eigen unieke PrintCode
        Assert.NotEqual(response1.PrintCode, response2.PrintCode);

        // STAP 3: Bij ingang - elke bezoeker toont eigen PrintCode
        var orders1 = await _db.Orders.Where(o => o.PrintCode == response1.PrintCode).ToListAsync();
        var orders2 = await _db.Orders.Where(o => o.PrintCode == response2.PrintCode).ToListAsync();

        Assert.Equal(2, orders1.Count); // Bezoeker 1 heeft 2 tickets
        Assert.Single(orders2); // Bezoeker 2 heeft 1 ticket

        // Bevestig betaling voor beide
        foreach (var order in orders1.Concat(orders2))
        {
            await _ordersController.ConfirmPayment(order.OrderId);
        }

        // SUCCES: Beide bezoekers mogen onafhankelijk de zaal in met eigen PrintCode!
    }
}