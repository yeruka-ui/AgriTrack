using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations; // Needed for [Key]

namespace agrify.Models
{
    public class User
    {
        [Key] // This tells EF Core that 'Id' is the primary key
        public int Id { get; set; }

        public string Username { get; set; }

        public string PasswordHash { get; set; }

        public string Role { get; set; }
    }
}
