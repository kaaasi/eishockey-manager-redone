# Review PR #934 – Spur einzelpunkte (Teile 1–3, Fortsetzung 1)

Geprüft: Base `c58e738`, Head `32aadc4` (`git rev-parse HEAD` = `32aadc4eb517384f40fe6c07d7c1ef417fb14e15`, `git status
--porcelain` leer). Reviewer: Claude Opus 5.5 (geplanter Lauf, Sitzung ohne Implementierungskontext). Erstreview, alle drei Läufe.

## 1. Merge-Empfehlung

**Nein** für genau diesen Head, wegen eines offenen P2 (P2-1). Der Regressionstest für den Aufstiegs-Hook von A582 prüft nichts.
Das Verhalten selbst ist richtig, ein Fixlauf nur am Test genügt. Danach reicht die Delta-Prüfung der Leitstelle (nur P2/P3).

- Alle Fundstellen der Spur habe ich gegen em.exe nachgelesen: 12 bestätigt, bei A582 widerlegt der Code ein `[APPROX]` (P3-1).
- Die neuen Tests werden bei gezielter Mutation rot, außer dem einen Test aus P2-1.
- Der fremde Test `KaderRestSaisonendeTests` ist nach R15-E2 = C zulässig angepasst. Die Rückschaltprobe habe ich nachgeholt (s. u.).
- Laden, Speichern, Migration 56 → 59 und fünf Saisonwechsel mit Laden und Speichern sind sauber (Proben in Teil 3).
- Erwartet offen und bestätigt: Schema 57–59 sind vorläufig. Gegen das aktuelle `main` (`41ad124`) gibt es Konflikte in
  `SaveGame.cs`, `docs/formate/spielstand-original.md` und mehreren Abdruck-Tests. Das ist der Rebase der Integrationsfolge.
- Erwartet offen und bestätigt: A775 ist nicht umgesetzt (N2 = B, `docs/arbeitspakete.md` Z. 138).

## 2. Befunde

**P2-1 – Der Test `Promotion_MarksAllSponsorContractsCancellable` ist leer und deckt den Hook 0x2713F nicht ab.**

