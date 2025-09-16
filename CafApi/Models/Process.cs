using System.ComponentModel.DataAnnotations;

namespace CafApi.Models
{
    public class Process
    {
        public int ProcessID { get; set; }

        [Required, StringLength(80)]
        public required Enum TypeProcess { get; set; }   // "Batik", "Gel", "Nevado"

        [StringLength(300)]
        public string? Observations { get; set; }         // opcional
        
    }
}
