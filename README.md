# Dalamud.CharacterSyncTC

CharacterSyncX 的 **台服 / 繁中服 (TC)** 移植版，目標為 **Dalamud SDK 12**（國際服 / TC 線）。

以 `syncx`（CharacterSyncX, 國服 CN SDK 15）的功能集為基礎，移植至 Dalamud SDK 12 API，並全面繁體中文化。

## 功能（沿用 CharacterSyncX 積極模式）

- 無安全模式 / 無警告視窗，載入後即依設定改寫存檔。
- 設定介面分「目前角色」與「主角色」兩區，可一鍵設為主角色。
- 額外提供「設為主角色並重啟」按鈕（`RaiseException` + `Process.Kill` 快速重啟）。
- 同步 `HOTBAR.DAT` 時一併同步 `ACQ.DAT`（十字熱鍵）。
- 介面、指令說明、log 皆為繁體中文。

## 與 CharacterSyncX 的技術差異（因應 SDK 12）

| 項目 | CharacterSyncX (CN SDK 15) | 本版 (TC SDK 12) |
|------|---------------------------|------------------|
| SDK | `Dalamud.CN.NET.Sdk/15.0.0`（net10） | `Dalamud.NET.Sdk/12.0.0`（net8/9） |
| ImGui 綁定 | `Dalamud.Bindings.ImGui` | **`ImGuiNET`**（SDK 12 仍用 ImGui.NET） |
| 取得角色 | `IPlayerState.ContentId` / `IObjectTable.LocalPlayer` | **`IClientState.LocalPlayer` / `IClientState.LocalContentId`** |
| Hook 方式 | `HookFromSignature` + 非託管 `FileInterface*` / `MemoryHelper.WriteString` | `HookFromSignature` + 託管 `[MarshalAs(LPWStr)] string` detour（與 goat 原版同，最穩定） |
| 攔截簽章 | `E8 ?? ?? ?? ?? 3C 01 0F 85 1C 04 00 00` | 相同 |

## 建置

```
dotnet build Dalamud.CharacterSyncTC/Dalamud.CharacterSyncTC.csproj -c Release
```

可安裝包輸出於 `Dalamud.CharacterSyncTC/bin/Release/Dalamud.CharacterSyncTC/latest.zip`。

### Dalamud 組件來源

台服經 **FFXIVSimpleLauncher** 啟動，Dalamud 12 開發組件位於：

```
%AppData%\FFXIVSimpleLauncher\Dalamud\Injector\
```

`Dalamud.NET.Sdk` 在 Windows 預設指向 `%AppData%\XIVLauncher\addon\Hooks\dev\`，且為**無條件設定**，無法用一般的 `<DalamudLibPath>` 覆寫。SDK 唯一的覆寫掛勾是 `DALAMUD_HOME` 屬性（見 `Sdk.props` 最後一行），因此本專案於 **`Directory.Build.props`** 設定：

```xml
<DALAMUD_HOME>$(AppData)\FFXIVSimpleLauncher\Dalamud\Injector</DALAMUD_HOME>
```

> 若你的 TC Dalamud 安裝在別處，改這一行即可（勿加尾斜線，SDK 會自動補）。

### 已對實機驗證

- 台服 Dalamud：**12.0.2.0**，TFM **net9.0**（故 csproj 指定 `net9.0-windows7.0`）。
- 該 Injector 內為 `ImGui.NET` / `ImGuiScene` / `Lumina`，無 `Dalamud.Bindings.ImGui` → 確認用 `ImGuiNET`。
- `localPlayer.HomeWorld.Value.Name` 在 SDK 12 可正常編譯，**不需**改回舊式 `.GameData.Name`。
- `dotnet build -c Release` 對上述實機組件 **Build succeeded, 0 Error**。
