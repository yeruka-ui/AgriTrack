using System;
using System.ComponentModel.DataAnnotations;

namespace agrify.Models
{
    /// <summary>
    /// Represents a single Holiday event.
    /// </summary>
    public class Holiday
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime Date { get; set; }

        // A helper to format the date nicely
        public string FormattedDate => Date.ToString("MMM d"); // e.g., "Dec 25"
    }
}
