namespace MasterBeasProject.Models
{
    public class EngineerAvailability
    {
        public int Id { get; set; }

        public int EngineerProfileId { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public EngineerProfile EngineerProfile { get; set; } = null!;
    }
}
