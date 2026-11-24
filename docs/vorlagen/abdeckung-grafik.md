# Vorlage: Grafischer Überblick der Abdeckung von em.exe

Erzeugt eine Grafik, wie weit die Funktionen von `em.exe` im Remake umgesetzt sind, in fünf Farben. Nur lesen: nichts committen,
kein `--write` von `funktionskarte.py` oder `markerliste.py`.

**Kurzaufruf** (der Nutzer schreibt ihn in einen neuen Chat):

> Repo kaaasi/eishockey-manager-redone. Erstelle den Abdeckungsüberblick nach `docs/vorlagen/abdeckung-grafik.md`.
> Stand: `<main | Branch/SHA>`.

**Modell:** Claude Sonnet 5.5 reicht: Die Zuordnung läuft über Skripte und feste Regeln, Unklares wird rot. Claude Opus 5.5 nur,
wenn die unbekannten Funktionen inhaltlich eingeordnet werden sollen (Zusatz im Kurzaufruf: „mit Einordnung der roten
Funktionen“).

## Farben (je Funktion von `em.exe`, gewichtet nach Bytes)

| Farbe | Bedeutung | Regel |
|---|---|---|
| **grün** | belegt umgesetzt | Status `umgesetzt` der Funktionskarte, und keine Fundstelle der Funktion gehört zu einem `[APPROX]` der Klasse E |
| **gelb** | umgesetzt ohne Beleg | Status `genähert` mit `[APPROX]` der Klasse K oder D; ebenso `dokumentiert` oder `unbekannt`, wenn das Remake die Funktion nachweislich ohne Adresse nachbaut (z. B. nach Video, Lückenliste in `docs/re-abdeckung.md` Spalte „Remake“), mit Nachweis (Datei im Remake) |
| **blau** | anders umgesetzt | eine Fundstelle der Funktion gehört zu einem `[APPROX]` der Klasse E (Nutzerentscheidung, korrigierter Fehler des Originals nach N2, Remake-Erweiterung) – blau hat Vorrang vor grün und gelb |
| **grau** | weggefallen | nur mit Beleg: Doku oder Code nennt die Funktion als entfallen bzw. Plattformersatz ohne Gegenstück (Kopierschutz/Handbuchfrage, Sicher-/Ladesperre, `ERROR.HOK`, Speicherprüfung, DOS-Treiber für Grafik, Ton, Maus, Overlay-Manager, Borland-Laufzeitbibliothek mit erkennbarem Kennzeichen wie Textauszug „LIB-Error“ oder Funktionsname) |
| **rot** | noch unbekannt | alles Übrige, besonders `unbekannt` und `dokumentiert` ohne Remake-Nachweis und ohne Entfall-Beleg; im Zweifel rot |

„Gehört zu einem `[APPROX]`“ heißt wie in `funktionskarte.py` Schritt 4: der nächste Marker vor der Fundstelle im selben
Kommentar (sonst danach). Die Klasse steht in der Markerliste (`markerliste.py`, Spalte Klasse; Gründe in
`docs/analyse/restliste-v1-klassen.csv`).

## Ablauf

1. Lies `AGENTS.md` §3 und `docs/re-abdeckung.md` (Abschnitte „Vorgehen“ und „Grenzen“). Originaldaten:
   `tools/claude/fetch-reference.sh original` (Claude: Referenz-Repo per `add_repo`, access read). `ndisasm` (Paket `nasm`).
2. Stand auschecken; `git rev-parse --short HEAD` notieren.
3. Daten außerhalb des Repos erzeugen (Arbeitsordner, z. B. `/tmp/abdeckung/`):
   - `python3 tools/reverse-engineering/funktionskarte.py ~/referenz/original/gamedata/em.exe --csv /tmp/abdeckung/funktionen.csv`
     (Spalten u. a. `datei`, `bereich`, `groesse`, `status`, `texte`, `fundstellen`),
   - `python3 tools/reverse-engineering/markerliste.py --abweichungen` bzw. die Tabelle in `docs/analyse/restliste-v1.md`
     (ID, `Datei:Zeile`, Klasse, Bereich).
4. Zuordnung per Skript im Arbeitsordner: je Funktion die Fundstellen in `src/` auf den zugehörigen Marker abbilden (Datei +
   nächste Markerzeile), Klasse übernehmen, Farbe nach der Tabelle oben. Grau und die gelben Fälle ohne Adresse nur mit Beleg
   (Datei:Zeile oder Dokument), sonst rot. Beleg je grauer Funktion in einer Liste festhalten.
5. Probe: Summe der Bytes aller Farben = Summe `groesse` der CSV; Anzahl Funktionen gleich; Stichprobe 10 Funktionen je Farbe
   von Hand gegen Fundstelle und Marker prüfen (im Ergebnis nennen).
6. Grafik als eine selbständige HTML-Seite (keine externen Skripte oder Schriften):
   - Kopf: Stand (SHA), Datum, Legende mit den fünf Regeln.
   - Gesamtbalken nach Bytes und nach Anzahl Funktionen, je Farbe mit Prozent.
   - Karte je Bereich (Basissegmente, Overlays) in Adressreihenfolge: jede Funktion ein Rechteck, Fläche nach Größe, Farbe nach
     Status; beim Überfahren Dateioffset, Größe, Farbe, Klasse, Fundstellen, Hinweis aus `texte`. Filter nach Farbe.
   - Tabellen: die 20 größten roten und die 20 größten gelben Funktionen (Dateioffset, Größe, Bereich, Hinweis), die grauen mit
     Beleg.
   - Aus dem Original nur Adressen, Größen und kurze Textauszüge wie in `docs/re-abdeckung.md`; keine Bilder, keine Bytes.
7. Ausgabe: Claude als Artifact (zuerst die Artefakttypen auflisten; passt der Typ Dashboard, diesen nehmen, sonst die HTML-Seite
   veröffentlichen). Andere Agenten: HTML-Datei an den Nutzer. Nichts ins Repo.
8. Antwort an den Nutzer **knapp** (`AGENTS.md` §7): Link bzw. Datei, Anteil je Farbe in Bytes (Prozent), die drei größten roten
   Blöcke, Ergebnis der Probe.
