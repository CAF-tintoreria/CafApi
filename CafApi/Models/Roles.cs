namespace CafApi.Models
{
    public class RoleDto
    {
        public int RolID { get; set; }
        public string Nombre { get; set; } = string.Empty;

        public bool EsSistema { get; set; }
        public string? Descripcion { get; set; }
    }
}