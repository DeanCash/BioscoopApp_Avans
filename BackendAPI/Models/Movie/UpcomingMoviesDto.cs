using System;
using System.Collections.Generic;

namespace BackendAPI.DTOs.Movies
{
    public sealed class UpcomingMovieDto
    {
        public Guid MovieId { get; init; }
        public string Title { get; init; } = default!;
        public string? ImageUrl { get; init; }
        public DateTimeOffset FirstScreeningAtUtc { get; init; }
        public IReadOnlyList<UpcomingScreeningDto> Screenings { get; init; } = Array.Empty<UpcomingScreeningDto>();
    }

    public sealed class UpcomingScreeningDto
    {
        public Guid ScreeningId { get; init; }
        public DateTimeOffset StartTimeUtc { get; init; }
        public Guid HallId { get; init; }
        public int HallNumber { get; init; }
    }

    public sealed class MovieDetailsDto
    {
        public Guid MovieId { get; init; }
        public string Title { get; init; } = default!;
        public string Description { get; init; } = default!;
        public int DurationMinutes { get; init; }
        public int Age { get; init; }
        public string? ImageUrl { get; init; }
        public IReadOnlyList<ScreeningWithHallDto> Screenings { get; init; } = Array.Empty<ScreeningWithHallDto>();
    }

    public sealed class ScreeningWithHallDto
    {
        public Guid ScreeningId { get; init; }
        public DateTimeOffset StartTimeUtc { get; init; }
        public Guid HallId { get; init; }
        public int HallNumber { get; init; }
        public string HallName { get; init; } = default!;
        public int AvailableSeats { get; init; }
        public int TotalSeats { get; init; }
    }
}