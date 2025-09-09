// Models/Cliente.cs
using System.ComponentModel.DataAnnotations;

namespace CafApi.Models;

public class Client
{
    public int ClientID { get; set; }
    public string Username { get; set; } = "";
    public string? Contact { get; set; }
    public bool State { get; set; }
}

public class ClientCreateDto
{
    [Required]
    [MaxLength(24, ErrorMessage = "El nombre no puede superar los 24 caracteres.")]
    public string Username { get; set; } = "";
    public string? Contact { get; set; }
    public bool State { get; set; }
}

public class ClienteUpdateDto
{
    [Required]
    [MaxLength(24, ErrorMessage = "El nombre no puede superar los 24 caracteres.")]
    public string Username { get; set; } = "";
    public string? Contact { get; set; }
    public bool State { get; set; }
}
