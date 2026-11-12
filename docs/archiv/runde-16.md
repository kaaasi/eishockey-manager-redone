# Archiv: Runde 16 – Pool wie im Original, Restbefunde des Abgleichs (11.–12.11.2026)

Aus `docs/arbeitspakete.md` ausgelagert am 12.11.2026. Inhalt unverändert bis auf Überschrift, „Ergebnis“, „Abschluss“ und
„Entscheidungen während der Runde“, ohne „Zurückgestellt“ und „Vormerkungen“ (bleiben in `arbeitspakete.md`). Die Statuszeilen
der Spuren geben den Stand vor dem jeweiligen Merge wieder. Reviews: `docs/reviews/` (Dateien vom 11.–12.11.2026). Nur bei Bedarf
lesen. Relative Links beziehen sich auf `docs/`.

**Ergebnis:** Spuren `abgleich-rest` (#766, mit Prüfauftrag #771), `pruefung-karte` Teil 1 (#767), `pool` (#765) und
`pruefung-karte` Teil 2 (#781) gemergt. Behoben: #743 (Managerkader im Pool, Schema 49), R727N2-01, #732 (Satz 0 im Modus
„nur Weltkugel“ wie im Original, Schema 48), #699 Rest, #772 (Spielvorbereitung der Computervereine 0x8365), #757, #734, #703,
#768, #770, #775, #780; #776/#778 belegt geschlossen. Gesamtreview (#783, ChatGPT GPT-6.1 Terra) **Ja** für `d233989`: keine
neuen Produktfehler, zwei Testlücken (#784, #785).

**Abschluss (12.11.2026):** offen #730 (Belastung, zurückgestellt R16-E2 = C, DOSBox-Liste `analyse/belastung-staerke.md` §7.3),
#773 (Strafminuten erster Spieltage, DOSBox-Stand nach Spieltag 1 nötig), #784, #785 (Testlücken). Prozess: Namen geplanter
Läufe „EM R<runde> …“ (Nutzer 11.11.2026, `prozess/anhang.md` F). Repo `eishockey-manager-redone` öffentlich (R16-E3, Nutzer
12.11.2026), Referenz-Repo bleibt privat.

**Entscheidungen während der Runde (Nutzer):** R16-E1 = B (Prüfauftrag Spielstärke, #771); R16-E2 = B mit Fallback C (zweiter
Prüfauftrag samt #772, ohne Beleg #730 zurückgestellt); R16-E3 (CI-Läufe hingen in `queued`: Repo öffentlich, Ersatzlauf per
`workflow_dispatch` auf dem Head).

---

## Runde 16 – Pool wie im Original, Restbefunde des Abgleichs (Plan 11.11.2026, Go 11.11.2026)

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
  Dasselbe gilt für die Modus-1-Stellen in `Transfer*.cs`, `GameSetup.cs` und `OriginalImport.Market.cs` (R763-01).
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
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** Teile 1–3 fertig, Review offen (PR #765; Lesart in
  `formate/spielstand-original.md`, Abschnitt „Managerkader im Spielerpool“; Startkader auf den Poolplätzen nach 0x5AF50; Export
  ohne Ersatzregel, `PoolSlot.Manager`, Rundreise prüft `+0x1A`; Migration Schema 48 → 49). Fixlauf 11.11.2026: `main` 846e515
  (abgleich-rest #766) eingebunden, Modus 1 mit Satz 0 zusammengeführt (Export: Ersatzregel nur noch für den nicht eingesetzten
  Kader von Satz 0 im Modus 1, `[APPROX]`), Abdrücke erneuert; R765-01 (#775) behoben, R765-02 (#776) geprüft.

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
- **Lauf-Budget (R763-04):** ein Lauf darf mehrere Teile erledigen; reichen die vier Läufe nicht, bleibt Teil 4 offen
  (`spur-wartet`), die Leitstelle legt dem Nutzer einen Zusatzlauf oder Runde 17 vor.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** PR #766 (Ready for review, `spur-wartet`): Teile 1/2
  zurückgestellt (R16-E2 = C: Ursache #730 nicht belegt, [analyse/belastung-staerke.md](analyse/belastung-staerke.md) §7,
  `[APPROX]` an `OriginalMinutes.ChangeLines`, Pflichteinträge bleiben, DOSBox-Liste §7.3, #730 offen); Teil 3 fertig (#732);
  Teil 4 fertig (#699); #772 fertig (Spielvorbereitung 0x8365, Schema 48); #773 kein Fehler (Strafrechnung wie im Original,
  FILE02→03 Zufall, §8; Wächtertest FILE05→24).

### Spur `pruefung-karte`

- **Teil 1 (sofort):** #757 (Kurs 0: Verhalten des Originals 0x596C2h belegen, dann wie im Original absichern, `BuyStock` und
  Finanzbildschirm), #734 (FILE13→14 als „Eingriff/Sitzungszustand“ in `HokCompareClasses` und Bericht, dann schließen), #703
  R701-06 (115.000 DM im April-Abschluss erklären oder als Testlücke belegen); vor dem Schließen im Issue belegen, wo
  R701-01…05 erledigt sind (Runde 15 Teil 1, #753), sonst Reste auflisten (R763-02), P3 aus den Reviews dieser Runde.
- **Teil 2 (nach dem Merge von `pool`, eigener Branch `paket/pruefung-karte-2` mit eigenem PR, weil Teil 1 vorher gemergt
  wird; R763-03):** Inventur und Funktionskarte nachziehen (`--write`, zweiter Lauf ohne Diff),
  Inventurzeilen aller Spuren.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** Teil 1 fertig, gemergt (PR #767). Teil 2 fertig, Review offen (PR #781: Inventur A408–A417 und „Stand Runde 16“, Funktionskarte, `re-abdeckung.md`; P3 #780 Test, #778/#776 belegt mit Wächtertests, #768 behoben, #773 offen mit Befund).

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

