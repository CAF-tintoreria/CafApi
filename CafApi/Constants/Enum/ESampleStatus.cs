using System.ComponentModel.DataAnnotations;
namespace CafApi.Constants.Enum
{
    public enum ESampleStatus
    {
        Ingresada = 1,
        Entregada = 2
    }
    public static class ESampleStateExtension
    {
        private static readonly Dictionary<ESampleStatus, string> _enumToString = new()
        {
            { ESampleStatus.Ingresada, "Ingresada" },
            { ESampleStatus.Entregada, "Entregada" }
        };
    }
}