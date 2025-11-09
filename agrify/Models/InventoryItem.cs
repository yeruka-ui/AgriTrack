using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

namespace agrify.Models
{
    public class InventoryItem
    {
        [Key]
        public int Id { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        // ... other properties
    }
}
