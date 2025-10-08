using System.ComponentModel.DataAnnotations;

namespace CafApi.Constants.Enum
{
    public enum ESampleStatus
    {
        [Display(Name = "Realizado")]
        Realizado = 0,
        [Display(Name = "Entregado")]
        Entregado =  1,
    }

    public static class ESampleStateExtension
    {
        private static readonly Dictionary<ESampleStatus, string> _enumToString = new()
        {
            { ESampleStatus.Entregado, "Entregado" },
            { ESampleStatus.Realizado, "Realizado" }
        };
    }
}
