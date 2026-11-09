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

* Spielcode (ohne Systemsegmente), Stand nach Runde 14: **90 % der Bytes umgesetzt** (261 statt 257 Funktionen), 0 % genähert, 9 % dokumentiert (nach Runde 12/13: 90 / 1 / 10; nach Runde 11: 88 / 1 / 11; nach Runde 10: 82 / 2 / 16),
  unbekannt nur noch eine Funktion (0x575C3, 421 Byte, bei `livespiel` zurückgestellt). Maßgeblich sind die Zahlen im
  erzeugten Teil. Wegen der Körnung ist „umgesetzt“ eine Obergrenze; was die Funktionskarte als „dokumentiert“ führt, ordnet
  `docs/analyse/inventur.md` ein (fehlt im Remake, vorhanden ohne `[ORIGINAL]`, nicht nötig).
* Am schwächsten belegt war vor Runde 10 der Spielcode in Overlay 0x118 (Transfermarkt), 0x128 (Tabellen/Statistik), 0x110
  (Hauptschleife), 0xF8 (Nationalteam/WM), 0x120 (Meldungen), 0xC0 (Fehlermeldungen) und Basis 0x238, 0x99F, 0x902. Nach
  Runde 10 ist dort nichts mehr unbekannt; den Anteil „dokumentiert“ je Bereich zeigt die Übersicht im erzeugten Teil.
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
| #194 WM-Paarungsansicht | 0x2FC2F (Ov 0xE8) | umgesetzt (`CupPairingsScreen`) | war Lücke Nr. 11 (Runde 8, oben); die Karte führt die Funktion jetzt als umgesetzt |
| #195 Modus 1, erster Halt | Hauptmenü-/Spieltagsschleife 0x44E22 (Ov 0x110) | umgesetzt (34 Fundstellen) | kein unbekannter Code; die Halt-Bedingung nach „Los jetzt!“ ist in 0x44E22 zu suchen |
| #196 Regler Kursverlauf | 0x58A57 (Ov 0x138), Regler 0x58DF6–0x58EC5 | umgesetzt (Bildschirm) | Regler liegt in einer schon belegten Funktion → Körnung, im Issue vollständig beschrieben |
| #204 Meisterdominanz | Lader 0x2EF1B (Ov 0xE0), Managervereine 0x98F8–0x9C63 | umgesetzt | kein Codeloch, Abbildungsfrage |
| #135 Einzelprämien | Saisonende 0x26F20 (Ov 0xD0), Ranglisten 0x25E30 (Ov 0xC8) | umgesetzt | Rangliste und Prämien belegt; offen ist die Umsetzung, nicht die Analyse |
| Credits (#207, läuft in #209) | Auslöser in 0x44E22 (0x4544E–0x45488), Anzeige 0x399D0 (Ov 0xF8, lädt kb\15) | umgesetzt | deckt sich mit #209 (Credits sind das Bild kb\15). Der DGROUP-Text „Besteht aus: Werner Krahe und Jens Onnen.“ (Datei 0x1AA93, Teil einer Firmenvorstellung „Gegr. 1988 …“) gehört zu einem anderen Text, nicht zu den Credits |
| Zeitung (Overlay 502F, `kb\195`) | – | – | **Berichtigung:** kb\195 lädt nur 0x5D90D (Ov 0x140) – das ist der **Highscore**-Hintergrund (belegt in `highscore.md`). „Zeitung“ steht nur als Eintrag 0xEB8 der Optionstabelle 0xE98–0xEC4 (zwischen „Länderspiele:“ und „Hintergrundgrafik“); kein Code liest 0xEB8 direkt (0x2CD1C überspringt 0xEB4–0xEBA) → vermutlich nicht benutzte Option. `anforderungen/bildschirme.md` ist berichtigt; Ov 0x110 enthält keine Zeitung |
| Verletzungen im Spiel | „Verletzt:“ in 0x52520 (Ov 0x130, Live-Anzeige, dokumentiert); „Verletzt.“ (Zeiger 0xB84) in Spielerinfo 0x41683; außerhalb des Spiels 0x07169 (umgesetzt) und Versicherung 0x07089 (umgesetzt, genähert in `MatchEngine.Injuries.cs`) | gemischt | Die Ereignisse selbst liegen in der Spielberechnung (`spielberechnung.md`); 0x07089 (Versicherungszahlung, „Ein Fall für die Versicherung“) war eine kleine Lücke (Karte: umgesetzt) |
| Globale 0x5A14 | 0x28165, 0x2CD1C, 0x34B76, 0x44E22, 0x54C9F, 0x5AF50 | – | **Spielende (Jahr)**: Spielstart setzt 22222 (0x2986E, endlos) bzw. das Jahr, Vergleiche mit 1994 (0x7CA) und 2000 (0x7D0); so schon in `OriginalSaveGame.cs` und `TileLocks.cs` |
| Globale 0x5CF3 | Pokalfunktionen in Ov 0xE8, Sponsorbewertung 0x3F833 | – | **DEB-Pokal-Runde** (`finanzen-rekonstruktion.md`). Damit Auslöser 2 der Handbuchfrage: endliches Spiel (Ende < 2000) und Pokalrunde 4 |
| Globale 0x3BD | 0x26F20, 0x2C1EE, 0x54C9F, 0x5667A, 0x5D04F | – | **Saisonzähler** (erhöht in 0x56476, `highscore.md`). Auslöser 3 der Handbuchfrage: ab der 10. Saison bzw. genau in Saison 9 |
| Globale 0xDE15 | geschrieben in 0x977F (0x9920 löschen, 0x999A setzen), gelesen in 0x0A484 (0xA851/0xA875) | umgesetzt / dokumentiert | Byte je Manager (Index ab 1); „jeder Abschluss ein Tor“ (`spielberechnung.md`). Bedeutung des gesetzten Werts in 0x977F noch zu klären |

<!-- BEGIN generiert: tools/reverse-engineering/funktionskarte.py – nicht von Hand ändern -->

Erzeugt aus `em.exe` (387968 Byte): 679 Funktionen in 29 Bereichen (12 Basissegmente, 17 Overlays), 309525 Byte Code; 6463 Zuordnungen von Fundstellen zu Funktionen.

### Anteile je Status (Bytes)

„Spielcode“ = ohne die Systemsegmente (C-Laufzeit, Speicher, Grafik/Maus, Datei, Timer, Sound, Tastatur), die das Remake durch .NET/MonoGame ersetzt.

| Status | Funktionen | Bytes | Anteil | Funktionen Spielcode | Bytes Spielcode | Anteil Spielcode |
|---|---:|---:|---:|---:|---:|---:|
| umgesetzt | 274 | 246062 | 79 % | 263 | 236306 | 91 % |
| genähert | 3 | 896 | 0 % | 3 | 896 | 0 % |
| dokumentiert | 145 | 29778 | 10 % | 97 | 23278 | 9 % |
| unbekannt | 257 | 32789 | 11 % | 1 | 421 | 0 % |
| **Summe** | 679 | 309525 | 100 % | 364 | 260901 | 100 % |

### Übersicht je Bereich

Basissegment (Segmentwert, Rolle vermutet) oder Overlay (Stub-Selektor, in Klammern der Name der alten Ghidra-Analyse). Anteile in Bytes. Texte = längste benutzte Zeichenketten der größten Funktionen (Hinweis aufs Thema).

| Bereich | Datei | Größe | Funktionen | umgesetzt | genähert | dokumentiert | unbekannt | Rolle / Texte |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Basis 0x0 | 0x01E00–0x0417F | 9088 | 93 | 6 % | 0 % | 4 % | 90 % | C-Laufzeit (Borland C++ 1991: Copyright, INT 21h, rand/LXMUL/LDIV) |
| Basis 0x238 | 0x04180–0x064E9 | 9066 | 15 | 96 % | 0 % | 4 % | 0 % | Spiellogik Basis (Saisonende, Statistik) |
| Basis 0x46e | 0x064E0–0x080CA | 7147 | 14 | 93 % | 0 % | 7 % | 0 % | Spiellogik Basis (Training, Spieler) |
| Basis 0x62c | 0x080C0–0x0AE2A | 11627 | 16 | 98 % | 0 % | 2 % | 0 % | Spiellogik Basis (Spielberechnung, Zufall) |
| Basis 0x902 | 0x0AE20–0x0B7F9 | 2522 | 11 | 52 % | 0 % | 48 % | 0 % | Fensterverwaltung („Zu viele Fenster auf !“) |
| Basis 0x99f | 0x0B7F0–0x0C79E | 4015 | 23 | 33 % | 0 % | 67 % | 0 % | Bilder laden (kb\…vga), Texte, Geldausgabe |
| Basis 0xa99 | 0x0C790–0x0D9F1 | 4706 | 34 | 0 % | 0 % | 25 % | 75 % | Speicher (EMS INT 67h, XMS INT 2Fh), Datei |
| Basis 0xbbf | 0x0D9F0–0x12F58 | 21865 | 78 | 40 % | 0 % | 5 % | 55 % | Grafik und Maus (VGA-Ports, INT 10h, INT 33h) |
| Basis 0x1115 | 0x12F50–0x13DD0 | 3713 | 37 | 11 % | 0 % | 20 % | 69 % | Dateizugriff (Hüllen um die C-Laufzeit) |
| Basis 0x11fd | 0x13DD0–0x143A5 | 1494 | 33 | 0 % | 0 % | 44 % | 56 % | Timer und Interrupts (PIC 0x20/0x21, PIT 0x40/0x43) |
| Basis 0x125b | 0x143B0–0x14D6C | 2493 | 21 | 0 % | 0 % | 9 % | 91 % | Sound Blaster digital (DMA-Ports 0x02–0x0C) |
| Basis 0x12f7 | 0x14D70–0x1621B | 5292 | 19 | 0 % | 0 % | 44 % | 56 % | FM-Treiber SBFMDRV (CmfPlayer.cs), Tastatur (Port 0x60, INT 16h) |
| Ov 0xc0 (337B) | 0x255C0–0x25DB0 | 2033 | 14 | 0 % | 30 % | 70 % | 0 % | „Alles klar!“ |
| Ov 0xc8 (33FB) | 0x25E30–0x26E6A | 4155 | 2 | 100 % | 0 % | 0 % | 0 % | „Die besten“, „Ereignisse“ |
| Ov 0xd0 (34FF) | 0x26F20–0x27F8A | 4203 | 1 | 100 % | 0 % | 0 % | 0 % | „Erhaltene Prämien:“ |
| Ov 0xd8 (3606) | 0x28030–0x2A0C2 | 8339 | 4 | 98 % | 0 % | 2 % | 0 % | „Suchen Sie sich einen“ |
| Ov 0xe0 (3810) | 0x2A1E0–0x2F1EE | 20495 | 18 | 96 % | 0 % | 4 % | 0 % | „JWE Service GmbH“, „(noch keine...)“, „Im ersten Jahr gibt's hi“ |
| Ov 0xe8 (3D11) | 0x2F530–0x352A2 | 23923 | 33 | 97 % | 0 % | 3 % | 0 % | „Endrunde“, „Jugendteams“, „.Spieltag“ |
| Ov 0xf0 (42E9) | 0x35540–0x397BA | 17019 | 26 | 93 % | 0 % | 7 % | 0 % | „Spiele laufen“, „!    Penaltyschiessen   “, „DEB-Pokal“ |
| Ov 0xf8 (4711) | 0x399D0–0x3CD71 | 13218 | 18 | 100 % | 0 % | 0 % | 0 % | „WM Vorbereitung“, „WM-Statistik“, „Spieler in WM-Kader“ |
| Ov 0x100 (4A4C) | 0x3CFA0–0x3FDDD | 11838 | 13 | 100 % | 0 % | 0 % | 0 % | „Überdachung“, „bittet Sie an den Verhan“, „Sitzplätze“ |
| Ov 0x108 (4D30) | 0x3FFE0–0x42FC4 | 12261 | 13 | 98 % | 0 % | 2 % | 0 % | „Platz mehr !“, „Rückennummer des Spieler“, „Schon 11 Mann auf“ |
| Ov 0x110 (502F) | 0x43150–0x45DC3 | 11380 | 20 | 90 % | 0 % | 10 % | 0 % | „autosave.hok“, „Saisonende“, „.Spieltag“ |
| Ov 0x118 (52F7) | 0x46000–0x4ADE1 | 19938 | 18 | 99 % | 0 % | 1 % | 0 % | „ART   NAME              “, „Grundwert“, „Vertragsverhandlung“ |
| Ov 0x120 (57D6) | 0x4B0F0–0x4D1B6 | 8391 | 23 | 64 % | 0 % | 36 % | 0 % | „Wer weiß...“, „An Alle:“, „Keine Nachricht für Sie “ |
| Ov 0x128 (59E3) | 0x4D2F0–0x51BED | 18686 | 18 | 96 % | 0 % | 4 % | 0 % | „Bundesliga“, „PL. TD      VEREIN      “, „Play-Off“ |
| Ov 0x130 (5E73) | 0x51E90–0x57170 | 21217 | 29 | 69 % | 1 % | 30 % | 0 % | „Sie haben zu wenig Speic“, „Strafzeit:“, „ZUSCHAUER“ |
| Ov 0x138 (63A2) | 0x57480–0x5A68A | 12811 | 14 | 84 % | 0 % | 13 % | 3 % | „Herrenhäuser“, „Kosten pro Woche:“, „Kontostand:“ |
| Ov 0x140 (66C3) | 0x5A820–0x5E932 | 16659 | 21 | 97 % | 0 % | 3 % | 0 % | „VERZEICHNIS:“, „Spielstand aus“, „Speichern“ |

### Dünn belegte große Funktionen (> 2048 Byte, höchstens zwei Fundstellen)

Der Status gilt je Funktion: schon eine Fundstelle zählt die ganze Funktion. Diese großen Funktionen sind dadurch vermutlich zu günstig eingestuft.

| Datei | Bereich | Größe | Status | Fundstellen | Vermutung |
|---|---|---:|---|---|---|

### Die 20 größten Lücken (Spielcode, unbekannt oder nur genähert)

| # | Datei | Bereich | Größe | Status | Aufrufer | Vermutung (automatisch) |
|---:|---|---|---:|---|---|---|
| 1 | 0x25B4A | Ov 0xc0 (337B)+0x58A | 615 | genähert | 0x0B019, 0x0C17D, 0x0C729 … | „Alles klar!“, „LIB-Error“, „GRAFIK-Error“, „DISK-Error“ |
| 2 | 0x575C3 | Ov 0x138 (63A2)+0x143 | 421 | unbekannt | 0x567C8 | „toreÖ“ |
| 3 | 0x5434C | Ov 0x130 (5E73)+0x24BC | 212 | genähert | 0x38D96, 0x53C57 | gerufen von 0x38D96 (dokumentiert) |
| 4 | 0x52B91 | Ov 0x130 (5E73)+0xD01 | 69 | genähert | 0x0977F, 0x378C5, 0x37A62 … | gerufen von 0x0977F (umgesetzt) |

### Funktionen mit Zugriff auf offene Globale

Direkte Zugriffe `[g]`, `[g+1]` oder `[reg±g]` im disassemblierten Code.

| Globale (DGROUP) | Funktionen (Status) |
|---|---|
| 0x5A14 | 0x28165 (umgesetzt), 0x2CD1C (umgesetzt), 0x34B76 (umgesetzt), 0x44E22 (umgesetzt), 0x54C9F (umgesetzt), 0x5AF50 (umgesetzt) |
| 0x5CF3 | 0x33B83 (umgesetzt), 0x33E5E (umgesetzt), 0x341AB (umgesetzt), 0x3489D (umgesetzt), 0x34B76 (umgesetzt), 0x3F833 (umgesetzt), 0x44A48 (umgesetzt), 0x45CD0 (umgesetzt) |
| 0x3BD | 0x26F20 (umgesetzt), 0x2C1EE (umgesetzt), 0x54C9F (umgesetzt), 0x5667A (umgesetzt), 0x5D04F (umgesetzt) |
| 0xDE15 | 0x0977F (umgesetzt), 0x0A484 (umgesetzt) |

### Alle unbekannten und nur genäherten Funktionen > 64 Byte (Spielcode)

Vermutung = automatisch abgeleitete Hinweise (benutzte Texte, auch über Zeigertabellen; Bildnummern an den kb\-Lader; Aufrufer); **nur Vermutung**, nicht geprüft.

| Datei | Bereich | Größe | Status | Aufrufer | Vermutung |
|---|---|---:|---|---:|---|
| 0x25B4A | Ov 0xc0 (337B)+0x58A | 615 | genähert | 15 | „Alles klar!“, „LIB-Error“, „GRAFIK-Error“ |
| 0x52B91 | Ov 0x130 (5E73)+0xD01 | 69 | genähert | 4 | gerufen von 0x0977F (umgesetzt) |
| 0x5434C | Ov 0x130 (5E73)+0x24BC | 212 | genähert | 2 | gerufen von 0x38D96 (dokumentiert) |
| 0x575C3 | Ov 0x138 (63A2)+0x143 | 421 | unbekannt | 1 | „toreÖ“ |

<!-- END generiert -->
