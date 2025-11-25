using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace agrify.Models;
public class Sale
{
    public int SaleId { get; set; }
    public int ItemId { get; set; }
    public string ItemType { get; set; } = "";
    public string? ItemName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount => Quantity * UnitPrice;
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }


}