- Fundstellen: [EinzelpunkteTests.cs#L175](https://github.com/kaaasi/eishockey-manager-redone/blob/32aadc4eb517384f40fe6c07d7c1ef417fb14e15/tests/EishockeyManager.Core.Tests/EinzelpunkteTests.cs#L175)
  und [#L256](https://github.com/kaaasi/eishockey-manager-redone/blob/32aadc4eb517384f40fe6c07d7c1ef417fb14e15/tests/EishockeyManager.Core.Tests/EinzelpunkteTests.cs#L256).
- Ursache: Die Verträge haben `MonthsLeft = 6` und laufen bis zum Saisonende aus. `ads.Contracts` ist dann leer, und
  `Assert.All` ist für beide Fälle trivial grün.
- Beleg 1: Mutation `MarkContractsCancellable(manager)` → `{ }` in [SeasonFlow.cs#L474](https://github.com/kaaasi/eishockey-manager-redone/blob/32aadc4eb517384f40fe6c07d7c1ef417fb14e15/src/EishockeyManager.Core/SeasonFlow.cs#L474).
  Danach ist `dotnet test tests/EishockeyManager.Core.Tests --filter "FullyQualifiedName~Promotion_MarksAll"` weiter grün (2/2).
- Beleg 2: Mit zusätzlichem `Assert.NotEmpty(ads.Contracts)` ist der Test schon auf dem Head rot (2/2, „Collection was empty“).
- Gegenprobe mit `MonthsLeft = 60` im Helfer: Der Head ist grün, die Mutation rot (`promote: True`, 2 von 2 Verträgen falsch).
  Der Hook arbeitet also richtig.
- Vorschlag: In diesem Test Verträge mit mindestens 13 Restmonaten anlegen und `Assert.NotEmpty` vor `Assert.All` setzen.

**P3-1 – Bei „Kündbar.“ gegen „! Option !“ hat im Original die Option Vorrang; das Remake macht es umgekehrt.**

- Fundstelle: [AdvertisingScreen.cs#L403](https://github.com/kaaasi/eishockey-manager-redone/blob/32aadc4eb517384f40fe6c07d7c1ef417fb14e15/src/EishockeyManager.UI/Screens/Manager/AdvertisingScreen.cs#L403).
- Beleg (`ndisasm -b16 -e 0x2AC50 -o 0x2AC50 em.exe`):
  - 0x2AC5A `test byte [es:bx+0x6],0x7f` / `jz 0xac84`, dann 0x2AC61 `cmp byte [es:bx+0x5],0x0` / `jnz 0xac84`.
  - Damit wird „! Option !“ gezeichnet (`push word [0x1199]`), anschließend 0x2AC82 `jmp short 0xacae`, der Kündbar-Zweig entfällt.
  - Erst ab 0x2AC84 folgen `cmp byte [bp+0x12],0` und `test byte [es:bx+0x6],0x80`, also „Kündbar.“.
- Das `[APPROX]` „Vorrang vor ! Option !“ ist damit widerlegt. Die Reihenfolge der beiden Zweige tauschen und `[ORIGINAL] 0x2AC5A–0x2AC82` vermerken.
- Folge: nur die Anzeige, und nur bei einem kündbaren Vertrag mit Restlaufzeit 0 und Option.

**P3-2 – Für die Schemata 57–59 gibt es keinen Migrations- oder Roundtrip-Test.**

- `SaveGameTests` prüft nur Fingerabdruck und Version.
- Meine Proben in einer temporären Kopie sind grün (Teil 3). Ich empfehle, sie zu übernehmen, besonders 58 → 59.
  Diese Migration fügt Objekte mit `$id` ein und nummeriert neu (`Renumber`).

**P3-3 (Verdacht) – `Tactics.Goalie` ist toter Zustand.**

- Seit A492/A681 zeigt und schaltet Knopf 0 die Einstellung „automatisch aufstellen“
  ([Squad.Tactics.cs#L221](https://github.com/kaaasi/eishockey-manager-redone/blob/32aadc4eb517384f40fe6c07d7c1ef417fb14e15/src/EishockeyManager.Core/Squad.Tactics.cs#L221)).
- `grep -rn "\.Goalie\b" src` findet nur noch Spielstand, Migrationen und `HokCompareClasses.cs`.
- Kein Fehler. Ob das Feld entfällt, entscheidet die Leitstelle.

**P3-4 (Verdacht) – A671 prüft das Statusbit 0x04 nicht.**

- Das Original nimmt den Zweig 0x56238 nur bei Vereinsfeld 0x16 ≠ 0 und freiem Statusbit 0x04 (0x562C1 `or byte [bx-0x6a61],0x2`).
- [SeasonFlow.cs#L425](https://github.com/kaaasi/eishockey-manager-redone/blob/32aadc4eb517384f40fe6c07d7c1ef417fb14e15/src/EishockeyManager.Core/SeasonFlow.cs#L425)
  prüft das Bit nicht.
- Folgenlos, solange im Modus „nur Weltkugel“ niemand entlassen oder zwangsabgestiegen wird. Nicht als Test belegt.

**Hinweis zur PR-Beschreibung:** Nicht nur Seed 3 und 7 sind von A665 betroffen, sondern auch Seed 5 (passiv, Wechsel 3 mit
2 `InnerMoves`). Ohne Jugend bleiben passiv alle drei alten Seeds.

## 3. Ausgeführte Prüfungen

`tools/claude/fetch-reference.sh original`, `setup-workspace.sh`, `dotnet --version` = 10.0.402, `tools/pruefen.sh` (alle drei Läufe):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3658 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2557 | 0 | 602 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2558 | 1100 | 0 |

**Fundstellen gegen em.exe** (`ndisasm`, `tools/reverse-engineering/disasm.py`, Cache nur im Scratch): A104 0x4DB6 (Poolschleife,
`+0x18 == 0`, `+0x1D` = Verein, höchstens 3); A672 0x42749–0x4275A (`+0x15 = (Teil==2)<<7 + Reihe + 1`); A847
0x29D03/0x29D0E/0x29D23, 0x5D351; A492/A673/A681 0x47CB2 (0x375 + 2·m), 0x47F3E, 0x49048–0x49071, 0x55599/0x555BF; A371
0x4B09/0x4B52 (·0x5E/15, Begrenzung 0x4C75); A367 0x465B/0x4753; A206 0x5613E (Liga 0–2 := 1) vor 0x5645D, 0x565E0, Rest
0x4A87F–0x4A89C = 100 − 100·n/44 → 98; A421 0x32650–0x32666, 0x44D43; A671 0x296EC (kein Modus), 0x562D8 (0x124F80 Drittel-DM
= 400.000 DM); A698 0x566B8–0x56793 (12 − Platzfeld, +3 bei 0; 0x364B7/0x51115 nicht nachgelesen); A582 0x270A8/0x270B2,
0x2713F, 0x2BAF3/0x2BB12, 0x2BB3E; A665 0x561CC–0x5636D, 0x36169 (doppelter Index → zweiter Tausch wie `RelegationPlaces`, übrige
Fälle von `InsertForcedRelegation` ohne Doppel); #897 0x57F49–0x57FB8 (eigener Satz entfällt 0x57F62), 0x57C64 (·25/1000, Satz 4
·1/100 bzw. ·4/100), Vorzeichen über den Verleihzweig 0x591F6–0x59262.

**Mutationen** (temporäre Kopie, je Lauf eine Mutation, `--filter EinzelpunkteTests`; der neue Test wird jeweils rot): A665
`InnerMoves` → `[]`; A582 `IsFree` → `ContractAt is null`; A671 Zuweisung entfernt; A698 Nachrücker ohne Liga; #897 Zins 4 → 1
(2 Fälle) und Vorzeichen `+`; A104 alter Code (Aufgestellter zuerst); A421 Termingrenze entfernt; A847 Kennzeichen nicht gesetzt;
A672 `PlaceGoalieStandIn` entfernt; A492 (Bundestrainer) `!autoAtOpen` entfernt; A206 1 → 45 (rot in
`TransferVertraegeKomplettTests.SeasonEnd_ExpiredContract_WaitsForNegotiation`). Nur der A582-Hook bleibt grün (P2-1).

**Fremder Test `KaderRestSaisonendeTests` (R15-E2 = C / R21-D2 = A).** Rückschaltprobe mit den alten Seeds 3, 5, 7:

Head `active: False` rot („Keine Jugend am Saisonende“), Head mit A665 aus 2/2 grün. A665 ist also die Ursache; „Summe > 0“
hängt nur von der Zufallsfolge ab. Mit den Seeds 13 und 15 kommt passiv Jugend vor, mit 3, 5, 13 auch aktiv: Die Probe bleibt
aussagekräftig, die Ausweitung schwächt keine Aussage. Annehmbar; die Begründung im Test (Z. 56–57) genügt.

**Laden, Speichern, Migration und Saisonwechsel** (Probe `ReviewProbeTests` in einer temporären Kopie, alle grün):

- Roundtrip mit `OneSeasonFlag`, `_goalieStandIn` (Verweis auf einen Kaderspieler), `Cancellable` = true und zwei `InnerMoves`:
  Alle Werte bleiben erhalten, Speichern → Laden → Speichern ist byte-gleich.
- Migration: Stand mit `schema` = 56 und entfernten Feldern (1, 150, 1, 1 Stück).
  - Das Laden gelingt.
  - Erneutes Speichern ist stabil.
  - Nach `StartNextSeason` und 30 × `Continue()` ist der Stand gleich dem Neustand (Objektnummern normalisiert).
- Mehrsaisonprobe, 5 Saisonwechsel, Seeds 3 und 7, passiv und aktiv: Nach jeder Saison wird gespeichert und geladen.
  - Das Ergebnis ist byte-gleich zum durchgespielten Stand.
  - `InnerMoves` treten dabei auf: passiv je 2, aktiv bei Seed 7 2.

**Bildschirme ändern beim Öffnen nichts (`AGENTS.md` §4):** `TacticsScreen` merkt `_autoAtOpen` erst beim ersten Klick auf
Knopf 0, `Close` ruft `CloseTactics` nur nach einem Umschalten auf. `FinanceScreen` (`FinanceOverviewLoans`) und
`AdvertisingScreen` (`IsFree`) lesen nur.

**Akzeptierte `[APPROX]`** (getrennt von Annahmen): A582 – Bande als ein Vertrag, Rest bleibt kündbar; `MenuLamps` zählt
kündbare Flächen als belegt; ältere Stände ohne Kennzeichen. A665 – ältere Stände ohne Platzverschiebung. #897 – Saldo = Sparbuch
− Kredit. A104 – Torhüter ohne Poolplatz in Kaderreihenfolge. A367 – Gegentore des Torhüters je Spalte = Saisonwert, ohne
Vereinszähler. Import ohne `OneSeasonFlag`/`GoalieStandIn`.

Annahme, nicht geprüft: Die Herkunft des Platzfelds der Rückkehrer bei A698 (0x364B7/0x51115) und ein DOSBox-Beleg für A665.
