using System;

namespace agrify.Models
{
    public class Produce
    {
        public int ProduceId { get; set; } // For the database
        public string ProduceType { get; set; } // e.g., "Tomatoes", "Corn"
        public int Quantity { get; set; } // e.g., 150
        public string Weight { get; set; } // e.g., "50kg"
        public DateTime HarvestDate { get; set; } // Date harvested or acquired
        public string Notes { get; set; }
    }
}
