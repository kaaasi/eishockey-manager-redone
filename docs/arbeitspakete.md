# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 11 – Umsetzung der Inventur (ab 02.11.2026)

Runde 10 (Sonderrunde Analyse em.exe): [archiv/runde-10.md](archiv/runde-10.md); ältere Runden in [archiv/](archiv/).
**Ziel der Runde (Entscheidung Nutzer 01.11.2026, normale Runde):** die priorisierte Liste in
[analyse/inventur.md](analyse/inventur.md) („Runde 11: priorisierte Liste“, P1 9 · P2 16 · P3 6) umsetzen; jeder Eintrag
und jedes offene Issue aus Runde 9/10 ist genau einer Spur zugeordnet. **`livespiel` (L1–L8, 575C3h) bleibt
zurückgestellt.** Beschlossene Regeln aus re-geld.md: Stadionausbau über das Finanzbuch „Bau“ (E1); Leihen beim
Mitmanager lokal sofort wie im Original (E2, Netzwerk-Anfrage später).
**Schreibweise in diesem Abschnitt:** Adressen sind Dateioffsets mit Suffix `h`, damit die Funktionskarte den Plan nicht
als Fundstelle zählt (R10P-01). Im Code und in `docs/analyse/` gilt die normale Schreibweise.
**Base für alle Spuren:** aktueller `main`; tatsächlichen Base-SHA in den Draft-PR. Weiterhin verbindlich: erst Befund
reproduzieren, roter Regressionstest mit Fundstelle, dann grün; Mehrsaisonprobe (4 Wechsel, passiv und aktiv) bei
Saisonwechsel, Kader oder Geld; Schema mit Migration in neuer Datei `Core/SaveGame.Migration.<Spur>[<n>].cs` (vorläufiger
Eintrag in `Core/SaveGame.cs` erlaubt, die Leitstelle nummeriert; Stand Schema 22); jede Fundstelle `[ORIGINAL]`/`[APPROX]`
mit `Datei 0x…` bzw. Stub-Schreibweise; geänderte Abdrücke und Referenzwerte je Fall begründen; Befund-Issues mit
`Fixes #<nr>`; Inventur-IDs (A…, F…, G…, M-K…, S-K…) im PR nennen. **Entwickler und Reviews sind Claude-Sitzungen**
(geplante Läufe); ChatGPT nur für Reviews.

### Ablauf: Teile und Selbstfortsetzung (wie Runde 9)

- Jede Spur hat eine **geordnete Liste von Teilen**; jeder fertige Teil ist ein eigener PR (Branch `paket/<spur>-<n>`) mit
  Übergabe. Stapelregel, PR-Base, Rebase und Fortsetzung: [vorlagen/spur-start.md](vorlagen/spur-start.md), Abschnitt
  „Teile und Fortsetzung“. **Höchstens drei Fortsetzungen je Spur**; danach oder bei einem blockierten Teil entscheidet der
  Nutzer (Label `spur-wartet`). Ein Teil nur mit grünem `tools/pruefen.sh --schnell`.
- **Ausnahme `re-nachtrag`:** ein Lauf, keine Teile, keine Fortsetzung, nur Doku (Regeln wie Runde 10,
  [archiv/runde-10.md](archiv/runde-10.md), „Regeln der Runde“).
- **Abhängigkeit:** Teile mit „nach `re-nachtrag`“ beginnen erst, wenn dessen PR gemergt ist; sonst gilt der Teil als
  blockiert (`spur-start.md`, Teile und Fortsetzung, Schritt 4) – vorher die übrigen Teile der Spur.
- Integration **je Spur am Ende** (alle Teile). Folge: `geld` → `saisonwechsel` → `kader-transfer` →
  `wettbewerbe-spiel` → `ui-meldungen` → `marker-werkzeug`. Werden `BoerseStartTests`/`FinanceWerbungTests` durch
  geänderten Zufallsverbrauch rot, nennt die Spur das im PR; diese Dateien ändert nur die Leitstelle.

### Zuständigkeit für gemeinsame Dateien

