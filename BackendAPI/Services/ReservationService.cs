using API.Services;
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

        public async Task<ReservationGroupResponseDto?> ReserveAsync(Guid screeningId, int numberOfSeats = 1)
        {
            var screening = await _db.Screenings
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(s => s.ScreeningId == screeningId);

            if (screening == null)
                return null;

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
            int maxSeatNumber = allSeats.Max(s => s.SeatNumber);
            double centerRow = (totalRows - 1) / 2.0;
            double centerSeat = (maxSeatNumber + 1) / 2.0;

            double ScoreSeat(SeatModel s)
            {
                int rowIndex = s.RowLabel[0] - 'A';
                double rowScore = 10 - Math.Abs(rowIndex - centerRow) * 5;
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

            foreach (var seat in bestWindow)
            {
                var order = new OrderModel
                {
                    OrderId = Guid.NewGuid(),
                    ScreeningId = screeningId,
                    SeatId = seat.SeatId,
                    Status = "reserved",
                    PaymentStatus = "pending",
                    TotalAmount = 10.00m,
                    PrintCode = printCode,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                };
                orders.Add(order);
                _db.Orders.Add(order);
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

                return await ReserveAsync(screeningId, numberOfSeats);
            }

            return new ReservationGroupResponseDto
            {
                PrintCode = printCode,
                MovieTitle = screening.Movie.Title,
                HallNumber = screening.Hall.Number,
                Status = "reserved",
                TotalAmount = 10.00m * numberOfSeats,
                Seats = bestWindow.Select((seat, i) => new ReservationSeatDto
                {
                    OrderId = orders[i].OrderId,
                    RowLabel = seat.RowLabel,
                    SeatNumber = seat.SeatNumber,
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
