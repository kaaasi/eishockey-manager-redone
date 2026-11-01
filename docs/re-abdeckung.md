# Abdeckung von em.exe (Funktionskarte)

Spur `re-abdeckung` (Analyse als Auftakt für Runde 8). Ziel: Runde 8 nach Lücken statt nach Zufallsfunden planen.
Alle Adressen sind **Dateioffsets** in `em.exe` (Basissegmente: 0x1E00 + Segment · 16 + Offset; Overlays: 0x255C0 +
Versatz aus dem Stub, siehe [reverse-engineering.md](reverse-engineering.md), Abschnitte „Overlays“ und „Funktionskarte“).

## Aufruf

```bash
tools/claude/fetch-reference.sh original     # em.exe nach ~/referenz/original/gamedata/
python3 tools/reverse-engineering/funktionskarte.py ~/referenz/original/gamedata/em.exe --write   # erneuert den Block unten
python3 tools/reverse-engineering/funktionskarte.py ~/referenz/original/gamedata/em.exe --csv /tmp/funktionen.csv  # alle Funktionen
```

Braucht nur Python 3 und `ndisasm` (Paket `nasm`). Das Skript schreibt außer Adressen, Größen und kurzen Textauszügen
nichts aus dem Original; die CSV gehört nicht ins Repo. Nach jeder Spur mit neuen Fundstellen neu erzeugen.

## Vorgehen

1. **Bereiche:** Segmenttabelle ab Datei 0x163C0 (8 Byte je Eintrag: Segment, Größe, Flags). Flag 1 mit Größe ≠ 0 =
   Basissegment (12), Flag 3 = Overlay-Stub (17). Stub-Kopf +4 = Overlay-Versatz (zu 0x255C0), +8 = Codegröße; Einträge
   ab +0x20 je 5 Byte (`INT 3Fh`, Wort bei +2 = Offset im Overlay). Basissegmente sind auf Absätze ausgerichtet; die
   ersten Bytes eines Segments können noch zum vorigen gehören (z. B. 0x12F50–0x12F58).
2. **Funktionsanfänge:** Stub-Einträge, Prolog `push bp; mov bp,sp` (Bytefolge `55 8B EC`, auch außerhalb des
   Befehlsrasters – nach Sprungtabellen verliert die lineare Disassemblierung den Takt), Ziele von Fernaufrufen
   (`9A`; im Overlay Segmenttabellen-Selektor, im Basiscode Segmentwert bzw. Stub-Segment) und Nahaufrufen (`E8`, nur
   mit Prolog oder mindestens zwei Aufrufern, sonst sind es oft fehlgelesene Daten). `ENTER` kommt in em.exe nicht vor.
   `ndisasm` läuft mit allen bekannten Anfängen als Synchronisationspunkten, bis keine neuen Ziele mehr auftauchen. Eine
   Funktion reicht bis zum nächsten Anfang. Prüfung: 348 von 364 Funktionen des Spielcodes enden auf `ret`/`retf`/`jmp`,
   die übrigen 16 mit einer Sprungtabelle (Daten nach dem letzten Befehl).
3. **Fundstellen:** Adressen in Kommentarblöcken von `src/**/*.cs` und in Absätzen bzw. Tabellenzeilen von
   `docs/**/*.md` (ohne `docs/archiv/`, `docs/reviews/` und diese Datei). Erkannt werden `Segment:Offset` (Basissegment,
   Basissegment + 0x1000 der alten Ghidra-Analyse, Ghidra-Overlayname wie `3810:4D3B`, Stub-Selektor mit Eintrag wie
   `0x120:0x52`), `Stub 0x… … Offset/Einsprung 0x…`, Bereiche `0xA–0xB` und einzelne Werte `0x…`. Werte ≤ 0xE75A können
   auch DGROUP-Adressen sein. Sie zählen **nie** in eckigen Klammern (`[0x5CF3]` = Inhalt einer Variablen), direkt nach
   Wörtern wie `Liste`, `Tabelle`, `Runde`, `Termin`, `Struktur`, `Managerwert`, `Flag` oder in einer Zeile bzw.
   Tabellenzelle, die `DGROUP` nennt (außer mit eigenem Kennwort). Sonst zählen sie mit Kennwort **im selben Satz**
   (`Datei 0x`, `em.exe`, `Dateioffset`, ohne `DGROUP` dazwischen; ein Kennwort weiter oben im Kommentar reicht nicht),
   genau auf einem Funktionsanfang, oder in Dokumenten, die oben „Dateioffsets“ erklären – dort aber nicht, wenn der Code
   den Wert als DGROUP-Variable benutzt. Nach `DGROUP`, `Feld`, `Index`, `Byte`, `+` und in DGROUP-Aufzählungen bis zum
   Satzende zählt kein Wert. In `formate/spielstand-original.md` meint „Datei“ den Spielstand; dort zählt nur `em.exe`.
   Ausnahmeliste im Skript (`DGROUP_MISLABELLED`): 0x4B2E (Symboltabelle des Saisonendes, in einem Kommentar als „Datei“
   bezeichnet, an den Bytes von em.exe geprüft).
4. **Status je Funktion:** **umgesetzt** = Fundstelle in einem Codekommentar, dessen nächster `[ORIGINAL]`-Marker davor
   (sonst danach) steht; **genähert** = ebenso mit `[APPROX]`; **dokumentiert** = nur in Doku oder in Codekommentaren
   ohne Marker; **unbekannt** = keine Fundstelle. Es gilt der stärkste Status – mit einer Ausnahme: **„Aufruf genannt,
   Formel genähert“** zählt als genähert. Wird eine Funktion unter `[ORIGINAL]` nur als Aufruf genannt (Stub-Eintrag wie
   `0x118:0x70`; belegt ist dann der Aufrufer, nicht die Funktion) und an anderer Stelle als `[APPROX]` geführt, ist
   ihr Status „genähert“.
5. **Hinweise (Vermutung):** benutzte DGROUP-Zeichenketten (Direktwerte und initialisierte Fernzeiger in DGROUP,
   z. B. Texttabellen; auch XOR 0xD9/0xC9 wie bei der Handbuchfrage; Umlaute nach DIN 66003: `{` ä, `|` ö, `}` ü, `~` ß),
   Bildnummern, die direkt vor dem Aufruf des kb\-Laders 0x0C17D (bzw. 0x25764, 0x567C8) geschoben werden, Aufrufer
   und Globale.

## Grenzen

* **„unbekannt“ heißt: keine Fundstelle mit Adresse, nicht „fehlt im Remake“.** Viele Bildschirme sind nach Video
  nachgebaut und nennen keine Adresse (z. B. Transfermarkt, Tabelle, Jugend, Laden/Speichern). Die Spalte „Remake“ in
  der Lückenliste unten ordnet das von Hand ein.
* **Körnung:** Eine Fundstelle zählt die ganze Funktion. Große Funktionen sind damit zu günstig eingestuft (Tabelle
  „Dünn belegt“ im erzeugten Teil; z. B. 0x54C9F mit 6,6 KB zählt als umgesetzt, auch wenn nur Teile belegt sind).
* Bildnummern nur, wenn sie als Konstante unmittelbar vor dem Laderaufruf stehen; berechnete Nummern und Bilder, die
  über Hilfsfunktionen geladen werden, fehlen. Nummer 0 wird nicht gewertet (meist ein Schalter).
