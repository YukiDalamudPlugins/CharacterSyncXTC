using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.RichPresence.Config;

namespace Dalamud.CharacterSyncTC
{
    /// <summary>
    /// Dalamud and plugin services.
    /// </summary>
    internal class Service
    {
        /// <summary>
        /// Gets or sets the plugin configuration.
        /// </summary>
        internal static CharacterSyncConfig Configuration { get; set; } = null!;

        /// <summary>
        /// Gets the Dalamud plugin interface.
        /// </summary>
        [PluginService]
        internal static IDalamudPluginInterface Interface { get; private set; } = null!;

        /// <summary>
        /// Gets the Dalamud client state.
        /// </summary>
        [PluginService]
        internal static IClientState ClientState { get; private set; } = null!;

        /// <summary>
        /// Gets the Dalamud command manager.
        /// </summary>
        [PluginService]
        internal static ICommandManager CommandManager { get; private set; } = null!;

        /// <summary>
        /// Gets the Dalamud game interop provider.
        /// </summary>
        [PluginService]
        internal static IGameInteropProvider Interop { get; private set; } = null!;
    }
}
