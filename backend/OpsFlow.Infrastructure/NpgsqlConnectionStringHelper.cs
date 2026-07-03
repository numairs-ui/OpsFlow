using System.Net;
using System.Net.Sockets;
using Npgsql;

namespace OpsFlow.Infrastructure;

/// <summary>
/// Azure Container Apps (Consumption plan) has no outbound IPv6 route, but Supabase's Postgres
/// hosts (both direct and pooler) can resolve to an IPv6 address depending on region — Npgsql then
/// fails with "Network is unreachable". Resolving the host to its IPv4 address ourselves and
/// substituting it into the connection string sidesteps this; TrustServerCertificate=true already
/// disables hostname-based cert validation, so connecting via the bare IP is safe here.
/// </summary>
public static class NpgsqlConnectionStringHelper
{
    public static string PreferIPv4(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrEmpty(builder.Host) || IPAddress.TryParse(builder.Host, out _))
            return connectionString;

        var ipv4 = Dns.GetHostAddresses(builder.Host)
            .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

        if (ipv4 is not null)
            builder.Host = ipv4.ToString();

        return builder.ConnectionString;
    }
}
