using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace agrify.Models.Analytics;
public class Expenses
{
    [Key]
    public int ExpenseID { get; set; }
    public float Amount { get; set; }
    public DateOnly Date { get; set; }

}
