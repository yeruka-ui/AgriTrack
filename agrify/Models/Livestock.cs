using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace agrify.Models
{
    public class Livestock
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } // The auto-incrementing Primary Key

        // The specific fields used in your UI
        public string Species { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Activity { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        [NotMapped]
        public string DOBString => DateOfBirth.ToString("MM/dd/yyyy");
    }
}
