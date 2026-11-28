# Archiv: Runde 24 – Abweichungsliste vor v1.0, Konzept Aufräumen (23./24.11.2026)

Aus `docs/arbeitspakete.md` ausgelagert am 24.11.2026. Inhalt unverändert bis auf Überschrift, „Ergebnis“, „Abschluss“ und
„Entscheidungen während der Runde“, ohne „Zurückgestellt“ und „Vormerkungen“ (bleiben in `arbeitspakete.md`). Die Statuszeilen
der Spuren geben den Stand vor dem jeweiligen Merge wieder. Nur bei Bedarf lesen. Relative Links beziehen sich auf `docs/`.

**Ergebnis:** Plan #983 (Plan-Review #985), Prozess-PR #984 (#986, Nachprüfung #989: Erfahrungen Runde 23). Beide Spuren
gemergt: `abweichungsliste` (#987, #991 Nein P2, Fixlauf, Delta-Prüfung) → [analyse/abweichungen-v1.md](../analyse/abweichungen-v1.md);
`aufraeum-konzept` (#988, #990 Nein P2, Fixlauf, Delta-Prüfung) → [analyse/aufraeumen-v1.md](../analyse/aufraeumen-v1.md).
Zusatz auf Wunsch des Nutzers: Bewertung der Abweichungsliste durch ChatGPT (#992, GPT-6.2 Astra), Gemini 4 Hydrogen und die
Leitstelle (`analyse/abweichungen-bewertung-*.md`, #993). Kein Gesamtreview (nur Doku).

**Abschluss (24.11.2026):** Entscheidungen zur Abweichungsliste in `analyse/abweichungen-v1.md` Abschnitt 0, zum Aufräumen in
`analyse/aufraeumen-v1.md` §3. Offen: Umsetzung der Abweichungen (Runde 25), danach Aufräumen #936 (mit #978, #980), D 26
(DOSBox nach Lust), Abschlussprüfung.

**Entscheidungen während der Runde:** Nutzer 23.11.2026: R24-D1…D3 = B, Go; Prozess-PR #984 (Auftrag). Nutzer 24.11.2026:
Abweichungsliste – 15 Themen „ändern“, zusätzlich B31 = A, B11 = A, K07, S05, S07, T03, S01 (vor v1.0); B16 = A, F06/F08 = A
(prüfen); Aufräumen A1 = B, A2 = A, A3 = B, A4 = A, A5 = A, A6 = A.

## Aktuelle Runde (archiviert): Runde 24 – Abweichungsliste vor v1.0, P3-Rest, Konzept Aufräumen (Plan 23.11.2026, Go 23.11.2026)

Runde 23 abgeschlossen am 23.11.2026: [archiv/runde-23.md](runde-23.md). Stand: Restliste
[analyse/restliste-v1.md](../analyse/restliste-v1.md) **V = 0, X = 0**, D 26, K 224, E 323. Offen: P3 #978, #980; #936; Vormerkungen
unten. Keine neue DOSBox-Sitzung von `runde-22` aufgenommen (Referenz-Repo `main` `7412a50`), D 26 bleibt bis zur Aufnahme.
**Ziel der Runde:** Der Nutzer bekommt die Abweichungsliste (R20-D3-Zusatz) in einer Form, in der er je Thema „bleibt“ oder
„ändern“ entscheiden kann; für #936 liegt ein Konzept mit den Nutzerentscheidungen vor. Umsetzung der
Änderungswünsche aus der Liste in Runde 25, Aufräumen danach (letzter Schritt vor v1.0), dann Abschlussprüfung.
**Entscheidungen Nutzer 23.11.2026** (Optionen im Plan-PR #983): R24-D1 = B (Spur `abweichungsliste`, Opus 5.5), R24-D2 = B
(P3 #978, #980 gehen ins Aufräumen #936, keine eigene Spur), R24-D3 = B (Spur `aufraeum-konzept`, nur Konzept). **Go Nutzer
23.11.2026** (beide Spuren samt Folgeläufen, Modelle wie unten; Start nach Merge des Plans).

**Regeln wie Runde 23** (Archiv): Base = aktueller `main` beim Start, ein Branch `paket/<spur>`, ein PR, höchstens drei
Fortsetzungen; kein `markerliste.py --write`; keine geplanten Aufgaben aus Läufen (R15-E4). Bei Unklarheit „wie im Original“,
sonst Leitstelle bündelt. Entwickler und Erstreviews sind Claude-Sitzungen. **Kein Gesamtreview** (keine Änderung am
Spielverhalten in dieser Runde; nur Doku).

**Folge:** beide Spuren parallel in Welle 1, Dateien überschneiden sich nicht; Integration in der Reihenfolge der
Fertigstellung.

| Spur | Welle | Inhalt | Modell | Grund |
|---|---|---|---|---|
| `abweichungsliste` | 1 | R24-D1: Abweichungsliste verdichtet als Entscheidungsvorlage | Opus 5.5 | Einordnung „sichtbar für Spieler“ und Aufwand braucht Code- und em.exe-Kenntnis |
| `aufraeum-konzept` | 1 | R24-D3: Konzept #936 ohne Umbau | Opus 5.5 | Architektur, Migrationen, Risiken |

Erstreviews: `abweichungsliste` und `aufraeum-konzept` Sonnet 5.5 (reine Doku). Fixläufe im Modell der Spur.
Plan-Review Sonnet 5.5.

### Spur `abweichungsliste` – Entscheidungsvorlage vor v1.0 (Welle 1, R24-D1)

- **Teile:** 1. `python3 tools/reverse-engineering/markerliste.py --abweichungen` auf aktuellem `main` (nur lesen) und die
  Ausgabe in Themen bündeln (gleiche Funktion bzw. gleicher Bildschirm, Doppelzeilen wie A768/A769 zusammen): je Thema IDs,
  Abweichung in einem Satz, **sichtbar für den Spieler** ja/nein, Aufwand S/M/L für „wie im Original“, Vorschlag „bleibt“ oder
  „ändern“ mit Grund; E aus Nutzerentscheidungen (N1, N2, R15-E3 usw.) mit Verweis, Vorschlag „bleibt“. Oben eine Übersicht je
  Bereich (Anzahl Themen, davon sichtbar, davon Vorschlag „ändern“); ganz oben die Themen mit Vorschlag „ändern“. D-Zeilen
  getrennt (warten auf DOSBox). Die Vollliste als Anhang derselben Datei (generiert, unverändert).
- **Dateien:** `docs/analyse/abweichungen-v1.md` (neu). **Prüfung:** entfällt (nur Doku); Probe im PR: Anzahl IDs der
  Vollliste = Anzahl IDs in den Themen. **Erstreview:** Sonnet 5.5 (Stichprobe 20 Themen gegen Code/Fundstelle).
- **Status:** Teil 1 fertig (PR #987, 23.11.2026): 101 Themen + 8 D-Themen, 581 IDs (A 573: K 224, E 323, D 26; F 8),
  15 Themen mit Vorschlag „ändern“; Erstreview #991 (P2-1…P2-5, 13 P3) im Fixlauf 23.11.2026 behoben, wartet auf Delta-Prüfung.

### Spur `aufraeum-konzept` – Konzept für #936 ohne Umbau (Welle 1, R24-D3)

- **Teile:** 1. Bestandsaufnahme aller Teile des Repos nach #936 (`src/` aller Projekte, `tests/`, `tools/`, `docs/`): Dateien
  nach Spur/Runde benannt, Zielnamen nach Thema, Migrationen (Anzahl, Bündelung vs. alte Remake-Stände nicht mehr laden),
  verwaiste Skripte, veraltete Doku, Verdacht auf doppelte Logik und tote Pfade (mit Fundstelle); 2. Zielstruktur und Zuschnitt
  der Umbau-Spuren (Verhalten unverändert, alle drei Prüfläufe), Reihenfolge, Risiken, offene Nutzerentscheidungen mit 3–5
  Optionen; P3 #978 und #980 als Teil des Umbaus einplanen (R24-D2 = B). **Keine** Datei verschieben oder ändern außer der Konzeptdatei.
- **Dateien:** `docs/analyse/aufraeumen-v1.md` (neu). **Prüfung:** entfällt (nur Doku). **Erstreview:** Sonnet 5.5.
- **Status:** Go 23.11.2026, Start nach Merge des Plans.

**Modellwahl (Nutzer 17.11.2026):** je Lauf möglichst wenig Tokens für ein brauchbares Ergebnis – Haiku, Sonnet, Opus; Fable nur
in Ausnahmefällen. **Läufe melden ihr Ende** per Push-Benachrichtigung (Nutzer 22.11.2026, `prozess/anhang.md` F).

