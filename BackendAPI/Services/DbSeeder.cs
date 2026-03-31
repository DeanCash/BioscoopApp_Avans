using API.Services;
using BackendAPI.Models.Hall;
using BackendAPI.Models.Movie;
using BackendAPI.Models.Screening;
using BackendAPI.Models.Seat;
using BackendAPI.Models.Tariff;

namespace BackendAPI.Services;

public static class DbSeeder
{
    public static void Seed(ApplicationDbContext db)
    {
        SeedTariffs(db);
        SeedHalls(db);
        SeedMovies(db);
        SeedScreenings(db);
    }

    private static void SeedTariffs(ApplicationDbContext db)
    {
        if (db.Tariffs.Any()) return;

        db.Tariffs.AddRange(
            new TariffModel { TariffId = Guid.NewGuid(), TariffType = "Adult",   DisplayName = "Volwassene",     Price = 12.50m, SortOrder = 1 },
            new TariffModel { TariffId = Guid.NewGuid(), TariffType = "Child",   DisplayName = "Kind (t/m 12)",  Price =  8.50m, SortOrder = 2 },
            new TariffModel { TariffId = Guid.NewGuid(), TariffType = "Senior",  DisplayName = "Senioren (65+)", Price = 10.00m, SortOrder = 3 },
            new TariffModel { TariffId = Guid.NewGuid(), TariffType = "Student", DisplayName = "Student",        Price =  9.50m, SortOrder = 4 }
        );
        db.SaveChanges();
    }

    private static void SeedHalls(ApplicationDbContext db)
    {
        if (db.Halls.Any()) return;

        var halls = new List<HallModel>
        {
            new() { HallId = Guid.Parse("aaaaaaaa-0001-0000-0000-000000000000"), Number = 1, LayoutType = LayoutType.Standard,        CreatedAtUtc = DateTimeOffset.UtcNow },
            new() { HallId = Guid.Parse("aaaaaaaa-0002-0000-0000-000000000000"), Number = 2, LayoutType = LayoutType.Imax,             CreatedAtUtc = DateTimeOffset.UtcNow },
            new() { HallId = Guid.Parse("aaaaaaaa-0003-0000-0000-000000000000"), Number = 3, LayoutType = LayoutType.Vip,              CreatedAtUtc = DateTimeOffset.UtcNow },
            new() { HallId = Guid.Parse("aaaaaaaa-0004-0000-0000-000000000000"), Number = 4, LayoutType = LayoutType.Standard,         CreatedAtUtc = DateTimeOffset.UtcNow },
            new() { HallId = Guid.Parse("aaaaaaaa-0005-0000-0000-000000000000"), Number = 5, LayoutType = LayoutType.Standard,         CreatedAtUtc = DateTimeOffset.UtcNow },
            new() { HallId = Guid.Parse("aaaaaaaa-0006-0000-0000-000000000000"), Number = 6, LayoutType = LayoutType.Standard,         CreatedAtUtc = DateTimeOffset.UtcNow },
        };

        var rowLabels = new[] { "A", "B", "C", "D", "E", "F", "G", "H" };

        foreach (var hall in halls)
        {
            switch (hall.Number)
            {
                // Zaal 1-3: 8 rijen van 15 stoelen (120 stoelen)
                case 1 or 2 or 3:
                    for (int r = 0; r < 8; r++)
                        for (int s = 1; s <= 15; s++)
                            hall.Seats.Add(new SeatModel { SeatId = Guid.NewGuid(), HallId = hall.HallId, RowLabel = rowLabels[r], SeatNumber = s });
                    break;

                // Zaal 4: 6 rijen van 10 stoelen (60 stoelen)
                case 4:
                    for (int r = 0; r < 6; r++)
                        for (int s = 1; s <= 10; s++)
                            hall.Seats.Add(new SeatModel { SeatId = Guid.NewGuid(), HallId = hall.HallId, RowLabel = rowLabels[r], SeatNumber = s });
                    break;

                // Zaal 5 & 6: voorin 2 rijen van 10, achterin 2 rijen van 15 (50 stoelen)
                case 5 or 6:
                    for (int r = 0; r < 2; r++)
                        for (int s = 1; s <= 10; s++)
                            hall.Seats.Add(new SeatModel { SeatId = Guid.NewGuid(), HallId = hall.HallId, RowLabel = rowLabels[r], SeatNumber = s });
                    for (int r = 2; r < 4; r++)
                        for (int s = 1; s <= 15; s++)
                            hall.Seats.Add(new SeatModel { SeatId = Guid.NewGuid(), HallId = hall.HallId, RowLabel = rowLabels[r], SeatNumber = s });
                    break;
            }
        }

        db.Halls.AddRange(halls);
        db.SaveChanges();
    }