- `Core/Finance*.cs`, `Core/Transfer.Talent.cs`, `UI/Screens/Manager/*`: allein `geld`.
- `Core/Transfer*.cs` außer `Transfer.Talent.cs`, `Core/Player*.cs` (Ausnahme unten), `Core/Squad.Setup.cs`,
  `Core/NewGame.cs`, `UI/Screens/Team/*`: allein `kader-transfer`. `Core/Player.Factory.cs` ändert `geld` nur für die
  Talentwerte (Teil 6, im PR benannt).
- `Core/Season.cs`, `Core/SeasonFlow.cs`, `Core/SeasonRecord.cs`, `Core/SeasonDismissal.cs`, `Core/StatisticsHistory.cs`,
  `Core/Playoffs.cs`: allein `saisonwechsel`. **Ausnahme:** `ui-meldungen` ändert in `Core/Season.cs` nur das Feld `Months`
  (Monatstabelle, im PR benannt; R11P-01).
- `Core/MatchEngine*.cs`, `Core/Squad.Strength.cs`, `Core/Squad.Tactics.cs`, `Core/Squad.Training.cs`,
  `Core/Squad.Lineup.cs`, `Core/Cups*.cs`, `Core/MeetingHistory.cs`, `Core/Statistics{Strength,Club,Players}.cs`,
  `UI/Screens/Competitions/*`, `UI/Screens/Statistics/*`: allein `wettbewerbe-spiel`. **Ausnahme:** das Feld Belastung
  (Spielerplatz +13h) legt `wettbewerbe-spiel` in `Core/Player.Attributes.cs` an (nur dieses Feld, im PR benannt).
- Neue Meldungsbausteine unter `UI/`, `UI/Screens/SeasonEnd*.cs`, neue Dateien für Infofile und Tabellenkurve: allein
  `ui-meldungen`. **Ausnahme `MainMenuScreen.cs` (R11P-02):** `ui-meldungen` lagert `DrawRankCurve` in eine eigene Datei
  (z. B. `UI/Screens/RankCurve.cs`) aus und schließt in `MainMenuScreen.cs` nur den Aufruf und den versteckten Klick
  (Infofile) an; sonst nichts in dieser Datei. Aufrufer in fremden Dateien stellt `ui-meldungen` **nicht** um, sondern listet sie
  im PR; die Leitstelle setzt sie in der Integration. Bis dahin nutzen die anderen Spuren die vorhandenen Dialoge.
- `tools/reverse-engineering/*`, `docs/re-abdeckung.md`, `docs/analyse/inventur.md`: allein `marker-werkzeug`.
- `Core/GameState.Integration.cs`, `Core/SaveGame.cs`, `MainMenuScreen.cs`: nur minimal und begründet; Anschlüsse in fremde
  Dateien nennt die Spur im PR, die Leitstelle setzt sie in der Integration. Neue Tests in eigenen Dateien; Sollwerte von
  Referenz- und Mehrsaisontests nur je Fall begründet ändern.

### Spur `re-nachtrag` – Analysebedarf Gruppe d (zuerst)

- **Inhalt:** Tagesablauf 7169h/7358h/736Eh (Schreiber Kaderplatz +18h Bit 80h, Ankündigungen, Angebote der Computervereine;
  P1 Nr. 9); automatische Aufstellung 426BCh/42838h (A068, A165, A169, A190, A013); Liga-Spielplan 4E7C7h (A113, A282);
  Schreiber des Preisindex (A183); Startwert Spielerplatz +19h (A185, A186); Europapokal-Gegner und Freundschaftsspiele
  (A004, A008, A110); Verein ohne Liga beim Start (A112, R-K7, 45CD0h); Penalty-Zeilen DGROUP 5C4Bh (A077).
- **DOSBox-Spielstandpaar** (A085, A088, A091, A125): Saisonwechsel vorher/nachher mit `emtool hok`; Aufnahme durch den
  Nutzer. Die Spur beschreibt den Ablauf; fehlt das Paar, bleibt es begründet offen.
- **Abnahme:** wie Runde 10 „Abnahme je Spur“ (Selbstprüfung `funktionskarte.py --csv` ohne `--write`); Datei
  `docs/analyse/re-nachtrag.md` mit Abschnitt „Umsetzung“ je Stelle (Ziel, Dateien, Testidee, Spur dieser Runde).
- **Dateigrenzen:** nur `docs/analyse/re-nachtrag.md` und der eigene Statuspunkt. **Modell:** Claude Opus 5.5. **Status:** –

