using System;
using System.Collections.Generic;

namespace BackendAPI.DTOs.Movies
{
    public sealed class UpcomingMovieDto
    {
        public Guid MovieId { get; init; }
        public string Title { get; init; } = default!;
        public DateTimeOffset FirstScreeningAtUtc { get; init; }
        public IReadOnlyList<UpcomingScreeningDto> Screenings { get; init; } = Array.Empty<UpcomingScreeningDto>();
    }

    public sealed class UpcomingScreeningDto
    {
        public Guid ScreeningId { get; init; }
        public DateTimeOffset StartTimeUtc { get; init; }
        public Guid HallId { get; init; }
    }
}