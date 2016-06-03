// For development use only!

namespace Oxide.Plugins
{
    [Info("Protocol", "Wulf/lukespragg", 0.1)]
    [Description("Allows any client, regardless of protocol, to connect.")]

    class Protocol : RustPlugin
    {
        void OnClientAuth(Network.Connection connection)
        {
            Puts($"{connection.userid} joined with protocol {connection.protocol}");
            connection.protocol = Rust.Protocol.network; // Not working?
        }
    }
}
