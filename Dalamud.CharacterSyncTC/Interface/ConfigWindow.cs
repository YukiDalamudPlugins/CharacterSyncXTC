using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using ImGuiNET;

namespace Dalamud.CharacterSyncTC.Interface
{
    /// <summary>
    /// Main configuration window.
    /// </summary>
    internal class ConfigWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigWindow"/> class.
        /// </summary>
        public ConfigWindow()
            : base("Character Sync TC 設定介面", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar)
        {
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            if (Service.ClientState.LocalPlayer is not { } localPlayer)
            {
                ImGui.Text("請先登入");
                return;
            }

            using (ImRaii.Group())
            {
                ImGui.TextColored(ImGuiColors.TankBlue, "目前角色:");

                ImGui.SameLine();
                ImGui.Text($"{localPlayer.Name}@{localPlayer.HomeWorld.Value.Name} (FFXIV_CHR{Service.ClientState.LocalContentId:X16})");

                using (ImRaii.PushIndent())
                {
                    if (ImGui.Button("設為主角色"))
                    {
                        Service.Configuration.Cid = Service.ClientState.LocalContentId;
                        Service.Configuration.SetName = $"{localPlayer.Name}@{localPlayer.HomeWorld.Value.Name}";
                        Service.Configuration.Save();
                    }

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("請在設定後盡快重啟一次遊戲以讓設定生效\n你可以使用 /xlrestart 指令或點擊右側按鈕快速重啟");

                    ImGui.SameLine();

                    if (ImGui.Button("設為主角色並重啟"))
                    {
                        Service.Configuration.Cid = Service.ClientState.LocalContentId;
                        Service.Configuration.SetName = $"{localPlayer.Name}@{localPlayer.HomeWorld.Value.Name}";
                        Service.Configuration.Save();

                        [DllImport("kernel32.dll")]
                        [return: MarshalAs(UnmanagedType.Bool)]
                        static extern void RaiseException(uint dwExceptionCode, uint dwExceptionFlags, uint nNumberOfArguments, IntPtr lpArguments);

                        RaiseException(0x12345678, 0, 0, IntPtr.Zero);
                        Process.GetCurrentProcess().Kill();
                    }
                }
            }

            ImGui.Spacing();

            using (ImRaii.Group())
            {
                ImGui.TextColored(ImGuiColors.TankBlue, "主角色:");

                var isMainSet = Service.Configuration.Cid != 0 &&
                                !string.IsNullOrWhiteSpace(Service.Configuration.SetName);
                ImGui.SameLine();

                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed, Service.Configuration.Cid == 0))
                {
                    ImGui.Text(
                        !isMainSet
                            ? "尚未設定, 請登入至想要設為主角色的遊戲角色後點擊上方按鈕"
                            : $"{Service.Configuration.SetName} (FFXIV_CHR{Service.Configuration.Cid:X16})");
                }
            }

            ImGui.NewLine();

            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.ParsedBlue, "設定");

            ImGui.SameLine();
            if (ImGui.SmallButton("儲存"))
                Service.Configuration.Save();

            ImGui.Separator();

            ImGui.Checkbox("同步熱鍵列", ref Service.Configuration.SyncHotbars);
            ImGui.Checkbox("同步巨集", ref Service.Configuration.SyncMacro);
            ImGui.Checkbox("同步按鍵設定", ref Service.Configuration.SyncKeybind);
            ImGui.Checkbox("同步聊天設定", ref Service.Configuration.SyncLogfilter);
            ImGui.Checkbox("同步角色設定", ref Service.Configuration.SyncCharSettings);
            ImGui.Checkbox("同步鍵盤設定", ref Service.Configuration.SyncKeyboardSettings);
            ImGui.Checkbox("同步手把設定", ref Service.Configuration.SyncGamepadSettings);
            ImGui.Checkbox("同步九宮幻卡與萌寵之王設定", ref Service.Configuration.SyncCardSets);
        }
    }
}
