using System.ComponentModel.DataAnnotations;

namespace KnockKnockServer;

// Stærkt typet udgave af "Server"-sektionen i appsettings.json
class ServerOptions
{
    public const string SectionName = "Server";

    [Range(1, 65535, ErrorMessage = "Server:Port skal være et tal mellem 1 og 65535.")]
    public int Port { get; set; }

    [Range(1, 1000, ErrorMessage = "Server:MaxClients skal være et tal mellem 1 og 1000.")]
    public int MaxClients { get; set; }
}
