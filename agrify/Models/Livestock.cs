using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace agrify.Models
{
    public class Livestock
    {
        [Key]
        public int Id { get; set; } // Your database primary key

        // === PROPERTIES FROM YOUR MODEL ===
        public int Quantity { get; set; }          
        public DateTime Date { get; set; }
        public string? AnimalName { get; set; } // nullable
        public string? TagNumber { get; set; }  // nullable
        public string? Breed { get; set; }      // nullable
        public DateTime? DateOfBirth { get; set; } // nullable

        // === PROPERTIES NEEDED BY YOUR UI (FROM XAML) ===
        public string Species { get; set; } = string.Empty;
        public decimal? Weight { get; set; }    // nullable
        public string Gender { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Activity { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public decimal Cost { get; set; }

        // We will no longer use the "DOB" (string) property,
        // we will use your "DateOfBirth" (DateTime) property instead.
    }
}
