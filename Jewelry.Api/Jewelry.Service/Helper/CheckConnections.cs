using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Helper
{
    public static class CheckConnections
    {
        public static async Task<(bool, string)> CheckSQLDatabase(string connectionString, ILogger log)
        {
            var cs = new SqlConnectionStringBuilder(connectionString);
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    return (true, cs.DataSource);
                }
                catch (InvalidOperationException ex)
                {
                    log.LogError(ex, ex.Message);
                    return (false, $"{ex.Message}");
                }
                catch (SqlException ex)
                {
                    log.LogError(ex, ex.Message);
                    return (false, $"{ex.Message}");
                }
                catch (Exception ex)
                {
                    log.LogError(ex, ex.Message);
                    return (false, $"{ex.Message}");
                }
            }
        }
        public static async Task<(bool, string)> CheckNpgsqlDatabase(string connectionString, ILogger log)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    return (true, connection.Host);
                }
                catch (SqlException ex)
                {
                    log.LogError(ex, ex.Message);
                    return (false, $"{ex.Message}");
                }
                catch (Exception ex)
                {
                    log.LogError(ex, ex.Message);
                    return (false, $"{ex.Message}");
                }
            }
        }

        public static async Task<bool> CheckServer(string hostUri, int portNumber, ILogger log)
        {
            try
            {
                using (var client = new TcpClient(hostUri, portNumber))
                    return true;
            }
            catch (SocketException ex)
            {
                log.LogError(ex, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                return false;
            }
        }
    }
}
