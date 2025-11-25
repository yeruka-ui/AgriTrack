using System;
using System.ComponentModel.DataAnnotations;

namespace agrify.Models
{
    public class Expense
    {
        [Key]
        public int Id { get; set; } // Matches your DB column
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
    }
}
