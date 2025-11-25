using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace agrify.Models
{
    public class Investment
    {
        [Key]
        public int Id { get; set; } // Primary Key fixed
        public string Source { get; set; } // e.g., "Initial Capital"

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime Date { get; set; }

        [NotMapped]
        public string DateFormatted => Date.ToString("MM/dd/yyyy");

        [NotMapped]
        public string AmountFormatted => $"₱{Amount:N2}";
    }
}
