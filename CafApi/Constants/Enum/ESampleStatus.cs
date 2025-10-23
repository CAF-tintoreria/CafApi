using System.ComponentModel.DataAnnotations;

namespace CafApi.Constants.Enum
{
    public enum ESampleStatus
    {
        Entrada = 1,
        Terminado = 2,
        Entregado = 3
    }

    public static class ESampleStateExtension
    {
        private static readonly Dictionary<ESampleStatus, string> _enumToString = new()
        {
            { ESampleStatus.Entrada, "Entrada" },
            { ESampleStatus.Terminado, "Terminado" },
            { ESampleStatus.Entregado, "Entregado" }

        };
    }
}
