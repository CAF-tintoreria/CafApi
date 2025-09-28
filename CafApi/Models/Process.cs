
using System.ComponentModel.DataAnnotations;

namespace CafApi.Models
{
    public class Process
    {
        public int ProcessID { get; set; }

        [Required, StringLength(80)]
        public string TypeProcess { get; set; } = "";  

        [StringLength(300)]
        public string? Observations { get; set; }      
    }
}
