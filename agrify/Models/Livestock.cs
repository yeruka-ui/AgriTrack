using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace agrify.Models
{
    public class Livestock
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Species { get; set; } = string.Empty;

        // FIXED: Use decimal for precision
        public decimal Weight { get; set; }

        public string TagNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Activity { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        // FIXED: Use decimal for money
        public decimal Cost { get; set; }

        public int Quantity { get; set; }

        [NotMapped]
        public string DOBString => DateOfBirth.ToString("MM/dd/yyyy");
    }
}
