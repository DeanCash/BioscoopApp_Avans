using API.Services;
using BackendAPI.Models.Arrangement;
using BackendAPI.Models.Order;
using BackendAPI.Models.Reservation;
using BackendAPI.Models.Seat;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Services
{
    public class ReservationService
    {
        private readonly ApplicationDbContext _db;

        public ReservationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ReservationGroupResponseDto?> ReserveAsync(
            Guid screeningId,
            List<TicketLineDto> tickets,
            List<ArrangementLineDto>? arrangements = null)
        {
            arrangements ??= new List<ArrangementLineDto>();

            var screening = await _db.Screenings
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(s => s.ScreeningId == screeningId);

            if (screening == null)
                return null;

            // Look up tariff prices
            var allTariffs = await _db.Tariffs.ToListAsync();
            var tariffIds = tickets.Select(t => t.TariffId).Distinct().ToHashSet();
            var tariffs = allTariffs
                .Where(t => tariffIds.Contains(t.TariffId))
                .ToDictionary(t => t.TariffId);

            // Build a flat list: one entry per ticket with its tariff + price
            var ticketList = new List<(Guid TariffId, decimal Price)>();
            foreach (var line in tickets)
            {
                if (!tariffs.TryGetValue(line.TariffId, out var tariff))
                    throw new InvalidOperationException("INVALID_TARIFF");

                for (int i = 0; i < line.Count; i++)
                    ticketList.Add((line.TariffId, tariff.Price));
            }

            // Look up arrangement prices
            var arrangementIds = arrangements.Select(a => a.ArrangementId).Distinct().ToHashSet();
            var arrangementModels = await _db.Arrangements
                .Where(a => arrangementIds.Contains(a.ArrangementId) && a.IsActive)
                .ToDictionaryAsync(a => a.ArrangementId);

            // Validate arrangements
            var arrangementLines = new List<(Guid ArrangementId, string Name, int Quantity, decimal UnitPrice, decimal LineTotal)>();
            foreach (var line in arrangements)
            {
                if (!arrangementModels.TryGetValue(line.ArrangementId, out var arrangement))
                    throw new InvalidOperationException("INVALID_ARRANGEMENT");

                var lineTotal = arrangement.Price * line.Quantity;
                arrangementLines.Add((line.ArrangementId, arrangement.Name, line.Quantity, arrangement.Price, lineTotal));
            }

            int numberOfSeats = ticketList.Count;
            if (numberOfSeats == 0)
                throw new InvalidOperationException("NO_TICKETS");

            var allSeats = await _db.Seats
                .Where(s => s.HallId == screening.HallId)
                .ToListAsync();

            var reservedSeatIds = await _db.Orders
                .Where(o => o.ScreeningId == screeningId)
                .Select(o => o.SeatId)
                .ToListAsync();

            var availableSeats = allSeats
                .Where(s => !reservedSeatIds.Contains(s.SeatId))
                .ToList();

            int totalRows = allSeats.Select(s => s.RowLabel).Distinct().Count();
            double centerRow = (totalRows - 1) / 2.0;

            // Per-row max seat number so center is correct for mixed-width halls
            var seatsPerRow = allSeats
                .GroupBy(s => s.RowLabel)
                .ToDictionary(g => g.Key, g => g.Max(s => s.SeatNumber));

            double ScoreSeat(SeatModel s)
            {
                int rowIndex = s.RowLabel[0] - 'A';
                double rowScore = 10 - Math.Abs(rowIndex - centerRow) * 5;
                double centerSeat = (seatsPerRow[s.RowLabel] + 1) / 2.0;
                double seatScore = 10 - Math.Abs(s.SeatNumber - centerSeat) * 2.2;
                return rowScore + seatScore;
            }

            // Find the best group of adjacent seats
            var bestWindow = FindBestAdjacentGroup(availableSeats, numberOfSeats, ScoreSeat);

            if (bestWindow == null)
                throw new InvalidOperationException(
                    numberOfSeats == 1 ? "SOLD_OUT" : "NO_ADJACENT_SEATS");

            var printCode = Guid.NewGuid().ToString("N")[..6].ToUpper();
            var orders = new List<OrderModel>();
            var orderArrangements = new List<OrderArrangementModel>();

            // Calculate totals
            var ticketTotal = ticketList.Sum(t => t.Price);
            var arrangementTotal = arrangementLines.Sum(a => a.LineTotal);

            for (int i = 0; i < bestWindow.Count; i++)
            {
                var seat = bestWindow[i];
                var ticket = ticketList[i];
                var order = new OrderModel
                {
                    OrderId = Guid.NewGuid(),
                    ScreeningId = screeningId,
                    SeatId = seat.SeatId,
                    TariffId = ticket.TariffId,
                    Status = "reserved",
                    PaymentStatus = "pending",
                    TotalAmount = ticket.Price,
                    PrintCode = printCode,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                };
                orders.Add(order);
                _db.Orders.Add(order);
            }

            // Add arrangements to the first order (they belong to the group)
            if (orders.Count > 0 && arrangementLines.Count > 0)
            {
                var firstOrder = orders[0];
                foreach (var arrLine in arrangementLines)
                {
                    var orderArrangement = new OrderArrangementModel
                    {
                        OrderArrangementId = Guid.NewGuid(),
                        OrderId = firstOrder.OrderId,
                        ArrangementId = arrLine.ArrangementId,
                        Quantity = arrLine.Quantity,
                        UnitPrice = arrLine.UnitPrice,
                        LineTotal = arrLine.LineTotal
                    };
                    orderArrangements.Add(orderArrangement);
                    _db.OrderArrangements.Add(orderArrangement);
                }
                // Add arrangement total to first order
                firstOrder.TotalAmount += arrangementTotal;
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Race condition: another request reserved one of the seats.
                // Detach and retry once with fresh available-seats query.
                foreach (var order in orders)
                    _db.Entry(order).State = EntityState.Detached;

                return await ReserveAsync(screeningId, tickets, arrangements);
            }

            return new ReservationGroupResponseDto
            {
                PrintCode = printCode,
                MovieTitle = screening.Movie.Title,
                HallNumber = screening.Hall.Number,
                Status = "reserved",
                TotalAmount = ticketTotal + arrangementTotal,
                TicketAmount = ticketTotal,
                ArrangementAmount = arrangementTotal,
                Seats = bestWindow.Select((seat, i) => new ReservationSeatDto
                {
                    OrderId = orders[i].OrderId,
                    RowLabel = seat.RowLabel,
                    SeatNumber = seat.SeatNumber,
                }).ToList(),
                Arrangements = arrangementLines.Select(a => new ArrangementItemDto
                {
                    ArrangementId = a.ArrangementId,
                    Name = a.Name,
                    Quantity = a.Quantity,
                    UnitPrice = a.UnitPrice,
                    LineTotal = a.LineTotal,
                }).ToList(),
            };
        }

        private static List<SeatModel>? FindBestAdjacentGroup(
            List<SeatModel> availableSeats, int groupSize, Func<SeatModel, double> scoreSeat)
        {
            if (availableSeats.Count < groupSize)
                return null;

            List<SeatModel>? bestWindow = null;
            double bestScore = double.MinValue;

            // Group available seats by row
            var byRow = availableSeats
                .GroupBy(s => s.RowLabel)
                .Where(g => g.Count() >= groupSize);

            foreach (var rowGroup in byRow)
            {
                var sorted = rowGroup.OrderBy(s => s.SeatNumber).ToList();

                // Find consecutive runs within this row
                var runs = new List<List<SeatModel>>();
                var currentRun = new List<SeatModel> { sorted[0] };

                for (int i = 1; i < sorted.Count; i++)
                {
                    if (sorted[i].SeatNumber == sorted[i - 1].SeatNumber + 1)
                    {
                        currentRun.Add(sorted[i]);
                    }
                    else
                    {
                        runs.Add(currentRun);
                        currentRun = new List<SeatModel> { sorted[i] };
                    }
                }
                runs.Add(currentRun);

                // Slide window of groupSize over each run
                foreach (var run in runs.Where(r => r.Count >= groupSize))
                {
                    for (int start = 0; start <= run.Count - groupSize; start++)
                    {
                        var window = run.GetRange(start, groupSize);
                        double windowScore = window.Average(s => scoreSeat(s));

                        if (windowScore > bestScore)
                        {
                            bestScore = windowScore;
                            bestWindow = window;
                        }
                    }
                }
            }

            return bestWindow;
        }
    }
}