    private static void SeedMovies(ApplicationDbContext db)
    {
        if (db.Movies.Any()) return;

        db.Movies.AddRange(
            new MovieModel
            {
                MovieId         = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000000"),
                Title           = "Dune: Part Three",
                Description     = "Het epische avontuur op Arrakis gaat verder.",
                DurationMinutes = 155,
                Age             = 12,
                Genre           = "Science Fiction",
                ImageUrl        = "https://image.tmdb.org/t/p/w500/dune3.jpg",
                CreatedAtUtc    = DateTimeOffset.UtcNow
            },
            new MovieModel
            {
                MovieId         = Guid.Parse("bbbbbbbb-0002-0000-0000-000000000000"),
                Title           = "Avengers: Secret Wars",
                Description     = "De Avengers strijden tegen een multiversaal gevaar.",
                DurationMinutes = 180,
                Age             = 12,
                Genre           = "Actie",
                ImageUrl        = "https://image.tmdb.org/t/p/w500/avengers-secret-wars.jpg",
                CreatedAtUtc    = DateTimeOffset.UtcNow
            },
            new MovieModel
            {
                MovieId         = Guid.Parse("bbbbbbbb-0003-0000-0000-000000000000"),
                Title           = "The Wild Robot 2",
                Description     = "Roz en Brightbill beleven nieuwe avonturen in de wildernis.",
                DurationMinutes = 102,
                Age             = 6,
                Genre           = "Animatie",
                ImageUrl        = "https://image.tmdb.org/t/p/w500/wild-robot-2.jpg",
                CreatedAtUtc    = DateTimeOffset.UtcNow
            },
            new MovieModel
            {
                MovieId         = Guid.Parse("bbbbbbbb-0004-0000-0000-000000000000"),
                Title           = "Mission: Impossible – The Final Reckoning",
                Description     = "Ethan Hunt in zijn meest gevaarlijke missie ooit.",
                DurationMinutes = 143,
                Age             = 12,
                Genre           = "Thriller",
                ImageUrl        = "https://image.tmdb.org/t/p/w500/mission-impossible-final.jpg",
                CreatedAtUtc    = DateTimeOffset.UtcNow
            },
            new MovieModel
            {
                MovieId         = Guid.Parse("bbbbbbbb-0005-0000-0000-000000000000"),
                Title           = "Thunderbolts*",
                Description     = "Een team van Marvel-antihelden wordt samengesteld voor een gevaarlijke missie.",
                DurationMinutes = 127,
                Age             = 12,
                Genre           = "Actie",
                ImageUrl        = "https://image.tmdb.org/t/p/w500/thunderbolts.jpg",
                CreatedAtUtc    = DateTimeOffset.UtcNow
            },
            new MovieModel
            {
                MovieId         = Guid.Parse("bbbbbbbb-0006-0000-0000-000000000000"),
                Title           = "A Minecraft Movie",
                Description     = "Vier misfits worden meegesleurd naar een wereld van blokken en moeten samen een held vinden.",
                DurationMinutes = 101,
                Age             = 6,
                Genre           = "Avontuur",
                ImageUrl        = "https://image.tmdb.org/t/p/w500/minecraft-movie.jpg",
                CreatedAtUtc    = DateTimeOffset.UtcNow
            },
            new MovieModel
            {
                MovieId         = Guid.Parse("bbbbbbbb-0007-0000-0000-000000000000"),
                Title           = "Snow White",
                Description     = "Een live-action hervertelling van het klassieke sprookje.",
                DurationMinutes = 109,
                Age             = 6,
                Genre           = "Fantasie",
                ImageUrl        = "https://image.tmdb.org/t/p/w500/snow-white-2025.jpg",
                CreatedAtUtc    = DateTimeOffset.UtcNow
            }
        );
        db.SaveChanges();
    }

