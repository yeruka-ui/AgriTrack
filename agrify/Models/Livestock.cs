using System.ComponentModel.DataAnnotations;

namespace agrify.Models
{
    public class Livestock
    {
        [Key]
        public int Id { get; set; }
        public string TagNumber { get; set; }
        public string Breed { get; set; }
        public DateTime DateOfBirth { get; set; }
        // input other future properties
    }
}
