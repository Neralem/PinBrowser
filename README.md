# PinBrowser

A minimal Windows browser for **one** pinned website. Built for the case where you always drag a
specific page (e.g. a dashboard) into its own window at a fixed spot on a specific monitor —
separate from your regular browser with all its other tabs.

- Single `.exe`, no extra DLLs of its own next to it. Just drop it anywhere and run it — no
  installer.
- Framework-dependent: requires the matching **.NET Desktop Runtime** (currently .NET 10) to
  already be installed on the machine — kept this way deliberately to keep the exe small
  (~1-2 MB) instead of bundling the whole .NET runtime into it.
- Rendering via **WebView2** (the Chromium engine that's usually already preinstalled on current
  Windows 10/11).
- Settings live in `settings.json` right next to the `.exe` and get created with default values on
  first run if missing.
- Window size and position are saved automatically on close and restored on the next start.
- Optionally registers itself for Windows autostart (`HKCU\...\Run`) — multiple copies in
  different folders each get their own entry instead of overwriting each other's.
- The title bar automatically follows the light/dark Windows theme (live, even if you switch it
  while running).
- Optional custom window icon and optional fixed window title; without a fixed title, the window
  title automatically follows the loaded page's title.

## settings.json

Lives in the same folder as the `.exe` and looks like this:

```json
{
  "Url": "https://www.google.com/",
  "WindowX": 100,
  "WindowY": 100,
  "WindowWidth": 1000,
  "WindowHeight": 700,
  "Maximized": false,
  "AutoStart": true,
  "ShowInTaskbar": true,
  "IconPath": "",
  "Title": ""
}
```

| Field          | Meaning                                                                   |
|----------------|----------------------------------------------------------------------------|
| `Url`          | Loaded on startup. Edit it manually and restart the program to change it. |
| `WindowX/Y`    | Window position (in screen coordinates, works across multiple monitors).  |
| `WindowWidth/Height` | Window size.                                                         |
| `Maximized`    | Whether the window was maximized.                                         |
| `AutoStart`    | `true` = registers itself in Windows autostart, `false` = removes the entry again. |
| `ShowInTaskbar` | `true` (default) = normal taskbar entry, `false` = window doesn't show up in the taskbar. |
| `IconPath`     | Optional path to an icon (`.ico`, but `.png`/`.bmp`/... work too) for the window icon. Relative paths are resolved against the exe. Empty or file not found → falls back to the loaded page's favicon (updates live if the page's favicon changes). |
| `Title`        | Optional fixed window title. Empty → the window title automatically follows the loaded page's `<title>`. |
| `InstanceId`   | Generated automatically if missing. Uniquely identifies this installation in autostart — don't edit it by hand. |

`Url` is **not** automatically overwritten when you navigate within the page — only window
size/position and the autostart status are synced on every start/close.

If the saved position wouldn't be visible on any connected monitor at startup (e.g. because a
monitor got unplugged), the window is automatically centered on the primary screen.

## Building

Requires .NET SDK 10 (or newer).

```
dotnet build
```

## Publishing (single exe)

```
dotnet publish src/PinBrowser/PinBrowser.csproj -c Release -o publish
```

Result: `publish/PinBrowser.exe` — a single file, no other DLLs alongside it (~1-2 MB). This file
can be copied to any folder and run from there; `settings.json` gets created next to it.

This is a **framework-dependent** build: the target machine needs the **.NET 10 Desktop Runtime**
(x64) installed already — get it from Microsoft's official .NET downloads page if it's not there
yet. If you'd rather have zero prerequisites at the cost of a much bigger exe (the whole runtime
gets embedded instead), publish self-contained instead:

```
dotnet publish src/PinBrowser/PinBrowser.csproj -c Release -r win-x64 --self-contained true -p:EnableCompressionInSingleFile=true -o publish
```

> Note: the build prints an `MSB3277` warning about conflicting `WindowsBase` versions. This is a
> known, harmless side effect of the WebView2 NuGet package (it ships both a WinForms and a WPF
> variant) and has no effect on functionality.

## Setting up autostart

`AutoStart` defaults to `true`. On first run, the exe registers itself with its current path under
`HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`, using a registry value name that
includes the `InstanceId` from `settings.json` (`PinBrowser_<id>`). That way, multiple copies of
PinBrowser — e.g. one per pinned page, each in its own folder — can all be in autostart at the same
time without overwriting each other. If you move an exe to a different location later, its registry
entry is automatically updated to the new path on the next start. Setting `AutoStart` to `false`
removes that installation's entry on the next start.

## Light/dark theme

The native title bar automatically follows the light/dark theme set in Windows
(`DWMWA_USE_IMMERSIVE_DARK_MODE`) and also reacts live if the system theme is switched while the app
is running — no restart needed.

## Project structure

```
src/PinBrowser/
  PinBrowser.csproj   Project and publish settings
  Program.cs          Entry point
  MainForm.cs          Window with the WebView2 control, position/size handling, theme hooks
  Settings.cs          Loading/saving settings.json, InstanceId assignment
  AutoStart.cs          Registry entry for Windows autostart (per instance)
  ThemeHelper.cs        Detects the Windows theme and applies it to the title bar
```

## Creating a release

The version lives in `<Version>` in [PinBrowser.csproj](src/PinBrowser/PinBrowser.csproj) (currently
`0.0.2`). To produce a GitHub Release with a ready-to-use `.exe`:

1. Update the version in the `.csproj` if needed and commit.
2. Push a tag in the format `vX.Y.Z`, e.g.:
   ```
   git tag v0.0.2
   git push origin v0.0.2
   ```
3. The [.github/workflows/release.yml](.github/workflows/release.yml) workflow then builds the
   framework-dependent single-file exe on `windows-latest` and attaches it
   (`PinBrowser-<version>-win-x64.exe`) to the GitHub Release for that tag.

The workflow can also be triggered manually via "Run workflow" in the GitHub Actions tab, but that
only produces a downloadable build artifact — without a tag, no release is created.