    private static void SeedScreenings(ApplicationDbContext db)
    {
        if (db.Screenings.Any()) return;

        var now   = DateTimeOffset.UtcNow;
        var today = now.Date;

        // Vaste hall-IDs
        var hall1 = Guid.Parse("aaaaaaaa-0001-0000-0000-000000000000");
        var hall2 = Guid.Parse("aaaaaaaa-0002-0000-0000-000000000000");
        var hall3 = Guid.Parse("aaaaaaaa-0003-0000-0000-000000000000");
        var hall4 = Guid.Parse("aaaaaaaa-0004-0000-0000-000000000000");
        var hall5 = Guid.Parse("aaaaaaaa-0005-0000-0000-000000000000");
        var hall6 = Guid.Parse("aaaaaaaa-0006-0000-0000-000000000000");

        // Vaste film-IDs
        var dune         = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000000");
        var avengers     = Guid.Parse("bbbbbbbb-0002-0000-0000-000000000000");
        var wildRobot    = Guid.Parse("bbbbbbbb-0003-0000-0000-000000000000");
        var mission      = Guid.Parse("bbbbbbbb-0004-0000-0000-000000000000");
        var thunderbolts = Guid.Parse("bbbbbbbb-0005-0000-0000-000000000000");
        var minecraft    = Guid.Parse("bbbbbbbb-0006-0000-0000-000000000000");
        var snowWhite    = Guid.Parse("bbbbbbbb-0007-0000-0000-000000000000");

        var screenings = new List<ScreeningModel>();

        // Dune – binnen 7 dagen, IMAX
        for (int d = 1; d <= 6; d++)
        {
            screenings.Add(new ScreeningModel
            {
                ScreeningId  = Guid.NewGuid(),
                MovieId      = dune,
                HallId       = hall2,
                StartTimeUtc = new DateTimeOffset(today.AddDays(d).Add(TimeSpan.FromHours(14)), TimeSpan.Zero),
                CreatedAtUtc = now
            });
            screenings.Add(new ScreeningModel
            {
                ScreeningId  = Guid.NewGuid(),
                MovieId      = dune,
                HallId       = hall2,
                StartTimeUtc = new DateTimeOffset(today.AddDays(d).Add(TimeSpan.FromHours(20)), TimeSpan.Zero),
                CreatedAtUtc = now
            });
        }

        // Avengers – binnen 7 dagen
        for (int d = 1; d <= 6; d++)
        {
            screenings.Add(new ScreeningModel
            {
                ScreeningId  = Guid.NewGuid(),
                MovieId      = avengers,
                HallId       = hall1,
                StartTimeUtc = new DateTimeOffset(today.AddDays(d).Add(TimeSpan.FromHours(16)), TimeSpan.Zero),
                CreatedAtUtc = now
            });
        }

        // Wild Robot – binnen 7 dagen
        for (int d = 1; d <= 6; d++)
        {
            screenings.Add(new ScreeningModel
            {
                ScreeningId  = Guid.NewGuid(),
                MovieId      = wildRobot,
                HallId       = hall3,
                StartTimeUtc = new DateTimeOffset(today.AddDays(d).Add(TimeSpan.FromHours(12)), TimeSpan.Zero),
                CreatedAtUtc = now
            });
        }

        // Mission Impossible – binnen 7 dagen
        for (int d = 1; d <= 6; d++)
        {
            screenings.Add(new ScreeningModel
            {
                ScreeningId  = Guid.NewGuid(),
                MovieId      = mission,
                HallId       = hall4,
                StartTimeUtc = new DateTimeOffset(today.AddDays(d).Add(TimeSpan.FromHours(19, 30, 0)), TimeSpan.Zero),
                CreatedAtUtc = now
            });
        }

        // Thunderbolts* – binnen 7 dagen, zaal 1
        for (int d = 1; d <= 6; d++)
        {
            screenings.Add(new ScreeningModel
            {
                ScreeningId  = Guid.NewGuid(),
                MovieId      = thunderbolts,
                HallId       = hall1,
                StartTimeUtc = new DateTimeOffset(today.AddDays(d).Add(TimeSpan.FromHours(18)), TimeSpan.Zero),
                CreatedAtUtc = now
            });
        }

        // A Minecraft Movie – binnen 7 dagen, zaal 3
        for (int d = 1; d <= 6; d++)
        {
            screenings.Add(new ScreeningModel
            {
                ScreeningId  = Guid.NewGuid(),
                MovieId      = minecraft,
                HallId       = hall3,
                StartTimeUtc = new DateTimeOffset(today.AddDays(d).Add(TimeSpan.FromHours(15)), TimeSpan.Zero),
                CreatedAtUtc = now
            });
        }

        // Snow White – binnen 7 dagen, zaal 4
        for (int d = 1; d <= 6; d++)
        {
            screenings.Add(new ScreeningModel
            {
                ScreeningId  = Guid.NewGuid(),
                MovieId      = snowWhite,
                HallId       = hall4,
                StartTimeUtc = new DateTimeOffset(today.AddDays(d).Add(TimeSpan.FromHours(13)), TimeSpan.Zero),
                CreatedAtUtc = now
            });
        }

        db.Screenings.AddRange(screenings);
        db.SaveChanges();
    }
}
