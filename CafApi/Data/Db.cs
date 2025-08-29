using Microsoft.Data.SqlClient;     // <— cambia este using
using Microsoft.Extensions.Configuration;

namespace CafApi.Data;

public class Db
{
    private readonly IConfiguration _config;
    public Db(IConfiguration config) => _config = config;

    public SqlConnection GetConnection()
        => new SqlConnection(_config.GetConnectionString("CafDb"));
}
