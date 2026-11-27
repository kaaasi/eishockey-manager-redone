# Review PR #919 – DOSBox-Aufnahme `runde-22`

Geprüft: Base `bf136de`, Head `f3a3f98` (`git rev-parse HEAD` = Head, `git status --porcelain` leer). Nur Doku
(`docs/vorlagen/dosbox-aufnahme.md`, +43 Zeilen), unbeaufsichtigter Lauf, kein Mitwirken am PR.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Alle 21 D sind vorhanden, keine fremde ID, Schritte passen zu den Belegfeldern, Startstände und
Nummernraum stimmen. Die Befunde unten sind nur P3-Klarstellungen und blockieren nicht. Erwartet offen bestätigt: Aufnahme selbst
(nach Lust des Nutzers), Auswertung Runde 23.

## 2. Befunde

Alle P3, ohne roten Test (Doku); Beleg jeweils Lesen von CSV/README.

- **P3-1 Sitzung 4, Schritt 2 (A707): Ausgangsstand fehlt.** Es steht nicht, ob der Listenwechsel aus FILE18 oder nach Schritt 1
  (FILE81) beginnt. Beides ist möglich, das Ergebnis (Pool, Kader) unterscheidet sich. Vorschlag: „aus FILE18 neu“ bzw. „aus FILE81“ nennen.
- **P3-2 Sitzung 4, Schritt 1 (A411 „Pool belegt“):** FILE18 hat laut README „vorher kein Spieler im Kader“ (WM-Kader mit „auto“
  gewählt). Ob der Pool dort belegt ist, ist nicht belegt; ein Hinweis „Pool-Belegung vor dem Sichern prüfen“ wäre sicherer.
- **P3-3 Sitzung 1, Schritt 4 → Sitzung 2:** Die Vertragsverhandlung (A117) kommt nach FILE76, Sitzung 2 setzt aber „Fortsetzung von
  FILE76“ fort. Wer in einem Lauf durchspielt, hat Verhandlungsfolgen im Stand, wer aus FILE76 lädt, nicht. Eine Zeile „A117 nur
  fotografieren, Verhandlung abbrechen“ macht die Sitzungen deckungsgleich.
- **P3-4 Sitzung 1, Schritt 3 (A629):** Die Leihlaufzeit ist nicht vorgegeben. Das Belegfeld verlangt Laufzeit/Gehalt vor und
  nach dem Saisonende; ohne Vorgabe (z. B. „Laufzeit endet am Saisonwechsel“) kann die Leihe vor FILE77 auslaufen.
- **P3-5 Sitzung 3, Schritt 2 (A625):** Eine Matchstrafe ist nicht herbeiführbar, nur abwartbar. Ein Hinweis (mehrere Spieltage
  spielen, ggf. per `hok-patch` vorbereiten wie bei Sitzung 5) vermeidet leere Aufnahmen.

## 3. Ausgeführte Prüfungen

Alle drei Prüfpunkte der Anfrage. `tools/pruefen.sh` entfällt (nur Doku).

1. **Vollständigkeit (Besonders prüfen 1):** Python-Probe, Klasse D aus `docs/analyse/restliste-v1-klassen.csv` gegen alle
   `A\d{3}` im Abschnitt „Aufnahme `runde-22`“: D = 21; fehlend: `[]`; fremd: `[]`. Die IDs sind A057, A065, A095, A117, A128, A131,
   A132, A259, A402, A408, A411, A427, A599, A613, A614, A625, A629, A630, A667, A706, A707.
2. **Schritt ↔ Beleg (Besonders prüfen 2):** je ID das Feld „Beleg“ mit dem Schritt verglichen. Stimmig: A057 (mehrere Tage),
   A065 (Bestenliste vor 1. Spieltag; FILE75 im ersten Hauptmenü), A095 (4. Reihe, Modus 2/3), A117 (Gehalt 0 / sehr hoch),
   A128/A131/A132/A630 (Pool vor dem Saisonwechsel in FILE77, danach in FILE78), A259 (Gebot offen, Bieterkader voll, annehmen; als optional
   gekennzeichnet, passt zur Beleg-Aussage „evtl. nicht erreichbar“), A402/A613 (Sparbuch plus Darlehen), A614 (mehrere Werbeverträge
   mit Banden), A625 (Sperre nach Matchstrafe), A629 (Leihe, vor/nach Saisonende), A667 (Zwangsabsteiger Platz 3/4, vor/nach Wechsel),
   A706 (29 Spieler, eigener Spieler gelistet, vor/nach Wechsel), A707 (Bundestrainer-Stand, Listenwechsel mit Poolspieler), A408/A411
   (Weltkugel, Trainingstag/Vertragsablauf), A427 (Stand direkt nach Start), A599 (KAPUTT.HOK, nach „Mogelguru“). Abweichungen nur
   die P3 oben.
3. **Startstände (Besonders prüfen 3):** im Referenz-Repo `original/spielstaende/`: höchste Nummer ist FILE74 (README-Tabelle und
   Dateiliste), also ist FILE75 der erste freie Platz; FILE75–FILE85 sind elf Nummern, so viele wie im Abschnitt vergeben.
   FILE18 existiert und ist laut README der Bundestrainer-Stand „nur Weltkugel“ (14.04.1994) – passend zu Sitzung 4 und A707.
   `KAPUTT.HOK` existiert (README: FILE03 mit falscher Prüfsumme, Bild „Mogelguru“). `hok-patch` existiert
   (`src/EishockeyManager.Tools/HokPatch.cs`) für Sitzung 5.

**Akzeptierte `[APPROX]`:** keine betroffen. **Annahmen (nicht geprüft):** ob die beschriebenen Spielsituationen im Original
tatsächlich erreichbar sind (A259, A625, A667); der Plan selbst ist nur Doku, ob die Aufnahme gelingt, zeigt Runde 23.
