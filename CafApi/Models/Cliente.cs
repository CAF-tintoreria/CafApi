// Models/Cliente.cs
using System.ComponentModel.DataAnnotations;

namespace CafApi.Models;

public class Cliente
{
    public int ClienteID { get; set; }   // <- INT como antes
    public string Nombre { get; set; } = "";
    public string? Contacto { get; set; }
    public bool Estado { get; set; }
}

public class ClienteCreateDto
{
    [Required]
    [MaxLength(24, ErrorMessage = "El nombre no puede superar los 24 caracteres.")]
    public string Nombre { get; set; } = "";
    public string? Contacto { get; set; }
    public bool Estado { get; set; }
}

public class ClienteUpdateDto
{
    [Required]
    [MaxLength(24, ErrorMessage = "El nombre no puede superar los 24 caracteres.")]
    public string Nombre { get; set; } = "";
    public string? Contacto { get; set; }
    public bool Estado { get; set; }
}
