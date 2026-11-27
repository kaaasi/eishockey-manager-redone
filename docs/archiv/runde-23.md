# Archiv: Runde 23 – letzte V, Test-Infrastruktur, DOSBox `runde-22` Sitzung 1 (23.11.2026)

Aus `docs/arbeitspakete.md` ausgelagert am 23.11.2026. Inhalt unverändert bis auf Überschrift, „Ergebnis“, „Abschluss“ und
„Entscheidungen während der Runde“, ohne „Zurückgestellt“ und „Vormerkungen“ (bleiben in `arbeitspakete.md`). Die Statuszeilen
der Spuren geben den Stand vor dem jeweiligen Merge wieder. Reviews: `docs/archiv/reviews/runde-23/` (Dateien vom 23.11.2026). Nur bei Bedarf
lesen. Relative Links beziehen sich auf `docs/`.

**Ergebnis:** Plan #958 (Plan-Review #961), Prozess-PR #959 (#960: Erfahrungen Runde 22, Rebase statt Merge-Commit). Alle sechs
Spuren gemergt: `pruefen-sh` (#966, #967; Fixes #951), `v-rest` (#962, #968 Nein P2, Fixlauf mit A961 nach R23-E1 = A, Nachprüfung
#971 Ja; Fixes #948), `feldspieler-sechs` (#963, #970 Nein P2, Fixlauf, Delta-Prüfung; Schema 63; Fixes #939), `abgleich-dosbox-22`
(#964, #972; 0xA70D = 0xA70E), `re-reste` (#965, #973 Nein P2, Fixlauf mit `beleg-strittig`, Nachprüfung #975 Ja; Fixes #925),
`pruefung-karte` (#976, #979; #954, #969, #974; Generatorlauf **V = 0, X = 0**, D 26). Gesamtreview eng (R23-D4 = B, ChatGPT/Codex,
#977) **Ja** für `4ae1735`: keine Produktfehler, Testlücke #978.

**Abschluss (23.11.2026):** offen: P3 #978, #980; D 26 (DOSBox `runde-22` Sitzungen 2–7, nach Lust des Nutzers); Abweichungsliste
vor v1.0 an den Nutzer; Aufräumen #936 als letzter Schritt vor v1.0, danach Abschlussprüfung.

**Entscheidungen während der Runde:** Nutzer 23.11.2026: R23-D1 = A, R23-D2 = D (`re-reste` mit Claude Fable 5.1 statt 5.2),
R23-D3 = A, R23-D4 = B; Go. Prozess-PR #959 (Auftrag Nutzer). **R23-E1 = A** (A961 Spielwertung: Klasse V, Fixlauf `v-rest`).
Leitstelle: 0xA70D = 0xA70E im Export (belegt an 72 Originalständen, Kommentar #964); A876, A917 bleiben K; `beleg-strittig` zu
RR-01 per Nachprüfung allein dieses Punkts entschieden (Lesart Fixlauf: nur die ersten zwei gerechneten Paarungen eines Termins);
Integrationsfolge nach Fertigstellung (`abgleich-dosbox-22` vor `re-reste`).

## Aktuelle Runde: Runde 23 – letzte V, Test-Infrastruktur, DOSBox `runde-22` Sitzung 1 (Plan 23.11.2026, Go 23.11.2026)

Runde 22 abgeschlossen am 22.11.2026: [archiv/runde-22.md](archiv/runde-22.md). Grundlage: Restliste
[analyse/restliste-v1.md](analyse/restliste-v1.md) (V 5: A937/A875 → #939, A873, A913, A951 → #948; D 30), Issues #939, #948, #951
(nur noch die `pruefen.sh`-Punkte; die Markerliste-Punkte hat `marker-x` #952 erledigt), #954, #925, Reste #938/#930, DOSBox-Aufnahme
`runde-22` Sitzung 1 (Referenz-PR kaaasi/eishockey-manager-referenz#41: FILE75, FILE76, 20 Bilder; A427, A065, A057, A402, A613,
A614, A629, A117). **Ziel der Runde:** V = 0 nach dem Generatorlauf, `pruefen.sh`-Punkte erledigt, Sitzung 1 ausgewertet. Danach
bleiben bis v1.0 nur die übrigen D (Sitzungen 2–7 nach Lust), das Aufräumen (#936) und die Abschlussprüfung.
**Entscheidungen Nutzer 23.11.2026** (Optionen im Plan-PR #958): R23-D1 = A, R23-D2 = D (eigene RE-Spur `re-reste`, Claude
Fable 5.1), R23-D3 = A (Sitzung 7 in `vorlagen/dosbox-aufnahme.md`), R23-D4 = B; Referenz-PR #41 gemergt (23.11.2026).
**Go Nutzer 23.11.2026** (alle Spuren samt Folgeläufen, Modelle wie unten; Start nach Merge des Plans).

**Regeln wie Runde 22** (Archiv): Base = aktueller `main` beim Start der Spur, tatsächlicher Base-SHA in den Draft-PR; ein Branch
`paket/<spur>`, ein PR, Teile als Commits, höchstens drei Fortsetzungen; Befund reproduzieren, roter Regressionstest mit Fundstelle
`Datei 0x…`, dann grün; Mehrsaisonprobe (4 Wechsel, passiv/aktiv) bei Saisonwechsel, Kader oder Geld; `[ORIGINAL]`/`[APPROX]`;
fremde Tests nach R15-E2 = C, erwartete Werte nach R21-D2 = A; keine geplanten Aufgaben aus Läufen (R15-E4). Bei Unklarheit „wie im
Original“, sonst `[APPROX]` mit Begründung, die Leitstelle bündelt. N2 = B, N6 = A, R21-E1 = A gelten. **Markerliste:** kein
`markerliste.py --write` außer in `pruefung-karte`; jede Spur nennt im PR die geänderten, neuen und entfallenen Markerzeilen
(Datei, alte ID, neue Klasse). Neue Spielstandfelder: Migration in eigener Datei `Core/SaveGame.Migration.<Spur>.cs`, Schemanummer
(ab 63) beim Rebase in Integrationsfolge. **Rebase:** Base-Wechsel nur per Rebase und `git push --force-with-lease`, kein
Merge-Commit; ist Force-Push gesperrt, Label `spur-wartet` mit genauer Fehlermeldung (`vorlagen/spur-start.md`, PR #959). **Entwickler und Erstreviews sind Claude-Sitzungen;** ChatGPT nur fürs Gesamtreview.

**Folge (R23-D1 = A):** Welle 1 parallel `pruefen-sh`, `v-rest`, `feldspieler-sechs`, `re-reste` (Teil 1),
`abgleich-dosbox-22` (Teil 1; Referenz-PR #41 gemergt); Teil 2 von `re-reste` und `abgleich-dosbox-22` jeweils nach Merge von
`v-rest`. Integration in der Folge `pruefen-sh` → `v-rest` → `feldspieler-sechs` → `re-reste` → `abgleich-dosbox-22` (Spätere
rebasen und ziehen erwartete Werte nach). Welle 2
`pruefung-karte` nach Merge aller übrigen. Danach Gesamtreview eng über `feldspieler-sechs` und `v-rest` (R23-D4 = B).

**Modelle (Nutzer 17.11.2026 „wenig Tokens für brauchbares Ergebnis“; 09.11.2026 keine Sonnet-Wahl mit Nacharbeit):** Opus 5.5 für
Datenmodell, Spielrechnung, Zufallsstrom und em.exe; Claude Fable 5.1 nur `re-reste` (R23-D2 = D); Sonnet 5.5 für Skript- und Markerarbeit; Haiku nicht vorgesehen (jeder Lauf
baut, testet und liest Fundstellen). Erstreviews: Code-Spuren Opus 5.5, `pruefen-sh` und `pruefung-karte` Sonnet 5.5 (bewusste Abweichung von „Im Zweifel Opus“:
Skript klein, Befunde mit Fundstelle im Review #950). `re-reste` und `abgleich-dosbox-22` nur mit Erstreview (R23-D4 = B). Fixläufe im
Modell der Spur, Fixläufe ohne Befund Sonnet 5.5. Plan-Review Sonnet 5.5. Gesamtreview ChatGPT GPT-6.1 Terra (Codex).

| Spur | Welle | Inhalt | Modell | Grund |
|---|---|---|---|---|
| `pruefen-sh` | 1 | #951: Review #950 P3-1 (Selbsttest prüft Summen), P3-2 (Tabellenzeile bei Hänger mit Fehlervermerk), P3-3 | Sonnet 5.5 | S, Skript, Fundstellen im Review |
| `v-rest` | 1 | #948: A951, A913, A873 | Opus 5.5 | Zufallsstrom (A913), Datenmodell Wertesatz (A873), em.exe |
| `feldspieler-sechs` | 1 | #939: A937 (Verein), A875 (Nationalteam), Rest A683/A490 | Opus 5.5 | L, Datenmodell `LineupSlot`, Spielrechnung, Migration |
| `re-reste` | 1 | #925 (b), #938 P3-3, A876/A917 bestätigen | Fable 5.1 | Lesung em.exe ohne Beleg bisher (R23-D2 = D) |
| `abgleich-dosbox-22` | 1 | Sitzung 1: A427, A065, A057, A402, A613, A614, A629, A117 | Opus 5.5 | Abgleich mit Originalständen, HOK-Felder, em.exe |
| `pruefung-karte` | 2 | P3 der Runde, #954, 24 X-Funktionszeilen, Generatorlauf | Sonnet 5.5 | mechanisch, Einordnung nach PR-Angaben |

### Zuständigkeit für gemeinsame Dateien

- `Core/CupsNational.cs`, `Core/CupsNational.GameState.cs` nur `v-rest` (A873); `Core/CupsNational.Lineup*.cs`,
  `Core/MatchEngine.National.cs` nur `feldspieler-sechs`. `Core/MatchEngine.Managed.cs`: `feldspieler-sechs` nur die
  Aufstellungsstellen (`LineupSlot`), `re-reste` nur 0x8C53; in `Core/CupsNational.GameState.cs` darf `re-reste` nur den Kommentar zu 0x8C53 pflegen, nach Merge
  von `v-rest`.
  `Core/MatchEngine.Minutes.cs`, `Core/MatchEngine.PlayoffFastPath.cs`, `Core/Cups.Tiebreak.cs`, `Core/OriginalImport.Playoffs.cs`,
  `Core/OriginalImport.Rest.cs` (A917) nur `re-reste`. `Squad.AutoLineup.cs`, `OriginalImport.National.cs` (`LineupSlot`) und
  `UI/ShortMessageTexts.cs` (nur Meldung 0x40EF7, gemeinsame Datei, Begründung im PR) nur `feldspieler-sechs`.
  `Core/Player.Value.cs`, `Core/Player.Slot.cs` (Wert 0x4A0A7) nur `v-rest`, falls nötig. `Core/MatchGate.cs` nur `v-rest` (A913).
- `Core/Transfer.Purchase.cs`, `Core/Player.Contract.cs` (A951) erst `v-rest`; `abgleich-dosbox-22` Teil 2 (A117 Verhandlung) rebased
  danach. `UI/Screens/Team/ContractDialog.cs` nur `abgleich-dosbox-22` (A117), Kernänderung A951 ohne Bildschirm.
- `Core/OriginalImport.cs` und `Core/OriginalExport*.cs`: `feldspieler-sechs` nur Aufstellungsbelegung, `abgleich-dosbox-22` nur
  Felder aus A402/A613/A614/A629; je eigene `OriginalImport.<Teil>.cs`, wo möglich. `Core/SaveGame.cs` nur Schemanummer und
  Migrationstabelle.
- `tools/pruefen.sh`, `tools/pruefen-selbsttest.sh` nur `pruefen-sh`. `docs/analyse/restliste-v1.md` (generierter Block, Zahlen),
  `restliste-v1-klassen.csv`, `markerliste.py --write`, `funktionskarte.py --write` allein `pruefung-karte`.

### Spur `pruefen-sh` – Test-Infrastruktur (Welle 1)

- **Teile:** 1. #951 `pruefen.sh`-Punkte aus `docs/archiv/reviews/runde-22/2026-11-22-test-zeitlimit.md` (#950): P3-1 Selbsttest prüft die Summen der
  Tabelle, P3-2 Tabellenzeile bei Hänger mit Fehlervermerk, P3-3 Kleinigkeiten; je Punkt roter Fall im Selbsttest. Fixes #951.
- **Dateien:** `tools/pruefen.sh`, `tools/pruefen-selbsttest.sh`. **Prüfung:** alle drei Läufe und `tools/pruefen-selbsttest.sh`
  (`AGENTS.md` §5). **Erstreview:** Sonnet 5.5. **Status:** Teil 1 fertig, PR #966 (Ready for review).

### Spur `v-rest` – letzte V aus #948 (Welle 1)

- **Teile:** 1. A951 Kauf vom Markt: Startgehalt und Verhandlung nach neuem Kaderplatz (Wert 0x4A0A7; Datei 0x477E7, 0x4786A);
  2. A913 Spielwertung: Manager mit kleinerem Index würfeln die Vorbereitung (Datei 0x547AA; Zufallsstrom, erwartete Werte nach
  R21-D2); 3. A873 WM-Kader „auto“ und Umschalten ohne Verletzungsprüfung (Datei 0x3CC01; Verletzung des Vereinsplatzes darf nicht
  mitgehen), Mehrsaisonprobe. Fixes #948.
- **Dateien:** `Core/Transfer.Purchase.cs`, `Core/Player.Contract.cs`, `Core/MatchGate.cs`, `Core/CupsNational.cs`,
  `Core/CupsNational.GameState.cs`, Tests. **Erstreview:** Opus 5.5. **Status:** Teile 1–3 fertig; Fixlauf VR-01, VR-02 und A961 (R23-E1 = A) gepusht, PR #962 wartet auf Nachprüfung (Opus 5.5).

### Spur `feldspieler-sechs` – sechster Feldspielerplatz ohne Torhüter (Welle 1, R22-E2 = B)

- **Teile:** 1. em.exe Datei 0x413E8–0x414B4, 0x412CF–0x412DB, 0x40EF7, 0x414D3–0x414ED lesen; Datenmodell (neuer Platz in
  `LineupSlot`, Abwehrhälfte Platz 3–5 der Reihe 1), Aufstellen im Verein (andere Hälfte, Meldung „Schon 6 Feldspieler“), Migration,
  HOK-Import/-Export der Belegung (A937, A683 Rest); 2. Spielrechnung ohne Torhüter mit sechs Feldspielern (Spiel und Eingriff im
  Live-Spiel); 3. Nationalaufstellung (A875, A490 Rest) und Aufstellungsbilder (`LineupScreen.cs`, `NationalLineupScreen.cs`,
  Bildschirmfoto gegen Videobild, falls vorhanden). Mehrsaisonprobe. Fixes #939.
- **Dateien:** `Core/Squad.Lineup*.cs`, `Core/CupsNational.Lineup*.cs`, `Core/MatchEngine.National.cs`, Stellen oben,
  `Core/SaveGame.Migration.FeldspielerSechs.cs`, `UI/Screens/Team/LineupScreen.cs`, `UI/Screens/Competitions/NationalLineupScreen.cs`,
  Tests. **Erstreview:** Opus 5.5. **Status:** Teile 1–3 fertig (#963), Erstreview Nein (R963-01 P2, R963-02 P3), Fixlauf behoben – Delta-Prüfung Leitstelle.

### Spur `re-reste` – Reste ohne Beleg per em.exe (Welle 1, R23-D2 = D)

- **Teile:** 1. em.exe lesen, Bericht `docs/archiv/analyse/re-reste-23.md` je Punkt mit ausführbarem Beleg: #925 (b) eigener Gleichstand
  bleibt nach Wurf x = y stehen und zählt für den Gast (0x3166E) auch beim DEB-Pokal? (0x32B5F, FILE44 `D014` 4:4, widerspricht
  A003); #938 P3-3 Belastungszug 0x8C53 im Länderspiel zu stark?; A876 (Zähler Satz 5 über den Saisonwechsel) und A917 (HOK-Import
  0x18–0x1D nach Neuaufstellung, Datei 0x54D84–0x54DAA) nur bestätigen bzw. Klasse begründen; 2. belegte Abweichungen umsetzen (nach
  Merge von `v-rest`), sonst Vermerk K/E bzw. „nicht belegt“ im PR. Fixes #925.
- **Dateien:** `docs/archiv/analyse/re-reste-23.md`, Stellen oben, Tests. **Erstreview:** Opus 5.5. **Status:** Teil 1 und 2 fertig
  (PR #965, 23.11.2026): #925 (b) im DEB-Pokal umgesetzt (`FastCupResultWithTie` `debCup`, `CupMatch.FromReport` zwei Marken, Heimverein),
  #938 P3-3 nicht belegt (Kommentar 0x8C53), A876/A917 K bestätigt – Review. Fixlauf RR-01/RR-02 (23.11.2026): Zusatzwurf des
  schnellen Wegs auch für ungleiche Paarungen mit offener Paarung dahinter (`MatchEngine.DebCupExtraRoll`, nur die ersten zwei je
  Termin – Beleg strittig zu „jede Paarung“), Wahrscheinlichkeiten im Bericht 1/3 – Leitstelle-Delta.

### Spur `abgleich-dosbox-22` – DOSBox-Aufnahme `runde-22` Sitzung 1 (Welle 1)

- **Voraussetzung:** kaaasi/eishockey-manager-referenz#41 gemergt (23.11.2026; FILE75, FILE76, Bilder `file75_*`, `file76_*`).
- **Teile:** 1. Auswertung je ID mit Beleg (Spielstand, Bild, em.exe): A427 Startwerte der Computervereinsspieler, A065 Bestenliste,
  A057 Zugfolge, A402/A613/A614/A629 (Sparbuch, Darlehen, Leihe, Bandenwerbung im HOK), A117 Gehalt 0 und sehr hoch; Bericht
  `docs/archiv/analyse/abgleich-dosbox-22.md`, Klassenvorschlag je ID (V/X/K/E) für `pruefung-karte`; 2. belegte Abweichungen der Größe S
  umsetzen (nach Merge von `v-rest`, A117 an `ContractDialog.cs`/Kern); größere als Issue mit Vorschlag an die Leitstelle.
- **Dateien:** `docs/archiv/analyse/abgleich-dosbox-22.md`, Stellen oben, eigene `OriginalImport.<Teil>.cs`, Tests mit FILE75/76.
  **Erstreview:** Opus 5.5. **Status:** Teil 1 fertig (PR #964, Bericht `archiv/analyse/abgleich-dosbox-22.md`: V A065, A402, A614,
  A629-Export; X A057, A613; K A427, A117; A629-Leihende bleibt D); Teil 2 fertig (Fortsetzung 1: A065, A402, A614, A629-Export,
  A117-Anzeige, 0xA70D; Bericht „Teil 2“), Ready for review.

### Spur `pruefung-karte` – P3 der Runde und Generatorlauf (Welle 2)

- **Voraussetzung:** alle übrigen Spuren gemergt. **Teile:** 1. P3-Issues der Runde und #954 (Wortlaut `[ORIGINAL]` mit Fundstelle
  bzw. Aussage über das Remake-Datenmodell); 2. 24 X-Funktionszeilen (F-IDs ohne Markerzeile) umklassifizieren oder ausblenden;
  Klassen aus `abgleich-dosbox-22` übernehmen; 3. `markerliste.py --write`, `funktionskarte.py --write` (Ziel V = 0, X = 0).
- **Erstreview:** Sonnet 5.5. **Status:** Teile 1–3 fertig (PR #976): #954, #969, #974; Klassen A961–A973, A964 belegt (`[ORIGINAL]`); 24 X-Funktionszeilen im Generator ausgeblendet; Generatorlauf V = 0, X = 0, unklassifiziert = 0; Ready for review.
