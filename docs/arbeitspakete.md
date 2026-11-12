# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 17 – Restlücken außerhalb des Live-Spiels (Plan 12.11.2026, Go 12.11.2026)

Runde 16 abgeschlossen am 12.11.2026: [archiv/runde-16.md](archiv/runde-16.md). Grundlage: offene Issues #730, #773 (P2/P3,
DOSBox nötig), #784, #785 (Testlücken), Inventur `analyse/inventur.md` (offene `[APPROX]` der Gruppen a/c/d mit Prio P2) und
Funktionskarte `re-abdeckung.md`: Spielcode zu 91 % umgesetzt; fehlende Funktionen gehören bis auf F107 (R-K7) alle zum
Live-Spiel (L1–L8, `analyse/re-livespiel.md`). Diese Runde schließt die übrigen P2-Lücken, damit danach das Live-Spiel und die
Abschlussprüfung folgen können.

**Base für alle Spuren:** aktueller `main` (Planstand `3a8070c`); tatsächlichen Base-SHA in den Draft-PR. Regeln wie Runde 16
(Archiv): ein Branch `paket/<spur>`, ein PR, Teile als Commits, höchstens drei Fortsetzungen; **je Inventur-ID zuerst prüfen,
ob die Zeile noch offen ist** (Stand-Vermerke, Code), erledigte IDs nur im PR nennen; Befund reproduzieren, roter
Regressionstest mit Fundstelle, dann grün; Mehrsaisonprobe bei Saisonwechsel, Kader oder Geld; Schema-Änderung mit Migration in
neuer Datei `Core/SaveGame.Migration.<Spur>.cs` (Leitstelle nummeriert beim Merge); `[ORIGINAL]`/`[APPROX]` mit `Datei 0x…`;
fremde Tests nach R15-E2 = C; keine geplanten Aufgaben aus Läufen (R15-E4). Bei Unklarheit „wie im Original“; bleibt es
unklar, Punkt als `[APPROX]` mit Begründung belassen, die Leitstelle bündelt die Fragen. Live-Anzeige gehört **nicht** in
diese Runde (Stellen mit „Spur livespiel“ bleiben). **Entwickler und Erstreviews sind Claude-Sitzungen**; ChatGPT nur fürs
Gesamtreview.

**Folge:** Welle 1 parallel `wettbewerbe-rest`, `kader-rest`, `ui-rest`, `pruefung-karte` Teil 1, `re-livespiel-2`; Merge in der Folge
`pruefung-karte` Teil 1 → `re-livespiel-2` (nur Doku) → `wettbewerbe-rest` → `kader-rest` → `ui-rest` (Bildschirme zuletzt, weil sie Core-Werte zeigen);
danach Welle 2 `pruefung-karte` Teil 2 (eigener Branch `paket/pruefung-karte-2`), am Rundenende Gesamtreview.

### Zuständigkeit für gemeinsame Dateien

- `Core/Cups*.cs`, `Core/MatchEngine.cs`, `Core/MatchEngine.Live.cs` (nur Nationalteam/Freundschaftsspiel und A077),
  `Core/NewGame.cs`, `Core/Season.cs`/Spielplan, `Core/Playoffs.cs`, `Core/FinanceSponsorRating.cs`,
  `Core/FinanceStadium.cs` (nur Liga-Code A045): allein `wettbewerbe-rest`.
- `Core/Transfer*.cs`, `Core/Player*.cs`, `Core/MatchEngine.Injuries.cs`, `Core/StatisticsClub.cs`, `Core/FinanceTalent.cs`,
  für das Ereignissymbol 9 auch `Core/SeasonFlow*.cs`/`GameState.SeasonEndTurn.cs`: allein `kader-rest` (R788-03).
- `UI/Screens/**`: allein `ui-rest`; braucht `ui-rest` einen Core-Wert, den eine andere Spur ändert, nennt es die Stelle im PR,
  die Leitstelle löst beim Merge auf. Umgekehrt nennen Core-Spuren Bildschirmfolgen im PR statt den Bildschirm zu ändern.
- `Core/OriginalImport*.cs`, `Core/OriginalExport*.cs`, `tests/**/AbgleichOriginalPaareTests.cs`: jede Spur nur die Stellen
  bzw. erwarteten Einträge ihrer IDs, im PR genannt.
