using System;

namespace agrify.Models
{
    public class Supplies
    {
        public int SuppliesId { get; set; } // For the database later
        public string ItemName { get; set; } // e.g., "Fertilizer", "Seeds"
        public int Quantity { get; set; }
        public double UnitCost { get; set; } // Cost for one unit
        public DateTime DateAcquired { get; set; }
        public string Notes { get; set; }

        // This is your calculated column.
        // It's a read-only property that multiplies quantity by unit cost.
        public double TotalCost => Quantity * UnitCost;
    }
}
