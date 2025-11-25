using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace agrify.Models;
public class Investment
{
    public int InvestmentID { get; set; }  // matches DB column now
    public string InvestmentName { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = "";
    public string Notes { get; set; }

}
