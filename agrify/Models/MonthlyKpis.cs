using System;
using System.ComponentModel.DataAnnotations;

namespace agrify.Models
{
    public class MonthlyKpis
    {
        [Key]
        public int Id { get; set; }

        public int Month { get; set; }   // 1-12
        public int Year { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal Profit { get; set; }
        public decimal Roi { get; set; }  // percentage
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }
}
