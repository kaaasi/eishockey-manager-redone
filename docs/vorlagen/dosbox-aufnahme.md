# Vorlage: DOSBox-Aufnahme mit dem Nutzer (nur Ablage, keine Analyse)

**Kurzaufruf** (der Nutzer schreibt ihn in einen neuen Chat; empfohlenes Modell: **Claude Sonnet 5.5**):

> Repo kaaasi/eishockey-manager-redone. Arbeite nach `docs/vorlagen/dosbox-aufnahme.md`. Aufnahme: `<runde-12 | …>`.

Zweck: Der Agent führt den Nutzer Schritt für Schritt durch DOSBox-Aufnahmen, prüft jeden Spielstand und legt Spielstände und
Bilder im Referenz-Repo ab. **Keine Auswertung, keine Codeänderung**; die Auswertung macht später eine Analysespur. Die Ablage
im Referenz-Repo erfolgt im Auftrag des Nutzers per PR (AGENTS.md §3: ablegen nur Leitstelle und Nutzer per PR); gemergt wird
vom Nutzer.

## Ablauf für den Agenten

1. **Zugriff:** `add_repo` für `kaaasi/eishockey-manager-redone` (access `read`) und `kaaasi/eishockey-manager-referenz` (access
   `push`; ggf. erst per `ToolSearch` mit `select:mcp__claude-code-remote__add_repo` laden). Scheitert das, dem Nutzer melden.
2. **Einrichten:** Code-Repo klonen, `tools/claude/fetch-reference.sh original`, `tools/claude/setup-workspace.sh
   ~/referenz/original/gamedata`, `source ~/claude-tools/env.sh`, `dotnet build src/EishockeyManager.Tools`. Nur `emtool hok dump`
   wird gebraucht (`dotnet run --no-build --project src/EishockeyManager.Tools -- hok dump <datei>`).
3. **Vorhandenes lesen:** `~/referenz/original/spielstaende/README.md` (Tabelle, SHA-256, Bildliste). Höchste vorhandene
   FILE-Nummer bestimmen; die Nummern unten sind ein Vorschlag und rücken bei Belegung auf.
4. **Je Sitzung:** dem Nutzer nur den nächsten Schritt nennen (kurz, nummeriert), auf Dateien und Bilder warten.
   Je hochgeladenem Spielstand sofort `hok dump` (erste ~10 Zeilen): Prüfsummen gültig, Datum, Verein, Liga-Stufe, Konto; passt
   etwas nicht zum Schritt (falsches Datum, ungültige Prüfsumme), den Nutzer bitten, den Schritt zu wiederholen.
   Bilder unverändert übernehmen (DOSBox-Fenster, ungeschnitten).
5. **Ablage:** Branch `daten/<aufnahme>-<sitzung>` vom aktuellen `main` des Referenz-Repos (Sparse-Klon `original/` reicht);
   Spielstände nach `original/spielstaende/`, Bilder nach `original/spielstaende/bilder/` (Namen `fileNN_<k>_<inhalt>.png`).
   README: je Spielstand eine Tabellenzeile (Herkunft, Stand, Beobachtungen des Nutzers wörtlich knapp, Bilder), SHA-256 in den
   Block, Bildliste ergänzen; Zweck je Datei mit der Inventur-ID (A…) nennen. Commit mit Agenten-Trailer, PR gegen `main` des
   Referenz-Repos; **nicht mergen**. Ein PR je Sitzung (oder einer am Ende, wenn der Nutzer es so will).
6. **Abschluss:** kurze Liste an den Nutzer (PR-Nummern, Dateien) und ein Kommentar im Code-Repo am offenen PR der Leitstelle
   oder – falls keiner offen ist – ein Issue „DOSBox-Aufnahme <aufnahme>: abgelegt“ mit Liste der Dateien und Inventur-IDs
   (Label `daten`, falls vorhanden). Keine Änderungen am Code-Repo.

## Aufnahme `runde-12` (Inventur-IDs aus `docs/analyse/inventur.md`)

Allgemein: DOSBox mit der DOS-Version aus `original/`. Nach dem Sichern bricht das Original das Weiterspielen ab und lädt keinen
Stand derselben Sitzung („Keinen heutigen Spielstand laden !“) – dann DOSBox neu starten und den gesicherten Stand laden.

### Sitzung 1 – Bundestrainer („nur Weltkugel“), Start FILE11 (14.04.1994)

1. FILE11 laden. Hauptmenü fotografieren; jede Kachel anklicken, besonders „Mannschaft“, und das Logo – Bild je Reaktion
   (A284, A300).
2. Vor dem nächsten Freundschaftsspiel sichern als **FILE18**; Kader mit Spalte „KO TE FO“ fotografieren (A023). Optional ohne
   eigene Aufstellung spielen (A009).
3. Nach dem Freundschaftsspiel sichern als **FILE19**, Kader erneut fotografieren (A023, A009).
4. In der Aufstellung einen Spieler auf einen besetzten Platz ziehen, fotografieren (Status des verdrängten Spielers, A010).
5. Nach der WM-Gruppenphase die WM-Tabelle fotografieren, neben Zeilen und Knöpfe klicken und fotografieren (A293, A294).
6. Nach dem Ausscheiden Deutschlands beobachten: Hält das Spiel an jedem WM-Tag an oder nur an Spieltagen Deutschlands? Notiz
   des Nutzers in die README (A028).
7. Vor dem 29. April sichern als **FILE20**, nach dem Saisonwechsel (erstes Hauptmenü) als **FILE21** (A058, A163).

### Sitzung 2 – Saisonwechsel als Spielstandpaar, Start FILE16A oder ein Vereinsspiel

1. Bis zum 14. April (Saisonende) spielen, sichern als **FILE22**, Hauptmenü fotografieren.
2. Bis zum ersten Hauptmenü am 10. September spielen, sichern als **FILE23**, Hauptmenü und Kader fotografieren
   (A088, A085, A091, A125).

### Sitzung 3 – Kader, Start FILE05 (20.09.1993)

1. Einen Spieler verletzt oder gesperrt in der Aufstellung lassen (sonst bis zur ersten Verletzung weiterspielen), Aufstellung
   fotografieren, sichern als **FILE24** (A189).
2. Einen Tag weiter, Aufstellung fotografieren, sichern als **FILE25** (A189).
3. Von FILE05 aus einen Spieler verkaufen, Kader vorher und nachher fotografieren, sichern als **FILE26** (A166, A167; Kauf ist
   FILE07).

### Sitzung 4 (optional, für Runde 13: U15-Rest, Entscheidung D7)

1. Neues Spiel, **fest gewählter Verein** (Bundesligist), **Start-Liga 2. Liga**, Spieldauer ≥ 2: Spielvorbereitung und erstes
   Hauptmenü fotografieren (Verein, Liga), sichern als **FILE27**.
2. Neues Spiel wie 1, aber **Spieldauer „eine Saison“**: dieselben Bilder, sichern als **FILE28**.