### Spur `geld` – Stadion, Jugend, Bank, Sponsoren, Finanzbedienung

- **Teile:**
  1. **Stadionausbau (G1, P1 Nr. 1):** Kosten, Bauzeit `random(lo, hi)` + Restwochen, parallele Baustellen, Platzgrenze,
     wöchentlicher Fortschritt (3D7F8h/3D316h); Buchung im Finanzbuch „Bau“ (E1). A041–A043, A047, F090.
  2. **Sponsorbewertung (P1 Nr. 4):** Ø-Platz der letzten 5 Spieltage der laufenden Saison (3F928h–3F962h, 43BEDh), Summe
     0 → 6, Randfall n ≤ 5 (Test laut Inventur). A036. **Sponsorangebote auch bei laufendem Vertrag** (P2 Nr. 14, A039).
     **Monatseinnahme aus Teamwerten** (P1 Nr. 5, A035).
  3. **Jugendteams (G2, P1 Nr. 2):** Stärke und Platz je Ligaspieltag (39DECh), Auf-/Abstieg im Saisonwechsel (3A13Bh,
     Anschluss über die Leitstelle), Startliga aus `ManagerSetup.StartLeague` (#327, 29BBBh). A049, F085, F086.
  4. **Bank und Börse (G3, P1 Nr. 3):** Kreditrahmen 2 Mio. DM, 1 Mio. DM zwischen Managern, Gebühr 1,5 %. A033.
  5. **Finanzbedienung (G4, P2 Nr. 10):** Betragsbalken 58409h, Leihen beim Mitmanager lokal sofort (E2). A034, A312,
     A315, A316, F158.
  6. **Talentsuche (R-K1, P2 Nr. 7):** Start aus der Transferliste (465F1h), Takt, Erfolg, Grenze 28 mit Leihspielern
     (3CCEBh), Talentwerte 6D7Fh. A127–A132, A242–A246, A328, A050, A118, A120, F011, F012, F089. Der Knopf in
     `UI/Screens/Team/TransferScreen.cs` ist Anschluss für die Leitstelle (im PR benannt); Bildschirmfoto-Abnahme nach der
     Integration (R11P-04).
  7. **Probe:** Ausweg der Finanzprobe (#352).
- **Abnahme:** je Regel roter Test, danach grün; Mehrsaisonprobe (Geld) je Teil mit Geldwirkung; Jugend: Platzverlauf mit
  festem Zufall über 4 Saisonwechsel.
- **Dateigrenzen:** siehe Zuständigkeit; `docs/finanzen-rekonstruktion.md`, `docs/analyse/re-geld.md` (nur Status), Tests.
- **Modell:** Claude Opus 5.5. **Status:** –

### Spur `saisonwechsel` – Oberliga, Zwangsabstieg, Ewige Tabelle

- **Teile:**
  1. **Oberliga-Austausch und Managerverein (S-K1, S-K2, #166, 562C6h):** DOSBox-Beleg #389; A162, A197.
  2. **Zwangsabstieg (S-K3):** Bit 04h, Konto < −666.666,67 DM, Einschub in die Aufstiegsrunde; Schemafeld. Test
     −700.000/−600.000 DM.
  3. **Ewige Tabelle mit den Punkten der neuen Liga (S-K4, P2 Nr. 13):** Aufsteiger Platzfeld 1 → 51, Absteiger
     Platzfeld 10 → 22.
  4. **Proben und Altlasten:** Startordnung bei geladenen Spielständen ab Saison 2 (#329, `Season.StartOrder`);
     Regressionstest Play-offs mit allen Feldern der Meldungsmatrix (#362).
- **Abnahme:** Mehrsaisonprobe 20 Saisons (Modus 1, 2, 4; passiv/aktiv); je Regel roter Test.
- **Dateigrenzen:** siehe Zuständigkeit; `docs/analyse/re-saisonwechsel.md` (nur Status), Tests. **Modell:** Claude Opus 5.5.
  **Status:** –

### Spur `kader-transfer` – Kader, Transfermarkt, Verträge, Pool

- **Teile:**
  1. **Kadergröße nach Start-Liga (P1 Nr. 7):** DGROUP 169h mit `[11Ah] · 4 + Gruppe` (29A3Fh); `ManagedSquadSize` und
     Marker berichtigen. A170.
  2. **Spielerdaten im Modell (P2 Nr. 8, ohne Belastung +13h):** Kaufpreis +2Ch, Versicherung +30h, Verletzungsart +17h,
     Sperre in Minuten +0Dh (41ACh); Hinweiszeile. A066, A092, A359, A360, A367–A372. Die Anzeige der neuen Felder in
     `UI/Screens/Statistics/PlayerStatisticsScreen.cs` ist Anschluss für die Leitstelle (im PR benannt; R11P-03).
  3. **Transferfolgen beim Computerverein (P1 Nr. 6):** Teamwerte nach Verkauf (58DCh), Sponsorzuschuss nach Kauf vom
     Computermarkt (63BEh, Regel vorher gegenlesen). F005, F006.
  4. **Transfermarkt-Takt (P2 Nr. 4):** Listenwechsel täglich (54E71h), bietender Verein (59B8h), Rückfrage an den Verkäufer
     (47608h–47743h), Start-Tendenz `random(35, 65)` (4907Ah), Kontoleiste (G5, 463CCh). A251, A256–A258, A381, A196,
     A209, F109, F113.
  5. **Vertrag am Saisonende vor dem Zählerreset (P2 Nr. 5, A226)** und **Poolumverteilung (M-K7, M-K8, P2 Nr. 6):** 57E3h,
     7C7Eh/7EF1h/5521h, Drift 54D9h (F002–F004, F013–F015); #348 (Belegung Pool +15h/+16h).
  6. **Proben:** Lockerungen (#353), weitere Mutationen ohne roten Test (#347), Kommentar der Verhandlungsprobe (#364),
     Schwelle `renamedClashes` (#357, `TransferVertraegeKomplettTeil5Tests.cs`; R11P-05).
  7. **Ankündigungen der Kaderspieler (P1 Nr. 9) – nach `re-nachtrag`:** A136, A229–A232, A238, A239.
- **Abnahme:** je Regel roter Test; Goldtest aus FILE03/05/07; Mehrsaisonprobe (Kader, Geld); Spielstand-Rundlauf der
  neuen Felder.
- **Dateigrenzen:** siehe Zuständigkeit; `docs/transfer.md`, Tests. **Modell:** Claude Opus 5.5. **Status:** –

### Spur `wettbewerbe-spiel` – Spielberechnung, Pokale, Nationalteam

- **Teile:**
  1. **Pokal- und WM-Gleichstand (P2 Nr. 3):** schneller Weg `random(0, 2)` bis ungleich, Sieger Heim + 30 (376F0h);
     Minutenweg mit Penaltyschießen (3901Bh). A003, A032, A275, A277.
  2. **Begegnungstabellen für alle Wettbewerbe (P2 Nr. 11, 50283h):** A111; Bundestrainer-Zweige mit Test (#344).
  3. **Nationalteam-Stärke (P2 Nr. 2, 368B1h):** A011, A014, A015, A019, A022, A024, A025, A030, A031, A079, A084.
  4. **Teamwerte in Stärketabelle (P1 Nr. 5):** A200, A201; Über-/Unterzahl-Zähler der Vereinsstatistik (P2 Nr. 16,
     A349).
  5. **Taktik und Spielstärke (P2 Nr. 1, G6):** Minuten je Teil ≤ 20, Einsatz-Zuschlag, Belastung +13h als eigenes Feld,
     „eingesetzt“ wirkt (48E62h). A168, A174, A176–A178, A182, A184, A089, A090, A070.
  6. **Doku und Marker:** Verteilungstabellen `docs/spielberechnung.md` §15 auf den integrierten Stand (#354); `[ORIGINAL]`
     für Code 10 mit Vorbehalt (#358).
  7. **Versteckte Fähigkeiten +19h (P2 Nr. 15) – nach `re-nachtrag`:** A185, A186.
- **Abnahme:** je Regel roter Test; Saisonverteilung (Meister, Tore, Strafminuten) im PR; Mehrsaisonprobe Modus 1, 2, 4;
  Referenzwerte begründet.
- **Dateigrenzen:** siehe Zuständigkeit; `docs/spielberechnung.md`, `docs/wettbewerbe.md`, Tests.
  **Modell:** Claude Opus 5.5. **Status:** –

### Spur `ui-meldungen` – Meldungsbausteine, Infofile, Tabellenkurve

- **Teile:**
  1. **Meldungsbausteine (M-K1, M-K2, M-K3, P2 Nr. 9):** Meldung/Frage 4C246h, Kurzmeldung C0B8h (Bundestrainer-Warnung
     ohne Knopf), 4BCCCh. A270, A273, A298, A304, A308, A375; Aufruferliste für die Integration.
  2. **Infofile (M-K5, 432ECh/43596h)** und **Tabellenkurve (M-K6, 445E0h):** F099, F100, F106, A302, A303.
  3. **Kosmetik:** Monatsnamen (P3 Nr. 3, A157, vorher gegen das Video prüfen); letzte Saison ohne Saisonende-Fenster
     (S-K5, P3 Nr. 5, nach Video); Kommentar über dem Rechtsklick-Test (#342).
- **Abnahme:** Bildschirmfoto je Baustein bzw. Aufrufer gegen das Referenzbild; Bildschirme zustandsrein.
- **Dateigrenzen:** siehe Zuständigkeit; Tests. **Modell:** Claude Sonnet 5.5. **Status:** –

### Spur `marker-werkzeug` – Funktionskarte, Doku, Marker

- **Teile:**
  1. **Funktionskarte:** Adressen mit folgendem Doppelpunkt erkennen (#401, Code-Teil von R10N-01); `docs/analyse/inventur.md`
     als Fundstelle ausnehmen (#406); Befunde R-1 bis R-4 und R-6 zu `disasm.py`/README (#414, R-5 erledigt);
     Karte mit `--write` neu.
  2. **Doku:** R10A-01 (Tabelle 2 und P3 Nr. 1 in `inventur.md`), R10A-02 (Kopfsatz Archiv Runde 10), R10A-03
     (`re-saisonwechsel.md`), Hinweise des Reviews außerhalb des Diffs (`re-abdeckung.md` „Einordnung“,
     `reverse-engineering.md` FILE13-Beleg); Doku GameOptions/PlayWithEvents (#363).
  3. **Marker ohne Codeänderung (P3 Nr. 1)** – erst nach der Integration der übrigen Spuren (Leitstelle legt den Lauf an):
     A026, A044, A139–A141, A264, A265, A276 und die Funktionen „vorhanden“ mit P3 der Tabelle 2; ein Sammel-PR je Schicht.
- **Abnahme:** Gegenbeispiel R10N-01 (Mutation `perl -pi` aus dem Review #408) ohne Statuswechsel; `funktionskarte.py
  --write` danach ohne Diff; für Teil 3 je Marker der Abgleich mit der Doku im PR.
- **Dateigrenzen:** siehe Zuständigkeit; die genannten Doku-Dateien; in Teil 3 nur Kommentarzeilen in `src/`.
  **Modell:** Claude Sonnet 5.5. **Status:** Teil 1 fertig im PR (Branch `paket/marker-werkzeug-1`); Teile 2–3 offen.

### Nicht in Runde 11

- P3 Nr. 2 (Pixel nach Analyse: G7, G8, M-K4, M-K9, R-K2…R-K5, L8), Nr. 4 (M-K10, R-K6 – Entscheidung Nutzer) und Nr. 6
  (S-K6, nur Analyse): Kandidaten für Runde 12.

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- `livespiel` (Torszenen, Anzeigetafel, Reporter; Nr. 14/16): Umsetzung als letzter Schritt vor v1.0; Analyse in
  `re-livespiel`. Noch unbekannt und bei der Umsetzung von `livespiel` mitzuanalysieren (zusammen mit L7, `tore\`-Format):
  575C3h (Ov 138h, Lader `tore\`, einziger Aufrufer 567C8h; R10R-01 #387).
- #349 Spieltag mit Managerverein ganz in der Minutenrechnung: Remake-Verhalten bleibt; Original als Einstellung nach
  v1.0 (`docs/anforderungen/zusatzfunktionen.md`, Abschnitt C).
- Import/Export von Original-Spielständen ins Remake; Highscore am Spielende und Saisonende im Modus „nur Weltkugel“
  (Kandidat für eine DOSBox-Aufnahme mit `hok-patch`).
- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

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
