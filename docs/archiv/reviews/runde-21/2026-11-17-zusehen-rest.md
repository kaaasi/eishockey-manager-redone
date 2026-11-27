# Review PR #922 – Spur zusehen-rest (Teile 1–3)

Geprüft: Base `191f3ee`, Head `7a0c44c` (`git rev-parse HEAD` = `7a0c44c23429a455f1a12d3b6f074613826de197`, `git status
--porcelain` leer). Reviewer: Claude (geplanter Lauf, Sitzung ohne Implementierungskontext). Erstreview, alle drei Läufe.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Die Gegenbeispiele A858, A834, A837/A840 sind erneut ausgeführt und grün. Jeder neue Test wird
bei gezielter Mutation rot. Die drei Prüfläufe sind grün, mit 3606 Tests mit Originaldaten.

**#908 (`beleg-strittig`): Die Lesart der Spur ist richtig. Import = Laden verwirft den Tausch.** Die Lesart aus Review #906
P3-1 („Import muss den Tausch behalten“) gilt nicht. Ich habe es selbst nachgelesen (`ndisasm -b16`):
- Laden ruft die Tagesfunktion 0x54C9F mit `[bp+6]` ≠ 0 auf. Datei 0x54CA6–0x54CC2 überspringt nur den Spielstart. Über
  0x54CEE/0x54D3B wird die Markierung des Tagesindex gelesen (0x54D41). Ist sie ≠ 0 und ≠ 0xFF, beginnt die Schleife über die
  Managersätze ab 0x54D68.
- Für jeden Managersatz holt Datei 0x54D84–0x54D9A `[0x375+2m] := [0x376+2m]`. Danach folgen `call 0x108:0x4D` (0x426BC,
  Datei 0x54D9E) und `0x952C(m, 0)` (Datei 0x54DAA).
- Erst danach setzt Datei 0x54E3F–0x54E48 den Manager am Zug auf `[bp-0x19]`.
- Vor den Spielen sichert Datei 0x54FCE–0x54FDC `0x376 := 0x375`; 0x54FE8 setzt danach `0x375 := 0`.
- Bytes aus der Probe (`0375`, 8 Byte): FILE71 `0101010101010101`, FILE72 `0101000101…` (Manager 1: 0/1),
  FILE73 `0101000001…` (0/0).
- Nach einem Laden von FILE72 stünde in FILE73 `0x376` = 1 (1/1, Automatik). Der Wert 0/0 passt nur zum Weiterspielen ohne
  Laden.
