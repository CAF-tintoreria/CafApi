using CafApi.Constants.Enum;
using System.ComponentModel.DataAnnotations;

namespace CafApi.Models.DTO
{
    public class ProcessDTO
    {
        public int ProcessID { get; set; }

        [Required, StringLength(80)]
        public required string TypeProcess { get; set; }   // "Batik", "Gel", "Nevado"

        [StringLength(300)]
        public string? Observations { get; set; }         // opcional
    }

    public class ProcessCreateDto
    {
        public string TypeProcess { get; set; } = "";
        public string? Observations { get; set; }
    }

    public class ProcessUpdateDto
    {
        public string TypeProcess { get; set; } = "";
        public string? Observations { get; set; }
    }
}
