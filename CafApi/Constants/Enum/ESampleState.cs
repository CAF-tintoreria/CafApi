using System.ComponentModel.DataAnnotations;

namespace CafApi.Constants.Enum
{
    public enum ESampleState
    {
        [Display(Name = "Realizado")]
        Realizado = 0,
        [Display(Name = "Entregado")]
        Entregado =  1,
    }

    public static class ESampleStateExtension
    {
        private static readonly Dictionary<ESampleState, string> _enumToString = new()
        {
            { ESampleState.Entregado, "Entregado" },
            { ESampleState.Realizado, "Realizado" }
        };
    }
}