- Beide Belege sind ausgeführt und grün:
  [`Import_File72_LikeLoading_DiscardsSwap`](https://github.com/kaaasi/eishockey-manager-redone/blob/7a0c44c23429a455f1a12d3b6f074613826de197/tests/EishockeyManager.Core.Tests/ZusehenRestAufstellungTests.cs#L55) und
  [`ContinueWithoutLoading_File71SwapToFile73_KeepsSwap`](https://github.com/kaaasi/eishockey-manager-redone/blob/7a0c44c23429a455f1a12d3b6f074613826de197/tests/EishockeyManager.Core.Tests/ZusehenRestAufstellungTests.cs#L72).
- Mutation M1: Der Import liest `0x375` statt `0x376` ([OriginalImport.Rest.cs:282](https://github.com/kaaasi/eishockey-manager-redone/blob/7a0c44c23429a455f1a12d3b6f074613826de197/src/EishockeyManager.Core/OriginalImport.Rest.cs#L282)).
  Danach ist `ZusehenRestAufstellungTests` rot (1 von 4).

Empfehlung an die Leitstelle: Das Label `beleg-strittig` kann entfallen, #908 ist mit dem Merge erledigt. Eine Restlücke
bleibt, siehe P3-1.

## 2. Befunde

Keine P0–P2.

**P3-1 – Der FILE72-Import bildet das Laden nur halb nach: Die Vereinsfelder bleiben auf dem Tausch, die Aufstellung nicht.**
Beim Laden folgt auf die Neuaufstellung 0x426BC sofort `0x952C(m, 0)` (Datei 0x54DAA, siehe oben). Der Import stellt neu auf
(LUTZ in Block 0), übernimmt aber die Felder 0x18–0x1D roh aus dem HOK.
- Probe (temporärer Test, verworfen): `HokCompare.Import(FILE72)`, EC HEDOS MÜNCHEN, Werte Abwehr/Sturm für Kondition,
  Technik und Form.
  - Felder nach dem Import: 69 69 55 / 70 70 48 (Stand mit Tausch).
  - Nach `OnLineupLeft`, also derselben Rechnung wie 0x952C: 68 69 55 / 70 69 48. Das ist der Wert, den das Original nach dem
    Laden hätte.
- Folge: Bis zum nächsten Schreiben stimmen die Anzeigen und Zuschauer nicht. Das nächste Schreiben kommt am selben Tag vor den
  Spielen (0x54FBF), die Spiele selbst sind daher nicht betroffen.
- Die Ursache stammt aus #733 und ist nicht neu in diesem PR. Seit #908 entschieden ist, gehört sie in ein Issue: Nach dem Import
  bei `0x375 ≠ 0x376` an einem Tag mit Ereignis `WriteLineupTeamValues` aufrufen. Kein Merge-Hindernis.

**Ohne Befund geprüft:**
- **(2) Berührungen fremder Dateien in Teil 2 liegen nur an den genannten Stellen** (`git diff 191f3ee 7a0c44c`):
  - `MatchEngine.Minutes.cs`: Bedingung vor `Injuries` in `PlayMinute` und drei Zeilen in `Score`.
  - `MatchEngine.Managed.cs`: `RecountSkaters` und `PenalizedPlayer` (Kommentar und Bedingung).
  - `MatchEngine.Injuries.cs`: eine Zeile.
  - `MatchEngine.National.cs`: `NationalLiveTeam` (`Squad` und `PenaltySquad`). `NationalSquad` läuft jetzt auch ohne
    Reihe, ist aber ohne Seiteneffekt.
  - `MatchEngine.Live.cs` zieht `PrepareMatchGate` und `ScoreboardRow` vor `MatchRules` vor; der Ablauf bleibt gleich.
- **(3) Die Tormeldung zeichnet nur.** Der Kern legt ohne Zufall und ohne Spielwert nur `GoalNotice` in
  `LiveScoreboard.Notices` ab. `LiveScoreboard.Notices` wird nicht gespeichert (`grep Notices src` findet nur Live-UI und
  Scoreboard). Live-Bildschirm ([LiveMatchScreen.cs:368](https://github.com/kaaasi/eishockey-manager-redone/blob/7a0c44c23429a455f1a12d3b6f074613826de197/src/EishockeyManager.UI/Screens/Live/LiveMatchScreen.cs#L368)) und
  `LiveGoalNotice` ändern nur den Darstellungszustand (`_notice`, `ClearLines`). `LiveScreen_ShowsGoalNotices` vergleicht die
  Spielstände mit einem Lauf ohne Anzeige. `LineupScreen` merkt sich den Stand erst bei `Update`
  ([:114](https://github.com/kaaasi/eishockey-manager-redone/blob/7a0c44c23429a455f1a12d3b6f074613826de197/src/EishockeyManager.UI/Screens/Team/LineupScreen.cs#L114)) und ruft `OnLineupLeft` nur nach einer Änderung auf
  (AGENTS.md §4 eingehalten).
- A858: Datei 0x41513–0x4151A ist nachgelesen (`push 0`, `push [0xA70E]`, `call 0x28:0x146C`).

**Erwartet offen, bestätigt:**
- Tore später gerechneter Paarungen und aus der Verlängerung fehlen (`[APPROX]` in `NoticeTarget`/`LiveGoalNotice`).
- Kein Videobild der Tormeldung.
- `MatchEngine.LiveDriver.cs:54` prüft weiter `home.WatchedLive` am Objekt aus `RatedNation`.
- Schemanummer: Der PR ändert keine `SaveGame*`-Datei und bringt keine Migration mit, es ist also nichts zu nummerieren.

## 3. Ausgeführte Prüfungen

`tools/claude/setup-workspace.sh`, `dotnet --version` = 10.0.402, `tools/pruefen.sh` (alle drei Läufe, Head `7a0c44c`):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3606 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2518 | 0 | 595 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2519 | 1087 | 0 |

Gegenbeispiele (`dotnet test --filter ZusehenRest|TeamwertAufstellung`): Core 35/35 grün, UI 4/4 grün.
- **A858:** `A858_LeavingLineupAfterSwap_FieldsLikeFile72` grün. Nach dem Tausch stehen die Felder auf 69 70 69 70 55 48 wie in FILE72.
- **A834:** Probe auf die Bytes 0x34 >> 6 (Rohwert/Import):
  - FILE11: 7:2/2, 54:2/2
  - FILE18: 7:2/2, 54:2/2; `0x22C` = 1, Sicherung `True`
  - FILE49: 7:2/2, 54:2/2; `0x22C` = 0, Sicherung `False`
  - FILE54: 0:2/2, 53:2/2
  - FILE72: 3:2/2, 8:2/2; ohne Amt, Sicherung `null`
- **A837/A840:** `MarkedComputerClub_GetsManagerMatch` und `ManagerClubWithoutRow_IsNoManagerMatch` grün.

Mutationen in der Arbeitskopie (danach `git checkout src`):

| Mutation | rot |
|---|---|
| M1 `[2*m+1]` → `[2*m]` in `ApplyTactics` | `ZusehenRestAufstellungTests` (1 von 4) |
| M2 `PlayMinute`: `home.Managed \|\| away.Managed` → `rules.ManagerMatch` | `ZusehenRestReiheTests` (1 von 6) |
| M3 `LiveMatchScreen`: Aufruf `Notices(minute)` entfernt | `ZusehenRestUiTests` (1 von 4) |
| M4 `PenalizedPlayer`: `team.Squad is null \|\|` entfernt | `ZusehenRestReiheTests` (1 von 6) |

Disassembly mit `ndisasm -b16 -e <Datei> -o <Datei> em.exe` an 0x54C90, 0x54E3F, 0x54FB0 und 0x41500.

**Akzeptierte `[APPROX]`:**
- `NoticeTarget`: Die Meldung wird ohne Wissen um eine spätere Reihe abgelegt.
- `LiveGoalNotice`: Reihenfolge nacheinander gerechneter Paarungen; das Zurückholen der Hintergrundkopie ist hier das Leeren
  des Reporterkastens.
- Schneller Pokalweg am Manager statt am Merker 0x36FEA.
- `OnLineupLeft` nur nach einer Änderung.

**Annahmen (nicht geprüft):**
- Pixellage der Tormeldung (es gibt kein Videobild).
- Tore eines Managervereins ohne Reihe zählen bei keinem Spieler (nur aus dem Disassembly, so wie im PR).
