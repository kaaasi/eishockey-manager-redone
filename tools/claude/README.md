# Arbeitsumgebung für Claude

Claude arbeitet in einer abgeschotteten Linux-Umgebung, in der die Downloadserver von Microsoft und
nuget.org gesperrt sind, GitHub aber erreichbar ist. Damit Claude trotzdem alles selbst bauen und testen kann:

1. Der Workflow **„Werkzeuge für Claude“** (`.github/workflows/werkzeuge.yml`) legt das offizielle .NET-SDK
   und alle NuGet-Pakete der Solution im Release `claude-werkzeuge` ab. Er läuft automatisch, sobald sich
   Projektdateien ändern (neue Pakete), und kann manuell gestartet werden.
2. `tools/claude/setup-workspace.sh [originaldateien]` lädt beides, richtet eine Offline-Paketquelle ein und
   verlinkt die Originaldateien nach `gamedata/`.
3. Danach: `source ~/claude-tools/env.sh`, dann `dotnet build`, `dotnet test`, `emtool screens` usw.

Die Originaldateien des Spiels liegen **nie** auf GitHub; Claude nutzt dafür die im Chat hochgeladene ZIP-Datei.
Das Release kann jederzeit gelöscht werden – es wird beim nächsten Workflow-Lauf neu erstellt.
