using System;
using System.ComponentModel.DataAnnotations;

namespace agrify.Models
{
    /// <summary>
    /// Represents a single event or task on the calendar.
    /// </summary>
    public class CalendarTask
    {
        // TODO: Add an 'int Id' property for the database
        // [Key]
        // public int Id { get; set; }
        [Key]
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsComplete { get; set; } = false;
        public string Notes { get; set; }

        // A helper property to make the time look nice in the list
        public string FormattedTime => StartTime.ToString("hh:mm tt");
    }
}
