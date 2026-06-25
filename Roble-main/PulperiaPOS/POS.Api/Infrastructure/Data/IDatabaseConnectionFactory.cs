using Microsoft.Data.SqlClient;

namespace POS.Api.Infrastructure.Data;

public interface IDatabaseConnectionFactory
{
    SqlConnection CreateConnection();
}
