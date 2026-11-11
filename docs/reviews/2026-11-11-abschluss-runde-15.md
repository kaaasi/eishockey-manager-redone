# Review PR #761 – Abschluss Runde 15 (Archiv, Prozessnachträge, Vormerkungen Runde 16)

Geprüft: Head `f4f1ebf3174395d844fe7d37d4a6fed45a660199` (lokal ausgecheckt, Arbeitsbaum sauber), Base laut Aufruf
`98e59265906ac7f57150c56db239917105f46bac`. Der Head sitzt auf `main` @ `7e13a17` (nach #760); der Diff Base..Head enthält daher
zusätzlich die Review-Datei `docs/reviews/2026-11-11-runde-15-gesamt.md` aus #760 – sie gehört nicht zum PR (die 11 Dateien des PR
laut API sind alle geprüft). Reviewer: Claude Code (Sonnet 5.5), geplanter Lauf, eigene Sitzung ohne Implementierungskontext.
Reine Doku-Prüfung: kein Build, keine Tests, keine Mutationen; Belege sind `git diff`, `diff` und `grep` am Head.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Der Archivabschnitt ist am Base unverändert übernommen, alle Punkte aus #744 (samt Nachträgen) und
#724 N1–N5 sind umgesetzt, R15-E4 ist in den vier Dateien widerspruchsfrei, die Kurzaufrufe bleiben wörtlich verwendbar, die
Inventur-Zeilen entsprechen R758-01. Nur P3-Hinweise, keiner blockiert.

## 2. Besonders zu prüfen

**(1) Archiv `docs/archiv/runde-15.md`:** `sed -n 11,222p arbeitspakete.md@Base` gegen `sed -n 22,232p runde-15.md` → genau zwei
Unterschiede: Überschrift („Aktuelle Runde: Runde 15 …“ → „Runde 15 …“) und eine entfallene Leerzeile am Ende. Neu sind nur
Kopf (Z. 1–20: Überschrift, Hinweis, **Ergebnis**, **Abschluss**) und die Trennlinie. „Zurückgestellt“ (Base Z. 223–231) steht im
Head unverändert in `arbeitspakete.md` (Diff der neun Zeilen: identisch). Behauptungen im Ergebnis: Inventur bis A407 vorhanden
(`inventur.md` Z. 604), Gesamtreview #760 „Ja“ für `98e5926` und Testlücke R727N2-01 stimmen mit `2026-11-11-runde-15-gesamt.md`
überein. `arbeitspakete.md` ist von 273 auf 81 Zeilen geschrumpft; Verweis `archiv/runde-15.md` und Vormerkungen vorhanden.

**(2) #744 und #724:**

