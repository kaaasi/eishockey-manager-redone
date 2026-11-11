# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 16 – Pool wie im Original, Restbefunde des Abgleichs (Plan 11.11.2026, Go 11.11.2026)

Runde 15 abgeschlossen am 11.11.2026: [archiv/runde-15.md](archiv/runde-15.md). Grundlage: Vormerkungen R15-E6 = A und R15-E5 = A
(Pool-Umbau #743 in Runde 16), offene Issues #699 (Rest), #703 (R701-06), #730 (Rest), #732, #734, #743, #757, Testlücke
R727N2-01 (Gesamtreview Runde 15).

**Base für alle Spuren:** aktueller `main` (Planstand `31597e4`); tatsächlichen Base-SHA in den Draft-PR. Regeln wie Runde 15
(Archiv): ein Branch `paket/<spur>`, ein PR, Teile als Commits, höchstens drei Fortsetzungen; Befund reproduzieren, roter
Regressionstest mit Fundstelle, dann grün; Mehrsaisonprobe (4 Wechsel, passiv und aktiv) bei Saisonwechsel, Kader oder Geld;
Schema-Änderung mit Migration in neuer Datei `Core/SaveGame.Migration.<Spur>[<n>].cs` (Stand Schema 47, die Leitstelle
nummeriert beim Merge); `[ORIGINAL]`/`[APPROX]` mit `Datei 0x…`; `Fixes #<nr>`; fremde Tests nach R15-E2 = C; keine geplanten
Aufgaben aus Läufen (R15-E4). Bei Unklarheit „wie im Original“; bleibt es unklar, Teil blockiert lassen, die Leitstelle bündelt
die Fragen. **Entwickler und Erstreviews sind Claude-Sitzungen** (geplante Läufe); ChatGPT nur für das Gesamtreview.

**Folge:** Welle 1 parallel `abgleich-rest`, `pool`, `pruefung-karte` Teil 1; Merge in der Folge `abgleich-rest` →
`pruefung-karte` Teil 1 → `pool` (breiteste Spur, Import/Export und erwartete Listen zuletzt; vor dem Merge
Fixlauf ohne Befund, falls die Listen gegen den neuen `main` zu erneuern sind); danach Welle 2 `pruefung-karte` Teil 2
(Fortsetzung durch die Leitstelle), am Rundenende Gesamtreview.

### Zuständigkeit für gemeinsame Dateien

- `Core/Transfer*.cs`, `Core/NewGame.cs`, `Core/GameSetup.cs`, `Core/OriginalExport*.cs`, `Core/OriginalImport.Market.cs`,
  `src/EishockeyManager.Formats/OriginalSave*.cs`, `docs/formate/spielstand-original.md`,
  `tests/**/OriginalExportRoundTripTests.cs`: allein `pool`. Übrige `Core/OriginalImport*.cs`: `pool`, außer den
  Modus-1-Stellen (`WorldOnly`, Satz 0), die `abgleich-rest` braucht – beide nennen ihre Stellen im PR, `pool` löst beim Merge auf.
- `Core/Squad*.cs`, `Core/MatchEngine*.cs`, `Core/Player*.cs`, `Core/SeasonFlow*.cs`, `Core/GameState.DailyRun.cs`,
  `Core/GameState.Manager.cs`, `Core/Cups*National*.cs`, `Core/Finance*.cs` außer `FinanceStocks.cs`: allein `abgleich-rest`;
  braucht `pool` dort eine Zeile (Besitzer, Poolplatz), nennt es sie im PR. Satz 0 im Export (`OriginalExport*.cs`): Stellen
  nennt `abgleich-rest` im PR, `pool` löst beim Merge auf.
- `Core/FinanceStocks.cs`, `UI/Screens/**/Finance*Screen*.cs` (nur Kurs 0), `Tools/HokCompare*.cs`,
  `docs/analyse/abgleich-original.md`: allein `pruefung-karte`.
- `tests/**/AbgleichOriginalPaareTests.cs`: jede Spur nur die erwarteten Einträge ihrer Issues.
- `Core/GameState.Integration.cs`, `GameState.cs`: höchstens minimal, im PR begründet.
- `docs/analyse/inventur.md`, `docs/re-abdeckung.md`, `tools/reverse-engineering/*`, `tests/**/Support/`: allein
  `pruefung-karte` (Teil 2); die anderen Spuren nennen ihre Inventurzeilen im PR.

### Spur `pool` – Managerkader im Pool wie im Original (#743, R727N2-01)

- **Teile:**
  1. **Lesart belegen:** Ziehung der Managerkader aus den 199 Poolplätzen (DGROUP 0x381) beim Spielstart und Saisonwechsel,
     Besitzerbyte +0x1A, Rückgabe in den Pool (Verkauf, Vertragsende, Entlassung); Fundstellen in em.exe, Gegenprobe an
     FILE01/FILE14/FILE46 (`emtool hok dump`). Ergebnis als Abschnitt in `formate/spielstand-original.md`.
  2. **Umbau:** Managerkader belegen Poolplätze wie im Original (`Transfer.Pool.cs` `BuildPool`/`ReservePool`), Import ohne
     Platzverlust, Migration alter Remake-Spielstände (neue Datei), Mehrsaisonprobe passiv/aktiv.
  3. **Export verlustfrei:** Ersatzregel „freier Ligaspieler weicht“ (`[APPROX]`) entfällt; Rundreise FILE01–49A ohne diese
     Ausnahme; **R727N2-01:** Rundreisetest prüft den exportierten Besitzer des Marktzwillings (Mutation `marketTwins` → rot).
- **Abnahme:** FILE41/43/43A/44 behalten Markt-Poolplatz 179 im Export; kein Spieler mehr außerhalb der 199 Plätze; `Fixes #743`.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** nicht gestartet.

### Spur `abgleich-rest` – Belastung, Modus „nur Weltkugel“ (#730 Rest, #732, #699 Rest; R16-D1 = B)

- **Teile:**
  1. **Belastung – Ursache belegen (#730):** Zuwachs je Reihe/Einsatz im Original (Minutenrechnung, Reihenwechsel, Training vor
     dem Tagesabschluss), warum Vielspieler der Reihen 1/2 im Original vor dem Training über 127 kippen
     (`reviews/2026-11-11-abgleich-fix.md` B1; FILE02→03 HOCK/SIKORA, FILE16A→17).
  2. **Belastung – umsetzen:** wie im Original; Regressionstest FILE02 → 3 Spieltage (HOCK ≤ 70), Pflichteinträge unter #730 aus
     `AbgleichOriginalPaareTests` entfernen, Mehrsaisonprobe.
  3. **#732 (R16-D2 = A):** Kader und Finanzen des Satzes 0 im Modus 1 wie im Original führen – Import des Kaders von Satz 0,
     Tagesabschluss je Manager ohne Modusprüfung (Sommerpause Datei 0x564E1–0x56564, Aufruf 0x56512; WM-Schleife
     0x5568B–0x5569F), Export von Satz 0; Abnahme FILE20→21 Konto im Bereich des Abgleichs, Migration (neue Datei),
     Mehrsaisonprobe im Modus „nur Weltkugel“.
  4. **#699 Rest:** Tag 0 im Modus 1 wie im Original (FILE20→21, erwarteter Eintrag `DayZero = 699` entfällt).
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** nicht gestartet.

### Spur `pruefung-karte`

- **Teil 1 (sofort):** #757 (Kurs 0: Verhalten des Originals 0x596C2h belegen, dann wie im Original absichern, `BuyStock` und
  Finanzbildschirm), #734 (FILE13→14 als „Eingriff/Sitzungszustand“ in `HokCompareClasses` und Bericht, dann schließen), #703
  R701-06 (115.000 DM im April-Abschluss erklären oder als Testlücke belegen, dann schließen), P3 aus den Reviews dieser Runde.
