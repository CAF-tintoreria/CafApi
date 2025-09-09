using Microsoft.Data.SqlClient;

public class Db
{
    private readonly string _connString;

    public Db(IConfiguration config)
    {
        
        _connString = config.GetConnectionString("CafDb")
                      ?? config["CafDb"]
                      ?? throw new InvalidOperationException("Falta ConnectionStrings:CafDb");
    }

    public SqlConnection GetConnection() => new SqlConnection(_connString);
}
