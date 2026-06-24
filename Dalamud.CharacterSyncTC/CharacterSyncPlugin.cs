using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Dalamud.CharacterSyncTC.Interface;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.RichPresence.Config;

namespace Dalamud.CharacterSyncTC
{
    /// <summary>
    /// Main plugin class.
    /// </summary>
    internal class CharacterSyncPlugin : IDalamudPlugin
    {
        // the "0F 85 1C 04 00 00" part of the signature is a jnz, likely to change if logic of function or compiler used changes
        private const string FileInterfaceOpenFileSignature = "E8 ?? ?? ?? ?? 3C 01 0F 85 1C 04 00 00";

        public static IPluginLog PluginLog = null!;

        private readonly WindowSystem windowSystem;
        private readonly ConfigWindow configWindow;

        private readonly Hook<FileInterfaceOpenFileDelegate> openFileHook;

        private readonly Regex saveFolderRegex = new(
            @"(?<path>.*)FFXIV_CHR(?<cid>.*)\/(?!ITEMODR\.DAT|ITEMFDR\.DAT|GEARSET\.DAT|UISAVE\.DAT|.*\.log)(?<dat>.*)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterSyncPlugin"/> class.
        /// </summary>
        public CharacterSyncPlugin(IDalamudPluginInterface interf, IPluginLog pluginLog)
        {
            PluginLog = pluginLog;

            interf.Create<Service>();

            Service.Configuration = Service.Interface.GetPluginConfig() as CharacterSyncConfig ?? new CharacterSyncConfig();

            this.configWindow = new();
            this.windowSystem = new("CharacterSyncTC");
            this.windowSystem.AddWindow(this.configWindow);

            Service.Interface.UiBuilder.Draw += this.windowSystem.Draw;
            Service.Interface.UiBuilder.OpenConfigUi += this.OnOpenConfigUi;

            Service.CommandManager.AddHandler("/pcharsync", new CommandInfo(this.OnChatCommand)
            {
                HelpMessage = "開啟設定介面",
                ShowInHelp = true,
            });

            try
            {
                this.DoBackup();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "無法備份角色資料");
            }

            this.openFileHook = Service.Interop.HookFromSignature<FileInterfaceOpenFileDelegate>(FileInterfaceOpenFileSignature, this.OpenFileDetour);
            this.openFileHook.Enable();
        }

        private delegate IntPtr FileInterfaceOpenFileDelegate(
            IntPtr pFileInterface,
            [MarshalAs(UnmanagedType.LPWStr)] string filepath, // IntPtr pFilepath
            uint a3);

        /// <inheritdoc/>
        public string Name => "Character Sync TC";

        /// <inheritdoc/>
        public void Dispose()
        {
            Service.CommandManager.RemoveHandler("/pcharsync");
            Service.Interface.UiBuilder.Draw -= this.windowSystem.Draw;
            this.openFileHook?.Dispose();
        }

        private void OnOpenConfigUi()
        {
            this.configWindow.Toggle();
        }

        private void OnChatCommand(string command, string arguments)
        {
            this.configWindow.Toggle();
        }

        private void DoBackup()
        {
            var configFolder = Service.Interface.GetPluginConfigDirectory();
            Directory.CreateDirectory(configFolder);

            var backupFolder = new DirectoryInfo(Path.Combine(configFolder, "backups"));
            Directory.CreateDirectory(backupFolder.FullName);

            var folders = backupFolder.GetDirectories().OrderBy(x => long.Parse(x.Name)).ToArray();
            if (folders.Length > 2)
            {
                folders.FirstOrDefault()?.Delete(true);
            }

            var thisBackupFolder = Path.Combine(backupFolder.FullName, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            Directory.CreateDirectory(thisBackupFolder);

            var xivFolder = new DirectoryInfo(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "My Games",
                "FINAL FANTASY XIV - A Realm Reborn"));

            if (!xivFolder.Exists)
            {
                PluginLog.Error("未找到遊戲資料資料夾");
                return;
            }

            foreach (var directory in xivFolder.GetDirectories("FFXIV_CHR*"))
            {
                var thisBackupFile = Path.Combine(thisBackupFolder, directory.Name);
                PluginLog.Information(thisBackupFile);
                Directory.CreateDirectory(thisBackupFile);

                foreach (var filePath in directory.GetFiles("*.DAT"))
                {
                    File.Copy(filePath.FullName, filePath.FullName.Replace(directory.FullName, thisBackupFile), true);
                }
            }

            PluginLog.Information("已完成備份");
        }

        private IntPtr OpenFileDetour(IntPtr pFileInterface, [MarshalAs(UnmanagedType.LPWStr)] string filepath, uint a3)
        {
            try
            {
                if (Service.Configuration.Cid != 0)
                {
                    var match = this.saveFolderRegex.Match(filepath);
                    if (match.Success)
                    {
                        var rootPath = match.Groups["path"].Value;
                        var datName = match.Groups["dat"].Value;

                        if (this.PerformRewrite(datName))
                        {
                            filepath = $"{rootPath}FFXIV_CHR{Service.Configuration.Cid:X16}/{datName}";
                            PluginLog.Debug("REWRITE: " + filepath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "嘗試攔截遊戲檔案寫入時發生錯誤");
            }

            return this.openFileHook.Original(pFileInterface, filepath, a3);
        }

        private bool PerformRewrite(string datName)
        {
            switch (datName)
            {
                case "HOTBAR.DAT" or "ACQ.DAT" when Service.Configuration.SyncHotbars:
                case "MACRO.DAT" when Service.Configuration.SyncMacro:
                case "KEYBIND.DAT" when Service.Configuration.SyncKeybind:
                case "LOGFLTR.DAT" when Service.Configuration.SyncLogfilter:
                case "COMMON.DAT" when Service.Configuration.SyncCharSettings:
                case "CONTROL0.DAT" when Service.Configuration.SyncKeyboardSettings:
                case "CONTROL1.DAT" when Service.Configuration.SyncGamepadSettings:
                case "GS.DAT" when Service.Configuration.SyncCardSets:
                case "ADDON.DAT":
                    return true;
            }

            return false;
        }
    }
}
