using CafApi.Constants.Enum;
using System.ComponentModel.DataAnnotations;

namespace CafApi.Models.DTO
{
    // Models/DTO/ProcessDTO.cs
    public class ProcessDTO
    {
        public int ProcessID { get; set; }

        [Required, StringLength(50)]
        public string TypeProcess { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Observations { get; set; }
    }

    public class ProcessCreateDto
    {
        [Required, StringLength(50)]
        public string TypeProcess { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Observations { get; set; }
    }

    public class ProcessUpdateDto
    {
        [Required, StringLength(50)]
        public string TypeProcess { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Observations { get; set; }
    }

}