- `Core/GameState.Integration.cs`, `GameState.cs`: höchstens minimal, im PR begründet.
- `docs/reverse-engineering.md`: je Spur ein eigener Abschnitt, die Leitstelle löst beim Merge auf (R788-04).
- `docs/analyse/inventur.md`, `docs/re-abdeckung.md`, `tools/reverse-engineering/*`, `tests/**/Support/`: allein
  `pruefung-karte` (Teil 2); die anderen Spuren nennen ihre Inventurzeilen im PR.

### Spur `wettbewerbe-rest` – Wettbewerbe, Nationalteam, Spielplan

- **Teile:**
  1. **Nationalteam/Länder:** A008/A110 (Länder-Freundschaftsspiele: Rechenweg des Originals statt Poisson bzw. WM-Gruppenspiel,
     Freundschaftsspiel 0x30365), A084 (Reihenwerte/Energie und Strafen-Managerwert 20 des Nationalteams), A024 (Automatik bei
     leerer Formation).
  2. **Europapokal und Liga-Code:** A004 (Gegnerstärke: Sturmwert, Formabschlag −5), A045 (Liga-Code ausländischer Vereine),
     A037 (Teilnahmebits Masken 0x40/0x08/0x20, Rücksetzen nach Ausscheiden; Leser 0x44A48), A077 (Penaltyschießen im
     Minutenweg je Wettbewerb, Zeilen DGROUP 0x5C4B; R788-01).
  3. **Spielplan und Start:** A113 (Spielplanfunktion 0x4E7C7 lesen, Spielplan aller Ligagrößen wie im Original), A112/F107
     (Verein ohne Liga, Filter DEB-Paarungen 0x45CD0 beim Zufallsverein, R-K7), A142 (Termine der Aufstiegsrunde aus den
     Kalendermarken, sonst Video `folge06_01257`/`folge07_03307`).
