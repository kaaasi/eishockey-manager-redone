# Review PR #830 – Spur livespiel-ui-2 (B2, B3)

Erstreview (Claude Opus 5.5, geplanter Lauf, unbeaufsichtigt). Geprüft: Head `716897bbccc24b74e2910ed7f35f73c602f5ac0d`,
Base `1a4c038985f4af3399b5dc53180505205368e964`; Checkout nachgewiesen (`git rev-parse HEAD` = Head, `git status --porcelain`
leer), SDK 10.0.402, Originaldaten aus `kaaasi/eishockey-manager-referenz`. Keine Mitarbeit an diesem PR.

## 1. Merge-Empfehlung

**Nein** für genau diesen Head – wegen eines P2 (R830-01, Reihenfolge der Spieltagsanzeige am ersten Play-off-Tag). Sonst
tragfähig: Architektur, Determinismus, Abnahme „Live = ohne Anzeige“, #823, Disassembly-Stellen und Bildvergleich halten
den eigenen Proben stand. Nach Behebung von R830-01 genügt die Delta-Prüfung der Leitstelle (nur P2/P3).

## 2. Befunde

**R830-01 (P2) – Play-off-Banner erscheint nach dem ersten Live-Spiel der Play-offs.**
[`MatchDayScreen.cs` Z. 115–128, 139](https://github.com/kaaasi/eishockey-manager-redone/blob/716897bbccc24b74e2910ed7f35f73c602f5ac0d/src/EishockeyManager.UI/Screens/MatchDayScreen.cs#L115-L139):
Mit `LiveSession` werden Halte (Banner + Live) sofort eingereiht, alle übrigen `SeasonStep`s erst nach dem Ende von
`Continue()`. Liegt ein Nicht-Spiel-Schritt **vor** der Managerpaarung in derselben Liste, kippt die Reihenfolge. Fundort
(einziger Treffer in einer Saison, Saat 1993, Verein 42): `Continue` Nr. 55 liefert
`BannerStep(149, "Play-Off / Aufstiegsrunden") | MatchesStep(149, Playoffs, ManagerInvolved)`; `PlayPlayoffDay`
([`SeasonFlow.cs` Z. 331–332](https://github.com/kaaasi/eishockey-manager-redone/blob/716897bbccc24b74e2910ed7f35f73c602f5ac0d/src/EishockeyManager.Core/SeasonFlow.cs#L331-L332)).
Base zeigt beide Banner vor „Spiele laufen“; der Head zeigt
`Banner:Play-Off / Aufstiegsrunden -> Live -> MatchDay -> Banner:Play-Off / Aufstiegsrunden` – die Ankündigung der Play-offs
kommt also nach deren erstem Spiel. Probe (temporäre Datei in `tests/EishockeyManager.UI.Tests/`, Hilfen `NewGame`/`Driver` =
`Core.NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, seed)` + `Host.Game`/`MainMenuScreen`):

```csharp
var d = Driver(NewGame(1993, 42), 1993);
for (var i = 0; i < 55; i++) PlayLiveDay(d, skip: true);           // bis Tag 149
var seq = new List<string>(); string? last = null; d.NextDay();
while (d.Host.Current is not MainMenuScreen) {
    var now = d.Host.Current switch { MatchDayScreen m => m.CurrentBanner is { } b ? "Banner:" + b : "MatchDay",
                                      LiveMatchScreen => "Live", var x => x!.GetType().Name };
    if (now != last) seq.Add(now); last = now;
    if (d.Host.Current is LiveMatchScreen l) { if (l.WaitingForClick) d.Click(160, 120); else d.Frame(); }
    else d.Press(Key.Enter);
}
// seq = [Banner:Play-Off / Aufstiegsrunden, Live, MatchDay, Banner:Play-Off / Aufstiegsrunden]
```

Vorschlag: Schritte, die vor dem Halt in der Liste stehen, vor dem Banner des Halts zeigen (z. B. `LiveSession` reicht beim
Halt die bis dahin gesammelten Schritte durch) und Regressionstest mit dieser Folge.

**R830-02 (P3) – Rückstellung von `CurrentManager` ist ungetestet.**
[`LiveMatchScreen.cs` Z. 273](https://github.com/kaaasi/eishockey-manager-redone/blob/716897bbccc24b74e2910ed7f35f73c602f5ac0d/src/EishockeyManager.UI/Screens/Live/LiveMatchScreen.cs#L273):
Mutation „Zeile 273 löschen“ → `CrestClick_OpensLineupAndTactics_InMatch_AppliesOnReturn` bleibt grün (ein Manager: Wert
ändert sich nie; Assert Z. 258 trivial). Eigene Probe mit zwei Managern derselben Liga (42 + nächster Verein, Saat 3, Klick
auf den Kopf des **anderen** Managers in Minute 5): mit Mutation rot (`Expected: 1, Actual: 0`), ohne grün. Code korrekt,
nur Testlücke – Test mit ≥ 2 Managern ergänzen.

**R830-03 (P3) – Formatierung** [`MatchEngine.Live.cs` Z. 69](https://github.com/kaaasi/eishockey-manager-redone/blob/716897bbccc24b74e2910ed7f35f73c602f5ac0d/src/EishockeyManager.Core/MatchEngine.Live.cs#L69):
`var match =BeginMatch(…)` (Leerzeichen fehlt).

Hinweis ohne Befund: `ClickPlayerInMatch` Z. 79 `_dressed.Add` ist nach `TakeOverDressed` für Aufgestellte redundant
(Mutation „Zeile entfernen“ bleibt in `LeftClickOnPlaced_InMatch_TakesOut_StaysDressed` grün – gleichwertige Mutante).

## 3. Ausgeführte Prüfungen

`tools/pruefen.sh` (alle drei Läufe, Head 716897b, SDK 10.0.402) – Ergebnis bestanden:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3199 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2344 | 0 | 536 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2345 | 854 | 0 |

CI auf dem Head: `build` success.

**Eigene Proben (temporär, mit Originaldaten, alle grün außer den genannten Mutationen):**

- **Abnahme „Live = ohne Anzeige“, mehrere Manager, eigene Saaten:** 3 Manager derselben Liga (42 + zwei weitere), Saaten
  7/4711/2026 bei Geschwindigkeit 60/65/0, je 8 „Tag weiter“ über `LiveMatchScreen` (jeder dritte mit Enter), Hot-Seat-Züge
  über das Hauptmenü, nach Tag 4 Speichern/Laden in einen neuen `GameState` + neues Hauptmenü: nach jedem Tag
  `SaveGame.Save` gleich dem Lauf `Continue()` ohne Anzeige ab derselben Sicherung; 1–3 Live-Bildschirme je Tag, Geschwindigkeit
  danach unverändert.
- **Abbruch mitten im Spiel:** Live-Spiel bei Minute > 0 verlassen, Sicherung vor dem Tag zweimal laden und live spielen →
  beide Läufe byte-gleich und gleich ohne Anzeige (kein Zustand am verlassenen Thread/`ThreadStatic`-Treiber).
- **#823 erneut:** `LiveClock.Minute` bei 62/66/69 → 91 von 400 Minuten Pause ≤ 0, alle mit Zwinkern, `PauseFrames` = 0;
  Spieltag über den Live-Bildschirm bei diesen Geschwindigkeiten endet (≈ 2800–3300 Bilder) und ist gleich ohne Anzeige.
- **Torwartwechsel wie FILE51:** Minute 26 Kopf/Logo → Torwart-Platz leeren, Ersatztorwart setzen (`ClickPlayerInMatch`) →
  Escape: Spielkader Ersatz Platz 1, alter Platz 0, beide eingesetzt, `HasGoalie` wahr; Belastung alter −1, neuer +3 gegenüber
  ohne Eingriff (Richtung wie FILE51: −2/+2).
- **Architektur (Codelesen + Proben):** Zeit vergeht nur in `GameState.Continue()` (Treiber im Aufruf, `UseLiveDriver` je
  Thread, verschachtelte Aufrufe gesperrt); immer nur ein Thread aktiv (Semaphoren 0/1, Befehle und Eingriffe nur während der
  Kern wartet); `CurrentManager` nur während des Eingriffs umgestellt und vor dem nächsten `Step` zurück (Probe oben); Ausnahme
  im Kern landet als `InvalidOperationException` beim nächsten Warten der Oberfläche statt zu hängen. Öffnen/Zeichnen ändert
  nichts (`Drawing_DoesNotChangeState`); die Ausnahme in `BildschirmeFreipruefungTests.NotConstructible` ist begründet.
- **Mutationen** (je Test rot): `PlayWithDriver` → `return null` (2 Core- + 4 UI-Tests rot); `LiveLineup.Apply` ohne
  Platz (`Apply_TakesPositions…`, `CrestClick…`); Rechtsklick-Sperre Z. 75 entfernt (`RightClickOnPlaced…`);
  `PauseFrames` `<= 0` → `== 0` (`PauseFrames_NonPositive…`); Auffüllen „bis 8“ → „8 anhängen“ (`MouthSequence…`).
- **Disassembly:** `disasm.py em.exe 0x5224E 0x522EC`: Vokaltabelle DGROUP 0x541D = `"25AEIOU"` (Datei 0x1C1EF), höchstens
  0x1C Öffnungen, Auffüllen mit 9 solange Zähler < 8 (0x522C2–0x522D5), dann 0xFF → „Chance:“ = [0, 0, 9 × 6, 0xFF]; die
  Berichtigung der Plan-Abnahme in §15 ist richtig.
- **Bilder:** Szenario `live-spiel` gerendert, gegen `duell_s2a_01126` (Reihe 1) und `bundestrainer_00200` (gleiche 320 × 240):
  Hintergrund y 95–178 98,8 % Pixel innerhalb ±24 (Verschiebung 0/0 am besten), Reporterkasten 96,1 %, Uhrkasten 87,6 %
  (Ziffern/Balken variabel); Sichtvergleich Reihenkästen, Kopf/Logo, „0 min.“, Spalten, Ziffern deckungsgleich.

**Akzeptierte `[APPROX]`** (wie PR/§15, bestätigt offen): Managerpaarungen nacheinander statt gleichzeitig (Folge: je Halt
eigenes Banner, Lampe/Sicherung je Live-Bildschirm statt je Tag); Spiele am Spielende gezählt (0x41533); Linienwechsel aus der
Taktik im Spiel erst nächstes Spiel; Bestrafte bleiben draußen; Tasten A/C im Spiel aus; Nationalteam ohne Eingriff; Farbe
erster Taktikeintrag; Reporterbild Durchsicht Farbe 0; Zuschauer im Heimspiel leer; vergebene Chancen ohne Schützen;
Meldungsfolge je Minute; Klick je Bild; Enter/Escape = Rest ohne Anzeige; Leertaste nicht angeschlossen; lrandom je
Spielstand; Pause ≤ 0. Erwartet offen: Torszenen (Runde 19), kein Videobild für den Eingriff.

**Dateigrenzen:** `LineupScreen` geht über „nur Kennzeichen“ hinaus (zweiter Konstruktor mit Taktikweg, Weiche auf die Regeln
im Spiel) – minimal und im PR begründet, akzeptiert; `MatchEngine.Managed.cs` `init → set` begründet (Übernahme wie 0x41533);
neue Core-Dateien `MatchEngine.LiveDriver.cs`, `MatchEngine.LiveLineup.cs`, `Squad.Lineup.InMatch.cs` je eigene Datei.

**Annahmen:** Die Eingriffsregeln 0x40E2A/0x40CFD/0x4115F/0x40F35/0x41298/0x4135E/0x413E8 wurden gegen §10/§15 und die
Tests gelesen, nicht erneut vollständig disassembliert.
