// Models/PrecioCliente.cs
namespace CafApi.Models;

public class PrecioProcesoItem
{
    public int ProcesoID { get; set; }
    public string TipoProceso { get; set; } = "";
}

public class PrecioCliente
{
    public int PrecioClienteID { get; set; }
    public int ClienteID { get; set; }
    public string TipoPrenda { get; set; } = "";
    public string UnidadCobro { get; set; } = "Kg";   // "Kg" o "Unidad"
    public decimal Precio { get; set; }
    public string? Notas { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public List<PrecioProcesoItem> Procesos { get; set; } = new();
}

// Para crear un precio (el cliente viene en la URL)
public class PrecioClienteCreateDto
{
    public string TipoPrenda { get; set; } = "";
    public string UnidadCobro { get; set; } = "Kg";
    public decimal Precio { get; set; }
    public string? Notas { get; set; }
    public List<int> ProcesoIds { get; set; } = new();
}

// Para editar un precio (mismos campos que al crear)
public class PrecioClienteUpdateDto
{
    public string TipoPrenda { get; set; } = "";
    public string UnidadCobro { get; set; } = "Kg";
    public decimal Precio { get; set; }
    public string? Notas { get; set; }
    public List<int> ProcesoIds { get; set; } = new();
}
