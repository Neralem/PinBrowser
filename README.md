# PinBrowser

Ein minimaler Windows-Browser für **eine** fest angepinnte Webseite. Gedacht für den Fall, dass man
sich eine bestimmte Seite (z. B. ein Dashboard) immer in einem eigenen Fenster an eine feste Stelle
auf einem bestimmten Monitor zieht – getrennt vom normalen Browser mit den restlichen Tabs.

- Einzelne, selbstständige `.exe` (kein separates .NET muss installiert sein – self-contained
  Single-File-Build).
- Rendering über **WebView2** (Chromium-Engine, die auf aktuellem Windows 10/11 i. d. R. schon
  mitgeliefert ist).
- Einstellungen liegen in `settings.json` direkt neben der `.exe` und werden beim ersten Start mit
  Standardwerten angelegt, falls sie fehlen.
- Fenstergröße und -position werden beim Schließen automatisch gespeichert und beim nächsten Start
  wiederhergestellt.
- Registriert sich optional selbst für den Windows-Autostart (`HKCU\...\Run`) – mehrere Kopien in
  unterschiedlichen Ordnern bekommen dabei jeweils einen eigenen Eintrag statt sich gegenseitig zu
  überschreiben.
- Titelleiste folgt automatisch dem hellen/dunklen Windows-Design (auch live bei Themenwechsel).

## settings.json

Liegt im selben Ordner wie die `.exe` und sieht z. B. so aus:

```json
{
  "Url": "https://www.google.com/",
  "WindowX": 100,
  "WindowY": 100,
  "WindowWidth": 1000,
  "WindowHeight": 700,
  "Maximized": false,
  "AutoStart": true
}
```

| Feld           | Bedeutung                                                                 |
|----------------|----------------------------------------------------------------------------|
| `Url`          | Wird beim Start aufgerufen. Manuell anpassen und Programm neu starten.     |
| `WindowX/Y`    | Fensterposition (in Bildschirmkoordinaten, auch über mehrere Monitore).    |
| `WindowWidth/Height` | Fenstergröße.                                                        |
| `Maximized`    | Ob das Fenster maximiert war.                                              |
| `AutoStart`    | `true` = trägt sich automatisch in den Windows-Autostart ein, `false` = entfernt den Eintrag wieder. |
| `InstanceId`   | Wird automatisch generiert, sobald es fehlt. Identifiziert diese Installation eindeutig im Autostart – nicht manuell ändern. |

`Url` wird **nicht** automatisch überschrieben, wenn man innerhalb der Seite navigiert – nur Fenster-
größe/-position und der Autostart-Status werden bei jedem Start/Beenden synchronisiert.

Falls die gespeicherte Position beim Start auf keinem angeschlossenen Monitor mehr sichtbar wäre
(z. B. weil ein Monitor abgesteckt wurde), wird das Fenster automatisch auf dem Hauptbildschirm
zentriert.

## Bauen

Voraussetzung: .NET SDK 8 (oder neuer).

```
dotnet build
```

## Veröffentlichen (einzelne exe)

```
dotnet publish src/PinBrowser/PinBrowser.csproj -c Release -o publish
```

Ergebnis: `publish/PinBrowser.exe` – eine einzelne, self-contained Datei (~70 MB, da die .NET-Runtime
eingebettet ist). Diese Datei kann in einen beliebigen Ordner kopiert und von dort gestartet werden;
`settings.json` wird daneben angelegt.

> Hinweis: Beim Build erscheint eine Warnung `MSB3277` wegen widersprüchlicher `WindowsBase`-Versionen.
> Das ist ein bekanntes, harmloses Nebenprodukt des WebView2-NuGet-Pakets (liefert sowohl eine
> WinForms- als auch eine WPF-Variante mit) und hat keinen Einfluss auf die Funktion.

## Autostart einrichten

`AutoStart` ist standardmäßig `true`. Beim ersten Start trägt sich die exe selbst mit ihrem aktuellen
Pfad in `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run` ein, unter einem Registry-
Wertnamen, der die `InstanceId` aus der `settings.json` enthält (`PinBrowser_<id>`). Dadurch können
mehrere Kopien von PinBrowser – etwa je eine pro angepinnter Seite, in eigenen Ordnern – gleichzeitig
im Autostart stehen, ohne sich gegenseitig zu überschreiben. Verschiebt man eine exe später an einen
anderen Ort, wird ihr Registry-Eintrag beim nächsten Start automatisch auf den neuen Pfad aktualisiert.
Setzt man `AutoStart` auf `false`, wird der Eintrag dieser Installation beim nächsten Start entfernt.

## Dunkles/helles Design

Die native Titelleiste folgt automatisch dem in Windows eingestellten hellen/dunklen Design
(`DWMWA_USE_IMMERSIVE_DARK_MODE`) und reagiert auch live, wenn das System-Design während der Laufzeit
umgeschaltet wird – ein Neustart ist nicht nötig.

## Projektstruktur

```
src/PinBrowser/
  PinBrowser.csproj   Projekt- und Publish-Einstellungen
  Program.cs          Einstiegspunkt
  MainForm.cs          Fenster mit WebView2-Steuerelement, Positions-/Größen-Handling, Theme-Hooks
  Settings.cs          Laden/Speichern von settings.json, InstanceId-Vergabe
  AutoStart.cs          Registry-Eintrag für Windows-Autostart (pro Instanz)
  ThemeHelper.cs        Windows-Design erkennen und auf die Titelleiste anwenden
```
