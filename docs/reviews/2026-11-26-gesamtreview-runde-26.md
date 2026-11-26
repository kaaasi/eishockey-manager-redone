## 1. Merge-Empfehlung

**Nein für `33669d3c155d9a05747ae0f79a5deee2e1907438`** (Base `c59f7d5`, R26-D4 = B). Bei zwei Managern entscheidet die Position der ersten zugesehenen Paarung, ob eine frühere Managerpaarung Strafgrundwürfe verbraucht. Das verändert nachweislich Spielergebnisse. Laden und Speichern sowie die geprüften Geldbuchungen geben keinen weiteren Anlass, die Runde zurückzuhalten.

## 2. Befunde

**R26-G1 · P2 · Produktfehler im gemeinsamen Zufallsstrom.** Manager A schaltet seinen Verein von Live auf Aus, Manager B schaut weiter zu. Bei Tempo 60 und Darstellung 0 bekommt die Partie von A eine Zweiminutenstrafe. Steht sie vor der ersten Anzeigereihe im Spielplan, verwendet sie für alle Minuten den Anzeigestatus aus ihrer eigenen Vorbereitung. Das Original prüft beim Strafgrund den Status des gesamten Spieltags. Betroffen sind auch Läufe ohne UI-Treiber. [Frühes Speichern des Merkers](https://github.com/kaaasi/eishockey-manager-redone/blob/33669d3c155d9a05747ae0f79a5deee2e1907438/src/EishockeyManager.Core/MatchEngine.Live.cs#L219) · [Verwendung je Minute](https://github.com/kaaasi/eishockey-manager-redone/blob/33669d3c155d9a05747ae0f79a5deee2e1907438/src/EishockeyManager.Core/MatchEngine.LiveStep.cs#L93)

Originalprobe ab der Spielerwahl einer Zweiminutenstrafe, Heimverein mit Manager und ohne Zusehmarkierung. Ausgabe für Anzeige aus/an war `0 []` bzw. `1 [(0, 12)]`. Damit ist der zusätzliche Aufruf unabhängig von der konkreten Zufallszahl belegt.

```bash
PYTHONPATH=tools/reverse-engineering/live python3 - gamedata/em.exe <<'PY'
from torszene_dauer import cpu, emu16
for live in (0, 1):
    c = cpu([(0x35c1c, 0x35c7b), (0x52520, 0x52bd6)])
    c.sdb(0x5a56, live)
    c.sdw(0x262, 60)
    c.wb(emu16.FAR_HEAP_SEG, 0x16, 1)  # Heimverein 0 mit Manager
    c.r["bp"] = 0xe000
    for off, val in [(6, 0), (8, 1), (-2, 2), (-6, 0)]:
        c.ww(c.s["ss"], c.r["bp"] + off, val)
    draws = []
    c.hooks[0xc037] = lambda t: draws.append((t.arg(0), t.arg(1))) or 0
    c.s["cs"], c.ip = c.at(0x35c1c)
    c.stop_at = {0x35c7b}
    try: c.run()
    except emu16.Stop: assert c.here() == 0x35c7b
    print(live, draws)
PY
```

Remake-Gegenprobe mit Originalstand FILE09, Saat 26, zwei Managerzügen und demselben Minutentreiber. Beide Läufe melden eine Anzeigereihe, aber für die Partie 0/1 `Flag=False, Register=False`. Am Head endet sie 3:5. Setzt die Gegenprobe ausschließlich `DayActive` vor Minute 1 auf den tatsächlichen Tageswert, endet sie 1:3. Auch das Register für Verletzungen übernimmt den frühen Anzeigewert. Eine Auswirkung auf eine konkrete Verletzung wurde nicht nachgestellt. [Registerbildung](https://github.com/kaaasi/eishockey-manager-redone/blob/33669d3c155d9a05747ae0f79a5deee2e1907438/src/EishockeyManager.Core/MatchEngine.Live.cs#L171)

```bash
p=$(mktemp -d)
cat > "$p/Probe.csproj" <<EOFPROJ
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
  <ItemGroup><ProjectReference Include="$PWD/src/EishockeyManager.Core/EishockeyManager.Core.csproj" /></ItemGroup>
</Project>
EOFPROJ
cat > "$p/Program.cs" <<'CS'
using EishockeyManager.Core;
using EishockeyManager.Formats;
using System.Reflection;
var db = ClubDatabase.Load(GameData.Locate("gamedata"));
foreach (var fix in new[] { false, true }) {
    var g = OriginalImport.Load(args[0], db, 26);
    g.Options.MatchSpeed = 60; g.Options.LiveMode = 0;
    g.ToggleWatch(g.Clubs[g.Managers[0].ClubIndex]);
    using var driver = MatchEngine.UseLiveDriver(new Probe(fix));
    g.Continue(); g.Continue();
}
class Probe(bool fix) : ILiveMatchDriver {
    public void Run(LiveMatchDay day) {
        var matches = day.Matches.Where(m => m.Stepped && m.Row == null && (m.HomeTeam!.Managed || m.AwayTeam!.Managed)).ToList();
        foreach (var m in matches) {
            var s = typeof(LiveMatch).GetProperty("Setup", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(m)!;
            var flag = s.GetType().GetProperty("DayActive")!;
            Console.WriteLine($"Reihen={day.Matches.Count(p => p.Row != null)}, Flag={flag.GetValue(s)}, Register={m.HomeTeam!.TurnManager!.LiveDisplay}");
            if (fix) flag.SetValue(s, true);
        }
        while (day.Step()) {}
        foreach (var m in matches) Console.WriteLine($"{m.Home.Index}/{m.Away.Index} {m.Report.HomeGoals}:{m.Report.AwayGoals}");
    }
}
CS
dotnet run --project "$p/Probe.csproj" -c Release -- ../eishockey-manager-referenz/original/spielstaende/FILE09.HOK
```

**Testlücke zu R26-G1.** Der Strafgrundtest setzt den Tagesmerker bereits auf wahr. Der Vergleich mit und ohne Treiber nutzt auf beiden Seiten dieselbe Vorbereitung. Beide Prüfungen lassen den oben reproduzierten Fehler durch. [Strafgrundtest](https://github.com/kaaasi/eishockey-manager-redone/blob/33669d3c155d9a05747ae0f79a5deee2e1907438/tests/EishockeyManager.Core.Tests/TorszeneDauerTests.cs#L38) · [Treibertest](https://github.com/kaaasi/eishockey-manager-redone/blob/33669d3c155d9a05747ae0f79a5deee2e1907438/tests/EishockeyManager.Core.Tests/LiveVerschraenktTests.cs#L83)

## 3. Ausgeführte Prüfungen

Separater Checkout, `git rev-parse HEAD` wie oben, `git status --porcelain` leer, SDK 10.0.402. `NuGetAudit=false tools/pruefen.sh --schnell` mit Originaldaten. Der erste Build mit Online-Audit war wegen NU1900 blockiert. Der Wiederholungslauf verwendete den lokalen Paketcache, Release-Build mit 0 Warnungen und 0 Fehlern.

| Lauf | Bestanden | Fehlgeschlagen | Übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 4075 | 0 | 0 |

Alle folgenden Originalproben liefen mit `python3 tools/reverse-engineering/<Pfad>.py gamedata/em.exe`:

- **S01 und Pokaltermine** · `live/minutenschleife`: 18 Paarungen in drei Ligen, gleiche Reihenfolge in jeder Minute 1–20, die zwei Reihen an ihren Positionen. Im DEB-Pokal laufen in Minute 61 nur die beiden markierten Gleichstände weiter.
- **SG-01/R26-E1** · `tagesabschluss/hauptweg`, `tagesabschluss/stadionschaeden`: Abschluss und Bewertung für 0, dann 1. Im Hauptweg folgen am Bautag Tageslauf und Bau je Manager. In der WM-Schleife kommt der Bau direkt nach der Bewertung desselben Managers. Der Schaden stoppt nach einem Treffer. Beim Stadionbau selbst fällt keine Schadensgebühr an. Training, Unterhalt, Zinsen und Bauauftrag auf ihre Buchungswege über `GameState.Book` geprüft.
- **A472/A1027** · `live/torszene_dauer`: Darstellung 2 hält das Eis bis zur Verlängerung, baut für das Penaltyschießen eigenes Eis auf und wieder ab. Darstellung 0 beginnt bei 0:0 und zeigt jeden Treffer, Pause 80 je Schuss. Den Bildschirmablauf `torszene-dauer` mit Originalgrafiken gerendert (fünf Bilder, Gleichstand im Szenario vorbereitet).
- **S04** · `live/drittelseiten`, `live/pokalseiten`, `live/aufstieg_europapokal`: Aufstiegsgruppen mit eigenem Meldungsfilter, Unterschied zwischen Drittelpause und Endstand. Szenario `live-aufstieg-europapokal` gerendert. Im direkten Bildvergleich mit `eisbaeren_f51_03346` stimmen Runde 3 (3:3 und 2:2), Tabellenführung (Eisbären 5:1 Punkte, 12:6 Tore) und ausgeblendete Rückspieltermine überein.
- **Ladeübergang** · Eigene Probe mit FILE09, Saat 26, beide Manager zugesehen. Ein Zwilling lief durch, der andere wurde vor jedem `Continue()` aus `SaveGame.Save` neu geladen und ohne Eingriff live gespielt. Nach allen 24 Zügen bytegleiche Spielstände, zwölf Live-Termine, davon acht mit mehreren Reihen.
- **Vier Saisonwechsel** · `FourSeasonChanges_TwoManagersLive_Plausible` und `FourSeasonChanges_DayClosePerManager`, jeweils aktiv und passiv. Verglichen wurden unter anderem die gespeicherten Zustände mit/ohne Treiber sowie Konten und gebuchter Unterhalt beider Manager.

**Bestätigt offen** (Issue-Status geprüft): #1019, #1024, #978 und #980, Aufräumen #936. **Akzeptierte Entscheidungen und Näherungen**: R26-D2 = A (A449 Enter/Escape, A453 Bildtakt, A541/A542 Minutenweg, Zufallsfolge nicht bitgenau), A1010 getrennte Europapokalgruppen, A1011 Zahlenbreite der Schadensmeldung, A1012 Untergrenze bei Platzschäden und Darstellung 1/2 ohne `tore\` mit dem Pausenablauf von Darstellung 0.

**Prüfgrenze:** Stichproben an den genannten Übergängen. Kein eigener DOSBox-Mitschnitt zum Dauer-Eisfeld und kein vollständiger Bildvergleich aller Penaltyphasen.
