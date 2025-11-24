using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace agrify.Models.Analytics;
public class Revenues
{
    [Key]
    public int RevenueID { get; set; }
    public float Amount { get; set; }
    public DateOnly Date { get; set; }

}
