namespace CafApi.Models
{
    public class EstadoPrenda
    {
        public int EstadoPrendaID { get; set; }  
        public string EstadoActual { get; set; } = default!;
    }

    public class EstadoPrendaCreateDto
    {
        public string EstadoActual { get; set; } = default!;
    }

    public class PrendaProcesoCreateDto
    {
        public int ProcesoID { get; set; }
    }
}