- **Abnahme:** je ID roter Test → grün mit Fundstelle oder begründetes `[APPROX]`; Mehrsaisonprobe (4 Wechsel) für Teil 3;
  Abgleich FILE-Paare unverändert oder erwartete Einträge begründet angepasst.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** Go 12.11.2026, Start nach Merge des Plans (#788).

### Spur `kader-rest` – Spieler, Transfer, Verletzungen

- **Teile:**
  1. **Spielerwerte und Laufbahn:** A130 (Startwerte ±2, Form, Energie, Gehalt), A232/A238 (Höchstalter, Rücktritt je Alter),
     A066 (Verletzungsart, Wochen, Form vom Pool).
  2. **Transfermarkt:** A251 (Takt des Listenwechsels, bietender Verein, Antwort menschlicher Verkäufer 0x47655–0x47743), A253
     (Mindestbestand je Art, Verkaufszweig 0x46E04–0x46FCF), A226 (Verhandlung am Saisonende, Rest), F089 (Grenze 28 der
     Talentsuche inkl. verliehener, 0x3CCEB), A244/A245 (Starttext und laufende Suche, 0x465F1).
  3. **Geld und Saisonende:** A196 (Schulden gesamt je Partner, 0x463CC/0x57768); Ereignissymbol 9 im Fenster „Saisonende“
     (`FinanceTalent.cs:177`, Core-Teil; die Anzeige macht `ui-rest`).
- **Abnahme:** wie oben; Mehrsaisonprobe passiv und aktiv für Teile 1–2.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** Go 12.11.2026, Start nach Merge des Plans (#788).

### Spur `ui-rest` – Bildschirmlücken ohne Live-Anzeige

- **Teile:**
  1. **Ligapaarungen (A280/A282):** Seiten „Play-Off“ und „Aufstieg“ am mittleren Knopf, Klick auf eine Zeile → Vereinsinfo,
     zuletzt gewählte Seite (DGROUP 0x5125); Bildschirmfoto gegen Referenzbilder.
  2. **Einzelstellen:** A332 (Lampe 1, Spielerfeld 0x1E), A360 (Spielerstatistik: Kaufpreis statt Spielerwert), A363 (Absage
     der Vertragsverhandlung), Hintergrund des Vertragsbildschirms (`file46_3`, `ContractsScreen.cs:81`), Ereignissymbol 9 im
     Fenster „Saisonende“ (nur Zeichnen; den Core-Teil macht `kader-rest`, R788-03).
- **Abnahme:** Bildschirmfoto-Abläufe je Bildschirm, Pixelvergleich mit Fundstelle des Referenzbilds, Bedienweg per Test.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** Go 12.11.2026, Start nach Merge des Plans (#788).

### Spur `re-livespiel-2` – Analyse-Nachtrag Live-Spiel (R17-D2 = B)

- **Teile:**
  1. **Eingriff (L6):** Eingriffsmenü Datei 0x38D96 (Mausweg in der Managerzeile: Einsatz, Angriff, Modus, Reihe), Treffer-Tests
     0x534BC/0x53508, Neuaufbau 0x5434C; Ergebnis als Abschnitt in `analyse/re-livespiel.md`.
  2. **Torszenen-Lader (L7):** 575C3h (Ov 138h, einziger Aufrufer 567C8h), Szenenwahl `138h:25h`, Format `tore\` so weit
     lesbar; Umsetzungsplan L1–L8 für Runde 18 (Teile, Dateien, Abnahme) am Ende von `analyse/re-livespiel.md`.
- **Dateigrenzen:** nur `docs/analyse/re-livespiel.md`, `docs/reverse-engineering.md` (eigener Abschnitt); kein Code.
- **Abnahme:** jede Lesart mit Fundstelle und ausführbarem Beleg (`ndisasm`-Befehl, Bytes), Funktionskarte führt 575C3h danach
  nicht mehr als „unbekannt“ (Probe mit `funktionskarte.py`, Schreiben macht `pruefung-karte` Teil 2).
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5 (Disassembly). **Status:** Go 12.11.2026; in Arbeit (Claude Code, geplanter Lauf 12.11.2026), Teil 1 begonnen.

### Spur `pruefung-karte`

- **Teil 1 (sofort, kleiner PR):** #784 (exakter Einsatz 40/60 je Computerverein im #772-Test, Managergegner), #785 (Kontoanzeige
  #768 für weitere Manager und Bundestrainer); Rückmutation je Test.
- **Teil 2 (nach dem Merge von `ui-rest`, Branch `paket/pruefung-karte-2`):** Inventur „Stand Runde 17“ und Funktionskarte
  (`--write`, zweiter Lauf ohne Diff), Inventurzeilen aller Spuren, veraltete Zeilen A092/A370 nachziehen (R788-02),
  gesammelte P3 der Runde.
- **Modell:** Claude Opus 5.5 (Teil 1 Claude Sonnet 5.5 möglich: reine Testergänzung). **Erstreview:** Claude Opus 5.5.
  **Status:** Go 12.11.2026, Start nach Merge des Plans (#788). Teil 1 fertig (PR #794, Review offen): #784 mit Fund
  „Gast der Spielvorbereitung nach dem Heimverein“ (0x8365, Datei 0x36D12/0x36D49) behoben, #785 belegt (`duell_s2c`,
  `eisbaeren_f02`).

### Gesamtreview am Rundenende

ChatGPT (Codex mit Netzzugang) nach `vorlagen/review-anfrage.md`, risikoorientiert (`prozess/anhang.md` C). **Modell:** GPT-6.1 Terra.

### Entscheidungen zum Plan (Nutzer 12.11.2026, PR #788)

**Ergebnis:** R17-D1 = A (vier Spuren), R17-D2 = B (zusätzlich `re-livespiel-2`, Umsetzung ab Runde 18), R17-D3 = A
(Aufnahmeliste `runde-17` in `vorlagen/dosbox-aufnahme.md`); Modelle wie in den Spurabschnitten.

**R17-D1 – Zuschnitt.** Problem: Außer dem Live-Spiel bleiben rund 25 offene P2-Stellen in Core und UI (Wettbewerbe,
Nationalteam, Spielplan, Kader, Transfer, Bildschirme) und zwei Testlücken; Core und Bildschirme berühren teils dieselben Werte.
- **A** Drei Fachspuren und `pruefung-karte` wie oben. + parallel, Dateien getrennt, eine Runde; − Merge-Folge nötig (UI zuletzt).
- **B** Eine Core-Spur `kern-rest` (Wettbewerbe und Kader) und `ui-rest`. + weniger Läufe; − lange Spur, Fortsetzungen reichen
  evtl. nicht.
- **C** Nur Core-Spuren, `ui-rest` mit dem Live-Spiel zusammen in Runde 18. + Bildschirmarbeit gebündelt; − Ligapaarungen bleiben
  unvollständig.
- **D** Nur Wettbewerbe/Nationalteam und `pruefung-karte`, Kader/UI in Runde 18. + kleinste Runde; − zwei Runden statt einer.
- **Empfehlung A:** bewährter Zuschnitt (Runden 13–16), Lücken in einer Runde geschlossen.

**R17-D2 – Live-Spiel (L1–L8).** Problem: Das Live-Spiel ist als letzter Schritt vor v1.0 vorgesehen; außer ihm ist der
Spielcode nahezu vollständig. Offen ist, wann es beginnt.
- **A** Start in Runde 18 nach Abschluss dieser Runde (L1–L5 zuerst, L6–L8 danach). + klare Reihenfolge, keine Konflikte mit
  `MatchEngine.Live.cs`/`MatchDayScreen`; − eine Runde später.
- **B** In dieser Runde zusätzlich Analysespur für L6 (Eingriffsmenü 0x38D96, Treffer-Tests 0x534BC/0x53508) und 575C3h
  (`tore\`) als Spur `re-livespiel-2` (Doku-Nachtrag zu `analyse/re-livespiel.md`, kein Code; Claude Opus 5.5, Erstreview
  Claude Opus 5.5, weil Disassembly). + Runde 18 startet mit vollständiger Analyse; − ein Lauf mehr.
- **C** In dieser Runde schon L1 (Drittelzähler im Core) als fünfte Spur. + früher Fortschritt; − Konflikt mit
  `wettbewerbe-rest` an `MatchEngine.Live.cs`.
- **D** Live-Spiel erst nach der Abschlussprüfung. + Prüfung am stabilen Kern; − Abschlussprüfung müsste danach erneut laufen.
- **Empfehlung B:** kostet wenig, beseitigt die letzte unbekannte Funktion und macht Runde 18 planbar.

**R17-D3 – #730/#773 (DOSBox nötig).** Problem: Beide Befunde lassen sich ohne Aufnahme im Original nicht belegen
(R16-E2 = C); weitere Prüfaufträge haben in Runde 16 nichts ergeben.
- **A** Die Leitstelle legt eine gebündelte Aufnahmeliste `runde-17` in `vorlagen/dosbox-aufnahme.md` an (#730 nach
  `analyse/belastung-staerke.md` §7.3, #773, Highscore „nur Weltkugel“, A115/A189/A321/A357/A364, EXPORT2C); Aufnahme nach Lust,
  Auswertung in der Runde nach Eintreffen. + kein Druck, Liste fertig; − Issues bleiben offen.
- **B** Weiter zurückgestellt, keine Liste. + kein Aufwand; − Vorbereitung später nötig.
- **C** #730 als Remake-Verhalten mit `[APPROX]` annehmen und schließen, #773 ebenso. + Issues weg; − Abweichung bleibt
  unbelegt.
- **D** Dritter Prüfauftrag im Disassembly (Reihenwahl). + ohne Nutzerarbeit; − zweimal ohne Ergebnis.
- **Empfehlung A.**

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- DOSBox-Aufnahme `runde-17` (`vorlagen/dosbox-aufnahme.md`, R17-D3 = A): #730, #773, Highscore „nur Weltkugel“, A115, A189,
  A321, A357, A364, EXPORT2C – wenn der Nutzer Lust hat.
- `livespiel` (Torszenen, Anzeigetafel, Reporter; Nr. 14/16): Umsetzung als letzter Schritt vor v1.0; Analyse in
  `re-livespiel`. Noch unbekannt und bei der Umsetzung von `livespiel` mitzuanalysieren (zusammen mit L7, `tore\`-Format):
  575C3h (Ov 138h, Lader `tore\`, einziger Aufrufer 567C8h; R10R-01 #387).
- #730 (Belastung der Managerspieler, R16-E2 = C) und #773 (Strafminuten der ersten Spieltage): DOSBox-Aufnahmen nach
  `analyse/belastung-staerke.md` §7.3 bzw. Stand nach Spieltag 1 eines neuen Spiels (Befund im Issue).
- Highscore am Spielende und Saisonende im Modus „nur Weltkugel“ (Kandidat für eine DOSBox-Aufnahme mit `hok-patch`).
- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

### Vormerkungen (nach Runde 16)

- **Testlücken aus dem Gesamtreview Runde 16:** #784 (Einsatz 40/60 je Computerverein im #772-Test), #785 (Kontoanzeige #768
  für weitere Manager und Bundestrainer).
- **DOSBox-Aufnahme** (nach Lust des Nutzers, `vorlagen/dosbox-aufnahme.md`): Punkte unter „Zurückgestellt“, dazu EXPORT2C und
  die Exportstände ab Head `97c372a` (Restlücke R758-01).
- **Nach v1.0:** Einstellungen aus R15-E3 und bitgenauer Zufallsmodus (`anforderungen/zusatzfunktionen.md` C).

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
