namespace Oxide.Plugins
{
    [Info("NoSleep", "Wulf/lukespragg", "0.0.1")]
    [Description("Disables sleeping screen.")]

    class NoSleep : RustPlugin
    {
        bool OnPlayerSleep(BasePlayer player) => false;
    }
}
