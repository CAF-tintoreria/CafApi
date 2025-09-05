using Microsoft.Data.SqlClient;

public class Db
{
    private readonly string _connString;

    public Db(IConfiguration cfg)
    {
        // Preferí ConnectionStrings:CafDb; cae a "CafDb" plano si querés
        _connString = cfg.GetConnectionString("CafDb")
                      ?? cfg["CafDb"]
                      ?? throw new InvalidOperationException("Falta ConnectionStrings:CafDb");
    }

    public SqlConnection GetConnection() => new SqlConnection(_connString);
}
