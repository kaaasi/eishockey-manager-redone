# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 27 – Aufräumen vor v1.0 (#936; Plan 27.11.2026, Go 27.11.2026)

Runde 26 abgeschlossen am 26.11.2026: [archiv/runde-26.md](archiv/runde-26.md). Stand: Restliste
[analyse/restliste-v1.md](analyse/restliste-v1.md) **V = 0**, D 25, E 321, K 197; `main` `6506d62`, Schema 65.
**Ziel der Runde:** Repo nach [analyse/aufraeumen-v1.md](analyse/aufraeumen-v1.md) aufgeräumt (Entscheidungen A1 = B, A2 = A,
A3 = B, A4 = A, A5 = A mit Skripten ins Archiv, A6 = A; Nutzer 24.11.2026), **Verhalten unverändert** (Konzept §2.1 Nr. 6). Dazu
P3 #978, #980, #1019, #1024 (Rest P3-2) und die Themen K04, K11, K13, L11, L12, C02, C08 aus
[analyse/abweichungen-v1.md](analyse/abweichungen-v1.md) Abschnitt 0 („ins Aufräumen #936“). **Nicht in dieser Runde:** D-Themen
(DOSBox nach Lust), Abschlussprüfung (Runde 28). **Entscheidungen Nutzer 27.11.2026** (Optionen im Plan-PR #1056):
R27-D1 = C (eigene Spur `aufraeumen-archiv` parallel in Welle 1), R27-D2 = B (nur Schema 65 ladbar, alle Migrationsschritte
entfallen), R27-D3 = B (enges Gesamtreview am Rundenende, ChatGPT GPT-6.2 Astra; Nutzerangabe „R22-D3 = B“, vom Nutzer am
27.11.2026 als R27-D3 bestätigt), R27-D4 = B (Modelle wie unten). **Go Nutzer
27.11.2026** (alle Spuren samt Folgeläufen; Start nach Merge des Plans).

**Regeln der Runde:**

- **Stand neu erheben (Teil 0 jeder Code-Spur):** Das Konzept ist auf `e0b0ea5` gezählt; Runde 25/26 haben Code geändert (u. a.
  Schema 65). Jede Spur prüft ihre Befunde zuerst auf dem aktuellen `main` (Fundstelle noch da, Zeilennummer, Aufrufer per grep)
  und vermerkt Abweichungen im PR. **Erledigt und ausgenommen:** K2, K9, K11, K13 des Konzepts (Runde 25, #1003, #1004, #1006).
  Neu gefundene Verhaltensunterschiede werden **nicht** mit umgebaut, sondern als P3 „Verdacht“ (Issue) gemeldet.
- **Abnahmeproben** nach Konzept §2.4 in jedem Code-PR: Testliste `dotnet test --list-tests` vor/nach (Abbildung alt → neu,
  entfernte Fälle nur bei totem Weg oder entfallener Migration mit Begründung, neue Fälle nur #978, #1019 und der
  Lader-Test von `aufraeumen-migration`); Zustandsabdruck (`StateFingerprint`) einer
  festen Partie über 4 Saisonwechsel (passiv/aktiv) auf Base und Head gleich – Harness im PR (nicht eingecheckt) mit Seed und
  Partie, entfernte oder umbenannte Felder (Reflexion liest alle Instanzfelder) als erwartete Abweichung aufgelistet; Bildschirmfotos bytegleich, wo UI berührt ist;
  `markerliste.py` **ohne** `--write` vor/nach (verlorene IDs nur bei gelöschten toten Pfaden, Liste im PR; `[APPROX]`-Wortlaut
  beim Verschieben nicht ändern; zweiter zulässiger Grund für entfallene IDs: entfallene Migration, z. B. L11, Teile von
  L12/C02, Liste je ID im PR).
- **Testhilfen:** Dateien unter `tests/*/Support/` (Link-Dateien zwischen den Testprojekten) werden nicht umbenannt oder
  verschoben; in Welle 4 nach dem Rebase der zweiten Spur `dotnet build` beider Testprojekte.
- **Abdeckungskarte:** `docs/re-abdeckung.md` bleibt (erzeugt von `tools/reverse-engineering/funktionskarte.py`). Wer Doku
  verschiebt, die `funktionskarte.py` (`scan_docs`) als Beleg liest, weist `funktionskarte.py <em.exe> --csv` vor/nach mit
  gleicher Statusverteilung nach.
- **Prüfläufe:** Code-Spuren alle drei Läufe von `tools/pruefen.sh` (Konzept §2.2); reine Doku-Spuren entfällt.
- **Migration (A1 = B, R27-D2 = B):** nur Spielstände mit Schema 65 bleiben ladbar, alle Schritte `[1]`…`[64]` entfallen;
  `SchemaVersion` bleibt 65, keine neue Migration in dieser Runde.
- **Testnamen (A3 = B):** deutsch erlaubt, nur Spur/Runde/Teil/Fix/Review/Welle aus den Namen; Ausnahme steht in `AGENTS.md` §1.
- **Gemeinsame Dateien:** `GameState.cs`, `GameState.Integration.cs`, `Widgets.cs` je minimal, nur in der Spur, die sie laut
  Konzept braucht, nie zwei Spuren derselben Welle.
- Sonst wie Runde 26: Base = aktueller `main` beim Start, ein Branch `paket/<spur>`, ein PR, Teile als Commits, höchstens drei
  Fortsetzungen; keine geplanten Aufgaben aus Läufen (R15-E4); fremde Tests nach R15-E2 = C. Kein `markerliste.py --write` außer
  in `aufraeumen-doku` (A6 = A).

**Folge (R27-D1 = C):** Welle 1 `aufraeumen-migration` und `aufraeumen-archiv` parallel (Dateien disjunkt: `SaveGame*`/Tests
gegen `docs/**` und `funktionskarte.py`); Welle 2 `aufraeumen-tot` nach Merge von `aufraeumen-migration`; Welle 3 `aufraeumen-doppelt`; Welle 4
`aufraeumen-tests-core` und `aufraeumen-tests-ui` parallel (Prüfbefehl „keine Überschneidung“, Konzept §2.2); Welle 5
`aufraeumen-doku`. Danach Leitstelle: Workflow `aufraeumen.yml` mit `nur_anzeigen=true`, Liste dem Nutzer vorlegen; Löschen
der Branches (`nur_anzeigen=false`) nur nach seinem Ja. Dann enges Gesamtreview (R27-D3 = B): Verhaltensgleichheit Base
`6506d62` gegen den Stand nach Welle 5, Linkprüfung, Ladeweg Schema 65. Abschlussprüfung in Runde 28.

| Spur | Welle | Inhalt | Modell (R27-D4 = B) | Grund |
|---|---|---|---|---|
| `aufraeumen-migration` | 1 | A1 = B/R27-D2 = B, L11, Teile von L12/C02 | Opus 5.5 | `AfterLoadPool`, Lader, Rückfälle |
| `aufraeumen-archiv` | 1 | A2 = A, D2–D4, W2 | Sonnet 5.5 | mechanisch, `docs/**` und `scan_docs` |
| `aufraeumen-tot` | 2 | K3–K7, K15–K18, K04, C08, #980 P3-3 | Opus 5.5 | Tests auf den echten Weg umstellen |
| `aufraeumen-doppelt` | 3 | K1, K8, K10, K12, K19 (nur neutral), U1–U8, U10, U11, #1019, `.Rest`/`.Einzelpunkte` | Opus 5.5 | Zusammenlegen ohne Verhaltensänderung |
| `aufraeumen-tests-core` | 4 | Konzept §1.2 Core-Tests, #978 | Sonnet 5.5 | Umbenennen, Testliste belegt |
| `aufraeumen-tests-ui` | 4 | Konzept §1.2 UI-Tests, U9, U14 (A5 = A) | Sonnet 5.5 | Umbenennen, Rauchtest |
| `aufraeumen-doku` | 5 | D1 Rest, D5, D6, W1, K13, #980 P3-1/2, #1024, A6 | Sonnet 5.5 | Verweise, Generatorlauf |

Erstreviews: Opus 5.5 für `aufraeumen-migration`, `-tot`, `-doppelt`; Sonnet 5.5 für `-archiv`, `-tests-core`, `-tests-ui`,
`-doku` (R27-D4). Fixläufe im Modell der Spur. Plan-Review Sonnet 5.5. Gesamtreview ChatGPT GPT-6.2 Astra mit Claude-Auswertung.

### Spur `aufraeumen-migration` – alle Migrationen entfernen, nur Schema 65 (Welle 1)

- **Teile:** 0. Stand neu erheben (Wörterbuch `Migrations`, `SaveGame.cs`; Dateien `SaveGame.Migration*.cs`; Testdateien mit
  `Migrat|SchemaVersion|schema[0-9]+-synth`). 1. Alle Schritte `[1]`…`[64]` und ihre Dateien entfernen (R27-D2 = B), Wörterbuch
  und Kommentarliste `SaveGame.cs:54` ff. entfallen bzw. auf einen Satz gekürzt; Lader nimmt nur Schema 65 an, älteres meldet
  „Spielstände mit Schema n können nicht mehr geladen werden.“ (`SaveGame.Reader.cs`); `AfterLoadPool` für `loadedSchema = 65`
  prüfen und vereinfachen; Fixtures `schema{19,22,51,52,53}-synthetisch.ems.gz` und ihre Tests entfernen; 2. Rückfälle aus
  L12/C02, die **nur** Stände vor Schema 65 betreffen, entfernen (je Marker-ID im PR), Rückfälle für Aufbauten ohne
  `NewGame.Create` bleiben; Migrationstests entfernen, Lader-Test „Schema 64 kann nicht mehr geladen werden“; Rundreise Speichern
  → Laden mit Schema 65 bleibt.
- **Dateien:** `src/EishockeyManager.Core/SaveGame*.cs`, Fundstellen der L12/C02-Rückfälle in `src/EishockeyManager.Core/**` (je
  minimal, Liste im PR), Migrationstests, `tests/EishockeyManager.Core.Tests/Fixtures/`.
- **Prüfung:** alle drei Läufe; Rundreise Schema 65 (Zustandsabdruck vor Speichern = nach Laden); Zustandsabdruck Base/Head;
  Markerprobe.
- **Status:** nicht gestartet.

### Spur `aufraeumen-archiv` – Reviews und Arbeitsanalysen ins Archiv (Welle 1, `docs/**` und `funktionskarte.py`)

- **Teile:** 1. `docs/reviews/*` abgeschlossener Runden (bis Runde 26) unverändert nach `docs/archiv/reviews/runde-<n>/`
  (Zuordnung nach dem Rundenarchiv `docs/archiv/runde-<n>.md`, nicht nach dem Datum; Tabelle im PR), Reviews der Runde 27 bleiben; 2. Arbeitsstände nach Konzept
  D2 (`analyse/re-*`, `abgleich-*`, `belastung*` samt Skripten W2) nach `docs/archiv/analyse/`;
  `funktionskarte.py` (`scan_docs`) liest danach zusätzlich `docs/archiv/analyse/` (minimal, sonst fallen deren Adressen aus der
  Karte); **bleiben:** `inventur.md`,
  `restliste-v1*`, `abweichungen-v1.md`, `aufraeumen-v1.md` und jede Analyse, die eine Spur dieser Runde noch braucht (Kopf der
  Spur prüfen); 3. D3, D4 (`bestandsaufnahme.md`: Gültiges in `formate/dateiformate.md` bzw.
  `reverse-engineering.md` übernehmen, Rest ins Archiv; `re-abdeckung.md` bleibt);
  4. relative Verweise **innerhalb von `docs/**`** nachziehen. Verweise aus `src/`, `tests/`, `tools/` (Kommentare) zeigen bis
  `aufraeumen-doku` auf alte Pfade; Liste der betroffenen Stellen (grep) als Datei `docs/analyse/aufraeumen-verweise.md` für
  `aufraeumen-doku`.
- **Dateien:** `docs/**` ohne Planabschnitte von `docs/arbeitspakete.md`; `tools/reverse-engineering/funktionskarte.py` (nur
  `scan_docs`). **Prüfung:** `funktionskarte.py <em.exe> --csv` vor/nach mit gleicher Statusverteilung; Linkprüfung aller relativen Verweise in
  `docs/**` (Skript im PR, nicht eingecheckt); `tools/reverse-engineering/markerliste.py` (ohne `--write`) läuft unverändert
  (liest `inventur.md`). CI entfällt.
- **Status:** Teile 1–4 fertig (PR #1060, 27.11.2026): 284 Reviews (Runden 3–26) nach `docs/archiv/reviews/runde-<n>/` (Fixlauf zu Review P2-1/P3-2: 35 Reviews in die Runde nach Rundenarchiv umsortiert, 4 nach `runden-1-2/`), `analyse/re-*`, `abgleich-*`, `belastung*` samt Skripten nach `docs/archiv/analyse/`, D3/D4 archiviert (Amiga-Abschnitt in `formate/dateiformate.md`), `scan_docs` liest `docs/archiv/analyse/`; Verweisliste für `aufraeumen-doku`: [analyse/aufraeumen-verweise.md](analyse/aufraeumen-verweise.md). Wartet auf Review.

### Spur `aufraeumen-tot` – tote und Nur-Test-Pfade im Core (Welle 2, nach Merge von `aufraeumen-migration`)

- **Teile:** 0. Stand neu erheben (Konzept §1.5 K3–K7, K15–K18 und abweichungen-v1 K04, C08, K11 – Thema K11 der Abweichungsliste deckt sich mit Konzept K6); 1. K3 (Poisson-Weg,
  `MatchStrength`; Feld `ReferenceRating` bleibt im Schema, wird nur nicht mehr gelesen), K16, K17, C08, K04 (tote Wege) entfernen;
  2. K4–K7 und K18 („API ohne Anschluss“, gewollte Testhaken bleiben): Tests auf den echten Weg (`Continue()`,
  `TestSeason.PlayMatchDay`) umstellen, dann Nur-Test-Pfad entfernen (K6 = Thema K11 der Abweichungsliste, soweit nur Tests) – erwartete Werte je Testdatei begründen (R21-D2 = A);
  K15 bleibt (A4 = A, Kommentar genügt); 3. #980 P3-3 (Kommentar `MatchEngine.Managed.cs`, Bedingung +0x0A ≠ 0, Datei
  0x54918–0x54927).
- **Dateien:** `src/EishockeyManager.Core/**` außer `SaveGame*`; `GameState.cs`, `GameState.Integration.cs` minimal (K3); betroffene
  Tests. **Prüfung:** alle drei Läufe, Zustandsabdruck, Markerprobe (entfallene IDs: Liste).
- **Status:** nicht gestartet.

### Spur `aufraeumen-doppelt` – neutrale Zusammenlegung (Welle 3)

- **Teile:** 0. Stand neu erheben (K1, K8, K10, K12, K14-Reste, K19, U1–U8, U10, U11); 1. Core: K8, K10, K12; K19 nur Stellen mit
  gleicher Bedeutung (Helfer je Bedeutung, z. B. `IsManagedClub`/`IsActiveManagedClub`); K1 nur, wenn die Gleichheit per Test
  belegt ist, sonst liegen lassen und im PR begründen; 2. UI/Tools/Formats: U1–U6, U10 (Konstanten in `OriginalSaveGame`), U11;
  U12/U13 liegen lassen („fraglich“); 3. Test-Hilfen U7/U8 einmal in `tests/EishockeyManager.Core.Tests/Support/`, UI-Tests als
  Link-Datei (Muster `GameDataFact.cs`); 4. #1019: Testnaht für den Stromstand nach `FinishSeason` (minimal) und Test zu T03;
  5. nach Spur benannte Core-Dateien (Konzept §1.2): `Manager.Einzelpunkte.cs`, `OriginalImport.Rest.cs`, `OriginalExport.Rest.cs`
  nach Inhalt umbenennen bzw. aufteilen (z. B. `.Finance.cs`, `.Season.cs`), Verhalten unverändert.
- **Dateien:** Core (ohne `SaveGame*`), UI (`Widgets.cs` minimal), Tools, Formats; aus `tests/` nur U7/U8-Dateien, Testprojekte,
  betroffene Tests. **Prüfung:** alle drei Läufe, Zustandsabdruck, Bildschirmfotos bytegleich, Markerprobe.
- **Status:** nicht gestartet.

### Spur `aufraeumen-tests-core` – Core-Tests nach Thema (Welle 4, parallel zu `aufraeumen-tests-ui`)

- **Teile:** 0. Stand neu erheben (Familien Konzept §1.2 auf aktuellem `main`); 1. Familien `Abgleich*`, `KaderStart*`,
  `KaderTransfer*`, `TransferVertraege*`, `Kaderplatz*`, `Integration*`; 2. übrige Familien und Einzeldateien; gleiche Dateinamen in
  Core/UI auflösen (Core-Seite); Herkunft als `Trait("Befund", …)` bzw. Kommentar; 3. #978 (eigener Fall Europapokalfeld
  `[bp+0xA] = 8`, Datei 0x5494C–0x5499E).
- **Dateien:** `tests/EishockeyManager.Core.Tests/**`. **Prüfung:** alle drei Läufe; Testliste alt → neu (gleiche Fallzahl + #978);
  Zeile „Dateiüberschneidung: keine …“ im PR.
- **Status:** nicht gestartet.

### Spur `aufraeumen-tests-ui` – UI-Tests und Abläufe (Welle 4, parallel zu `aufraeumen-tests-core`)

- **Teile:** 0. Stand neu erheben; 1. UI-Familien nach Konzept §1.2, gleiche Dateinamen (UI-Seite), U9 (`Click(ScreenDriver, Rect)`
  in eine Hilfe); 2. Abläufe aus Konzept §1.2 (`FinanzenRest.cs` …) nach Bildschirm umbenennen; 3. U14/A5 = A: Rauchtest jedes
  Ablaufs als `[GameDataTheory]`, gescheiterte Abläufe (2 auf `69e9c40`: `pixel-verein-bundestrainer`,
  `transfer-vertrag-kauf-saisonende`) reparieren, wenn der Ablauf nur den Testaufbau braucht, sonst löschen (Begründung je Ablauf).
- **Dateien:** `tests/EishockeyManager.UI.Tests/**`, `src/EishockeyManager.Tools/Scenarios/**`. **Prüfung:** alle drei Läufe;
  Testliste alt → neu; Bildschirmfotos der umbenannten Abläufe bytegleich; Zeile „Dateiüberschneidung: keine …“.
- **Status:** nicht gestartet.

### Spur `aufraeumen-doku` – Verweise, Themendokus, Markerlisten (Welle 5, zuletzt)

- **Teile:** 1. Verweise aus `src/`, `tests/`, `tools/` auf verschobene Doku nach `docs/analyse/aufraeumen-verweise.md` nachziehen
  (nur Pfadangaben in Kommentaren), Datei danach entfernen; 2. D5 Themendokus (`saison.md`, `tagesablauf.md`, `spielberechnung.md`,
  `transfer.md`, `wettbewerbe.md`, `finanzen-rekonstruktion.md`, `highscore.md`) gegen den Code prüfen, Veraltetes korrigieren
  (Widerspruch zum Code = P3-Issue, nicht still angleichen); D6/#980 P3-1/P3-2; #1024; Klassenlistentext K13 (abweichungen-v1);
  W1 (`markerbericht.py` bleibt, nur Doku); #980 P3-4 ohne Dateiänderung, nur Vermerk im PR; 3. A6 = A: `markerliste.py --write` einmal, Restliste und Abweichungsliste auf neue
  Dateinamen, Diff im PR (keine ID verloren außer den Listen der Spuren).
- **Dateien:** `docs/**`, `tools/**`, Kommentare mit Doku-Pfaden in `src/`/`tests/` (nur die Pfadangabe). **Prüfung:** alle drei
  Läufe (Kommentare in Code), Linkprüfung des ganzen Repos ohne tote Ziele, Markerdiff, `funktionskarte.py <em.exe> --csv`
  vor/nach (Statusverteilung gleich).
- **Status:** nicht gestartet.

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

### Vormerkungen

- **DOSBox-Aufnahme `runde-22` Sitzungen 2–6** und **Sitzung 7** (neue D aus Runde 22: A671, A713/A949, A896, A899, A910, A912,
  A918, A945; Liste nach R23-D3 in `vorlagen/dosbox-aufnahme.md`; Zusätze Runde 26 nach R26-D3 = B): Aufnahme nach Lust, Auswertung
  in der Runde danach, spätestens in der Abschlussprüfung.
- **Abschlussprüfung:** 24 X-Funktionszeilen sind im Generator ausgeblendet (#976) – dort bestätigen. P3 #978, #980 im Aufräumen
  (R24-D2 = B).
- **Vor v1.0 (R20-D3-Zusatz):** Abweichungsliste vorgelegt und entschieden (24.11.2026, Runde 24).
- **Letzter Schritt vor v1.0 (Nutzer 17.11.2026, #936):** Aufräumen des gesamten Repos, danach nur noch die Abschlussprüfung.
- **Testlücke Gesamtreview #848:** Speichern/Laden mitten in einer angehaltenen Live-Paarung (über die Oberfläche nicht
  erreichbar; prüfen, sobald ein Speicherweg im Live-Spiel dazukommt).
- **Mit v1.0 und dem bitgenauen Zufallsmodus (N6 = A, Nutzer 16.11.2026):** eigene Zufallsquellen der Darstellung (A061, A271,
  A274, A438, A452) erneut prüfen.
- **Nach v1.0:** Einstellungen aus R15-E3, bitgenauer Zufallsmodus (`anforderungen/zusatzfunktionen.md` C), Sprachen und
  Währungen (N1 = B, `anforderungen/zusatzfunktionen.md` A 2).

## Paket `hok-format` – Original-Spielstände `*.HOK`

Branch: `paket/hok-format` · Status: Analyse gemergt (#176, Review #178): Verschlüsselung, Prüfsummen, Kopf und Blockfolge
belegt, `emtool hok dump`, Doku `docs/formate/spielstand-original.md` (offene Liste dort). Entscheidung Nutzer:
Sicher-/Ladesperre des Originals nicht übernehmen. Weiter in Runde 7 mit `hok-patch` (Spielstände fürs Original);
Import/Export ins Remake danach.

- Nachtrag (#197, Branch `paket/hok-format-2`): Kaderplätze, Spielerpool, Training und Taktik an FILE05 belegt;
  Kaderausgabe in `emtool hok dump`. Spielstände FILE01–05 und `KAPUTT.HOK` im Referenz-Repo `original/spielstaende/`.

## Paket `wettbewerbe` – Paarungen, Stärken, Statistiken, Pokale

Branch: `paket/wettbewerbe` · Status: PR 1–7 gemergt (Runde 3); Restarbeiten in Runde 4 gemergt; PR 8 in Runde 5 gemergt (`paarungen-liga`, #128). Regeln und Belege: [wettbewerbe.md](wettbewerbe.md)

| PR | Inhalt | Stand |
|---|---|---|
| 1 | DEB-Pokal: Auslosung, Heimrecht, „best of three“, Abrechnung, Bildschirm „DEB-Pokal Auslosung“ | gemergt (Runde 3, #54) |
| 2 | Europapokal (Gruppe + Endrunde), Bildschirm Gruppen/Endrunde | gemergt (Runde 3, #54) |
| 3 | WM (2 Gruppen + K.-o.), Bildschirm Gruppe/Tabelle WM | gemergt (Runde 3, #54) |
| 4 | Stärken (Ligen, Länderteams) | gemergt (Runde 3, #54) |
| 5 | Statistiken: Ewige Tabelle (Totale), Früher | gemergt (Runde 3, #54) |
| 6 | Bundestrainer: WM-Kader, „WM Vorbereitung“ (Freundschaftsspiele), Etat; WM: Werte Deutschlands neu ausgewürfelt, Länder 20-mal getauscht | gemergt (Runde 3, #54) |
| 7 | Spielmodus „nur Weltkugel“: Spielvorbereitung, Vereinssaison überspringen, Menü des Bundestrainers | gemergt (#87 über #89) |
| 8 | Paarungen Liga | gemergt (Runde 5, #128); Pixelvergleich Runde 6 (`pixel-wettbewerbe`) |
| – | Statistiken Spieler, Verein, Fairplay, WM-Statistik | Runde 8 (`statistiken`, `nationalteam`) |

- Paarungen (Liga, Playoffs, DEB-Pokal, Europapokal, WM), Freundschaftsspiele.
- Stärken (Ligen, Länderteams: Gesamt/Abwehr/Sturm, Smileys), Statistiken (Totale, Früher, Verein, Spieler, Fairplay, WM).
- DEB-Pokal (32 Teams), Europapokal (Gruppe + Endrunde), WM (2 Gruppen + K.-o.).
- Eigene Dateien: `Core/Cups*.cs`, `Core/Statistics*.cs`, `UI/Screens/Competitions/*`, `UI/Screens/Statistics/*`.

## Paket `speichern` – Laden und Sichern

Branch: `paket/speichern` · Status: Format, Dialog und automatisches Speichern gemergt (Runde 3, [archiv/runde-3.md](archiv/runde-3.md)); #80 in Runde 4 behoben (#105)

- Eigenes Spielstandformat, Laden/Sichern-Dialog wie im Original (Dateiliste, Verzeichnis, Dateiname).
- Optional: Import von Original-Spielständen `*.HOK` (Verschlüsselung in `dateiformate.md`) und Export per Vorlage (`hok-format`) – wartet auf Original-Spielstände des Nutzers.

## Später (nach v0.5)

- `livespiel` – Anzeigetafel, Reporter, Torszenen auf den Ereignissen aus `spielberechnung` (optional abschaltbar, letzter Schritt vor v1.0)
- Zusatzfunktionen nach `docs/anforderungen/zusatzfunktionen.md`