| Punkt | Fundstelle im Head | Stand |
|---|---|---|
| R15-E2 = C (fremde Tests) | `zusammenarbeit.md` §4 (neuer Punkt), `spur-start.md` Schritt 4 | umgesetzt, beide mit Begründung/Fundstelle + Rückschaltprobe |
| R15-E4 | siehe unten | umgesetzt |
| R15-E3 = C | `anforderungen/zusatzfunktionen.md` (Vertragslose aus Managervereinen) | umgesetzt |
| R15-D2 bitgenauer Modus | `zusatzfunktionen.md` | umgesetzt |
| 8.3-Namen | `vorlagen/dosbox-aufnahme.md` Schritt 5 | umgesetzt |
| `gegenliste-verdichtung.md` archivieren | `git mv` → `archiv/gegenliste-verdichtung-runde-15.md`; keine Restverweise (`grep`) | umgesetzt |
| Nachtrag R15-E5 = A (Welle 2, #743 → Runde 16) | Archiv „Welle 2 (R15-E5 = A …)“ (aus Base) und Vormerkung #743 | umgesetzt |
| Nachtrag R15-E6 = A | Vormerkungen: #730 (Rest), #732, #743, #757; #734 | umgesetzt, Abweichung P3-2 |
| #724 N1 (Zeichenzahl) | Archivdatei: „43.092 statt 43.057“ | eingetragen, Zahl nicht nachgemessen (Doku-Prüfung) |
| N2 (runde-14 → `anhang.md` C) | `archiv/runde-14.md` Z. 196 | umgesetzt |
| N3 (AGENTS.md §8 DOSBox-Aufnahme) | Vermerk in der Archivdatei; Zeile „Kurzaufrufe … DOSBox-Aufnahme“ steht in `AGENTS.md` §8 | umgesetzt |
| N4 („wie oben“) | `spur-start.md`: „Einzelner Folgeteil“ und „Fixlauf“ nennen „Geplanter Lauf mit Teilen“; `grep 'wie oben'` ohne Treffer | umgesetzt |
| N5 (Netzbefehle bei Codex) | `anhang.md` D Punkt 3 („weil nach dem Klonen nicht sicher Netz besteht“) | umgesetzt |

**R15-E4 widerspruchsfrei:** `spur-start.md` (Startaufruf „keine geplanten Aufgaben anlegen“, eigener Absatz, Fixlauf, Schritt 5
„Fortsetzung anfordern“), `zusammenarbeit.md` §5 Punkt 8 (Fixlauf setzt `spur-wartet` + Startaufruf im Übergabekommentar, Leitstelle
legt an; Labeltabelle: `spur-wartet` ohne „Freigabe `create_trigger` abgelehnt“, mit „Nachprüfung anzulegen“), `leitstelle-start.md`
Schritt 7, `anhang.md` F („Läufe legen keine Aufgaben an“, Nachprüfung nach P0/P1 ausdrücklich genannt). `grep -rn create_trigger`
außerhalb von Archiv/Reviews: nur `spur-start.md` Z. 29 („Läufe rufen nie `create_trigger` auf“) und `anhang.md` Z. 83 (Leitstelle,
„Zugriff vor dem ersten `create_trigger` der Sitzung“) – beides korrekt. Kein Rest verlangt ein Anlegen aus einem Lauf.
`CLAUDE.md` („Anlegen nur nach Einbinden beider Repos in der anlegenden Sitzung“) bezieht sich auf die Leitstelle und passt.

**(3) Kurzaufrufe und Inventur:** Der Startaufruf „Geplanter Lauf mit Teilen“ ist vollständig und ohne Platzhalterlücke; die
Ersetzungsregel für „Einzelner Folgeteil“ passt auf den Wortlaut des Startaufrufs; die Kurzaufrufe in `review-anfrage.md` und
`leitstelle-start.md` sind unverändert (diese Aufgabe selbst wurde mit dem Kurzaufruf gestartet). Inventur: A399 „R15-D5 = D
bestanden (EXPORT1C, Referenz-PR #27; belegt Laden und Anzeige, nicht die einzelnen Vorgabefelder; EXPORT2C und Stände ab Head
`97c372a` nicht erneut im Original geprüft)“, A404 und A405 „im Original geladen (EXPORT1C, R15-D5)“ – entspricht R758-01 samt der
dort genannten Restlücke; Einordnung d/P3 bleibt. Die Restlücke steht zusätzlich in der DOSBox-Vormerkung (`arbeitspakete.md`).

## 3. Erwartet offen (nur bestätigt)

#699 (Rest „nur Weltkugel“), #703 (R701-06), #730 (Rest), #732, #734, #743, #757 stehen als Vormerkungen Runde 16 in
`arbeitspakete.md` und im Abschluss des Archivs; keine eigene Prüfung ihres Inhalts.

## 4. Befunde

**R761-01 (P3) – Archivkopf ungenau:** Er sagt „ohne ‚Zurückgestellt‘ und ‚Vormerkungen‘“; im Base-Abschnitt gab es keinen Abschnitt
„Vormerkungen“ (Treffer nur „Vormerkung Vorschlag …“ in Spurtiteln). Harmlos; „Vormerkungen Runde 16“ sind neu in `arbeitspakete.md`.

**R761-02 (P3) – #734 weicht vom Wortlaut des Nachtrags R15-E6 ab:** Der Kommentar zu #744 (08:41) sagt „#734 auf die Liste der
nächsten DOSBox-Aufnahme“; im Head steht #734 als eigene Vormerkung („danach schließen“), und die DOSBox-Zeile nennt #734 nicht. Das
passt zum Auftrag dieses Reviews (#734 erwartet offen als Vormerkung) und zur Probe vom 11.11.; wer E6 wörtlich nimmt, ergänzt #734
in der DOSBox-Zeile oder vermerkt die Abweichung.

**R761-03 (P3) – Lesbarkeit:** `zusammenarbeit.md` §5 Punkt 8 hat eine überlange Zeile (Satzumbruch mitten im Satz, „…legt sie an
(etwa 5 Minuten später). Die Leitstelle wartet auf Labels …“); nur Format.

## 5. Nicht geprüft

Kein Build, keine Tests (reine Doku, CI entfällt). Die Zeichenzahl 43.092 (N1) und die Bildinhalte des Referenz-PR #27 nicht
nachgemessen. Archiv-Links innerhalb von `docs/` nicht einzeln aufgelöst, nur die geänderten Verweise stichprobenartig.
