# Gesamtreview Runde 25 · 25. November 2026

## 1. Merge-Empfehlung

**Nein für `6877442` (Base `1daa223`).** Die fünf Gegenbeispiele verhalten sich wie erwartet. Gültige Altstände laden.
Ein beschädigter Schema-63-Stand kann seit dieser Runde bereits beim Anzeigen seiner Info eine unbehandelte Ausnahme im Ladedialog auslösen. Dieser P2 ist vor der Freigabe zu beheben.

## 2. Befunde

**R25-G1 · P2 · Produktfehler beim Laden und bei der Spielstand-Info.** Die Migration 63 → 64 liest `ClubIndex` mit
`GetValue<int>()`. Enthält das Feld eine Zeichenkette, wirft die Migration vor dem Fehlerhandler des Readers.
Das Laden erreicht seine Warnmeldung nicht. Beim Rechtsklick passiert dasselbe über `SaveInfo.FromRemake`.
Auf der Base kehren beide Aufrufe mit dem vorgesehenen Fehlerergebnis zurück. [Migration, Zeile 23](https://github.com/kaaasi/eishockey-manager-redone/blob/68774420f13c3c61d408ea75a96ed51293b2c341/src/EishockeyManager.Core/SaveGame.Migration.Aufstellung.cs#L23) · [Reader, Zeile 49](https://github.com/kaaasi/eishockey-manager-redone/blob/68774420f13c3c61d408ea75a96ed51293b2c341/src/EishockeyManager.Core/SaveGame.Reader.cs#L49) · [Dialog, Zeile 313](https://github.com/kaaasi/eishockey-manager-redone/blob/68774420f13c3c61d408ea75a96ed51293b2c341/src/EishockeyManager.UI/Screens/SaveLoad/SaveLoadScreen.cs#L313)

Ausführbare Minimalprobe im jeweiligen Checkout mit SDK 10. Derselbe Fehler trat an einem vollständigen Stand aus `1daa223` auf, nachdem nur der Typ von `manager[0].ClubIndex` geändert wurde.

```sh
p=$(mktemp -d)
dotnet new console -o "$p" --name Probe --no-restore
dotnet add "$p/Probe.csproj" reference src/EishockeyManager.UI/EishockeyManager.UI.csproj
cat > "$p/Program.cs" <<'CS'
using System.Text;
using EishockeyManager.Core;
using EishockeyManager.UI.Screens.SaveLoad;
var data = Encoding.UTF8.GetBytes("""{"kennung":"EISHOCKEY-MANAGER-REDONE-SPIELSTAND","schema":63,"manager":[{"ClubIndex":"x"}]}""");
foreach (Action read in new Action[] { () => SaveGame.Load(data), () => SaveInfo.FromRemake(data) })
    try { read(); Console.WriteLine("Fehler behandelt"); } catch (Exception e) { Console.WriteLine(e.GetType().Name); }
CS
dotnet run --project "$p/Probe.csproj"
```

Ergebnis auf `6877442` zweimal `InvalidOperationException`, auf `1daa223` zweimal `Fehler behandelt`.
Im ScreenDriver schlugen auch die Bedienwege „Laden → Dateiname → Enter“ und „Laden → Rechtsklick auf Datei“ mit dieser Eingabe fehl (Dialogzeilen 432 bzw. 313).

**Testlücke zu R25-G1.** Die vorhandenen Dialogtests prüfen eine fremde Datei und eine neuere Version. Eine Datei mit bekanntem Altschema und falsch typisiertem Migrationsfeld erreicht einen anderen Fehlerpfad.

## 3. Ausgeführte Prüfungen und akzeptierte Näherungen

Sauberer Checkout vor der Prüfung, `git rev-parse HEAD` = `68774420f13c3c61d408ea75a96ed51293b2c341`.
SDK 10.0.402, lokaler Paketcache, `NuGetAudit=false DOTNET_PROCESSOR_COUNT=2 tools/pruefen.sh --schnell`, Release-Build ohne Warnungen.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3982 | 0 | 0 |

Die genannten Python-Proben liefen mit `python3 tools/reverse-engineering/abweichungen/<name>.py gamedata/em.exe`. Die C#-Fälle lassen sich mit `dotnet test tests/EishockeyManager.Core.Tests -c Release --filter FullyQualifiedName~<Fall>` wiederholen, für B31 mit `EishockeyManager.UI.Tests`.

| Gegenbeispiel | Ergebnis der Wiederholung |
|---|---|
| T03 | `t03_saisonwechsel` zog 38, erneut 38, dann 40. Nach den Tauschen kam die Wanderung vor der Mischung, ohne Schreibzugriff auf den Tageszähler. Im Remake meldeten drei temporäre Protokollausgaben direkt nach den Aufrufen in `FinishSeason` bei **einem** Lauf von `OberligaDraw_On29April_NotOn13April` jeweils den 29. April 1994, in der Folge `ExchangeOberliga`, `DriftSeasonEnd`, `PrepareNextLeagueSlots`. |
| K07 | `k07_sechster`, Fall c, und `CaseC_AfterForwardCleared_GoalkeeperAccepted` bestätigen, dass der Spieler auf dem sechsten Feldspielerplatz nach dem Leeren der Sturmzelle eingesetzt bleibt und der Torhüter hinzukommt. Bei noch offenem sechsten Platz wird der Torhüter abgewiesen. |
| F06 | `f06_bande` und `BoardOffer_TakesFirstFreeOrCancellableFieldFromZero` mit Klick auf Feld 3. Feld 0 erhält den neuen Partner, Feld 1 behält 300, Feld 2 bleibt fest, Feld 3 frei. Auch im ScreenDriver mit diesen Verträgen geprüft: Feld 3 angeklickt, Angebot gewählt und angenommen. Monatseinnahme 900 + 300 + 500 = 1.700. |
| B31 | Verein und Nationalteam, Aufstellung und Taktik. `LeavingUnchangedLineup_WritesClubFields`, `TacticsScreen_Leave_WritesMatchEffort` und `AufstellungB31Tests` trennen Öffnen und Escape. Beim Verein bleibt der Einsatz beim Öffnen 33 und wird beim Verlassen ohne Eingabe 60. Der Bildschirm löst das Kern-Ereignis beim Verlassen aus, wie in §4 vorgesehen. |
| #1007 | `h1_highscore_liga` und `SeasonCodes_LeagueByte_AddsLeagueOffset` ergeben für Platz 1 in Bundesliga, Zweiter Liga und Oberliga die Codes 1, **13**, 25. |

- **Speichern und Kader.** Mit dem Werkzeug aus `1daa223` einen Schema-63-Stand erzeugt (`hok export --neu Probe 7 ... --ems ...`), auf dem Head geladen und als HOK mit gültigen Prüfsummen exportiert. Tag 0 und 500.000 DM blieben erhalten. Alle Poolspieler hatten nach der Migration den vorgesehenen Rollen-Ersatzwert. Speichern → Laden → Speichern war byteidentisch. `Migration_63To64_ManagerTakesClubSetting` und `SaveLoad_RoleValueKept_Schema64Migrated` prüfen auch den ausgeschalteten Manager-Schalter bzw. die Rollenmitten 0/33/83. Original-Rundreisen mit FILE01 und FILE27 prüfen die gespeicherten Rollenbytes.
- **K07 je Manager.** Die Bindung läuft bei Spielstart, Hinzufügen und Laden. `k07_automatik` trennt die beiden Managerbytes. `AddManager_MidGame_BindsNewcomerToClub_SwitchSurvivesSaveLoad` schaltet den neuen Verein um und liest die Einstellung nach dem Laden in beide Richtungen.
- **F08 und Zufall.** `f08_baufirma` zeigt elf Würfe je Manager. `f08_sommerpause` und `SummerDay_InterleavesPerManager` bestätigen die Abfolge je Manager in der Sommerpause. In einer separaten Kopie mit nur zehn Bauwürfen wurden der direkte Stromvergleich sowie beide Varianten von `JugendTeamsTests.FourSeasonChanges_StrengthFollowsFunding` rot.
- **K11.** Die Würfe für Pool-Feldspieler gab es schon auf der Base. Neu ist die Verwendung ihres Werts bei der Torschützenauswahl. `k11_rollenwert` bestätigt die Spannen 25–41 und 75–91. Bei einer separaten Rückmutation der Torschützenauswahl auf die Rollenmitte wurden `CreditSkaters_ReadsRoleValueOfPlayer` und beide Vier-Saison-Abdrücke mit Originaldaten rot.
- **C07.** `c07_marken` und `WettbewerbeAnzeigeMarkenTests` bestätigen für DEB-Pokal, Play-offs und WM-Endrunde im Minutenweg 0 Marken nach regulärem Ende, 1 nach Verlängerungstor, 2 nach Penaltyschießen. Das Referenzbild `eisbaeren_f44_01330` zeigt unterschiedliche Punktzahlen hinter den Ergebnissen. Den Entscheidungsweg klärt die Probe mit den EXE-Bytes.

**Erwartet offen.** GitHub bestätigt #1018 (Tagesabschluss im Hauptweg, Runde 26), #1019 (Testlücke zur Stromlage, Aufräumen #936), #1024 (Doku-P3), #978 und #980. S01, S02, S04 und S05 sind für Runde 26 vorgemerkt.

**Akzeptierte `[APPROX]`.** Die Ausgabe von `python3 tools/reverse-engineering/markerliste.py` stimmt mit dem Generatorblock der Restliste überein. Alle dort akzeptierten Näherungen gelten weiter, darunter Rollenmitten alter Stände, die alte Saisonende-Phase und der schnelle Play-off-Weg. Die genaue DOS-Zufallsfolge über eine ganze Karriere und ein interaktiver DOSBox-Lauf waren außerhalb dieser Prüfung.
