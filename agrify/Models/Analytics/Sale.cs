using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace agrify.Models
{
    public class Sale
    {
        [Key]
        public int SaleId { get; set; }

        public int ItemId { get; set; } // ID of the Cow or Produce Batch
        public string ItemType { get; set; } // "Livestock" or "Produce"
        public string ItemName { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public DateTime SaleDate { get; set; }

        // Read-only property for the UI total
        [NotMapped]
        public decimal TotalAmount => Quantity * UnitPrice;

        [NotMapped]
        public string DateString => SaleDate.ToShortDateString();
    }
}
