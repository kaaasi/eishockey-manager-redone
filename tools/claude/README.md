# Arbeitsumgebung für Claude

Claude arbeitet in einer abgeschotteten Linux-Umgebung, in der die Downloadserver von Microsoft und
nuget.org gesperrt sind, GitHub aber erreichbar ist. Damit Claude trotzdem alles selbst bauen und testen kann:

1. Der Workflow **„Werkzeuge für Claude“** (`.github/workflows/werkzeuge.yml`) legt das offizielle .NET-SDK
   und alle NuGet-Pakete der Solution im Release `claude-werkzeuge` ab. Er läuft automatisch, sobald sich
   Projektdateien ändern (neue Pakete), und kann manuell gestartet werden.
2. `tools/claude/setup-workspace.sh [originaldateien]` lädt beides, richtet eine Offline-Paketquelle ein und
   verlinkt die Originaldateien nach `gamedata/`.
3. Danach: `source ~/claude-tools/env.sh`, dann `dotnet build`, `dotnet test`, `emtool screens` usw.

Die Originaldateien liegen im **privaten** Repo `kaaasi/eishockey-manager-referenz`: ZIP unter
`original/Eishockey-Manager_DOS_DE.zip`, entpackte Daten unter `original/gamedata/`.
[Ablagestand und Abruf](../../docs/originaldateien.md) sind werkzeugneutral dokumentiert. Laden mit
`tools/claude/fetch-reference.sh original` (nur dieser Ordner), dann `setup-workspace.sh ~/referenz/original/gamedata`;
`setup-workspace.sh` lädt die Originaldaten nicht selbst.
In dieses Code-Repository und in dessen Werkzeug-Release gehören keine Originaldateien.
Das Release kann jederzeit gelöscht werden – es wird beim nächsten Workflow-Lauf neu erstellt.
