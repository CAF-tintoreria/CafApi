using System.ComponentModel.DataAnnotations;

namespace CafApi.Constants.Enum
{
    public enum ETypeProcess
    {
        [Display(Name = "Batik")]
        Batik = 0,
        [Display(Name = "Gel")]
        Gel = 1,
        [Display(Name = "Nevado")]
        Nevado = 2,
        [Display(Name = "Lavado")]
        Lavado = 3,
    }
}
