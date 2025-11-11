using System;
using System.ComponentModel.DataAnnotations;

namespace agrify.Models
{
    public class Livestock
    {
        [Key]
        public int Id { get; set; } // Your database primary key

        // === PROPERTIES FROM YOUR MODEL ===
        public string TagNumber { get; set; } = string.Empty;
        public string Breed { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; } // Your original DateTime property

        // === PROPERTIES NEEDED BY YOUR UI (FROM XAML) ===
        public string Species { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Activity { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        // We will no longer use the "DOB" (string) property,
        // we will use your "DateOfBirth" (DateTime) property instead.
    }
}
