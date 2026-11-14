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
   Spielstände immer mit DOS-8.3-Namen (höchstens 8 Zeichen vor `.HOK`, z. B. `EXPORT1B.HOK`); längere zeigt das Original als
   `EXPORT~1.HOK` (Runde 15, #744).
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

## Aufnahme `runde-13` (Plan Leitstelle 04.11.2026)

Gesammelt aus den offenen Punkten von `docs/analyse/re-runde-13.md` (Frage 7, „nicht entscheidbar“) und allen noch offenen
Zeilen der Gruppe c in `docs/analyse/inventur.md`, die nur mit DOSBox belegbar sind. Videopunkte (A002, A005, A057, A142, A272,
A291, A301) sind ausgenommen. Nummern ab **FILE29** (höchste vorhandene: FILE28); sie rücken auf, wenn eine Nummer schon belegt ist.
Je Schritt sind die Inventur-IDs genannt. Die mit „optional“ markierten Schritte nur machen, wenn sie ohne großen Aufwand gehen.
Bilder heißen `fileNN_<k>_<inhalt>.png`, Notizen des Nutzers kommen wörtlich in die README.

### Sitzung 1 – Neues Spiel: Spielvorbereitung und Spielstart

1. Spielvorbereitung: Modus „nur Weltkugel“ wählen, danach einen zweiten Manager hinzufügen. Fotografieren, ob das Original den
   Start zulässt oder etwas sperrt (A056).
2. Neues Spiel, **ein Manager**, Spieldauer **„eine Saison“**, Verein frei. Spielvorbereitung fotografieren und direkt im ersten
   Hauptmenü sichern als **FILE29** (U15-c, A180 Taktik-Startwerte, A166 Eingesetzt nach Spielstart, A082 Strafen-Managerwert
   bei einem Manager).
3. Mit demselben Spiel bis vor das **erste Heimspiel** spielen, sichern als **FILE30**; nach dem Spiel sichern als **FILE31**
   (A046 Tabellenplatz vor Spieltag 1).

### Sitzung 2 – Kader und Taktik im Handbetrieb, Start FILE05 (20.09.1993)

1. „Automatisch aufstellen“ ausschalten, sichern als **FILE32**. Taktik-Bildschirm öffnen, die Modus-Pfeile oben und unten je
   mehrmals klicken und die Folge notieren (A373). Taktik ohne Änderung verlassen, sichern als **FILE33**. Danach einen Tag weiter,
   sichern als **FILE34** und notieren, ob die Automatik wieder an ist (Taktik-Befund aus `re-runde-13.md`, DGROUP 0x375).
2. Aufstellung bedienen: auf nicht eingesetzte und auf aufgestellte Spieler links klicken, je ein Bild der STATUS-Spalte (A364).
   Ein Bild der Kaderliste mit einem verletzten, **nicht eingesetzten** Spieler (A012).
3. Einen Spieler kaufen. Kader vorher und nachher fotografieren, nach dem Kauf sichern als **FILE35** (A167, A091, A135).
4. Einen **gesperrten** Spieler in der Aufstellung lassen (bis zur ersten Sperre weiterspielen), sichern als **FILE36**, einen Tag
   weiter, sichern als **FILE37** (A189 Gesperrte).
5. Vor einem **Pokalspiel** sichern als **FILE38**, nach dem Pokalspiel als **FILE39**, nach dem folgenden Ligaspiel als **FILE40**
   (A135 Tore je Wettbewerb).
6. Transfermarkt: einen eigenen Spieler auf die Liste setzen und die Liste fotografieren (A380), mit der Maus über einem Eintrag
   fotografieren (A382). Ein Angebot für einen Spieler auf dem Markt abwarten oder abgeben, Angebotsfenster fotografieren (A377).
   Vertragsverhandlung über buntem Hintergrund fotografieren (A374).
7. Finanzen: einen Regler ziehen, loslassen, fotografieren (A313). Eine Nachricht öffnen und die Leiste fotografieren (A321).
   Historie fotografieren (A357).
8. Optional: einen Spieler **ausleihen** bzw. verleihen und den Stand vor und nach dem Leihende sichern als **FILE41/FILE42**
   (A115).

### Sitzung 3 – Stadion ausbauen (Fortsetzung von Sitzung 2 oder ein Stand mit viel Geld)

1. Einen Ausbau mit Bauzeit beauftragen. Während der Bauzeit das Stadion fotografieren, auch die Tabelle „Bau“ (rechte Maustaste
   unten im Bild schaltet „Ist/Bau“ um), sichern als **FILE43** (Spur `ui-rest`: Farbe der Bau-Tabelle, Zeile „(Noch n Wochen)“).
2. Gastronomie Stufe 1 und 2 sowie Kino bauen, nach Fertigstellung das Stadionbild fotografieren, sichern als **FILE44**
   (A323, A325).

### Sitzung 4 – Saisonende mit Kaderabgang, Start ein Vereinsspiel kurz vor dem 29. April

1. Einen Spieler mit auslaufendem Vertrag nicht verlängern. Saisonende-Verhandlung fotografieren, auch die Liste dahinter (A362).
   Vor dem 29. April sichern als **FILE45**, nach dem Saisonwechsel im ersten Hauptmenü als **FILE46** und den Kader fotografieren
   (A085, A091 Abgang am Saisonende).

### Sitzung 5 – Bundestrainer, Start FILE11 (14.04.1994) oder FILE18

1. Die Bundestrainer-Aufstellung **ganz leeren**, sichern als **FILE47**, einen Tag weiter, sichern als **FILE48** (A009).
2. WM-Kader auf „auto“ stellen, danach einen Spieler **von Hand** aufnehmen, Kader fotografieren, sichern als **FILE49** (A085).
3. Nach der Gruppenphase aus dem Menü zu den **Endrundenspielen** gehen und jeden Schritt fotografieren (A293, „Endrunde“).

### Sitzung 6 (optional) – Speichern-Dialog

1. Mehr als 48 Spielstände anlegen (Kopien reichen), Speichern-Dialog mit Seitenwechsel fotografieren (A338).

## Aufnahme `runde-17` (Plan Leitstelle 12.11.2026, R17-D3 = A; nach Lust des Nutzers)

Gebündelt aus `docs/arbeitspakete.md` „Zurückgestellt“ und „Vormerkungen“. Nummern ab **FILE50** (Vorschlag; höchste vorhandene
laut README bestimmen, die Nummern rücken auf). Auswertung erst in der Runde nach Eintreffen (Analysespur, nicht hier).

### Sitzung 1 – Belastung #730 (`analyse/belastung-staerke.md` §7.3), Start FILE02

1. FILE02 laden, den 1. Spieltag **ohne** Mausklick im Live-Spiel spielen; je Spielminute ein Bild der Reihenanzeige beider
   Managerteile (Bildschirmfoto-Taste reicht). Danach im Hauptmenü sichern als **FILE50**.
2. Gegenprobe: FILE02 erneut laden, denselben Spieltag mit einigen Klicks auf die Reihenknöpfe der Managerzeile spielen (Klicks
   kurz notieren), sichern als **FILE51**.
   Dieselben Bilder belegen auch A093 (Technik −10 außerhalb der eigenen Reihe, R788-02).

### Sitzung 2 – Strafminuten #773, neues Spiel

1. Neues Spiel (ein Manager, Verein frei), im ersten Hauptmenü sichern als **FILE52**.
2. Genau den **ersten Spieltag** spielen, im Hauptmenü danach sichern als **FILE53** (Vereinsfelder 0x26 und Strafminuten je Verein).

### Sitzung 3 – Modus „nur Weltkugel“: Highscore

1. Ein Spiel im Modus „nur Weltkugel“ bis zum Saisonende spielen bzw. einen per `hok-patch` vorbereiteten Stand kurz davor
   (Leitstelle legt ihn auf Wunsch ab); Highscore am Saisonende und am Spielende fotografieren, vorher sichern als **FILE54**.

### Sitzung 4 – Einzelpunkte (aus `runde-13`, noch offen)

1. A115: einen Spieler verleihen, Stand vor und nach dem Leihende sichern (**FILE55/FILE56**).
2. A189: einen verletzten oder gesperrten Spieler aufgestellt lassen, sichern, einen Tag weiter, sichern (**FILE57/FILE58**).
3. A321: eine Nachricht öffnen, Nachrichtenleiste fotografieren. A357: Historie fotografieren.
4. A364: in der Aufstellung links auf nicht eingesetzte und auf aufgestellte Spieler klicken, je ein Bild der STATUS-Spalte.

### Sitzung 5 – Exportprobe (Restlücke R758-01)

1. `EXPORT2C.HOK` und die mit `emtool` am aktuellen `main` neu erzeugten Exportstände im Original laden; Kopf, Konto und Kader
   fotografieren (wie EXPORT1C, Referenz-PR #27). Die Leitstelle erzeugt die Stände vorher und nennt ihre SHA-256.

## Aufnahme `runde-19` (Plan Leitstelle 14.11.2026; Sitzung 1 nur, falls Spur `re-belastung` die Ursache von #730 nicht findet – R19-D3 = B, bei Fehlschlag A; Sitzungen 2/3 nach Lust)

Gebündelt aus `docs/arbeitspakete.md` „Zurückgestellt“ (#730 Rest, A093, #838) und zur Gegenprobe für #847. Nummern: nächste
freie laut README (Vorschlag ab **FILE60**). Auswertung erst in der Runde nach Eintreffen (Analysespur, nicht hier).

### Sitzung 1 – Belastung #730 Spieltag 2/3 (`analyse/abgleich-dosbox.md` §1.3), Start FILE50

1. FILE50 laden (Stand nach Spieltag 1). Bis vor den 2. Spieltag spielen (Training wie eingestellt, nichts ändern), im Hauptmenü
   sichern als **FILE60**.
2. Den 2. Spieltag (Auswärtsspiel) **ohne** Mausklick spielen; je Spielminute ein Bild der Reihenanzeige; danach sichern als
   **FILE61**.
3. Bis vor den 3. Spieltag weiterspielen, sichern als **FILE62**; den 3. Spieltag ohne Klick spielen, sichern als **FILE63**.
   Dieselben Bilder belegen A093 (Technik −10 außerhalb der eigenen Reihe).

### Sitzung 2 – Echtzeit „Spiele laufen“ #838

1. Einen Spieltag ohne Managerpaarung im Bild (oder mit Live-Spiel aus) laufen lassen und die Dauer von Minute 1 bis 60 mit der
   Stoppuhr messen; DOSBox-Zyklen (`cycles`) und die Geschwindigkeit aus den Optionen notieren. Am besten zweimal (Geschwindigkeit
   0 und 60).

### Sitzung 3 (optional) – Neuer Manager mitten in der Saison #847

1. Einen Vereinsstand mitten in der Saison laden (z. B. FILE05), sichern als **FILE64** (vorher).
2. Über „Neuer“ einen Manager mit einem Verein aus einer anderen Liga als der gewählten Start-Liga hinzufügen; Tabelle der
   Start-Liga und der Herkunftsliga fotografieren, sichern als **FILE65**.