* Textauszüge aus Direktwerten können Zufallstreffer sein (ein Zähler, der zufällig auf einen Textanfang zeigt); in
  den Systemsegmenten ist das häufig, deshalb sind sie aus den Listen ausgenommen.
* Fehlzuordnungen von Adressen sind möglich, wenn eine Doku einen DGROUP-Wert ohne Kennwort nennt, der genau auf einen
  Funktionsanfang fällt. Die Stichprobe im PR (#210) fand keinen solchen Fall, aber ein Bereichsende, das zwei Byte in
  die nächste Funktion ragte (`0x5A9AE–0x5AA35`); Bereiche zählen deshalb erst ab 3 Byte Überlappung. Die Gegenprobe
  (zweite Umsetzung) fand DGROUP-Werte, die als Dateioffset zählten (0x05CA0 über `[0x5CF3]`, `Liste 0x5D3B`,
  `Tabelle 0x5CCB`); seitdem gelten die strengeren Regeln aus Schritt 3. Umgekehrt gehen dadurch wenige echte
  Fundstellen ohne Kennwort verloren (z. B. `Tor (0xA88C ff.)` in `spielberechnung.md`, weil der Code 0xA88C auch als
  DGROUP-Wert benutzt) – Fundstellen deshalb mit `Datei 0x…` schreiben.

## Ergebnis in Kürze

* Spielcode (ohne Systemsegmente): rund **54 % der Bytes umgesetzt**, 2 % nur genähert, 9 % dokumentiert, **35 %
  unbekannt** (Zahlen im erzeugten Teil; vor der Berichtigung der DGROUP-Regeln 56/1/11/33 %). Wegen der Körnung ist
  „umgesetzt“ eine Obergrenze.
* Am schwächsten belegt: Overlay **0x118** (Transfermarkt, 72 % unbekannt, weitere 6 % nur genähert), **0x128**
  (Tabellen/Statistik, 68 %), **0x110** (Hauptschleife, Kalender, Infofile, 50 %), **0xF8** (Nationalteam/WM, 42 %),
  **0x120** (Meldungen, 38 %), **0xC0** (Fehlermeldungen, 100 %); im Basiscode **0x238** (79 %, nach der Berichtigung:
  0x041AC und 0x05CA0 waren nur über DGROUP-Werte „umgesetzt“), 0x99F (Bildlader, Texte) und 0x902 (Fenster).
* Systemsegmente (C-Laufzeit 0x0, Speicher 0xA99, Grafik/Maus 0xBBF, Datei 0x1115, Timer 0x11FD, Sound 0x125B/0x12F7)
  ersetzt das Remake durch .NET/MonoGame; sie werden nicht portiert und zählen nicht als Lücke.

## Die 20 größten Lücken mit Vorschlag

Reihenfolge nach Größe (erzeugte Tabelle unten). „Remake“ = gibt es den Teil im Remake schon (Stichwortsuche in
`src/`, ohne Prüfung der Originaltreue)? Vermutungen sind aus Texten, Bildern und Aufrufern abgeleitet und **nicht
geprüft**.

| # | Datei | Größe | Vermutung | Remake | Vorschlag Spur |
|---:|---|---:|---|---|---|
| 1 | 0x465F1 | 5596 | Transferliste: Anzeige, Kaufen, Angebot (Ov 0x118) | ja (`TransferScreen`, nach Video) | **`transfer-original`**: 0x465F1 mit 0x46000 („DM (K)/(L)“), 0x491F9 (Ablöse/Angebot), 0x49A3B; Regeln (Preise, Annahme) belegen |
| 2 | 0x4D385 | 4688 | Vereins-/Gegnerinfo („Höchster H-Sieg“, „Stärke:“, „Noch keine Eintragung“), aus WM-, Europapokal- und Jugendansichten gerufen | unklar | `vereinsinfo` (klein): erst Bildschirm am Video zuordnen |
| 3 | 0x5DBE5 | 3275 | Dateidialog Laden/Speichern („VERZEICHNIS:“, „.HOK“) | ja (`SaveLoadScreen`) | keine eigene Spur; Fundstellen beim nächsten Speichern-Paket nachtragen |
| 4 | 0x4A1DB | 3079 | Vertragsverhandlung (Gehalt, Dauer, Versicherung) | ja (`TransferDialog`, `Player.Contract`) | zu `transfer-original` |
| 5 | 0x4F78D | 2806 | Tabellenansicht („PL. TD VEREIN … BILANZ S-U-N“) | ja (`TableScreen`) | **`tabellen-original`**: 0x4F78D mit Sortierung 0x50283/0x50B45 (Nr. 7, 19) |
| 6 | 0x041AC | 2761 | Spielerdaten-Fenster (kb\10; „GELIEHEN“, „Kaufpreis“, „Alter:“, „Unterzahl/Überzahl“), aus Ranglisten 0x25E30, Aufstellung 0x3FFE0/0x40364 und Transferliste 0x465F1 gerufen; ruft den Spielerwert 0x49D36 | unklar | zu `spielerinfo` (mit Nr. 15) oder `transfer-original`; vorher am Video zuordnen. Bisher fälschlich „umgesetzt“ (nur DGROUP-Tabelle 0x4B2E) |
| 7 | 0x50283 | 2242 | ohne Texte; von Nationalteam, Pokalen und 0x50B45 gerufen → Tabellenberechnung/Sortierung | teilweise (`CupsNationalMode` nennt 0x50E60) | zu `tabellen-original` |
| 8 | 0x2CD1C | 2142 | Spielmodus-Optionen („Neuer Spielmodus:“, Drittelpause, Torszenen, WM-Spiele, Hintergrundgrafik, „RAM: … Bytes frei“) | unklar | **`spielmodus-optionen`**: Optionen und ihre Wirkung belegen (auch #195) |
| 9 | 0x43E21 | 1983 | Kalender/Terminübersicht („Saisonende“, „Spielfrei“, Wochentage, kb\3, kb\20) | unklar | `kalender` mit 0x43B1F/0x43C38/0x445E0  |
| 10 | 0x3901B | 1952 | Penaltyschießen („Penaltyschiessen“, „n.P.“), von 0x32AA3 (K.-o.-Spiel) | ja (`Playoffs`, `[APPROX]`) | **`penalty-original`** (klein): Ablauf nach Verlängerung belegen |
| 11 | 0x2FC2F | 1846 | Paarungen WM/Europapokal „Gruppe … Spieltag“, „Endrunde“ (kb\36) | nein | **#194 (WD-03)** – passt genau |
| 12 | 0x05CA0 | 1822 | Spielplan des Gegners im Info-Fenster („GEGNER“, „DATUM/ERGEBNIS“, „PL.(ST)“, „Reicht mir“), nur von 0x4D385 gerufen | unklar | zu `vereinsinfo` (Nr. 2). Bisher fälschlich „umgesetzt“ (DGROUP `[0x5CF3]`, `0x5D3B`, `0x5CCB`) |
| 13 | 0x3ADE1 | 1765 | WM-Statistik (kb\12) | ja (Menüeintrag) | zu `nationalteam`-Spur (#192/#193) |
| 14 | 0x5353C | 1725 | Live-Spiel-Anzeige („(ausw.)“, kb\19) | nein | ausgeklammert (`livespiel`, Torszenen/Anzeigetafel zuletzt) |
| 15 | 0x41683 | 1693 | Spielerinfo-Fenster (Rückennummer, Tendenz, Verletzung, „IM TEAM/RESERVE“) | ja (`Player.Attributes`) | `spielerinfo` (mit Nr. 6) |
| 16 | 0x52BD6 | 1630 | Live-Spiel, Minutenanzeige („ min.“) | nein | ausgeklammert (`livespiel`) |
| 17 | 0x3A24B | 1599 | Jugendteams-Tabelle (kb\33) | ja (`YouthScreen`) | zu `jugend`-Paket, Fundstellen nachtragen |
| 18 | 0x36169 | 1476 | ohne Texte; aus Spielstart, WM und Pokal gerufen → vermutlich Termin-/Paarungserzeugung | unklar | zuerst einordnen (zu `kalender`) |
| 19 | 0x50B45 | 1284 | Tabellenreihenfolge (Gesamt/Heim/Auswärts), nur `[APPROX]` | genähert | zu `tabellen-original` |
| 20 | 0x2A9AD | 1245 | Bandenwerbung-Angebot („JWE Service GmbH“, Option, kündbar) | ja (`FinanceSponsors`) | zu Werbung/Finanzen, Belege nachtragen |

**Empfehlung für Runde 8** (nach Nutzen fürs Spiel): `transfer-original` (Nr. 1, 4 und Kleinteile in Ov 0x118, zusammen
≈ 12 KB), **`spielerwert`** (0x49D36, siehe unten), `tabellen-original` (Nr. 5, 7, 19, ≈ 6 KB, berührt Platzierungen
und damit Prämien), #194 (Nr. 11), `spielmodus-optionen` (Nr. 8), `kalender` (Nr. 9, 18 und 0x445E0, „Vor dem ersten
Spieltag.“, 1128 B), `vereinsinfo` (Nr. 2, 12). Nr. 14/16 bleiben ausgeklammert. Aus den 20 größten gefallen sind
0x445E0 (zu `kalender`) und der kb\-Bildlader 0x0C17D (keine Spur).

### Spielerwert 0x49D36 (Vorschlag: eigene Spur `spielerwert`)

0x49D36 (Ov 0x118 + 0x3D36, Einsprung `0x118:0x70`, 1189 B, 12 aufrufende Funktionen an 21 Aufrufstellen; die frühere Angabe „13“ war ein Zählfehler, #225) berechnet den Wert eines Spielers. Er
fließt in Transfers (Transferliste 0x465F1, Vertragsverhandlung 0x4A1DB, Aufstellung 0x42CB1), Highscore (Vermögen
0x5D04F), Sponsorbewertung 0x3F833 und Saisonende 0x26F20. Status **genähert**: Das Remake nennt den Aufruf
(`Highscore.cs`, `[ORIGINAL]`), rechnet aber mit „Kaufpreis des Remakes · 3“ statt der Originalformel
(`Highscore.cs`, `FinanceSponsorRating.cs`, beide `[APPROX]`). Vorschlag: eigene Spur **`spielerwert`** – Formel aus
0x49D36 belegen (Emulatorprobe wie bei 0x5D04F), dann in Transfers, Highscore und Geld einsetzen (Mehrsaisonprobe,
weil Geld und Kader berührt sind). Alternativ als erster Teil von `transfer-original`.

## Einordnung bekannter offener Punkte

| Punkt | Funktion(en) | Status | Einordnung |
|---|---|---|---|
| #192 Taktik Nationalteam | 0x08365 (Basis 0x62C), Aufrufe aus 0x36B5D (Ov 0xF0) | umgesetzt | Funktion belegt; der fehlende Startwert ist ein Zustandsthema, keine Lücke im Code |
| #193 Länderwerte nach Freundschaftsspielen | Herkunft 0x952C/0x977F (Basis 0x62C), Freundschaftsspiel 0x30365 | umgesetzt / dokumentiert | 0x30365 nur dokumentiert; Spur nach #186 |
| #194 WM-Paarungsansicht | 0x2FC2F (Ov 0xE8) | unbekannt | Lücke Nr. 11 |
| #195 Modus 1, erster Halt | Hauptmenü-/Spieltagsschleife 0x44E22 (Ov 0x110) | umgesetzt (34 Fundstellen) | kein unbekannter Code; die Halt-Bedingung nach „Los jetzt!“ ist in 0x44E22 zu suchen |
| #196 Regler Kursverlauf | 0x58A57 (Ov 0x138), Regler 0x58DF6–0x58EC5 | umgesetzt (Bildschirm) | Regler liegt in einer schon belegten Funktion → Körnung, im Issue vollständig beschrieben |
| #204 Meisterdominanz | Lader 0x2EF1B (Ov 0xE0), Managervereine 0x98F8–0x9C63 | umgesetzt | kein Codeloch, Abbildungsfrage |
| #135 Einzelprämien | Saisonende 0x26F20 (Ov 0xD0), Ranglisten 0x25E30 (Ov 0xC8) | umgesetzt | Rangliste und Prämien belegt; offen ist die Umsetzung, nicht die Analyse |
| Credits (#207, läuft in #209) | Auslöser in 0x44E22 (0x4544E–0x45488), Anzeige 0x399D0 (Ov 0xF8, lädt kb\15) | umgesetzt | deckt sich mit #209 (Credits sind das Bild kb\15). Der DGROUP-Text „Besteht aus: Werner Krahe und Jens Onnen.“ (Datei 0x1AA93, Teil einer Firmenvorstellung „Gegr. 1988 …“) gehört zu einem anderen Text, nicht zu den Credits |
| Zeitung (Overlay 502F, `kb\195`) | – | – | **Berichtigung:** kb\195 lädt nur 0x5D90D (Ov 0x140) – das ist der **Highscore**-Hintergrund (belegt in `highscore.md`). „Zeitung“ steht nur als Eintrag 0xEB8 der Optionstabelle 0xE98–0xEC4 (zwischen „Länderspiele:“ und „Hintergrundgrafik“); kein Code liest 0xEB8 direkt (0x2CD1C überspringt 0xEB4–0xEBA) → vermutlich nicht benutzte Option. `anforderungen/bildschirme.md` ist berichtigt; Ov 0x110 enthält keine Zeitung |
| Verletzungen im Spiel | „Verletzt:“ in 0x52520 (Ov 0x130, Live-Anzeige, dokumentiert); „Verletzt.“ (Zeiger 0xB84) in Spielerinfo 0x41683; außerhalb des Spiels 0x07169 (umgesetzt) und Versicherung 0x07089 (unbekannt) | gemischt | Die Ereignisse selbst liegen in der Spielberechnung (`spielberechnung.md`); 0x07089 (Versicherungszahlung, „Ein Fall für die Versicherung“) ist eine kleine Lücke |
| Globale 0x5A14 | 0x28165, 0x2CD1C, 0x34B76, 0x44E22, 0x54C9F, 0x5AF50 | – | **Spielende (Jahr)**: Spielstart setzt 22222 (0x2986E, endlos) bzw. das Jahr, Vergleiche mit 1994 (0x7CA) und 2000 (0x7D0); so schon in `OriginalSaveGame.cs` und `TileLocks.cs` |
| Globale 0x5CF3 | Pokalfunktionen in Ov 0xE8, Sponsorbewertung 0x3F833 | – | **DEB-Pokal-Runde** (`finanzen-rekonstruktion.md`). Damit Auslöser 2 der Handbuchfrage: endliches Spiel (Ende < 2000) und Pokalrunde 4 |
| Globale 0x3BD | 0x26F20, 0x2C1EE, 0x54C9F, 0x5667A, 0x5D04F | – | **Saisonzähler** (erhöht in 0x56476, `highscore.md`). Auslöser 3 der Handbuchfrage: ab der 10. Saison bzw. genau in Saison 9 |
| Globale 0xDE15 | geschrieben in 0x977F (0x9920 löschen, 0x999A setzen), gelesen in 0x0A484 (0xA851/0xA875) | umgesetzt / dokumentiert | Byte je Manager (Index ab 1); „jeder Abschluss ein Tor“ (`spielberechnung.md`). Bedeutung des gesetzten Werts in 0x977F noch zu klären |

<!-- BEGIN generiert: tools/reverse-engineering/funktionskarte.py – nicht von Hand ändern -->

Erzeugt aus `em.exe` (387968 Byte): 679 Funktionen in 29 Bereichen (12 Basissegmente, 17 Overlays), 309525 Byte Code; 1558 Zuordnungen von Fundstellen zu Funktionen.

### Anteile je Status (Bytes)

„Spielcode“ = ohne die Systemsegmente (C-Laufzeit, Speicher, Grafik/Maus, Datei, Timer, Sound, Tastatur), die das Remake durch .NET/MonoGame ersetzt.

| Status | Funktionen | Bytes | Anteil | Funktionen Spielcode | Bytes Spielcode | Anteil Spielcode |
|---|---:|---:|---:|---:|---:|---:|
| umgesetzt | 136 | 169395 | 55 % | 132 | 159765 | 61 % |
| genähert | 5 | 2564 | 1 % | 5 | 2564 | 1 % |
| dokumentiert | 82 | 63147 | 20 % | 70 | 59837 | 23 % |
| unbekannt | 456 | 74419 | 24 % | 157 | 38735 | 15 % |
| **Summe** | 679 | 309525 | 100 % | 364 | 260901 | 100 % |

### Übersicht je Bereich

Basissegment (Segmentwert, Rolle vermutet) oder Overlay (Stub-Selektor, in Klammern der Name der alten Ghidra-Analyse). Anteile in Bytes. Texte = längste benutzte Zeichenketten der größten Funktionen (Hinweis aufs Thema).

| Bereich | Datei | Größe | Funktionen | umgesetzt | genähert | dokumentiert | unbekannt | Rolle / Texte |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Basis 0x0 | 0x01E00–0x0417F | 9088 | 93 | 5 % | 0 % | 1 % | 93 % | C-Laufzeit (Borland C++ 1991: Copyright, INT 21h, rand/LXMUL/LDIV) |
| Basis 0x238 | 0x04180–0x064E9 | 9066 | 15 | 51 % | 4 % | 31 % | 14 % | Spiellogik Basis (Saisonende, Statistik) |
| Basis 0x46e | 0x064E0–0x080CA | 7147 | 14 | 62 % | 0 % | 22 % | 16 % | Spiellogik Basis (Training, Spieler) |
| Basis 0x62c | 0x080C0–0x0AE2A | 11627 | 16 | 68 % | 0 % | 26 % | 6 % | Spiellogik Basis (Spielberechnung, Zufall) |
| Basis 0x902 | 0x0AE20–0x0B7F9 | 2522 | 11 | 14 % | 0 % | 37 % | 49 % | Fensterverwaltung („Zu viele Fenster auf !“) |
| Basis 0x99f | 0x0B7F0–0x0C79E | 4015 | 23 | 9 % | 0 % | 51 % | 40 % | Bilder laden (kb\…vga), Texte, Geldausgabe |
| Basis 0xa99 | 0x0C790–0x0D9F1 | 4706 | 34 | 0 % | 0 % | 7 % | 93 % | Speicher (EMS INT 67h, XMS INT 2Fh), Datei |
| Basis 0xbbf | 0x0D9F0–0x12F58 | 21865 | 78 | 40 % | 0 % | 1 % | 59 % | Grafik und Maus (VGA-Ports, INT 10h, INT 33h) |
| Basis 0x1115 | 0x12F50–0x13DD0 | 3713 | 37 | 9 % | 0 % | 1 % | 90 % | Dateizugriff (Hüllen um die C-Laufzeit) |
| Basis 0x11fd | 0x13DD0–0x143A5 | 1494 | 33 | 0 % | 0 % | 0 % | 100 % | Timer und Interrupts (PIC 0x20/0x21, PIT 0x40/0x43) |
| Basis 0x125b | 0x143B0–0x14D6C | 2493 | 21 | 0 % | 0 % | 9 % | 91 % | Sound Blaster digital (DMA-Ports 0x02–0x0C) |
| Basis 0x12f7 | 0x14D70–0x1621B | 5292 | 19 | 0 % | 0 % | 44 % | 56 % | FM-Treiber SBFMDRV (CmfPlayer.cs), Tastatur (Port 0x60, INT 16h) |
| Ov 0xc0 (337B) | 0x255C0–0x25DB0 | 2033 | 14 | 0 % | 0 % | 0 % | 100 % | „Alles klar!“ |
| Ov 0xc8 (33FB) | 0x25E30–0x26E6A | 4155 | 2 | 95 % | 0 % | 0 % | 5 % | „Die besten“, „Ereignisse“ |
| Ov 0xd0 (34FF) | 0x26F20–0x27F8A | 4203 | 1 | 100 % | 0 % | 0 % | 0 % | „Erhaltene Prämien:“ |
| Ov 0xd8 (3606) | 0x28030–0x2A0C2 | 8339 | 4 | 96 % | 0 % | 0 % | 4 % | „Suchen Sie sich einen“ |
| Ov 0xe0 (3810) | 0x2A1E0–0x2F1EE | 20495 | 18 | 54 % | 0 % | 35 % | 11 % | „JWE Service GmbH“, „(noch keine...)“, „Im ersten Jahr gibt's hi“ |
| Ov 0xe8 (3D11) | 0x2F530–0x352A2 | 23923 | 33 | 70 % | 0 % | 24 % | 6 % | „Endrunde“, „Jugendteams“, „.Spieltag“ |
| Ov 0xf0 (42E9) | 0x35540–0x397BA | 17019 | 26 | 60 % | 3 % | 29 % | 7 % | „Spiele laufen“, „!    Penaltyschiessen   “, „DEB-Pokal“ |
| Ov 0xf8 (4711) | 0x399D0–0x3CD71 | 13218 | 18 | 58 % | 0 % | 26 % | 16 % | „WM Vorbereitung“, „WM-Statistik“, „Spieler in WM-Kader“ |
| Ov 0x100 (4A4C) | 0x3CFA0–0x3FDDD | 11838 | 13 | 78 % | 0 % | 7 % | 15 % | „Überdachung“, „bittet Sie an den Verhan“, „Sitzplätze“ |
| Ov 0x108 (4D30) | 0x3FFE0–0x42FC4 | 12261 | 13 | 84 % | 0 % | 6 % | 10 % | „Platz mehr !“, „Rückennummer des Spieler“, „Schon 11 Mann auf“ |
| Ov 0x110 (502F) | 0x43150–0x45DC3 | 11380 | 20 | 49 % | 0 % | 35 % | 16 % | „autosave.hok“, „Saisonende“, „.Spieltag“ |
| Ov 0x118 (52F7) | 0x46000–0x4ADE1 | 19938 | 18 | 63 % | 0 % | 28 % | 10 % | „ART   NAME              “, „Grundwert“, „Vertragsverhandlung“ |
| Ov 0x120 (57D6) | 0x4B0F0–0x4D1B6 | 8391 | 23 | 16 % | 20 % | 31 % | 33 % | „Wer weiß...“, „An Alle:“, „Keine Nachricht für Sie “ |
| Ov 0x128 (59E3) | 0x4D2F0–0x51BED | 18686 | 18 | 60 % | 0 % | 27 % | 14 % | „Bundesliga“, „PL. TD      VEREIN      “, „Play-Off“ |
| Ov 0x130 (5E73) | 0x51E90–0x57170 | 21217 | 29 | 53 % | 0 % | 16 % | 31 % | „Sie haben zu wenig Speic“, „Strafzeit:“, „ZUSCHAUER“ |
| Ov 0x138 (63A2) | 0x57480–0x5A68A | 12811 | 14 | 58 % | 0 % | 20 % | 21 % | „Herrenhäuser“, „Kosten pro Woche:“, „Kontostand:“ |
| Ov 0x140 (66C3) | 0x5A820–0x5E932 | 16659 | 21 | 68 % | 0 % | 21 % | 11 % | „VERZEICHNIS:“, „Spielstand aus“, „Speichern“ |

### Dünn belegte große Funktionen (> 2048 Byte, höchstens zwei Fundstellen)

Der Status gilt je Funktion: schon eine Fundstelle zählt die ganze Funktion. Diese großen Funktionen sind dadurch vermutlich zu günstig eingestuft.

| Datei | Bereich | Größe | Status | Fundstellen | Vermutung |
|---|---|---:|---|---|---|
| 0x3D7F8 | Ov 0x100 (4A4C)+0x858 | 3379 | umgesetzt | `docs/finanzen-rekonstruktion.md:233`, `src/EishockeyManager.Core/FinanceStadium.cs:64` | kb\8,14; „Überdachung“, „Eintritt“, „Ja.“ |
| 0x5DBE5 | Ov 0x140 (66C3)+0x33C5 | 3275 | dokumentiert | `docs/arbeitspakete.md:295` | „.HOK“, „FILE“, „VERZEICHNIS: “ |
| 0x59DBF | Ov 0x138 (63A2)+0x293F | 2252 | dokumentiert | `docs/reverse-engineering.md:120`, `src/EishockeyManager.UI/Screens/Team/TrainingScreen.cs:7` | kb\5; „Kosten pro Woche:“, „ DM“ |

### Die 20 größten Lücken (Spielcode, unbekannt oder nur genähert)

| # | Datei | Bereich | Größe | Status | Aufrufer | Vermutung (automatisch) |
|---:|---|---|---:|---|---|---|
| 1 | 0x5353C | Ov 0x130 (5E73)+0x16AC | 1725 | unbekannt | 0x37A62, 0x3901B, 0x5434C … | kb\19; „(ausw.)“ |
| 2 | 0x52BD6 | Ov 0x130 (5E73)+0xD46 | 1630 | unbekannt | 0x37A62, 0x5353C, 0x53C57 | „ min.“ |
| 3 | 0x4C246 | Ov 0x120 (57D6)+0x1156 | 1136 | genähert | 0x063BE, 0x0C0B8, 0x26F20 … | „An Alle:“ |
| 4 | 0x3D45F | Ov 0x100 (4A4C)+0x4BF | 921 | unbekannt | 0x3D7F8 | „Überdachung“, „Sitzplätze“, „OHNE DACH“, „Konto“ |
| 5 | 0x5B661 | Ov 0x140 (66C3)+0xE41 | 892 | unbekannt | 0x5DBE5 | „.Spieltag“, „Bundesliga“, „spielstand“, „ Jahr“ |
| 6 | 0x39DEC | Ov 0xf8 (4711)+0x41C | 847 | unbekannt | 0x54C9F | gerufen von 0x54C9F (umgesetzt) |
| 7 | 0x2A6A2 | Ov 0xe0 (3810)+0x4C2 | 779 | unbekannt | 0x2B0C0 | gerufen von 0x2B0C0 (JWE Service GmbH) |
| 8 | 0x4B2F6 | Ov 0x120 (57D6)+0x206 | 766 | unbekannt | 0x07169, 0x26F20, 0x4B2BA | „Januar“, „$ bietet Ihnen an, $^Jahre z“ |
| 9 | 0x57ECF | Ov 0x138 (63A2)+0xA4F | 755 | unbekannt | 0x58A57 | „Kontostand:“, „ DM“ |
| 10 | 0x5ACA0 | Ov 0x140 (66C3)+0x480 | 688 | unbekannt | 0x28165, 0x54C9F | gerufen von 0x28165 (Suchen Sie sich einen) |
| 11 | 0x432EC | Ov 0x110 (502F)+0x19C | 682 | unbekannt | 0x44E22 | „infofile“, „KEIN INFOFILE VORHANDEN !“, „TASTE!“, „Neueste Informationen zum Ei“ |
| 12 | 0x2ECA0 | Ov 0xe0 (3810)+0x4AC0 | 635 | unbekannt | 0x2D5AE, 0x4D385 | gerufen von 0x2D5AE (!Serien) |
| 13 | 0x4BA58 | Ov 0x120 (57D6)+0x968 | 628 | unbekannt | 0x2B0C0, 0x2C1EE, 0x2E356 … | gerufen von 0x2B0C0 (JWE Service GmbH) |
| 14 | 0x07C7E | Basis 0x46e+0x179E | 627 | unbekannt | 0x54C9F | gerufen von 0x54C9F (umgesetzt) |
| 15 | 0x25B4A | Ov 0xc0 (337B)+0x58A | 615 | unbekannt | 0x0B019, 0x0C17D, 0x0C729 … | „Alles klar!“, „LIB-Error“, „GRAFIK-Error“, „DISK-Error“ |
| 16 | 0x581C2 | Ov 0x138 (63A2)+0xD42 | 583 | unbekannt | 0x58A57 | „Kurs:“, „ St.“, „ DM“ |
| 17 | 0x39BBD | Ov 0xf8 (4711)+0x1ED | 559 | unbekannt | 0x3A24B | „Schüler“ |
| 18 | 0x386D1 | Ov 0xf0 (42E9)+0x3191 | 549 | genähert | 0x541D9 | gerufen von 0x541D9 (umgesetzt) |
| 19 | 0x463CC | Ov 0x118 (52F7)+0x3CC | 549 | unbekannt | 0x465F1 | „Kontostand:“, „ DM“ |
| 20 | 0x48E62 | Ov 0x118 (52F7)+0x2E62 | 536 | unbekannt | 0x47F2B | gerufen von 0x47F2B (Grundwert) |

### Funktionen mit Zugriff auf offene Globale

Direkte Zugriffe `[g]`, `[g+1]` oder `[reg±g]` im disassemblierten Code.

| Globale (DGROUP) | Funktionen (Status) |
|---|---|
| 0x5A14 | 0x28165 (umgesetzt), 0x2CD1C (dokumentiert), 0x34B76 (umgesetzt), 0x44E22 (umgesetzt), 0x54C9F (umgesetzt), 0x5AF50 (umgesetzt) |
| 0x5CF3 | 0x33B83 (umgesetzt), 0x33E5E (umgesetzt), 0x341AB (umgesetzt), 0x3489D (umgesetzt), 0x34B76 (umgesetzt), 0x3F833 (umgesetzt), 0x44A48 (umgesetzt), 0x45CD0 (unbekannt) |
| 0x3BD | 0x26F20 (umgesetzt), 0x2C1EE (umgesetzt), 0x54C9F (umgesetzt), 0x5667A (umgesetzt), 0x5D04F (umgesetzt) |
| 0xDE15 | 0x0977F (umgesetzt), 0x0A484 (umgesetzt) |

### Alle unbekannten und nur genäherten Funktionen > 64 Byte (Spielcode)

Vermutung = automatisch abgeleitete Hinweise (benutzte Texte, auch über Zeigertabellen; Bildnummern an den kb\-Lader; Aufrufer); **nur Vermutung**, nicht geprüft.

| Datei | Bereich | Größe | Status | Aufrufer | Vermutung |
|---|---|---:|---|---:|---|
| 0x054D9 | Basis 0x238+0x1359 | 72 | unbekannt | 1 | gerufen von 0x05521 (dokumentiert) |
| 0x057E3 | Basis 0x238+0x1663 | 249 | unbekannt | 1 | gerufen von 0x437D5 (umgesetzt) |
| 0x058DC | Basis 0x238+0x175C | 220 | unbekannt | 1 | gerufen von 0x465F1 (ART   NAME              ) |
| 0x059B8 | Basis 0x238+0x1838 | 354 | genähert | 2 | gerufen von 0x05521 (dokumentiert) |
| 0x05B1A | Basis 0x238+0x199A | 390 | unbekannt | 2 | „Schädelbruch“ |
| 0x063BE | Basis 0x238+0x223E | 267 | unbekannt | 1 | „Nehmen Sie einen Sponsor-“, „zuschuß von“, „Aber immer!“ |
| 0x06CDF | Basis 0x46e+0x7FF | 160 | unbekannt | 2 | gerufen von 0x06CA6 (umgesetzt) |
| 0x06FA5 | Basis 0x46e+0xAC5 | 228 | unbekannt | 1 | „Ihre Talentsuche war ein Erf“, „Ein junger Spieler stößt zu “, „Ihr Talentspäher gibt die“ |
| 0x07C7E | Basis 0x46e+0x179E | 627 | unbekannt | 1 | gerufen von 0x54C9F (umgesetzt) |
| 0x07EF1 | Basis 0x46e+0x1A11 | 122 | unbekannt | 2 | Globale 0x7C0, 0x48FB, 0x48FE, 0x9657 |
| 0x08E5F | Basis 0x62c+0xD9F | 147 | unbekannt | 2 | gerufen von 0x08EF2 (umgesetzt) |
| 0x091FF | Basis 0x62c+0x113F | 411 | unbekannt | 1 | gerufen von 0x08EF2 (umgesetzt) |
| 0x0AD7A | Basis 0x62c+0x2CBA | 177 | unbekannt | 0 | „Pokal“ |
| 0x0AE2B | Basis 0x902+0xB | 354 | unbekannt | 4 | gerufen von 0x28165 (Suchen Sie sich einen) |
| 0x0AF8D | Basis 0x902+0x16D | 140 | unbekannt | 3 | gerufen von 0x28165 (Suchen Sie sich einen) |
| 0x0B019 | Basis 0x902+0x1F9 | 205 | unbekannt | 2 | gerufen von 0x0B155 (Zu viele Fenster auf !) |
| 0x0B0E6 | Basis 0x902+0x2C6 | 111 | unbekannt | 9 | gerufen von 0x25E30 (TORSCH.) |
| 0x0B447 | Basis 0x902+0x627 | 352 | unbekannt | 24 | „Fenster_Zu: Fehler !“ |
| 0x0B7B5 | Basis 0x902+0x995 | 69 | unbekannt | 40 | gerufen von 0x04C75 (dokumentiert) |
| 0x0B830 | Basis 0x99f+0x40 | 146 | unbekannt | 17 | gerufen von 0x25B4A (Alles klar!) |
| 0x0B954 | Basis 0x99f+0x164 | 350 | unbekannt | 2 | gerufen von 0x0BAB2 (dokumentiert) |
| 0x0BE71 | Basis 0x99f+0x681 | 92 | unbekannt | 1 | gerufen von 0x0BAFB (dokumentiert) |
| 0x0BEFA | Basis 0x99f+0x70A | 106 | unbekannt | 44 | gerufen von 0x041AC (GELIEHEN) |
| 0x0BF64 | Basis 0x99f+0x774 | 113 | unbekannt | 3 | gerufen von 0x0C17D (kb\) |
| 0x0C0B8 | Basis 0x99f+0x8C8 | 197 | unbekannt | 7 | „Na gut.“, „einlegen.“, „Auto-“ |
| 0x0C5CE | Basis 0x99f+0xDDE | 347 | unbekannt | 56 | kb\22 |
| 0x0C729 | Basis 0x99f+0xF39 | 96 | unbekannt | 13 | gerufen von 0x0C17D (kb\) |
| 0x25644 | Ov 0xc0 (337B)+0x84 | 288 | unbekannt | 2 | gerufen von 0x0C17D (kb\) |
| 0x25764 | Ov 0xc0 (337B)+0x1A4 | 459 | unbekannt | 1 | lädt kb\-Bilder |
| 0x2592F | Ov 0xc0 (337B)+0x36F | 96 | unbekannt | 1 | Globale 0xDE2B, 0xDE31, 0xDE33 |
| 0x2598F | Ov 0xc0 (337B)+0x3CF | 127 | unbekannt | 4 | gerufen von 0x0C17D (kb\) |
| 0x25A0E | Ov 0xc0 (337B)+0x44E | 97 | unbekannt | 1 | Globale 0x49BA, 0x49BC, 0xDE65, 0xDE67 |
| 0x25A6F | Ov 0xc0 (337B)+0x4AF | 70 | unbekannt | 1 | gerufen von 0x25B4A (Alles klar!) |
| 0x25AB5 | Ov 0xc0 (337B)+0x4F5 | 68 | unbekannt | 2 | gerufen von 0x0C17D (kb\) |
| 0x25B4A | Ov 0xc0 (337B)+0x58A | 615 | unbekannt | 15 | „Alles klar!“, „LIB-Error“, „GRAFIK-Error“ |
| 0x26D9A | Ov 0xc8 (33FB)+0xF6A | 209 | unbekannt | 1 | „Ereignisse“, „Saisonende“ |
| 0x28030 | Ov 0xd8 (3606)+0x0 | 170 | unbekannt | 1 | gerufen von 0x28165 (Suchen Sie sich einen) |
| 0x28111 | Ov 0xd8 (3606)+0xE1 | 84 | unbekannt | 2 | gerufen von 0x28165 (Suchen Sie sich einen) |
| 0x2A5F5 | Ov 0xe0 (3810)+0x415 | 173 | unbekannt | 2 | „ DM“ |
| 0x2A6A2 | Ov 0xe0 (3810)+0x4C2 | 779 | unbekannt | 1 | gerufen von 0x2B0C0 (JWE Service GmbH) |
| 0x2B008 | Ov 0xe0 (3810)+0xE28 | 184 | unbekannt | 1 | gerufen von 0x2B0C0 (JWE Service GmbH) |
| 0x2CBC5 | Ov 0xe0 (3810)+0x29E5 | 97 | unbekannt | 1 | gerufen von 0x2CD1C (RAM: ) |
| 0x2CC26 | Ov 0xe0 (3810)+0x2A46 | 246 | unbekannt | 1 | gerufen von 0x2CD1C (RAM: ) |
| 0x2E2CA | Ov 0xe0 (3810)+0x40EA | 140 | unbekannt | 1 | gerufen von 0x2D5AE (!Serien) |
| 0x2ECA0 | Ov 0xe0 (3810)+0x4AC0 | 635 | unbekannt | 2 | gerufen von 0x2D5AE (!Serien) |
| 0x2F530 | Ov 0xe8 (3D11)+0x0 | 134 | unbekannt | 4 | gerufen von 0x2F5B6 (Gruppe) |
| 0x2FB31 | Ov 0xe8 (3D11)+0x601 | 254 | unbekannt | 2 | gerufen von 0x2FC2F (Gruppe) |
| 0x303D9 | Ov 0xe8 (3D11)+0xEA9 | 78 | unbekannt | 1 | gerufen von 0x30852 (dokumentiert) |
| 0x31505 | Ov 0xe8 (3D11)+0x1FD5 | 228 | unbekannt | 2 | gerufen von 0x315E9 (dokumentiert) |
| 0x3183C | Ov 0xe8 (3D11)+0x230C | 248 | unbekannt | 1 | gerufen von 0x3A88A (umgesetzt) |
| 0x34F7D | Ov 0xe8 (3D11)+0x5A4D | 88 | unbekannt | 3 | gerufen von 0x34B76 (umgesetzt) |
| 0x34FF9 | Ov 0xe8 (3D11)+0x5AC9 | 189 | unbekannt | 1 | „Oberliga“, „ GEGEN “ |
| 0x35CBE | Ov 0xf0 (42E9)+0x77E | 159 | unbekannt | 1 | gerufen von 0x3901B (!    Penaltyschiessen   ) |
| 0x35D5D | Ov 0xf0 (42E9)+0x81D | 239 | unbekannt | 7 | gerufen von 0x30365 (Freundschaftsspiel) |
| 0x3672D | Ov 0xf0 (42E9)+0x11ED | 132 | unbekannt | 1 | gerufen von 0x36169 (dokumentiert) |
| 0x367B1 | Ov 0xf0 (42E9)+0x1271 | 222 | unbekannt | 6 | gerufen von 0x05CA0 (2.Runde ) |
| 0x369C7 | Ov 0xf0 (42E9)+0x1487 | 227 | unbekannt | 1 | gerufen von 0x37A62 (Spiele laufen) |
| 0x36AAA | Ov 0xf0 (42E9)+0x156A | 179 | unbekannt | 2 | gerufen von 0x36B5D (umgesetzt) |
| 0x386D1 | Ov 0xf0 (42E9)+0x3191 | 549 | genähert | 1 | gerufen von 0x541D9 (umgesetzt) |
| 0x39AD6 | Ov 0xf8 (4711)+0x106 | 231 | unbekannt | 2 | gerufen von 0x39BBD (Schüler) |
| 0x39BBD | Ov 0xf8 (4711)+0x1ED | 559 | unbekannt | 1 | „Schüler“ |
| 0x39DEC | Ov 0xf8 (4711)+0x41C | 847 | unbekannt | 1 | gerufen von 0x54C9F (umgesetzt) |
| 0x3A13B | Ov 0xf8 (4711)+0x76B | 272 | unbekannt | 1 | gerufen von 0x54C9F (umgesetzt) |
| 0x3CA71 | Ov 0xf8 (4711)+0x30A1 | 78 | unbekannt | 2 | gerufen von 0x3C37C (Spieler in WM-Kader) |
| 0x3CCEB | Ov 0xf8 (4711)+0x331B | 135 | unbekannt | 2 | gerufen von 0x06D7F (dokumentiert) |
| 0x3D316 | Ov 0x100 (4A4C)+0x376 | 329 | unbekannt | 1 | „Überdachung“, „Der Stadion-Ausbau (“, „ist abgeschlossen.“ |
| 0x3D45F | Ov 0x100 (4A4C)+0x4BF | 921 | unbekannt | 1 | „Überdachung“, „Sitzplätze“, „OHNE DACH“ |
| 0x3E52B | Ov 0x100 (4A4C)+0x158B | 300 | unbekannt | 1 | „OHNE DACH“, „ Kosten: “, „ DM“ |
| 0x3EB30 | Ov 0x100 (4A4C)+0x1B90 | 186 | unbekannt | 1 | gerufen von 0x2D5AE (!Serien) |
| 0x40290 | Ov 0x108 (4D30)+0x2B0 | 212 | unbekannt | 2 | gerufen von 0x40364 (Platz mehr !) |
| 0x425B6 | Ov 0x108 (4D30)+0x25D6 | 262 | unbekannt | 6 | gerufen von 0x26F20 (Erhaltene Prämien: ) |
| 0x426BC | Ov 0x108 (4D30)+0x26DC | 380 | unbekannt | 9 | gerufen von 0x07169 (Verletzung außerhalb des) |
| 0x42838 | Ov 0x108 (4D30)+0x2858 | 350 | unbekannt | 1 | Globale 0x375, 0xA70E |
| 0x431CB | Ov 0x110 (502F)+0x7B | 249 | unbekannt | 1 | „Aus d. letzten Spielen:“ |
| 0x432EC | Ov 0x110 (502F)+0x19C | 682 | unbekannt | 1 | „infofile“, „KEIN INFOFILE VORHANDEN !“, „TASTE!“ |
| 0x435CD | Ov 0x110 (502F)+0x47D | 272 | unbekannt | 1 | gerufen von 0x437D5 (umgesetzt) |
| 0x43714 | Ov 0x110 (502F)+0x5C4 | 193 | unbekannt | 1 | gerufen von 0x437D5 (umgesetzt) |
| 0x45CD0 | Ov 0x110 (502F)+0x2B80 | 118 | unbekannt | 1 | gerufen von 0x28165 (Suchen Sie sich einen) |
| 0x45D46 | Ov 0x110 (502F)+0x2BF6 | 126 | unbekannt | 24 | kb\22 |
| 0x463CC | Ov 0x118 (52F7)+0x3CC | 549 | unbekannt | 1 | „Kontostand:“, „ DM“ |
| 0x47BCD | Ov 0x118 (52F7)+0x1BCD | 211 | unbekannt | 1 | gerufen von 0x47F2B (Grundwert) |
| 0x47CA0 | Ov 0x118 (52F7)+0x1CA0 | 78 | unbekannt | 1 | gerufen von 0x47F2B (Grundwert) |
| 0x48E62 | Ov 0x118 (52F7)+0x2E62 | 536 | unbekannt | 1 | gerufen von 0x47F2B (Grundwert) |
| 0x490DE | Ov 0x118 (52F7)+0x30DE | 283 | unbekannt | 3 | „Manager“ |
| 0x4981E | Ov 0x118 (52F7)+0x381E | 255 | unbekannt | 1 | gerufen von 0x494EF (dokumentiert) |
| 0x4B127 | Ov 0x120 (57D6)+0x37 | 329 | unbekannt | 1 | gerufen von 0x54C9F (umgesetzt) |
| 0x4B270 | Ov 0x120 (57D6)+0x180 | 74 | unbekannt | 5 | gerufen von 0x07169 (Verletzung außerhalb des) |
| 0x4B2F6 | Ov 0x120 (57D6)+0x206 | 766 | unbekannt | 3 | „Januar“, „$ bietet Ihnen an, $^Jahre z“ |
| 0x4B9CD | Ov 0x120 (57D6)+0x8DD | 139 | unbekannt | 1 | gerufen von 0x4B5F4 (Keine Nachricht für Sie ) |
| 0x4BA58 | Ov 0x120 (57D6)+0x968 | 628 | unbekannt | 5 | gerufen von 0x2B0C0 (JWE Service GmbH) |
| 0x4BCCC | Ov 0x120 (57D6)+0xBDC | 105 | genähert | 11 | „Na gut.“ |
| 0x4BD7E | Ov 0x120 (57D6)+0xC8E | 104 | unbekannt | 6 | gerufen von 0x34FF9 (Oberliga) |
| 0x4BEBD | Ov 0x120 (57D6)+0xDCD | 309 | unbekannt | 13 | gerufen von 0x041AC (GELIEHEN) |
| 0x4BFF2 | Ov 0x120 (57D6)+0xF02 | 420 | genähert | 2 | gerufen von 0x4C196 (umgesetzt) |
| 0x4C246 | Ov 0x120 (57D6)+0x1156 | 1136 | genähert | 11 | „An Alle:“ |
| 0x4C6B6 | Ov 0x120 (57D6)+0x15C6 | 145 | unbekannt | 1 | gerufen von 0x26F20 (Erhaltene Prämien: ) |
| 0x4CCB4 | Ov 0x120 (57D6)+0x1BC4 | 146 | unbekannt | 1 | gerufen von 0x4C747 (Wer weiß...) |
| 0x4E5D5 | Ov 0x128 (59E3)+0x12E5 | 498 | unbekannt | 1 | „Noch keine Eintragung.“ |
| 0x4F1F1 | Ov 0x128 (59E3)+0x1F01 | 308 | unbekannt | 2 | „Platz, Stärke “ |
| 0x4F325 | Ov 0x128 (59E3)+0x2035 | 463 | unbekannt | 1 | gerufen von 0x50B45 (umgesetzt) |
| 0x4F5A8 | Ov 0x128 (59E3)+0x22B8 | 485 | unbekannt | 1 | gerufen von 0x50283 (umgesetzt) |
| 0x51049 | Ov 0x128 (59E3)+0x3D59 | 234 | unbekannt | 1 | gerufen von 0x37A62 (Spiele laufen) |
| 0x51133 | Ov 0x128 (59E3)+0x3E43 | 211 | unbekannt | 1 | gerufen von 0x50283 (umgesetzt) |
| 0x51AAF | Ov 0x128 (59E3)+0x47BF | 319 | unbekannt | 3 | gerufen von 0x4D385 ( Punkte) |
| 0x51F3D | Ov 0x130 (5E73)+0xAD | 173 | unbekannt | 1 | „Carpe diem !“ |
| 0x51FEA | Ov 0x130 (5E73)+0x15A | 422 | unbekannt | 3 | gerufen von 0x0A484 (umgesetzt) |
| 0x52190 | Ov 0x130 (5E73)+0x300 | 190 | unbekannt | 2 | gerufen von 0x0A484 (umgesetzt) |
| 0x5224E | Ov 0x130 (5E73)+0x3BE | 158 | unbekannt | 2 | gerufen von 0x52520 (Chance:) |
| 0x522EC | Ov 0x130 (5E73)+0x45C | 150 | unbekannt | 3 | gerufen von 0x37A62 (Spiele laufen) |
| 0x52382 | Ov 0x130 (5E73)+0x4F2 | 174 | unbekannt | 2 | gerufen von 0x52520 (Chance:) |
| 0x52430 | Ov 0x130 (5E73)+0x5A0 | 240 | unbekannt | 1 | gerufen von 0x52520 (Chance:) |
| 0x52BD6 | Ov 0x130 (5E73)+0xD46 | 1630 | unbekannt | 3 | „ min.“ |
| 0x53234 | Ov 0x130 (5E73)+0x13A4 | 211 | unbekannt | 2 | gerufen von 0x51E90 (dokumentiert) |
| 0x53307 | Ov 0x130 (5E73)+0x1477 | 201 | unbekannt | 1 | gerufen von 0x52BD6 ( min.) |
| 0x533D0 | Ov 0x130 (5E73)+0x1540 | 131 | unbekannt | 2 | gerufen von 0x51E90 (dokumentiert) |
| 0x53453 | Ov 0x130 (5E73)+0x15C3 | 105 | unbekannt | 4 | gerufen von 0x37A62 (Spiele laufen) |
| 0x534BC | Ov 0x130 (5E73)+0x162C | 76 | unbekannt | 1 | gerufen von 0x38D96 (dokumentiert) |
| 0x5353C | Ov 0x130 (5E73)+0x16AC | 1725 | unbekannt | 4 | kb\19; „(ausw.)“ |
| 0x53BF9 | Ov 0x130 (5E73)+0x1D69 | 94 | unbekannt | 1 | gerufen von 0x53C57 ( ZUSCHAUER) |
| 0x5434C | Ov 0x130 (5E73)+0x24BC | 212 | unbekannt | 2 | gerufen von 0x38D96 (dokumentiert) |
| 0x56EDD | Ov 0x130 (5E73)+0x504D | 523 | unbekannt | 1 | gerufen von 0x567C8 (kb\) |
| 0x570E8 | Ov 0x130 (5E73)+0x5258 | 137 | unbekannt | 2 | „.MINUTE“ |
| 0x575C3 | Ov 0x138 (63A2)+0x143 | 421 | unbekannt | 1 | „toreÖ“ |
| 0x57CFB | Ov 0x138 (63A2)+0x87B | 468 | unbekannt | 1 | „ DM“ |
| 0x57ECF | Ov 0x138 (63A2)+0xA4F | 755 | unbekannt | 1 | „Kontostand:“, „ DM“ |
| 0x581C2 | Ov 0x138 (63A2)+0xD42 | 583 | unbekannt | 1 | „Kurs:“, „ St.“, „ DM“ |
| 0x58409 | Ov 0x138 (63A2)+0xF89 | 441 | unbekannt | 1 | „ St.“, „ DM“ |
| 0x59D71 | Ov 0x138 (63A2)+0x28F1 | 78 | unbekannt | 1 | gerufen von 0x59DBF (Kosten pro Woche:) |
| 0x5A820 | Ov 0x140 (66C3)+0x0 | 66 | unbekannt | 1 | gerufen von 0x4CD46 (@QCCG_BDQRVBQWU (XOR 0xd) |
| 0x5AA34 | Ov 0x140 (66C3)+0x214 | 108 | unbekannt | 3 | gerufen von 0x5AAA0 (umgesetzt) |
| 0x5ACA0 | Ov 0x140 (66C3)+0x480 | 688 | unbekannt | 2 | gerufen von 0x28165 (Suchen Sie sich einen) |
| 0x5B661 | Ov 0x140 (66C3)+0xE41 | 892 | unbekannt | 1 | „.Spieltag“, „Bundesliga“, „spielstand“ |
| 0x5E8B0 | Ov 0x140 (66C3)+0x4090 | 131 | unbekannt | 1 | gerufen von 0x5DBE5 (.HOK) |

<!-- END generiert -->