- **Teil 2 (nach dem Merge von `pool`):** Inventur und Funktionskarte nachziehen (`--write`, zweiter Lauf ohne Diff),
  Inventurzeilen aller Spuren.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** nicht gestartet.

### Gesamtreview am Rundenende

ChatGPT (Codex mit Netzzugang) nach `vorlagen/review-anfrage.md`, risikoorientiert (`prozess/anhang.md` C). **Modell:** GPT-6.1 Terra.

### Entscheidungen zum Plan (Nutzer, Go 11.11.2026, PR #763)

**Ergebnis:** R16-D1 = B (`belastung` und `weltkugel` als eine Spur `abgleich-rest`), R16-D2 = A (Satz 0 im Modus „nur
Weltkugel“ wie im Original); Modelle wie in den Spurabschnitten.

**R16-D1 – Zuschnitt.** Problem: sieben Restpunkte, davon ein großer Umbau (#743) und zwei Spielkern-Befunde (#730, #732);
Import/Export und erwartete Listen werden von mehreren Spuren berührt.
- **A** Vier Spuren wie oben (drei Fachspuren parallel, `pruefung-karte` sammelt P3 und Inventur). + alles in einer Runde,
  Dateien weitgehend getrennt; − `pool` und `weltkugel` teilen sich `OriginalImport*`.
- **B** `belastung` und `weltkugel` als eine Spur `abgleich-rest`. + ein Lauf weniger; − Teile nacheinander, Spur länger.
- **C** Nur `pool` und `pruefung-karte`, #730/#732/#699 in Runde 17. + kleinste Runde; − P2 #730 bleibt liegen.
- **D** A plus DOSBox-Aufnahme (Kurs 0, Highscore „nur Weltkugel“, A115/A189/A321/A357/A364, EXPORT2C). + Belege aus dem
  Original; − Arbeit für den Nutzer (zurückgestellt nach E14-5 = C).
- **Empfehlung A.**

**R16-D2 – Konto des Satzes 0 im Modus „nur Weltkugel“ (#732).** Problem: Das Original bucht für Satz 0 auch im Modus 1 den
Tagesabschluss (Datei 0x56512, 0x5568B–0x5569F; FILE20 500.000 → FILE21 −1.530.327 DM), das Remake führt dort keine
Vereinsfinanzen. Der Highscore im Modus „nur Weltkugel“ nutzt kein Geld (`Highscore.cs`); sichtbar ist bisher nichts.
- **A** Wie im Original: Kader und Finanzen des Satzes 0 im Modus 1 führen. + Abgleich und Export exakt; − Aufwand und Risiko
  für einen unsichtbaren Wert.
- **B** Remake-Verhalten behalten, als `[APPROX]` (unsichtbar) dokumentieren, #732 schließen. + kein Aufwand; − Abweichung im
  Export und Abgleich bleibt.
- **C** Wie im Original erst nach v1.0 (Abschlussprüfung). + jetzt kein Aufwand; − offenes Issue bis dahin.
- **D** Erst Sichtbarkeit belegen (Teil 1): ist der Wert irgendwo sichtbar oder spielwirksam, wie A, sonst wie B. + Aufwand nur,
  wo er etwas bewirkt; − Ergebnis offen bis Teil 1.
- **Empfehlung D:** folgt „wie im Original“ dort, wo es zählt, ohne Umbau für einen nie gezeigten Wert.

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- DOSBox-Nachaufnahme für A115, A189, A321, A357, A364 (E14-5 = C): später, wenn der Nutzer Lust hat.
- `livespiel` (Torszenen, Anzeigetafel, Reporter; Nr. 14/16): Umsetzung als letzter Schritt vor v1.0; Analyse in
  `re-livespiel`. Noch unbekannt und bei der Umsetzung von `livespiel` mitzuanalysieren (zusammen mit L7, `tore\`-Format):
  575C3h (Ov 138h, Lader `tore\`, einziger Aufrufer 567C8h; R10R-01 #387).
- Highscore am Spielende und Saisonende im Modus „nur Weltkugel“ (Kandidat für eine DOSBox-Aufnahme mit `hok-patch`).
- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

### Vormerkungen (nach Runde 16)

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
