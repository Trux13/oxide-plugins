using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("DeathMessages", "Wulf/lukespragg", "1.0.0")]
    [Description("Customizes the death messages.")]

    class DeathMessages : HurtworldPlugin
    {
        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>            {                {"EntityStats/Sources/Fall Damage", "{Name} fell to death"},
                {"EntityStats/Sources/Damage Over Time", "{Name} just died"},
                {"EntityStats/Sources/Radiation Poisoning", "{Name} just died"},
                {"EntityStats/Sources/Starvation", "{Name} just died"},
                {"EntityStats/Sources/Hypothermia", "{Name} just died"},
                {"EntityStats/Sources/Asphyxiation", "{Name} just died"},
                {"EntityStats/Sources/Poison", "{Name} just died"},
                {"EntityStats/Sources/Burning", "{Name} just died"},
                {"EntityStats/Sources/Suicide", "{Name} committed suicide"},
                {"Unknown", "{Name} just died on a mystic way"}            };            lang.RegisterMessages(messages, this);
        }

        void Loaded() => LoadDefaultMessages();

        object OnDeathNotice(string name, EntityEffectSourceData source)
        {
            hurt.BroadcastChat((lang.GetMessage(source.SourceDescriptionKey, this) ?? lang.GetMessage("Unknown", this)).Replace("{Name}", name));

            return true;
        }
    }
}
