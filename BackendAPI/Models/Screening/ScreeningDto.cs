namespace BackendAPI.Models.Screening
{
    public class ScreeningDto
    {
        public Guid MovieId { get; set; }
        public Guid HallId { get; set; }
        public DateTimeOffset StartTimeUtc { get; set; }
    }
}
