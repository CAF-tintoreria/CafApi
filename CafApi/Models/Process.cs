
using CafApi.Constants.Enum;
using System.ComponentModel.DataAnnotations;

namespace CafApi.Models
{
    public class Process
    {
        public int ProcessID { get; set; }

        [Required, StringLength(50)]
        public string TypeProcess { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Observations { get; set; }      
    }
}
