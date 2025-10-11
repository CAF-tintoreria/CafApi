using CafApi.Models.Sample;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafApi.Models
{
    public class Prenda
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PrendaID { get; set; }

        [MaxLength(20)]
        public string? TipoPrenda { get; set; }

        public int? NroCorte { get; set; }

        public int? CantidadUnidades { get; set; }

        public decimal? PesoKilos { get; set; }

        public int? CantidadBolsas { get; set; }

        [Required]
        public DateTime FechaIngreso { get; set; }

        [MaxLength(20)]
        public string? Remito { get; set; }

        [Required]
        public int EstadoPrendaID { get; set; }

        [Required]
        public int ProcesoID { get; set; }

        [Required]
        public int ClienteID { get; set; }

        public int? MuestraID { get; set; }

        public DateTime? FechaCambio { get; set; }

        [MaxLength(200)]
        public string? Observaciones { get; set; }

        public Client? Cliente { get; set; }

        public SampleDetailDTO? Muestra { get; set; }
    }

    public class PrendaCreateDto
    {
        [Required, StringLength(20)]
        public string TipoPrenda { get; set; } = default!;

        [Required]
        public int NroCorte { get; set; }

        [Required, Range(1, int.MaxValue, ErrorMessage = "CantidadUnidades debe ser mayor a 0.")]
        public int CantidadUnidades { get; set; }

        [Required]
        public decimal PesoKilos { get; set; }

        [Required, Range(0, int.MaxValue)]
        public int CantidadBolsas { get; set; }

        [Required]
        public DateTime FechaIngreso { get; set; }

        [StringLength(20)]
        public string? Remito { get; set; } // opcional

        [Required]
        public int EstadoPrendaID { get; set; }

        [Required]
        public int ProcesoID { get; set; }

        [Required]
        public int ClienteID { get; set; }

        public int? MuestraID { get; set; } // opcional

        [StringLength(200)]
        public string? Observaciones { get; set; } // opcional
    }

    public class PrendaUpdateDto
    {
        [Required, StringLength(20)]
        public string TipoPrenda { get; set; } = default!;

        [Required]
        public int NroCorte { get; set; }

        [Required, Range(1, int.MaxValue, ErrorMessage = "CantidadUnidades debe ser mayor a 0.")]
        public int CantidadUnidades { get; set; }

        [Required]
        public decimal PesoKilos { get; set; }

        [Required, Range(0, int.MaxValue)]
        public int CantidadBolsas { get; set; }

        [Required]
        public DateTime FechaIngreso { get; set; }

        [StringLength(20)]
        public string? Remito { get; set; } // opcional

        [Required]
        public int EstadoPrendaID { get; set; }

        [Required]
        public int ProcesoID { get; set; }

        [Required]
        public int ClienteID { get; set; }

        public int? MuestraID { get; set; } // opcional

        [StringLength(200)]
        public string? Observaciones { get; set; } // opcional
    }

    public class PrendaEstadoUpdateDto
    {
        [Required]
        public int EstadoPrendaID { get; set; }
    }
}
