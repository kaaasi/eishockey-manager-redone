# Original-Spielstand `*.HOK` (DOS)

Spur `hok-format` (Lesen), `hok-patch` (Schreiben, Abschnitt unten). Leser: `src/EishockeyManager.Formats/OriginalSaveGame.cs`,
Werkzeug `emtool hok dump <datei> [block …]`. Belegt an drei Spielständen des Nutzers (DOSBox, Version DOS/DE;
im Referenz-Repo unter `original/spielstaende/` mit Bildern und README, nie ins Code-Repo kopieren): **FILE01** Neustart (ein Manager, Kölner EC, Bundesliga, Level 3, Dauer 3, Stärke Mittelfeld,
10.09.1993), **FILE02** wie FILE01, nur Eintritt 13 → 17 DM, **FILE03** = FILE02 nach drei Spieltagen (20.09.1993).
Alle drei: 47.949 Byte, beide Prüfsummen gültig. Weitere Stände FILE05–FILE08, FILE10, FILE11 (Abschnitte unten; FILE04 gehört zu PR #191).

## Fundstellen in `em.exe` `[ORIGINAL]`

| Was | Datei-Offset |
|---|---|
| Sichern (Dateiname, Öffnen, Kopf, alle Blöcke) | `0x5BA70`–`0x5C3DD` |
| Klartextkopf schreiben | `0x5B4CB`–`0x5B660` |
| Verschlüsselnd schreiben (Block, danach Puffer zurückverschlüsseln) | `0x5A894`–`0x5A9AD` |
| Laden (Kennungen, ID-Liste, alle Blöcke, Prüfsummen) | `0x5C440`–`0x5D049` |
| Entschlüsselnd lesen | `0x5A9AE`–`0x5AA35` |
| Fehler beim Schreiben/Lesen (Meldung, Abbruch) | `0x5A862` |
| Borland `rand` (Zustand DGROUP `0x5984`), `srand` | `0x24DF`, `0x24CE` |

## Klartextteil (0x00–0xB9)

| Offset | Länge | Inhalt |
|---|---:|---|
| `0x00` | 10 | `KRON_0.99` + Nullbyte (DGROUP `0x5604` über Zeiger `0x55DF`); Laden vergleicht (`0x5C4CA`) |
| `0x0A` | 4 | Jahr (DGROUP `0x5A18`), nur Anzeige im Ladedialog |
| `0x0E` | 1 | Zahl der Manager (`0x386`) |
| `0x0F` | 1 | DGROUP `0x7A9` (nächster Spieltag 1. Liga; FILE01/02: 1, FILE03: 4) |
| `0x10` | 4 × 40 | je Manager: Liga-Stufe (`0x9746` + 0x1AF·m), 1 = Modus „nur Weltkugel“ (`[0xD0B9] = 1`), Name 19 Byte (`0x965B` + 0x1AF·m), Vereinsname 19 Byte |
| `0x38`… | | Einträge hinter der Managerzahl sind **nicht initialisierter Stapel** (in FILE01 etwa `\EM\File01.HOK`) |
| `0xB0` | 4 | Kennung `EH0` + Nullbyte (DGROUP `0x55E7`); Laden vergleicht (`0x5C53D`) |
| `0xB4` | 4 | Spielstand-ID `random(0..0x8FFF) + 1`, vorzeichenerweitert (`0x5BB51`) |
| `0xB8` | 1 | Füllbyte `random(0..255)` ohne Verwendung (`0x5BC07`) |
| `0xB9` | 1 | Startschlüssel `random(0..255) + 0x6E` (8 Bit, `0x5BBF8`) |

Im Modus „nur Weltkugel“ kopiert der Kopf vorher den Namen des Nationalteams (`[0x6AC]`) über den Vereinsnamen des
Managers (`0x5B556`–`0x5B57F`); er wird **nicht** zurückgeschrieben und landet so auch im Vereinsblock (FILE11).

## Verschlüsselung und Prüfsumme `[ORIGINAL]`

Ab `0xBA` folgen 94 Blöcke (Tabelle unten), jeder mit dem fortlaufenden Schlüssel `k` (Start = Byte `0xB9`):

```
verschlüsselt = (klar XOR k) + k      (8 Bit)
k = k + 0xD7                          (= k − 0x29)
summe = summe + klar                  (8 Bit, Startwert = Startschlüssel)
```

Bestätigt an allen drei Dateien. Gegenüber der früheren Notiz in `dateiformate.md`:

- Der Startschlüssel ist `random(0..255) + 0x6E` **modulo 256** (FILE01 `0xE5`, FILE02 `0xB2`, FILE03 `0x06`).
- Die Prüfsumme beginnt mit dem Startschlüssel und steht **zweimal** unverschlüsselt im Strom: nach dem Block `0xA763`
  (Datei `0x8F09`) und als letztes Byte (`0xBB4C`). Die Summe läuft weiter; der Schlüssel `k` wird durch das
  Prüfsummenbyte nicht weitergezählt.
- Nullbytes ergeben `2·k`; deshalb fällt das Dateiende in Schritten von `0x52`.
- Laden: Abweichung einer Prüfsumme setzt Fehlercode 5 (`0x5CBE1`, `0x5CDB8`); der Stand wird trotzdem eingelesen und
  danach die Meldung gezeigt (`0x5CEFA` ff.; Text offen).

## Sperren beim Sichern und Laden (Original) – im Remake **nicht** übernommen

- Nach dem Sichern aus dem Menü setzt das Original DGROUP `0x176` (`0x5C3D5`). Das Hauptmenü verweigert dann das
  Weiterspielen („Sie können nicht mehr weiterspielen. Sie haben schon gespeichert …“, `0x454F5`), bis ein Stand
  geladen wird (`0x5D041` löscht die Sperre) oder das Programm neu startet.
- Jede gesicherte oder geladene Spielstand-ID kommt in eine Liste (DGROUP `0xDD4E`, 50 Einträge, Zähler `0x177`,
  `0x5BB87`/`0x5C5AB`). Laden einer ID aus der Liste wird abgewiesen („Keinen heutigen Spielstand laden !“, Vergleich
  `0x5C572`–`0x5C5A5`). Die Liste ist nicht Teil des Spielstands.
- Beides vom Nutzer im DOSBox-Test bestätigt (30.10.2026). **Entscheidung Nutzer:** Die Sperre gehört nicht ins Remake.
  Das Remake hat sie heute nicht (Stand `main` `a50a799`); ein Import muss ID und Sperre ignorieren.

## Zufallszustand

Der Zustand von Borland `rand` (DGROUP `0x5984`, 32 Bit) liegt in **keinem** gesicherten Block. Beleg zusätzlich:
FILE01 → FILE02 unterscheiden sich im Klartext nur im Eintritt, obwohl beim Sichern drei Zufallszahlen gezogen werden.
Nach dem Laden setzt das Original also mit dem laufenden Generator fort; ein Import kann keinen Originalzustand
übernehmen.

## Blöcke im verschlüsselten Strom

Reihenfolge `[ORIGINAL]` aus den Aufrufen von `0x5A894` (Sichern) und `0x5A9AE` (Laden, gleiche Folge). „Name“ ist
die DGROUP-Adresse; `far:` = Block hinter einem Fernzeiger. Deutungen aus `reverse-engineering.md`,
`finanzen-rekonstruktion.md`, `wettbewerbe.md` und den Vergleichen unten; ohne Eintrag = ungeklärt.
Datei-Offsets gelten für jede Datei (feste Länge). Prüfsummen bei `0x08F09` (nach `A763`) und `0x0BB4C`.

| Datei | Name | Byte | Deutung |
|---|---|---:|---|
| `0x000BA` | `02A0` | 24 | Zinskonto je Manager (6 × 4) |
| `0x000D2` | `020D` | 16 | Talentpreis je Manager (4 × 4, Drittel-DM) |
| `0x000E2` | `0118` | 4 | Setup-Regler Level, Dauer, Start-Liga, Start-Stärke |
| `0x000E6` | `0120` | 4 |  |
| `0x000EA` | `D214` | 128 | Aktienbesitz: 32 · Manager + 8 · Aktie (+0 Kaufpreis, +4 Stück) |
| `0x0016A` | `D0F4` | 288 | Kursverlauf: 4 Aktien × 36 Wörter (Drittel-DM), letztes = Kurs |
| `0x0028A` | `01FD` | 16 | Basiswert je Aktie (Hundertstel-DM) |
| `0x0029A` | `A89A` | 6 |  |
| `0x002A0` | `A891` | 4 | Restdauer der Kurstendenz je Aktie |
| `0x002A4` | `A895` | 4 | Kurstendenz je Aktie (0 fallend, 2 steigend) |
| `0x002A8` | `0178` | 4 | Managerkopf je Manager (0–11, Bild `kb\0`), unbenutzt `0xFF`; das Hauptmenü zeigt diesen Kopf (Review 2026-11-10 B3) |
| `0x002AC` | `017C` | 1 |  |
| `0x002AD` | `A707` | 6 | Taktikmodus Angriff je Manager |
| `0x002B3` | `A701` | 6 | Reihenwechsel-Modus je Manager (0–5) |
| `0x002B9` | `A6FB` | 6 | Taktikmodus Einsatz je Manager |
| `0x002BF` | `CCB2` | 70 | Play-offs: Tore je Spiel (14 · Spiel + Platz; Heimbyte + 30 je Marke, 100 = nicht ausgetragen) |
| `0x00305` | `CCA4` | 14 | Play-offs: Siege je Platz (0x14 = Gast des fünften Termins mit mehr als zwei Siegen, auch bei vorher entschiedener Serie; 0x3168B–0x316BF) |
| `0x00313` | `CCA3` | 1 | Play-offs: Spiel der laufenden Runde (0–4; 5 nach dem Endspiel) |
| `0x00314` | `D0B9` | 1 | Spielmodus Fahne/Weltkugel |
| `0x00315` | `CC73` | 48 | Minuten-Vorgabe umgerechnet (Summe 8; 8 · Manager + 4 · Teil + Reihe) |
| `0x00345` | `CC43` | 48 | Minuten-Vorgabe (8 · Manager + 4 · Teil + Reihe; Teil 0 Abwehr, 1 Sturm) |
| `0x00375` | `CD28` | 48 | Gruppentabellen: Tore je Platz (12 · Wettbewerb + 6 · Gruppe + Platz) |
| `0x003A5` | `CCF8` | 48 | Gruppentabellen: Gegentore je Platz |
| `0x003D5` | `D074` | 1 | DEB-Pokal: Spiel der laufenden Runde (0–2) |
| `0x003D6` | `D014` | 96 | DEB-Pokal: Tore je Spiel (32 · Spiel + Platz, Heimbyte + 30 je Marke, 100 = nicht ausgetragen) |
| `0x00436` | `D075` | 32 | DEB-Pokal: Siege je Platz |
| `0x00456` | `CFFC` | 24 | Gruppenteilnehmer DGROUP 0xCFF4 + 8 · Wettbewerb (+8 Europapokal, +16 Endrunde) |
| `0x0046E` | `CFF8` | 4 | +2 (0xCFFA): Anfang der laufenden Play-off-Runde in 0x5D3B (0, 8, 12) |
| `0x00472` | `CED8` | 288 | Gruppenpaarungen 0x48 · Wettbewerb + 0x24 · Gruppe + 6 · Termin (0 WM, 1 Aufstiegsrunden ab 0xCF20, 2 Europapokal, 3 Endrunde) |
| `0x00592` | `CDB8` | 288 | Gruppentore, Aufbau wie `CED8` |
| `0x006B2` | `CD88` | 48 | Gruppentabellen: Platzliste (Indizes der Teilnehmer, sortiert 0x30C36) |
| `0x006E2` | `CD58` | 48 | Gruppentabellen: Punkte je Platz |
| `0x00712` | `026C` | 48 | Kosten Freundschaftsspiele je Nationalteam |
| `0x00742` | `D0BA` | 4 | Kennzeichen „Ernennung zum Bundestrainer steht an“ je Manager (0x54D19 → 1, nach der Ernennung 0x554BA → 0; an allen Ständen 0) |
| `0x00746` | `06AC` | 1 | Nationalteam (Vereinsindex) |
| `0x00747` | `D0C1` | 1 | Zahl der Freundschaftsspiele |
| `0x00748` | `D0DB` | 8 | Freundschaftsspiele: Heim je Spiel (Satzplatz) |
| `0x00750` | `D0D3` | 8 | Freundschaftsspiele: Gast je Spiel |
| `0x00758` | `D0CB` | 8 | Freundschaftsspiele: Heimtore je Spiel |
| `0x00760` | `D0C3` | 8 | Freundschaftsspiele: Gasttore je Spiel |
| `0x00768` | `0256` | 2 | Highscore-Schlüssel |
| `0x0076A` | `02BC` | 4 | Preisindex |
| `0x0076E` | `02C0` | 4 | Konjunkturfaktor |
| `0x00772` | `A075` | 84 |  |
| `0x007C6` | `A8A1` | 112 | Trainingsplan (28 Byte je Manager) |
| `0x00836` | `D0E4` | 2 |  |
| `0x00838` | `022D` | 6 |  |
| `0x0083E` | `A88D` | 4 |  |
| `0x00842` | `A70E` | 1 | Manager am Zug |
| `0x00843` | `A70D` | 1 |  |
| `0x00844` | `0375` | 12 | „automatisch aufstellen“ je Manager: 0x375 + 2 · m aktuell, 0x376 + 2 · m gesichert |
| `0x00850` | `A0CB` | 1584 | Ergebnisse: 3 Ligen × 0x210, je Spieltag 6 Paarungen × 2 Tore |
| `0x00E80` | `0386` | 1 | Zahl der Manager |
| `0x00E81` | `0387` | 1 | DEB-Pokalsieger |
| `0x00E82` | `D0E6` | 1 | Deutscher Meister |
| `0x00E83` | `0388` | 1 | Europacupsieger |
| `0x00E84` | `0389` | 1 |  |
| `0x00E85` | `038A` | 1 |  |
| `0x00E86` | `038B` | 1 |  |
| `0x00E87` | `965B` | 2586 | Manager (6 × 0x1AF) |
| `0x018A1` | `far:9657` | 9150 | Vereine und Nationalteams (150 × 0x3D) |
| `0x03C5F` | `8257` | 5120 | Tabellensätze 64 × 0x50 ab DGROUP 0x825A (Punkte, Tore, Platzfelder 0x828B, Platz 0x8288, Strafminuten 0x82A1) |
| `0x0505F` | `far:8253` | 6600 | Spieler (200 × 33) |
| `0x06A27` | `5DC3` | 9360 | Kaderplätze (6 × 0x618, Platz 0x34) |
| `0x08EB7` | `5D7B` | 36 |  |
| `0x08EDB` | `5A20` | 4 | Tag |
| `0x08EDF` | `5A1C` | 4 | Monat (0 = Januar) |
| `0x08EE3` | `5A18` | 4 | Jahr |
| `0x08EE7` | `03B7` | 4 | Tageszähler (Spielstart = 1) |
| `0x08EEB` | `03BB` | 2 |  |
| `0x08EED` | `A776` | 10 |  |
| `0x08EF7` | `A763` | 18 |  |
| `0x08F0A` | `A75A` | 2 |  |
| `0x08F0C` | `5CFB` | 128 | Pokalplätze 32 · Wettbewerb (0 DEB-Pokal; +0x60 K.-o.-Plätze WM bzw. Endrunde/Aufstiegsrunde) |
| `0x08F8C` | `5CF3` | 8 | Pokalrunde je Wettbewerb (DEB-Pokal 1–5) |
| `0x08F94` | `5CF0` | 3 |  |
| `0x08F97` | `5CCB` | 36 | Tabelle: Vereinsindex je Liga × 12 + Platz |
| `0x08FBB` | `5C4B` | 128 | Pokaltore des laufenden Tages (nach dem Eintragen gelöscht) |
| `0x0903B` | `07A9` | 8 | 0x7A9–0x7AB nächster Spieltag je Liga; 0x7AE WM-Termine, 0x7AF Europapokal-Termine, 0x7B0 Play-off-Runde (0–2) |
| `0x09043` | `AB7B` | 5524 | Kopf-an-Kopf (+0), Serien (+0xA00), Rekordserien Manager (+0xF40), Rekordgegner (+0xF94), Rekorde (+0x1194) |
| `0x0A5D7` | `021D` | 16 | Wettbewerbsschalter (0x227 DEB-Pokal) |
| `0x0A5E7` | `C313` | 2000 | Werbeangebote: 0x1F4 · Manager + 0x64 · Art + 10 · Partner |
| `0x0ADB7` | `C15B` | 440 | Werbeflächen (0x6E je Manager) |
| `0x0AF6F` | `0ED8` | 1 | 5 − Spiel-Level |
| `0x0AF70` | `5A14` | 2 | Spielende (Jahr; Dauer 0 → 1994, Dauer 3 „∞“ → 22222) |
| `0x0AF72` | `D294` | 2712 |  |
| `0x0BA0A` | `0262` | 2 |  |
| `0x0BA0C` | `03BD` | 2 | Saisonzähler |
| `0x0BA0E` | `A969` | 2 | Tageszähler (wie 0x3B7, Rolle offen) |
| `0x0BA10` | `0246` | 16 |  |
| `0x0BA20` | `A8A0` | 1 |  |
| `0x0BA21` | `5A50` | 5 | Kennzeichen „neue Nachricht“ je Manager |
| `0x0BA26` | `CAE9` | 288 | Taktikwerte Einsatz/Angriff (0x30 je Manager) |
| `0x0BB46` | `CAE3` | 6 |  |

## An den Ständen belegte Felder

| Feld (DGROUP) | FILE01 | FILE02 | FILE03 | Bild des Nutzers |
|---|---|---|---|---|
| Manager 0 `0x965B` Name, `+0x14` Verein | Joerg, 2 | = | = | Joerg, KÖLNER EC (Vereinsindex 2 im Vereinsblock) |
| `0x973F` Eintritt (DM) | 13 | 17 | 17 | 13 / 17 DM |
| `0x97EA` Konto (Drittel-DM) | 1.500.000 | = | 2.212.062 | 500.000 / 737.354 DM |
| `0x9771` 9 Stadionstufen, `+0x1D`/`+0x21` Sitz/Steh | 2 1 1 0 2 1 1 0 0, 3.000/4.500 | = | = | Halle, digital, Frosta, Plastikschalen, mittel, Straße, Imbißbude, ohne, ohne |
| `0x5A20`/`0x5A1C`/`0x5A18` Tag, Monat (0 = Jan.), Jahr | 10, 8, 1993 | = | 20, 8, 1993 | 10.09. / 20.09.1993 |
| `0x118` Regler | 3 3 0 1 | = | = | Setup-Bild |
| `0x3B7` Tageszähler, `0xA969` | 1, 1 | = | 11, 11 | |
| `0x7A9`–`0x7AB` nächster Spieltag je Liga | 1 1 1 | = | 4 4 4 | „4. Spieltag“ im Hauptmenü |
| `0xA0CB` Ergebnisse (je Liga 0x210, je Spieltag 6 × Heim/Gast) | 0 | = | 18 Ergebnisse der Bundesliga | alle 18 aus den Paarungsbildern stimmen |
| `0x5CCB` Tabelle (Vereinsindex nach Platz) | Vorgabe | = | Hedos … Eisbären | Tabelle 3. Spieltag, Reihenfolge stimmt |

FILE01 → FILE02: im ganzen Klartext genau **ein** Byte verschieden (`0x973F`). FILE02 → FILE03: geändert sind
`20D`, `D0F4`, `1FD`, `A891`, `A895`, `A075`, `A88D`, `A0CB`, `965B` (21 Byte), `far:9657` (Vereine), `8257`,
`far:8253` (Spieler), `5DC3` (Kader), `5D7B`, `5A20`, `03B7`, `5CCB`, `07A9`, `AB7B`, `C313`, `A969`.

## Spielstände für das Original erzeugen: `emtool hok patch`

Spur `hok-patch`. Schreiber: `src/EishockeyManager.Formats/OriginalSaveGamePatcher.cs` (`Encode` ist das Gegenstück zu
`OriginalSaveGame.Read`; Lesen und Wiederschreiben ergibt an FILE01–03 **byteidentische** Dateien – damit sind
Verschlüsselung und beide Prüfsummen auch beim Schreiben belegt).

```
emtool hok patch <vorlage> <ziel> [--manager n] [--id n] [--force] feld=wert …
```

Gelesen wird die Vorlage (Prüfsummen müssen stimmen), geändert werden nur die genannten Felder, Klartextkopf mit
Namen, Füllbyte und Startschlüssel bleiben, Prüfsummen werden neu berechnet, die Spielstand-ID wird neu gewürfelt
(sonst weist das Original einen in derselben Sitzung gesicherten Stand ab, siehe oben). Das Ziel wird nie über die Vorlage
oder eine vorhandene Datei geschrieben (außer `--force`).

| Feld | Wirkung | Fundstelle |
|---|---|---|
| `konto=<DM>` | Konto, intern Drittel-DM (×3) | `0x97EA` |
| `eintritt=<DM>` | Eintritt 1–99 `[APPROX]` Obergrenze | `0x973F` |
| `stadion=a,…,i` | neun Stadionstufen je 0–9 `[APPROX]` Obergrenze | `0x9771` |
| `sitz=`, `steh=` | Sitz-/Stehplätze | `0x9771+0x1D`/`+0x21` |
| `datum=TT.MM.JJJJ` | Tag, Monat (im Stand 0-basiert), Jahr, Jahr im Kopf `0x0A` | `0x5A20/1C/18` |
| `zaehler=<n>` | Tageszähler | `0x3B7` und `0xA969` |
| `spieltag=<n>` | nächster Spieltag aller drei Ligen und Kopfbyte `0x0F` | `0x7A9`–`0x7AB` |
| `level=<0-5>` | Regler `0x118` und `0xED8 = 5 − Level` | em.exe `0x2988D` |

`manager` (Standard 0) wählt den Datensatz für Konto, Eintritt und Stadion. **Abgewiesen** mit Begründung: `dauer`
(Zuordnung zum Spielende `0x5A14` nicht belegt), `liga` (würde Verein und Tabelle betreffen), `staerke` (wirkt nur bei
der Erzeugung der Mannschaften); unbekannte, doppelte und außerhalb des Bereichs liegende Werte ebenfalls.

Belegt im Original (Nutzer, 30.10.2026, DOSBox 0.74-3): `hok patch FILE01.HOK FILE04.HOK konto=2000000 eintritt=20
datum=20.09.1993 zaehler=11 spieltag=4` lädt ohne Meldung; Hauptmenü zeigt „Mittwoch, 20. Sept. 1993“, „4.Spieltag“ und
2.000.000 DM, der Stadion-Bildschirm Eintritt 20 DM und Konto 2.000.000 DM, übrige Stadionwerte wie FILE01.

Grenzen: `datum`, `zaehler` und `spieltag` ändern **nur** diese Zähler, nicht Tabelle, Ergebnisse (`0xA0CB`), Form oder
Kader; Datum und Spieltag müssen vom Nutzer zusammenpassend gewählt werden (FILE03: 20.09.1993 ↔ Zähler 11 ↔ Spieltag 4).

`[APPROX]` Spielstand-ID: Alle drei Stände haben eine vorzeichenerweiterte 16-Bit-ID im Bereich `0x8000`–`0xFFFF`
(−26229, −20251, −27317), obwohl die Notiz zu `0x5BB51` `random(0..0x8FFF)+1` nennt – die Formel ist offen. Der Patcher
würfelt daher im beobachteten Bereich; das Original vergleicht die ID nur mit seiner Liste (`0x5C572`).

## Kader, Spieler, Training, Taktik (an FILE05 belegt)

**FILE05** = FILE03 unverändert neu gesichert (Klartext identisch). Abgleich mit den Bildern „Mannschaft“, „Aufstellung“,
„Taktik“, „Verträge“, „Transfer“ und „Training“ des Nutzers; Ausgabe `emtool hok dump` (Abschnitt Kader/Transfermarkt).

**Kaderplatz** (`0x5DC3` + 0x618 · Manager-Satz + 0x34 · Platz, 29 Plätze; Manager-Satz 4 = Transfermarkt):

| Offset | Inhalt | Beleg |
|---|---|---|
| `+0x00` | 5-Minuten-Strafen (Spalte 5m) | alle 25 (FILE05), Anzeige em.exe `0x4221D` |
| `+0x01` | 2-Minuten-Strafen (Spalte 2m) | alle 25, ebenda |
| `+0x03`–`+0x05` | Tore je Wettbewerb; Spalte TO = Summe (em.exe `0x421D6`), beim Torhüter „-“. Beim Torhüter stehen dort vermutlich Gegentore (CISSEWSKI 13 = Kölns Gegentore nach 3 Spielen) | alle 25 |
| `+0x06` | Spiele (Spalte SP); nach einem Kauf mit den Spielen beim alten Verein | alle 25; DAFFNER nach Kauf 2 (FILE07) |
| `+0x07`, `+0x08` | Spiele in weiteren Wettbewerben (eigene Spalten neben `+0x06`; eine Spalte zeigt `+0x06 + +0x07 + +0x08`; Zuordnung Wettbewerb offen) | em.exe Spalten 3–6 |
| `+0x0A` | eingesetzt (1 = Status „IM TEAM“ oder „RESERVE“) | 22 Plätze = „Eingesetzt: 22“; J.SCHULZ, DOBRYZINSKI, SANDNER ohne Status = 0 |
| `+0x0B` | Vertragsdauer in Jahren (Spalte V.DAUER); 0 = „kein V.“ (em.exe `0x4239A`) | FILE05 alle 2 = „2 Jahre“; FILE07 DAFFNER 1 (Angabe Nutzer: 1 Jahr) |
| `+0x0F` | Index im Spielerpool `far:8253` | Namen aller 25 Spieler und 7 Marktspieler |
| `+0x10`–`+0x12` | Stärken (drei Werte wie in der Spalte STÄRKEN) | alle 32 Spieler |
| `+0x13` | Spalte **E** = `+0x13 · 10 / 15` (ganzzahlig), hervorgehoben bei `+0x13 > 0x82` (em.exe `0x42335`) | alle 25 (z. B. HEISS 91 → 60, MENDE 50 → 33) |
| `+0x15` | Reihe 1–3 (untere Bits), Bit 0x80 = Sturm; der eingesetzte Torhüter hat Reihe 1 | Aufstellung: Reihen 1–3, TW Nr. 2 |
| `+0x1C` | Spiele für den jetzigen Verein (nach einem Kauf 0); je Wettbewerb `+0x1C`/`+0x1E`/`+0x20`, siehe „Saisonwechsel und Kader“ | alle 25, FILE07 |
| `+0x22` | gleich der Ligator-Zahl `+0x03` in allen Ständen: Tore beim jetzigen Verein; je Wettbewerb `+0x22`/`+0x24`/`+0x26`, siehe „Saisonwechsel und Kader“ | alle 25 |
| `+0x28` | Gehalt (4 Byte, Drittel-DM) = Spalte WERT bei „Verträge“ | alle 25, z. B. HEISS 17.100 DM |
| `+0x2C` | Preis (4 Byte, Drittel-DM) = Transferliste „(K)“; Leihe „(L)“ = ein Drittel davon. Nach dem Kauf steht dort der vom Nutzer gebotene und bezahlte Kaufpreis (FILE07) | 7 Marktspieler, FILE07 |
| `+0x30`–`+0x33` | Versicherung (4 Byte, als zwei Wörter `+0x30`/`+0x32` mit `or` verknüpft): ≠ 0 → Zusatz „(v)“ hinter der Vertragsdauer (em.exe `0x42410`) | – (kein Fall in den Ständen) |
| `+0x0C` | Leihe: Vereinssatz des Verleihers + 0x80 (em.exe `0x47920–0x47925`; „… ist leider von <Verein> nur ausgeliehen“ `0x4A692`), 0 sonst; eigene Farbe der Spalte ART (`0x42057`) | FILE44 VALLIERE `0xB9` (Satz 57), Pool `+0x1A` = 4 |
| `+0x02`, `+0x09`, `+0x0D`, `+0x0E`, `+0x14`, `+0x16`–`+0x1B`, `+0x20`, `+0x21`, `+0x23`–`+0x27` | offen (`+0x09` Bits 0x40/Wert 2: Hervorhebungen der Zeile) | em.exe `0x41F6A`, `0x42057` |

**Spalten des Bildschirms** (em.exe: Zeilenfunktion ab `0x41F13`, Spaltennummer → Sprungtabelle `cs:0x258E`, Segment ab
Datei `0x3FFE0`; Kopftexte über Zeigertabelle DGROUP `0x1065`): 0 Nr. (nur bei `+0x0A` ≠ 0), 1 ART, 2 Name (Pool),
3–5 Spiele `+0x06`/`+0x07`/`+0x08`, 6 deren Summe, 7 STÄRKEN, 8 ST = Mittel der drei Stärken (`0x41DA9`), 9–11 Tore
`+0x03`/`+0x04`/`+0x05`, 12 TO (Summe), 13 „2m 5m“, 14 STATUS, 15 E, 16 V.DAUER, 17 GEHALT. Alle an FILE05 geprüft
(E, TO, 2m, 5m: 25 von 25).

**Spielerpool** `far:8253` (200 × 33, Struktur aus `reverse-engineering.md`): Name `+0x00`, Talent `+0x12`, Alter `+0x14`
(Transferliste: KÜNAST 24, STARTSCHEW 31, KJELLBERG 32, D.HEGEN 33), Grundstärken `+0x15`–`+0x17` (beim Markt = Stärken
im Kaderplatz, im eigenen Kader nach dem Training teils abweichend), Rollenwert `+0x18` (Torhüter 0, Abwehr 25–41,
Sturm 76–89), Besitzer `+0x1A` (**4 = Transfermarkt**, nicht 6 wie in `reverse-engineering.md`), Verein `+0x1D`.

**Training** `0xA8A1`: je Manager 28 Byte = 7 Tage (Mo–So) × 4 Felder (oberer Kasten 2, unterer Kasten 2), Wert = Art
in der Reihenfolge der Leiste. FILE05 = Standardplan, alle 28 Felder stimmen mit dem Bild.

**Taktik:** Modi `0xA6FB`/`0xA707` = 0 („AUTOM.“), Werte `0xCAE9` alle 0 – bei Automatik stehen die angezeigten Werte
(40/20/20/15/20/0 und 0/5/6/4/4/0) nicht im Spielstand, sondern werden berechnet.

## Gezielte Änderungen FILE06–FILE08, neue Spiele FILE10/FILE11

| Stand | Änderung (Nutzer) | Unterschied im Klartext |
|---|---|---|
| FILE06 | aus FILE05: BRANDL aus Reihe 3 genommen, DOBRYZINSKI hinein | Kaderplatz 12 `+0x0A` 1→0, `+0x15` 0x83→0; Platz 19 `+0x0A` 0→1, `+0x15` 0→0x83. Dazu `0xCC73` (6 Byte 0 → 2 3 3 2 3 3), `0x375` 1→0, Vereinssatz Köln `+0x26` 48→55 (Deutung offen) |
| FILE07 | aus FILE05: DAFFNER vom Markt gekauft (Liste: 305.000 DM), Sponsorzuschuss 220.000 DM angenommen | Konto −180.000 DM (737.354 → 557.354). Pool-Satz 158 `+0x1A` 4→0 (Besitzer), `+0x1D` 3→2 (Verein Köln). Neuer Kaderplatz 25: Pool 158, Stärken 67 88 51, SP 2, Gehalt 9.900 DM, `+0x2C` **400.000 DM** = im Kaufdialog eingegebenes Gebot (Angabe Nutzer: höher als der Listenpreis, damit es nicht abgelehnt wird; 400.000 − 220.000 Zuschuss = 180.000), Vertragsdauer `+0x0B` = 1, Reihe 3 Sturm. Automatisch: LALA aus Reihe 3, HANEMANN nicht mehr eingesetzt (weiter 22 eingesetzt). Markt rückt nach. Teamwerte Köln (`+0x19`, `+0x1B`, `+0x1D`) neu berechnet |
| FILE08 | aus FILE05: Bankkredit 701.000 DM, Training Mi oben/oben → Freizeit | Konto +701.000 DM; Manager 0 `0x9806` (= `0x97F6` + 4 · 4, Darlehen Bank) = −2.103.000 Drittel; Bank-Satz (Manager-Satz 4): Konto `+0x18F` = −2.103.000, Darlehen von Manager 0 `+0x19B` = +2.103.000. Training `0xA8A1` + 8 (Mittwoch, erstes Feld) 5 → 1: **Art 1 = Freizeit** (Leiste: 0 Pinguin, 1 Strand, …) |
| FILE10 | neues Spiel, nur Fahne, „Tom“, MANNHEIMER ERC | `0xD0B9` = 2 (nur Fahne), Vereinsindex 1 = MANNHEIMER ERC: **die Vereinsindizes werden je Spiel neu verteilt** (FILE01: Index 1 = KREFELDER EV) |
| FILE11 | neues Spiel, nur Weltkugel, nach dem Sprung | `0xD0B9` = 1, Datum 14.04.1994, `0xA70E` = 5 (Bundestrainer am Zug), `0x6AC` = 54 (Deutschland), `0x256` = 30.000 (Highscore-Schlüssel „nur Weltkugel“). Kopf: Liga-Byte 0, Weltkugel-Byte 1, Verein „DEUTSCHLAND“. Der Vereinssatz des Managers (Index 7) heißt im gesicherten Block ebenfalls **„DEUTSCHLAND“**: Die Kopfroutine überschreibt den Vereinsnamen vor dem Schreiben der Blöcke und stellt ihn nicht zurück |

**FILE09** (neues Spiel): zwei Manager „Spieler 1“ (SC PREUSSEN BERLIN) und „Spieler 2“ (EC HEDOS MÜNCHEN), Level 5,
Dauer 0 (eine Saison), Bundesliga, Start-Stärke 2 („Abstiegskampf“), beide Spielmodi; gesichert im Zug von Spieler 2.

- Kopf: Managerzahl 2, zwei Einträge; `0x386` = 2, `0xA70E` = `0xA70D` = 1 (Manager am Zug).
- Regler `0x118` = 5 0 0 2, `0xED8` = 0, **Spielende `0x5A14` = 1994** (Dauer 0); FILE01 (Dauer 3, „∞“) hat 22222.
- Konto beider Manager 1.200.000 Drittel = **400.000 DM** (Level 5, wie `finanzen-rekonstruktion.md`), Eintritt 13 DM.
- Stadion je Verein verschieden: Preussen und Hedos 4.200 Sitz-/7.000 Stehplätze, Köln (FILE01) 3.000/4.500; Stufen gleich.
- Training `0xA8A1`: alle Felder beider Manager = 1 (Freizeit) – Standardplan bei Level 5 „immer frei“ bestätigt.
- Zwei Kader (je Manager-Satz 0 und 1) plus Transfermarkt.
- Start-Liga und Level sind **ein** Wert für alle Manager (Setup bietet nichts je Manager; Hinweis des Nutzers).

Spielmodus `0xD0B9`: 1 = nur Weltkugel, 2 = nur Fahne, 4 = beide – bestätigt (wie `wettbewerbe.md`).

## Falsche Prüfsumme: „Der kleine Mogelguru“ `[ORIGINAL]`

`KAPUTT.HOK` (FILE03 mit falscher letzter Prüfsumme) führt im Original zum Bildschirm „DER KLEINE MOGELGURU /
VERABSCHEUT IHRE TATEN !“ mit „SOLLTE ES IHNEN ETWA PASSIERT SEIN, DAß SIE OHNE JEGLICHE BÖSE ABSICHT MIT EINEM
MONITOR MANIPULIEREN WOLLTEN ?“ (Texte DGROUP `0x2664`, `0x269A`, `0x26AF`; Fehlercode 5, Anzeige ab `0x5CEFA`).
Bild: Referenz-Repo `original/spielstaende/bilder/kaputt_mogelguru.png`. Für `hok-patch`: Prüfsummen müssen stimmen.

## Saisonwechsel und Kader (FILE16A, FILE22–26, Runde 13)

Zusätzliche Feldbelege aus der Auswertung der DOSBox-Sitzungen 2 und 3 (Grundlage FILE16A, FILE22, FILE23 – Sitzung 2 –,
FILE05, FILE24–FILE26 – Sitzung 3 –, zum Vergleich FILE06/FILE07 und die Startstände FILE01, FILE27, FILE28; Auswertung
[re-runde-13.md](../analyse/re-runde-13.md), Abschnitt „Sitzung 2 und 3“). Kaderplatz = `0x5DC3` + 0x618 · Satz + 0x34 · Platz
(DGROUP), im Strom ab `0x06A27`; Spielerpool ab `0x0505F` (`far:8253`, 33 Byte). Befehlsadressen sind Dateioffsets in
`em.exe`, Offsets im Spielstand stehen als „HOK 0x…“.

| Feld | Befund | Beleg |
|---|---|---|
| Kaderplatz `+0x1C`/`+0x1E`/`+0x20` (Wörter) | Spiele beim jetzigen Verein je Wettbewerb 0/1/2; gleich `+0x06`/`+0x07`/`+0x08` bis zum Saisonwechsel, danach **nicht** zurückgesetzt | FILE16A/FILE22/FILE05/FILE24 Spiegel ohne Ausnahme (außer dem gekauften DAFFNER, FILE07 Platz 25); FILE23 `+0x1C` = 3, `+0x1E` = 2 bei `+0x06`/`+0x07` = 0 |
| Kaderplatz `+0x22`/`+0x24`/`+0x26` (Wörter) | Tore beim jetzigen Verein je Wettbewerb; gleich `+0x03`/`+0x04`/`+0x05` (Feldspieler), beim Saisonwechsel **nicht** zurückgesetzt; Torhüter 0 (sein `+0x03` sind Gegentore) | wie oben, z. B. HADRASCHEK FILE22 `+0x04` = 4, `+0x24` = 4 → FILE23 `+0x04` = 0, `+0x24` = 4; em.exe Datei `0x54300`/`0x54314` (`cmp word [+0x22],0xFF`, `inc word`) |
| Kaderplatz `+0x09` Bit 0x02, `+0x0D`, `+0x17` | verletzt, Restdauer, Verletzungsart | em.exe Datei `0x5C44` (`or +0x09,2`), `0x5BE5` (`+0x0D`), `0x5C4C` (`+0x17`), `0x5C50` (`+0x0A = 0`); FILE24 STEFAN `+0x09` 00→02, `+0x0D` 0→10, `+0x17` 00→10 |
| Kaderplatz `+0x02` | offen; steigt in der Saison (TRAVNICEK, MORRISON, LANGLOIS 0→2 von FILE16A nach FILE22), wird beim Saisonwechsel **nicht** zurückgesetzt | FILE22→FILE23 unverändert |
| Pool `+0x1C` | Ligaspiele der Saison (gleich Kaderplatz `+0x06`), Saisonwechsel → 0 | FILE22→FILE23, 16 von 16 → 0 |
| DGROUP `0x375 + 2 · m` | „automatisch aufstellen“ liegt in HOK `0x00844` | FILE05 01, FILE06 00, FILE24 00 (Byte `0x376` = 01), FILE25 01 |
| Anzeige „Nr.“ | = Kaderplatz + 1, keine Rückennummer: Beim Verkauf rücken die folgenden Plätze auf, beim Kauf wird der Spieler nach Spielerart einsortiert (vor dem ersten leeren Platz oder dem ersten Platz größerer Art; ein Stürmer kommt damit hinten an) | FILE05 → FILE07 (Kauf eines Stürmers) und FILE26 (Verkauf), em.exe Datei `0x42D55`–`0x42DA1` mit memmove ab `0x42DC3` (Kauf, R13GN-01/#622), `0x425B6` (Verkauf); Remake `KaderStartRunde13Teil4Tests` (PR #599), `KaufEinsortierenTests` (PR #623) |
| HOK `0x08EED` / `0x08EF7` | DGROUP `0xA776` (Teilnehmerliste Europapokal, deutsche Teilnehmer; die Mischung 0x35EDD führt Meister und Titelverteidiger über 0x36146 mit) bzw. DGROUP `0xA763` | FILE22 → FILE23: Kölner EC Satz 4 → 8, Liste = 8 (PR #579, 0x346D5 nach L8) |

## Feldkarte Remake-Spielstand (Schema 47) ↔ HOK

Spur `hok-import`, Teil 1 (Runde 14, E14-1 C). Jedes Feld des Remake-Spielstands (`SaveGame.SchemaDescription()`, ohne die
Hilfsfelder von Tupeln) steht hier genau einmal; der Test `OriginalImportFieldMapTests` prüft das gegen das Schema und wird
rot, sobald ein Feld fehlt oder wegfällt. Grundlage: Blocktabelle oben, `OriginalSaveGame.cs`, die `[ORIGINAL]`-Fundstellen
des Spielkerns und die Stände FILE01–FILE41. Die Karte ist Arbeitsgrundlage für den Import (Teil 2), keine neue Belegung:
was hier „B“ heißt, ist schon oben oder in den genannten Fundstellen belegt.

**Status:** **B** belegt – das Feld steht im HOK, Bedeutung an Ständen oder in em.exe belegt. **A** abgeleitet – aus
HOK-Feldern (oder der Datenbank `kb\23`) eindeutig berechenbar, Regel in der Spalte Import. **N** nicht im Original –
Remake-eigener Zustand oder ein Original-Zustand, der nachweislich **nicht** im Spielstand liegt; der Import setzt den
genannten Wert `[APPROX]`. **U** unbekannt – das Original führt den Zustand wahrscheinlich, der Ort im HOK ist nicht belegt;
der Import setzt eine Vorgabe `[APPROX]` und die Stelle bleibt ein offener Punkt.

**Schreibweise der Quelle:** Blockname wie in der Tabelle oben (DGROUP-Adresse), dahinter der Versatz. Sätze:
`965B+x` = Managersatz (DGROUP `0x965B + 0x1AF · m + x`; m = 0–3 Manager, 4 Bank/Transfermarkt, 5 Bundestrainer),
`far:9657+x` = Vereinssatz (`0x3D · Satzplatz + x`), `far:8253+x` = Pool-Spieler (33 Byte), `5DC3+x` = Kaderplatz
(`0x618 · Satz + 0x34 · Platz + x`; Satz 0–3 Manager, 4 Transfermarkt, 5 Nationalteam), `8257` = Tabellensatz des Vereins
(DGROUP `0x825A + 0x50 · Satzplatz + x`, Heim/Auswärts je Feld; reverse-engineering.md „Ligatabelle“), `Kopf` = Klartextteil.

**Grundregeln des Imports** (gelten für alle Zeilen):

1. **Vereine:** Der Satzplatz im HOK (0–149) ist nicht der Remake-Vereinsindex: die Ligavereine werden je Spiel und Saison
   gemischt (FILE10, FILE13 → FILE14, 0x35EDD). Zuordnung über den Namen `far:9657+0x00` zum Datenbankverein `kb\23`;
   Ausnahme Modus „nur Weltkugel“: der Vereinssatz des Managers trägt dort „DEUTSCHLAND“ (FILE11), sein Datenbankverein
   kommt aus dem übrigen Satzbestand (der fehlende Name). Alle Vereinsverweise (Tabelle `5CCB`, Ergebnisse, Pool `+0x1D`,
   Manager `965B+0x14`, Pokale) laufen über diese Zuordnung.
2. **Satzfolge:** Die Satzplätze 0–11, 12–23, 24–35 je Liga ergeben `_leagueSlots` (Remake-Indizes in HOK-Folge).
3. **Spieler:** Nur Managerkader (`5DC3` Satz 0–3), Transfermarkt (Satz 4), Nationalkader (Satz 5) und der Pool
   (`far:8253`, 200 Sätze) sind einzeln geführt. Die Kader der Computervereine erzeugt der Import wie ein neues Spiel aus der
   Datenbank (`NewGame`) und passt ihre Werte an die Teamwerte `far:9657+0x17…0x1D` an (`[APPROX]`, wie Spielstart).
   Spieler-IDs vergibt der Import neu (Pool vor Kadern, Reihenfolge Satz/Platz).
4. **Geld:** HOK in Drittel-DM, Remake in DM: Wert / 3 (Zinskonto und Talentpreis: Drittel bleiben, wo das Remake Drittel führt).
5. **Zeit:** Remake-Tag 0 = 10. September = Tageszähler `03B7` = 1, also `CurrentDay = 03B7 − 1`; Saison = 1993 + `03BD`.
6. **Zufall:** Kein Generatorzustand im HOK (Abschnitt „Zufallszustand“). Alle Remake-Zufallsquellen werden aus einer
   neuen Saat erzeugt `[APPROX]`; die Spielstand-ID und die Sicherungssperre werden ignoriert (Entscheidung Nutzer).

### GameState

| Feld | S | HOK-Quelle | Import |
|---|---|---|---|
| `GameState.Clubs` | B | `far:9657` (150 Sätze) | 150 Vereine aus der Datenbank, Werte je Satz nach Regel 1 |
| `GameState.Competitions` | A | `5CFB`, `CED8`, `A776`, `07A9+6` | DEB-Pokal, Europapokal, WM aus den Wettbewerbsblöcken (Zeilen der Wettbewerbe unten) |
| `GameState.CurrentDay` | A | `03B7` | `03B7 − 1` (Regel 5) |
| `GameState.CurrentManager` | B | `A70E` | Manager am Zug; 5 = Bundestrainer → `_nationalTurn` |
| `GameState.CurrentRecord` | N | `Kopf` – | null (Import nur in der laufenden Saison); nach `03B7` > 232 aus Tabellen neu bilden `[APPROX]` |
| `GameState.DayOpened` | A | `03B7` | true: Das HOK ist im Menü nach dem Tageswechsel zum Tag `03B7` − 1 gesichert (Tagesabschluss, Takt, Listenwechsel; #688, R688-04) |
| `GameState.FinanceDay` | A | `03B7` | `CurrentDay` (Tagesabschluss zum gesicherten Tag ist gebucht, #688; vorher `CurrentDay − 1`); am Markierungstag `CurrentDay + 1` (zweiter Tageswechsel, Ladeeigenheit) |
| `GameState.History` | N | – | leer; das Original führt nur Titelträger und Ewige Tabelle |
| `GameState.LoadCounterOffset` | A | `03B7`, `A969` | 1, wenn `A969` = `03B7` − 1 (im Menü des Markierungstags gesichert: FILE22, FILE44): Datei 0x54D41 → 0x55204 zählt nach dem Laden erneut weiter (Zähler 218 = 15. April, `file46_1`), bis zum Saisonwechsel (Datei 0x26F55); sonst 0 (Spur `pruefung-karte` Teil 5, Nutzer 09.11.2026 C) |
| `GameState.ManagerLoans` | B | `965B+0x19B` (+4 · i) | Darlehen Manager m bei i (i = 0–3; negativ = Schuld), / 3 |
| `GameState.Managers` | B | `0386`, `965B` | `0386` Manager, Sätze 0 … n − 1 |
| `GameState.NationalAccount` | B | `965B+0x18F` Satz 5 | Konto des Nationalteams (DGROUP 0xA055), / 3 |
| `GameState.NationalBookings` | N | – | eine Buchung „Carryover“ mit dem Kontostand |
| `GameState.NationalBudget` | N | – | DGROUP 0x29C liegt in keinem Block: Startwert `NationalCoach.FirstBudget` |
| `GameState.NationalCoach` | A | `far:9657+0x16` (Satz `06AC`), `965B+0xEB` (Satz 5) | Amt, wenn der Vereinssatz des Landes +0x16 = 6 ist (0x55494, Amtsende 0x55752); Manager aus Satz 5 +0xEB, Land `06AC` (Regel 1). `D0BA` ist nur das Kennzeichen „Ernennung steht an“ (0x54D19 → 1, 0x554BA → 0) |
| `GameState.NextPlayerId` | A | `far:8253` | höchste vergebene ID + 1 (Regel 3) |
| `GameState.Phase` | A | `03B7`, `0118` | aus dem Nachspielen bis `03B7` − 1: Running, ClubSeasonOver ab dem 14. April; GameOver (Dauer `0118`+1) erreicht kein importierbarer Stand (Saisonende liegt vor dem Import) |
| `GameState.PlayedMatchDays` | A | `07A9` | nächster Spieltag der Bundesliga − 1 |
| `GameState.PlayoffResults` | A | `CCB2`, `5CFB+0x40` | alle Spiele der Serien (Teil 4d), Tage aus dem Play-off-Kalender |
| `GameState.Playoffs` | A | `5CFB+0x40`, `07A9+7`, `CCA3`, `CCA4`, `CCB2` | ab Ende der Hauptrunde: Plätze DGROUP 0x5D3B (0–7, 8–11, 12–13), Runde 0x7B0, Spiel der Runde 0xCCA3, Spiele 0xCCB2 (Teil 4d) |
| `GameState.Random` | N | – | neue Saat (Regel 6) |
| `GameState.RankSlotsRecorded` | A | `07A9` | = `PlayedMatchDays` (Platzfelder DGROUP 0x828B sind geschrieben) |
| `GameState.RelegationGroups` | A | `CFFC`, `CED8+0x48`, `CDB8+0x48`, `CD58+12` | Wettbewerb 1 der Gruppentabellen: Teilnehmer `CFFC`+4 · Gruppe, Paarungen/Tore ab 0xCF20/0xCE00, gespielte Termine aus der Punktsumme; Platzliste `CD88`+12 ergibt sich aus der Tabelle (geprüft, OriginalImportPostseasonTests) |
| `GameState.Results` | A | `A0CB` | Tore je Liga · 0x210 + Spieltag · 12 + Paarung; Vereine aus dem Spielplan (siehe `Schedules`) |
| `GameState.RetiredPlayers` | N | – | leer |
| `GameState.Schedules` | A | `far:9657` | feste Paarungstabelle 0x4E7C7 über die Satzplätze 0–35 (re-runde-12.md); `5D7B` hält nur den laufenden Spieltag und wird nicht gelesen |
| `GameState.Season` | B | `03BD` | 1993 + Saisonzähler |
| `GameState.Setup` | B | `0118`, `D0B9` | Zeilen `GameSetup` (Spielende `5A14` folgt aus der Dauer `0118`+1) |
| `GameState.StatisticsRecordedSeason` | A | `03BD` | Saison − 1 (Statistik der laufenden Saison noch offen) |
| `GameState.TrainedUntil` | A | `03B7`, `03BD` | (Saison, `CurrentDay`) |
| `GameState.TransferMarket` | B | `5DC3` Satz 4, `far:8253` | Zeilen `TransferMarket` |
| `GameState._contractRandom` | N | – | null (wird bei Bedarf aus `Random` gebildet) |
| `GameState._contractTurnFactor` | N | – | 0 = im Zug neu ziehen |
| `GameState._cupRandom` | N | – | null |
| `GameState._eternal` | U | `AB7B`? | Ewige Tabelle: Ort nicht belegt; leer |
| `GameState._financeRandom` | N | – | null |
| `GameState._germanyRatings` | U | – | null (neu berechnet beim nächsten Länderspiel) |
| `GameState._headToHead` | B | `AB7B+0x000` | DGROUP 0xAB7B + 0x280 · m + 10 · Verein: im Original **4** Manager-Plätze (0xA00 Byte), im Remake 6; Plätze 4–5 leer |
| `GameState._leagueSlots` | A | `far:9657` | Regel 2 |
| `GameState._managerBalance` | U | – | leer (Bilanz Manager gegen Manager) |
| `GameState._managerHistory` | N | – | leer |
| `GameState._nationalForm` | U | – | Vorgabe wie neues Spiel |
| `GameState._nationalAutoLineupSaved` | B | `0375+11` | DGROUP 0x380 zwischen zwei Ämtern (A009, Schema 41); gleiche Quelle wie `NationalCoach._autoLineupSaved` |
| `GameState._nationalTurn` | A | `A70E` | `A70E` = 5 |
| `GameState._nextLeagueSlots` | N | – | leer (wird am 29. April gebildet) |
| `GameState._readyManagers` | N | – | leer |
| `GameState._stocks` | B | `D0F4`, `01FD`, `A891`, `A895` | Zeilen `Stock` |
| `GameState._teamRecordOpponents` | B | `AB7B+0xF94` | DGROUP 0xBB0F + 8 · Verein + Art (64 Vereine) |
| `GameState._teamRecords` | B | `AB7B+0x1194` | DGROUP 0xBD0F + 0x10 · Verein + 2 · Art |
| `GameState._trendRandom` | N | – | null |
| `GameState._turnRandom` | N | – | null |
| `GameState._vacantNationalTactics` | A | `far:9657+0x26`, `A6FB+5`, `CAE9` (+0x30 · 5) | ohne Bundestrainer: Modi und Werte des Satzes 5, Anzeige „AUTOM.“ aus dem Vereinssatz des Landes (+0x26–0x2B, +0x35–0x3A) |
| `GameState._worldCupRankings` | U | – | leer |

### GameSetup, GameOptions, ManagerSetup

| Feld | S | HOK-Quelle | Import |
|---|---|---|---|
| `GameSetup.ClubCompetitions` | B | `D0B9` | Bit 0x02 bzw. Wert 2/4 (Fahne) |
| `GameSetup.DebCup` | N | – | true (kein Schalter des Originals) |
| `GameSetup.Duration` | B | `0118+1` | Regler Dauer 0–3; Gegenprobe `5A14` (Dauer 0 → 1994, 3 → 22222) |
| `GameSetup._startLeague` | B | `0118+2` | Regler Start-Liga (DGROUP 0x11A), ein Wert für alle Manager (Schema 42) |
| `GameSetup.EuropeAndWorldCup` | B | `D0B9` | Wert 1/4 (Weltkugel) |
| `GameSetup.Managers` | B | `Kopf`, `965B` | je Manager eine `ManagerSetup` |
| `GameSetup.Options` | B | `021D`, `0262` | Zeilen `GameOptions` |
| `GameOptions.LiveMode` | B | `021D+9` | DGROUP 0x226 |
| `GameOptions.ManagerDayMinuteLoop` | N | – | Einstellung des Remakes „Ganzer Tag“ (R14-D4 = A, #349, Schema 40): kein Schalter des Originals; Import `false` |
| `GameOptions.MatchSpeed` | B | `0262` | DGROUP 0x262 |
| `GameOptions.Reports` | B | `021D+0` | Index 0–8 |
| `GameOptions.ShowDebCup` | B | `021D+10` | DGROUP 0x227 |
| `GameOptions.ShowEuropeanCup` | B | `021D+11` | DGROUP 0x228 |
| `GameOptions.ShowWorldCup` | B | `021D+12` | DGROUP 0x229 |
| `ManagerSetup.ClubIndex` | A | `965B+0x14` | Vereinsindex nach Regel 1 |
| `ManagerSetup.Head` | B | `0178+m` | Managerkopf: Bilder des Hauptmenüs gegen `kb\0` (file01_2, file05_1, file09_2/3, file11_01, file15_1, file27_2, file28_2, file47_1, file49_1) gleichen `0178+m`; EXPORT01 mit `0178` = 0 und `965B+0x13` = 6 zeigt Kopf 0 (`export01_1`). `965B+0x13` ist an allen Ständen eine Kopie (unbenutzte Sätze 2/3/4/0/0) |
| `ManagerSetup.Level` | B | `0118+0` | ein Wert für alle (Gegenprobe `0ED8` = 5 − Level) |
| `ManagerSetup.Name` | B | `965B+0x00` | Name (19 Byte), Gegenprobe `Kopf` |
| `ManagerSetup.StartLeague` | B | `0118+2` | ein Wert für alle |
| `ManagerSetup.StartStrength` | B | `0118+3` | ein Wert für alle |

### Manager, ManagerFinance

| Feld | S | HOK-Quelle | Import |
|---|---|---|---|
| `Manager.ClubIndex` | B | `965B+0x14` | Regel 1 |
| `Manager.Dismissed` | U | `965B+0xF3` | Statusbyte, Bit für „entlassen“ nicht belegt; false (seit Schema 46 setzt es kein Weg des Spiels mehr, #688 R688S-03) |
| `Manager.FinanceData` | B | `965B` | Zeilen `ManagerFinance` |
| `Manager.Head` | B | `0178+m` | wie `ManagerSetup.Head` |
| `Manager.Level` | B | `0118+0` | Level |
| `Manager.Money` | B | `965B+0x18F` | Konto / 3 |
| `Manager.Name` | B | `965B+0x00` | Name |
| `Manager.PowerPlayCounts` | B | `965B+0x177` | sechs Wörter (DGROUP 0x97D2 + 2 · k) |
| `Manager.SeriesRecords` | B | `AB7B+0xF40` | DGROUP 0xBABB + 0x15 · m (4 Manager) |
| `Manager.Training` | B | `A8A1` | Zeilen `TrainingPlan` |
| `Manager._newMessage` | B | `5A50` | Byte je Manager |
| `ManagerFinance.Bank` | B | `965B+0x19B` (+4 · 4) | Zeilen `BankAccount` |
| `ManagerFinance.Book` | N | – | Remake-Buchführung: leer (Anzeige „Ausgaben/Einnahmen“ beginnt neu) |
| `ManagerFinance.CostLeague` | B | `965B+0xEB` | Liga-Stufe 0–2 |
| `ManagerFinance.Holdings` | B | `D214` | Zeilen `StockHolding` |
| `ManagerFinance.Inbox` | B | `D294` | 24 Sätze zu 0x71 Byte, gemeinsam für alle Manager; Zeilen `Message` |
| `ManagerFinance.InterestAccrualThirds` | B | `02A0` | 4 Byte je Manager, Drittel-DM unverändert |
| `ManagerFinance.LastHomeGame` | U | `965B+0x197` | 0x97F2 „zuletzt berechnete Einnahmen“, Zuschauer nicht belegt; null |
| `ManagerFinance.ManagerLoanAccrualThirds` | N | – | 0 (Aufteilung nur fürs Finanzbuch) |
| `ManagerFinance.SponsorRating` | B | `965B+0x173` | Wort |
| `ManagerFinance.Sponsoring` | B | `C15B`, `C313` | Zeilen `Sponsoring` |
| `ManagerFinance.Stadium` | B | `965B+0x116` | Zeilen `Stadium` |
| `ManagerFinance.TalentPrice` | B | `020D` | 4 Byte je Manager, / 3 |
| `ManagerFinance.TalentSearch` | B | `965B+0xEC` | Restzähler; 0 → null |
| `ManagerFinance.Youth` | B | `A075` | 0x15 Byte je Manager; Zeilen `YouthTeam` |
| `ManagerFinance._homeStatistics` | B | `965B+0xED` | Zeilen `StadiumStatistics` |
| `BankAccount.Credit` | B | `965B+0x19B` (+4 · 4 = +0x1AB) | Darlehen Bank (i = 4), Vorzeichen umkehren, / 3 (FILE08) |
| `BankAccount.Savings` | A | `965B+0x19B` (+4 · 4 = +0x1AB) | positiver Bankeintrag (i = 4) / 3 `[APPROX]` (an keinem Stand belegt; Saldo wie im Remake Sparbuch − Kredit) |
| `FinanceBook._current` | N | – | 0 |
| `FinanceBook._previous` | N | – | 0 |
| `ManagerLoan.Amount` | B | `965B+0x19B` | Betrag / 3 |
| `ManagerLoan.Borrower` | B | `965B+0x19B` | Satz mit negativem Eintrag |
| `ManagerLoan.Lender` | B | `965B+0x19B` | Index i des Eintrags |

### Stadion, Zuschauer, Jugend, Training, Talentsuche

| Feld | S | HOK-Quelle | Import |
|---|---|---|---|
| `Stadium.Seats` | B | `965B+0x133` | Stadion +0x1D |
| `Stadium.SeatsTarget` | B | `965B+0x135` | Stadion +0x1F |
| `Stadium.Standing` | B | `965B+0x137` | Stadion +0x21 |
| `Stadium.StandingTarget` | B | `965B+0x139` | Stadion +0x23 |
| `Stadium.TicketPrice` | B | `965B+0xE4` | Eintritt (DM) |
| `Stadium._levels` | B | `965B+0x116` | Stadion +2 · i (9 Bereiche) |
| `Stadium._offended` | N | – | DGROUP 0xCC09 liegt in keinem Block: false |
| `Stadium._targets` | B | `965B+0x117` | Stadion +2 · i + 1 |
| `Stadium._weeksLeft` | B | `965B+0x128` | Stadion +0x12 + p |
| `StadiumStatistics.Games` | B | `965B+0xED` | Heimspiele (0x9748) |
| `StadiumStatistics.Minimum` | B | `965B+0x18B` | Minuskulisse (0x97E6) |
| `StadiumStatistics.MinimumOpponent` | B | `965B+0x115` | Gegner (0x9770), Regel 1 |
| `StadiumStatistics.Record` | B | `965B+0x187` | Rekord (0x97E2) |
| `StadiumStatistics.RecordOpponent` | B | `965B+0x114` | Gegner (0x976F), Regel 1 |
| `StadiumStatistics.Total` | B | `965B+0x183` | Summe (0x97DE) |
| `StadiumStatistics._curve` | B | `965B+0xFE` | Zuschauer / 1000 je Heimspiel (0x9759 + n, 22 Plätze), nur die ersten `+0xED` Werte (der Rest ist Vorsaison) |
| `YouthTeam.Funding` | B | `A075+9` | Förderung (DGROUP 0xA078 + 6 + Mannschaft), Stufe × Schritt |
| `YouthTeam.Kind` | A | `A075` | Platz in der Dreiergruppe (Schüler, Jugend, Junioren) |
| `YouthTeam.League` | B | `A075+6` | Liga (0xA078 + 3 + Mannschaft) |
| `YouthTeam.Rank` | B | `A075+3` | Platz (0xA078 + Mannschaft) |
| `YouthTeam.Strength` | B | `A075+0` | Stärke (0xA075 + Mannschaft) |
| `TrainingPlan.PriceIndex` | B | `02BC` | Preisindex (Code nennt DGROUP 0x2B8; der gesicherte Block heißt 0x2BC – in Teil 2 klären) |
| `TrainingPlan.PriceTierSet` | U | – | true, wenn `_priceTier` gesetzt |
| `TrainingPlan._priceTier` | U | `965B+0x193` | Trainingskosten/Woche (0x97EE) → Stufe zurückrechnen `[APPROX]` |
| `TrainingPlan._units` | B | `A8A1` | 28 Byte je Manager, Art = Leistenfolge (FILE05, FILE08) |
| `TalentSearch.TicksLeft` | B | `965B+0xEC` | Restzähler 1–16 |

### Sponsoren, Aktien, Nachrichten

| Feld | S | HOK-Quelle | Import |
|---|---|---|---|
| `Sponsoring.Contracts` | B | `C15B` | 11 Flächen zu 10 Byte je Manager; belegte Flächen |
| `Sponsoring.Offers` | B | `C313` | 500 · m + 100 · Art + 10 · Partner |
| `SponsorContract.Boards` | A | `C15B` | Bandenflächen 0–6 desselben Partners |
| `SponsorContract.Kind` | A | `C15B` | Flächenindex: 0–6 Bande, 7 Trikot, 8 Fernsehen, 9/10 Eis |
| `SponsorContract.MonthlyAmount` | B | `C15B+0` | Betrag / 3 |
| `SponsorContract.MonthsLeft` | B | `C15B+5` | Restmonate |
| `SponsorContract.OptionMonths` | B | `C15B+6` | Option (ohne Bit 0x80) |
| `SponsorContract.SignedRating` | B | `C15B+8` | Bewertung beim Angebot |
| `SponsorContract.Sponsor` | B | `C15B+4` | Partner |
| `SponsorOffer.Boards` | B | `C313+4` | Banden |
| `SponsorOffer.Kind` | A | `C313` | Art aus dem Satzindex |
| `SponsorOffer.MonthlyAmount` | B | `C313+0` | Betrag / 3 |
| `SponsorOffer.Months` | B | `C313+5` | Laufzeit |
| `SponsorOffer.OptionMonths` | B | `C313+6` | Option |
| `SponsorOffer.Rating` | B | `C313+8` | Bewertung (W / 10) |
| `SponsorOffer.Sponsor` | A | `C313` | Partner aus dem Satzindex |
| `StockMarket.Stocks` | B | `D0F4` | vier Aktien zu 0x48 Byte |
| `Stock.Base` | B | `01FD` | 4 Byte je Aktie, Hundertstel-DM |
| `Stock.Countdown` | B | `A891` | Byte je Aktie |
| `Stock.Direction` | B | `A895` | Byte je Aktie |
| `Stock.History` | B | `D0F4+0` | 36 Wörter Kursverlauf / 3 |
| `Stock.Name` | A | `D0F4` | feste Namen des Originals (Remake `FinanceStocks`) |
| `Stock.Price` | B | `D0F4` (+0x48 · i + 0x46) | letztes Wort des Verlaufs (0xD13A) / 3 |
| `StockHolding.Count` | B | `D214+4` | Stückzahl (32 · m + 8 · i) |
| `StockHolding.PurchaseCost` | B | `D214+0` | Kaufpreis / 3 |
| `Message.Amount` | U | `D294` | Kreditwunsch: Feld im Satz nicht belegt; 0 |
| `Message.Borrower` | U | `D294` | Kreditwunsch: nicht belegt |
| `Message.Contract` | N | – | null (nur Spielstände vor Schema 7) |
| `Message.Day` | A | `D294+1` | Sortierschlüssel im Satz (min(0x3B7, 255), FinanceInbox) − 1 (FILE20: 217, 221) |
| `Message.DaysLeft` | B | `D294+0` | oberes Halbbyte von +0 |
| `Message.Done` | U | `D294` | false |
| `Message.Kind` | A | `D294+0` | Art Bit 3 (ohne Spieler) / Angebot mit Spieler |
| `Message.Lines` | B | `D294+3` | Text im Satz, nullterminiert, Zeilen durch „^“ getrennt (FILE20/FILE48: Datum, Kopf, Spieler) |
| `Message.Player` | B | `D294+2` | Poolspieler (Pool +0x1E = offene Meldung) |
| `Message.Season` | A | `03BD` | laufende Saison |

### Vereine, Teamwerte, Taktik, Aufstellung

| Feld | S | HOK-Quelle | Import |
|---|---|---|---|
| `Club.ForfeitsMatch` | A | `5DC3+0x0A` | weniger als neun Eingesetzte im Managerkader; sonst false |
| `Club.Index` | A | `far:9657+0x00` | Datenbankindex nach Regel 1 |
| `Club.League` | A | `far:9657` | Satzplatz / 12 für Plätze 0–35, sonst null |
| `Club.LeaguePenaltyMinutes` | B | `8257` | DGROUP 0x82A1 + 0x50 · Satz (Wort) |
| `Club.Lineup` | A | `5DC3+0x15` | Managervereine aus Reihe/Sturm-Bit und `+0x0A`; sonst Auto-Aufstellung |
| `Club.LogoGraphic` | A | `far:9657` | Logo aus der Datenbank `kb\23` über den Namen (Regel 1); der Satz +0x34 wird nicht gelesen (R14HN-01: der Versatz stand bisher nur zufällig in einem Kommentar) |
| `Club.Name` | B | `far:9657+0x00` | Name; „nur Weltkugel“ siehe Regel 1 |
| `Club.Players` | A | `5DC3` | Managervereine aus dem Kader; sonst erzeugt (Regel 3) |
| `Club.ReferenceRating` | N | – | neu aus der Auto-Aufstellung |
| `Club.SeriesCurrent` | B | `AB7B+0xA00` | DGROUP 0xB57B + 0x15 · Satz (64 Sätze) |
| `Club.Strength` | A | `far:9657` | Datenbankstärke `kb\23` |
| `Club.Tactics` | B | `A6FB`, `A707`, `A701`, `CAE9` | Zeilen `Tactics` (Managervereine); sonst Automatik |
| `Club.TeamValues` | B | `far:9657+0x17` | Zeilen `ClubTeamValues` |
| `Club._autoLineup` | B | `0375+0` | 0x375 + 2 · m (aktuell); 0x376 + 2 · m sichert den Wert über die Spiele des Tages (re-runde-14.md, Taktik 375h) |
| `Club._dressed` | A | `5DC3+0x0A` | IDs der Spieler mit `+0x0A` = 1 |
| `Club._dressedByHand` | A | `0375` | nicht „automatisch aufstellen“ |
| `Club._rankSlots` | B | `8257` | DGROUP 0x828B + 0x50 · Satz, 44 Halbbytes |
| `Club._tableRank` | B | `8257` | DGROUP 0x8288 + 0x50 · Satz, Tabellenfeld der Zuschauerformel (A046, Schema 43; 0 = Platz 1) |
| `ClubTeamValues.AttackCondition` | B | `far:9657+0x19` | Kondition Sturm; im HOK absolut, im Remake Abweichung vom Datenbankwert (Import − / Export + `ComputerTeamValue`, #729) |
| `ClubTeamValues.AttackForm` | B | `far:9657+0x1D` | Form Sturm |
| `ClubTeamValues.AttackTechnique` | B | `far:9657+0x1B` | Technik Sturm |
| `ClubTeamValues.DayForm` | B | `far:9657+0x17` | random(47, 53), nach jedem Ligaspiel nachgeführt |
| `ClubTeamValues.DefenseCondition` | B | `far:9657+0x18` | Kondition Abwehr |
| `ClubTeamValues.DefenseForm` | B | `far:9657+0x1C` | Form Abwehr |
| `ClubTeamValues.DefenseTechnique` | B | `far:9657+0x1A` | Technik Abwehr |
| `Tactics.AttackMode` | B | `A707` | Byte je Manager |
| `Tactics.EffortMode` | B | `A6FB` | Byte je Manager |
| `Tactics.Goalie` | N | – | Knopf oben links ist „automatisch aufstellen“ (0x375, re-runde-14.md), kein Torwartwechsel; Auto. `A89A` (6 Byte) bleibt offen |
| `Tactics.LineChange` | B | `A701` | Byte je Manager (re-rest.md, Taktikbildschirm) |
| `Tactics._attack` | B | `CAE9` | 0x30 je Manager, Angriffshälfte |
| `Tactics._effort` | B | `CAE9` | 0x30 je Manager, Einsatzhälfte |
| `Tactics._minutes` | B | `CC43` | 8 · m + 4 · Teil + Block |
| `Tactics._shownAutoAttack` | A | `far:9657+0x35` | nur Nationalteam: Vereinssatz +0x35–0x3A |
| `Tactics._shownAutoEffort` | A | `far:9657+0x26` | nur Nationalteam: Vereinssatz +0x26–0x2B |
| `Lineup._defenders` | A | `5DC3+0x15` | Reihe 1–3 ohne Sturm-Bit, Nationalteam Satz 5 |
| `Lineup._forwards` | A | `5DC3+0x15` | Reihe 1–3 mit Bit 0x80 |
| `Lineup._goalkeeper` | A | `5DC3+0x15` | Torhüter mit Reihe 1 |

### Spieler, Pool, Transfermarkt

| Feld | S | HOK-Quelle | Import |
|---|---|---|---|
| `Player.Age` | B | `far:8253+0x14` | Alter |
| `Player.Assists` | B | `far:8253+0x1F` | Wort; beim Torhüter Spielminuten |
| `Player.Caps` | U | – | 0 |
| `Player.ClubGamesCarry` | A | `5DC3+0x1C` | `+0x1C` − `+0x06` (Ligaspiele beim Verein aus Vorsaisons; A135: je Wettbewerb) |
| `Player.ClubCupGamesCarry` | A | `5DC3+0x1E` | `+0x1E` − `+0x07` (A135, Schema 43) |
| `Player.ClubEuropeGamesCarry` | A | `5DC3+0x20` | `+0x20` − `+0x08` (A135; Europapokal: nur aus Datei 0x5494C–0x5499E belegt, kein Original-Stand hat Europapokalspiele oder -tore (R14KS9-01)) |
| `Player.ClubGoalsCarry` | A | `5DC3+0x22` | `+0x22` − `+0x03` (Feldspieler; Torhüter 0, [APPROX]) |
| `Player.ClubCupGoalsCarry` | A | `5DC3+0x24` | `+0x24` − `+0x04` (Feldspieler; Torhüter 0) |
| `Player.ClubEuropeGoalsCarry` | A | `5DC3+0x26` | `+0x26` − `+0x05` (Feldspieler; Torhüter 0; Europapokal: nur aus Datei 0x5494C–0x5499E belegt, kein Original-Stand hat Europapokalspiele oder -tore (R14KS9-01)) |
| `Player.CupGames` | B | `5DC3+0x07` | Saisonspiele DEB-Pokal (A135), Teil von `Games` |
| `Player.EuropeGames` | B | `5DC3+0x08` | Saisonspiele Europapokal (A135), Teil von `Games`; Europapokal: nur aus Datei 0x5494C–0x5499E belegt, kein Original-Stand hat Europapokalspiele oder -tore (R14KS9-01) |
| `Player.CupGoals` | B | `5DC3+0x04` | Saisontore DEB-Pokal (Feldspieler; Torhüter 0), Teil von `Goals` |
| `Player.EuropeGoals` | B | `5DC3+0x05` | Saisontore Europapokal (Feldspieler; Torhüter 0), Teil von `Goals`; Europapokal: nur aus Datei 0x5494C–0x5499E belegt, kein Original-Stand hat Europapokalspiele oder -tore (R14KS9-01) |
| `Player.Condition` | B | `5DC3+0x10` | Kader; Pool `+0x15` |
| `Player.ContractYears` | B | `5DC3+0x0B` | 0 = „kein V.“ |
| `Player.Form` | B | `5DC3+0x12` | Kader; Pool `+0x17` |
| `Player.Games` | B | `5DC3+0x06` | Summe `+0x06` … `+0x08` |
| `Player.GoalieMinutes` | B | `far:8253+0x1F` | Torhüter |
| `Player.Goals` | B | `5DC3+0x03` | Summe `+0x03` … `+0x05` (Feldspieler) |
| `Player.GoalsAgainst` | B | `5DC3+0x03` | Torhüter: Gegentore |
| `Player.HiddenSkill1` | B | `5DC3+0x19` | unteres Halbbyte |
| `Player.HiddenSkill2` | B | `5DC3+0x19` | oberes Halbbyte |
| `Player.Id` | N | – | neu vergeben (Regel 3) |
| `Player.InjuredGames` | B | `5DC3+0x0D` | Restdauer bei `+0x09` Bit 0x02 |
| `Player.InjuryKind` | B | `5DC3+0x17` | Art 0–27 |
| `Player.InsuranceLocked` | B | `5DC3+0x2C` | Bit 31 |
| `Player.InsurancePremium` | B | `5DC3+0x30` | Prämie / 3 |
| `Player.IsForeigner` | A | `far:8253+0x00` | Ausländerliste der Datenbank nach Namen |
| `Player.Loan` | B | `5DC3+0x0C` | Leihe: `+0x0C` = Vereinssatz + 0x80 (em.exe `0x47920–0x47925`), Verleiher über die Vereinsabbildung; Laufzeit/Gehalt/Versicherung aus dem Leih-Kaderplatz (`[APPROX]`, Stammvertrag nicht belegt); sonst null (#688 R688S-02, A115: FILE44 VALLIERE `0xB9`, Pool `+0x1A` 4) |
| `Player.MonthlySalary` | B | `5DC3+0x28` | Gehalt / 3 |
| `Player.Name` | B | `far:8253+0x00` | Name (17 Zeichen) |
| `Player.NegotiationLock` | B | `5DC3+0x18` | Tage |
| `Player.Notice` | B | `5DC3+0x18` | Bit 0x80 → Retiring (Test 0x4A6C7, R14HN-03), > 99 → Offered (Angebot des Spielers, 0x4A3AF; Saisonende 0x27D05 = 101), sonst None (Sperre, siehe `NegotiationLock`) |
| `Player.NoticeYears` | B | `5DC3+0x18` | bei Offered: +0x18 − 100 |
| `Player.Number` | A | `5DC3` | Kaderplatz + 1 (keine Rückennummer im Original) |
| `Player.OnLoan` | B | `5DC3+0x0C` | Bit 0x80 gesetzt → true (#688 R688S-02, A115) |
| `Player.Penalties2` | B | `5DC3+0x01` | 2-Minuten-Strafen |
| `Player.Penalties5` | B | `5DC3+0x00` | 5-Minuten-Strafen |
| `Player.PurchasePrice` | B | `5DC3+0x2C` | ohne Bit 31, / 3 |
| `Player.Role` | A | `far:8253+0x18` | Rollenwert 0 → Torhüter, < 50 Abwehr, sonst Sturm |
| `Player.Strain` | B | `5DC3+0x13` | Belastung 50–150 |
| `Player.SuspendedGames` | U | – | 0 |
| `Player.Talent` | B | `far:8253+0x12` | 0–9 |
| `Player.Technique` | B | `5DC3+0x11` | Kader; Pool `+0x16` |
| `Player.Trend` | A | `5DC3+0x0E` | Stufe (Wert − 30) / 13 aus dem Tendenzwert (0x41AE0); Wert 0 lässt die Stufe des Nachspielens `[APPROX]` |
| `Player.TrendControl` | B | `5DC3+0x14` | Steuerbyte |
| `Player.TrendScore` | B | `5DC3+0x0E` | Tendenzwert 30 … 68 (0x4907A, Player.Slot.cs) |
| `Player.UnsolicitedOffer` | B | `5DC3+0x09` | Bit 0x40, Verein `+0x16` (Regel 1) |
| `PlayerLoan.ContractYears` | U | – | wie `Player.Loan` |
| `PlayerLoan.Insured` | U | – | wie `Player.Loan` |
| `PlayerLoan.MonthlySalary` | U | – | wie `Player.Loan` |
| `PlayerLoan.Origin` | U | – | wie `Player.Loan` |
| `PoolSlot.Club` | B | `far:8253+0x1D` | Regel 1; 0xFF = keiner |
| `PoolSlot.Index` | B | `far:8253` | Satznummer 1–199 |
| `PoolSlot.Owner` | B | `far:8253+0x1A` | 0–3 Manager → Taken, 4 → Market, sonst Free |
| `PoolSlot.Player` | B | `far:8253` | Spieler mit Pool-Grundwerten (`+0x15`–`+0x17`) |
| `PoolDatabaseName.Club` | N | – | Remake-Zuordnung Datenbankname ↔ Pool: aus den Poolnamen neu bilden |
| `PoolDatabaseName.Name` | N | – | wie `Club` |
| `PoolDatabaseName.Slot` | N | – | wie `Club` |
| `TransferBid.Amount` | U | – | offene Gebote nicht belegt; leer |
| `TransferBid.Buyer` | U | – | leer |
| `TransferBid.Listing` | U | – | leer |
| `TransferListing.From` | B | `far:8253+0x1D` | Verein des Poolspielers (Regel 1) |
| `TransferListing.FromPool` | A | `5DC3+0x0F` | Marktplatz mit Pool-Spieler, nicht aus einem Managerkader |
| `TransferListing.IsFreeAgent` | A | `5DC3+0x0B` | Vertrag 0 und Preis 0 |
| `TransferListing.ListedDay` | U | – | `PlayedMatchDays` |
| `TransferListing.Player` | B | `5DC3+0x0F` | Satz 4, Poolindex |
| `TransferListing.Price` | B | `5DC3+0x2C` | Satz 4, Preis / 3 |
| `TransferListing.RejectedBy` | B | `5DC3+0x1A` | Satz 4: Bit 0x80 und 1 << Manager |
| `TransferListing.Seller` | U | – | null (vom Manager angebotene Spieler: Kennung nicht belegt) |
| `TransferMarket.UpdatedDay` | A | `07A9` | = `PlayedMatchDays` |
| `TransferMarket._bids` | U | – | leer |
| `TransferMarket._databaseNames` | N | – | neu bilden |
| `TransferMarket._freeAgents` | U | – | leer |
| `TransferMarket._listings` | B | `5DC3` | Satz 4 (FILE05: 7 Plätze) |
| `TransferMarket._pool` | B | `far:8253` | 200 Sätze |
| `TransferMarket._poolTurnBundesliga` | N | – | DGROUP 0x48FB liegt in keinem Block: 0 (wie nach Programmstart) |
| `TransferMarket._poolTurnOberliga` | N | – | wie oben (0x48FD) |
| `TransferMarket._poolTurnZweiteLiga` | N | – | wie oben (0x48FC) |

### Liga, Pokale, Nationalteam, Saisonbilanz

| Feld | S | HOK-Quelle | Import |
|---|---|---|---|
| `MatchDay.Matches` | A | `far:9657` | Paarungen nach Tabelle 0x4E7C7 über die Satzplätze (siehe `GameState.Schedules`) |
| `MatchDay.Number` | A | `07A9` | Spieltag |
| `MatchResult.Away` | A | `A0CB` | Gast der Paarung nach Spielplan |
| `MatchResult.AwayGoals` | B | `A0CB` | zweites Byte der Paarung |
| `MatchResult.Home` | A | `A0CB` | Heim der Paarung nach Spielplan |
| `MatchResult.HomeGoals` | B | `A0CB` | erstes Byte (Marken + 30 bei Pokalspielen) |
| `MatchResult.League` | A | `A0CB` | Liga aus dem Abschnitt (0x210 je Liga) |
| `MatchResult.MatchDay` | A | `A0CB` | Spieltag aus dem Versatz |
| `MatchBalance.Draws` | A | `8257` (0x825A + 0x50 · Satz + 0x1E) | Spiele − Siege − Niederlagen (Original zählt Unentschieden nicht) |
| `MatchBalance.GoalsAgainst` | B | `8257` (0x825A + 0x50 · Satz + 0x1A) | Heim/Auswärts |
| `MatchBalance.GoalsFor` | B | `8257` (0x825A + 0x50 · Satz + 0x16) | Heim/Auswärts |
| `MatchBalance.Losses` | B | `8257` (0x825A + 0x50 · Satz + 0x2A) | Heim/Auswärts |
| `MatchBalance.PointsMinus` | A | `8257` (0x825A + 0x50 · Satz + 0x1E) | 2 · Niederlagen + Unentschieden |
| `MatchBalance.PointsPlus` | B | `8257` (0x825A + 0x50 · Satz + 0x00) | Punkte Heim/Auswärts |
| `MatchBalance.Wins` | B | `8257` (0x825A + 0x50 · Satz + 0x26) | Heim/Auswärts |
| `KnockoutMatch.Away` | A | `5CFB`, `CED8` | Gast aus der jeweiligen Paarungsliste |
| `KnockoutMatch.AwayGoals` | A | `D014`, `CDB8` | DEB-Pokal: Platz des Gasts; Gruppen: Gasttor-Byte (Teil 4c) |
| `KnockoutMatch.Day` | A | `D014`, `CED8` | Termin aus Spielnummer bzw. Terminzeile (Teil 4c) |
| `KnockoutMatch.Home` | A | `5CFB`, `CED8` | Heim aus der Paarungsliste |
| `KnockoutMatch.HomeGoals` | A | `D014`, `CDB8` | Heimbyte modulo 30 (Teil 4c) |
| `KnockoutMatch.PenaltyWinner` | A | `D014` | Marke 1 (+30) = Penaltyschießen (Teil 4c) |
| `CupTie.First` | B | `5CFB` | Paarung, linke Seite |
| `CupTie.Games` | A | `D014`, `D074` | Spiele der Paarung (Teil 4c) |
| `CupTie.Marks` | A | `D014` | Marke = Heimbyte div 30 (Teil 4c) |
| `CupTie.Second` | B | `5CFB` | Paarung, rechte Seite |
| `CupGroup.Number` | A | `CFFC` | Gruppe aus dem Satzindex (DGROUP 0xCFF4 + 8 · Wettbewerb + 4 · Gruppe) |
| `CupGroup.Results` | B | `CED8`, `CDB8` | Paarungen und Tore der Gruppe |
| `CupGroup.Teams` | B | `CFFC` | DGROUP 0xCFF4 + 8 · Wettbewerb + 4 · Gruppe (im HOK ab 0xCFFC, Wettbewerb 1–3) |
| `DebCup.Winner` | B | `0387` | nach dem Endspiel (0x34ED6); vorher hält 0x387 den Vorjahressieger, den das Remake nur in `History` führt (N) → null |
| `DebCup._draw` | B | `5CFB` | Auslosung |
| `DebCup._rounds` | B | `5CF3`, `CFFC` | Runde und Spiele |
| `EuropeanCup.FinalRound` | A | `CFFC` (+16) | ab dem siebten Termin `07A9+6` |
| `EuropeanCup.GermanEntrants` | B | `A776` | deutsche Teilnehmer (FILE22/23) |
| `EuropeanCup.Winner` | B | `0388` | nach dem zwölften Termin (0x314D6); vorher Vorjahressieger wie `DebCup.Winner` → null |
| `EuropeanCup._draw` | A | `CFFC+8` | Teilnehmer DGROUP 0xD004 (Teil 4c); `A763` (18 Byte) ungedeutet |
| `EuropeanCup._groups` | A | `CFFC` (+8), `CED8` | Gruppen aus 0xCFF4 und Paarungen |
| `WorldCup.Ranking` | A | `5CFB` (+0x60) | aus den K.-o.-Plätzen DGROUP 0x5D5B nach dem letzten Termin und den Gruppenfünften/-sechsten |
| `WorldCup.Winner` | U | – | Weltmeister: Ort nicht belegt; aus dem Endspiel |
| `WorldCup._groups` | A | `far:9657`, `CED8` | Gruppe g = Sätze 52 + 6g … (Regel 1), Paarungen wie Europapokal (Wettbewerb 0) |
| `WorldCup._knockout` | A | `5CFB` (+0x60 = Wettbewerb 3) | Paare ab DGROUP 0x5D5B |
| `WorldCup._knockoutMarks` | U | – | Tore und Marken der WM-K.-o.-Spiele nicht im HOK (`5C4B` nach jedem Termin leer); Import 1:0, Marke 0 `[APPROX]` |
| `WorldCup._order` | U | – | Auslosungsfolge: nicht belegt; Indexfolge |
| `WorldCup._slots` | U | – | leer bis zur Auslosung |
| `Playoffs.Seeds` | A | `5CCB` | Plätze 1–8 der Abschlusstabelle |
| `Playoffs._rounds` | B | `5CFB+0x40`, `CCB2`, `CCA4` | Runden 0 … 0x7B0 aus den Plätzen 0x5D3B, Spiele 0xCCB2; Siege ergeben sich aus den Spielen (0xCCA4 mit 0x14 = Gast des fünften Termins mit drei Siegen, auch bei vorher entschiedener Serie, danach getauscht; R14HN-02). Halbfinale/Endspiel des Remakes nach Tabelle DGROUP 0x144 wie das Original (#676) |
| `PlayoffSeries.Games` | B | `CCB2` | 14 · Spiel + Platz, Tore modulo 30 (Marken abgezogen `[APPROX]`), 100 = nicht ausgetragen; Heimrecht Spiel 1/3/5 beim Platz 2k |
| `PlayoffSeries.HomeClub` | B | `5CFB+0x40` | Platz 2k (nach ungerader Spielzahl der laufenden Runde bzw. Siegbyte 0x14 im fertigen Endspiel getauscht) |
| `PlayoffSeries.AwayClub` | B | `5CFB+0x40` | Platz 2k + 1 |
| `RelegationGroup.Clubs` | B | `CFFC` | Teilnehmer DGROUP 0xCFF4 + 8 + 4 · Gruppe (zwei Letzte oben, zwei Erste unten, 0x30511) |
| `RelegationGroup.PlayedRounds` | A | `CD58+12` | Punktsumme der Gruppe / 4 (zwei Punkte je Spiel; das HOK führt keinen Terminzähler) |
| `RelegationGroup.Results` | B | `CED8+0x48`, `CDB8+0x48` | Paarungen und Tore + 0x24 · Gruppe + 6 · Termin (zwei Paare) für die gespielten Termine |
| `RelegationGroup.Rounds` | B | `CED8+0x48` | sechs Termine zu zwei Paaren (drittes Paar Füllwert) |
| `RelegationGroup.Upper` | A | `CFFC` | Gruppe 0: Bundesliga/2. Liga, Gruppe 1: 2. Liga/Oberliga |
| `NationalCoach.Formation` | A | `5DC3+0x15` | Satz 5, wie `Lineup` |
| `NationalCoach._autoLineup` | B | `0375+10` | DGROUP 0x37F (A009, Schema 41); Import: gesicherter Wert 0x380, Zug nicht offen `[APPROX]` |
| `NationalCoach._autoLineupSaved` | B | `0375+11` | DGROUP 0x380 (`0x375 + 2 · 5 + 1`) |
| `NationalCoach._turnOpen` | N | – | Zugzustand des Bundestrainers liegt nicht im Spielstand; Import `false`, der nächste Menüaufbau beginnt den Zug |
| `NationalCoach.Manager` | B | `965B+0xEB` | Satz 5 (0x554B1); `D0BA` ist an allen Ständen 0 (Kennzeichen, siehe `GameState.NationalCoach`) |
| `NationalCoach.Nation` | B | `06AC` | Land (Regel 1) |
| `NationalCoach.Tactics` | B | `A6FB+5` | Modi und Werte des Satzes 5 (`A707+5`, `CAE9+0xF0`) |
| `NationalCoach._dressed` | A | `5DC3+0x0A` | Satz 5 |
| `NationalCoach._friendlies` | B | `D0C1`, `D0DB`, `D0D3`, `D0CB`, `D0C3` | Zahl, je Spiel Heim, Gast, Heimtore, Gasttore (0x3C248–0x3C288; FILE19 Deutschland – Schweden 2:1) |
| `NationalCoach._squad` | B | `5DC3` | Satz 5 |
| `NationalSquadMember.Club` | B | `far:8253+0x1D` | Verein des Spielers |
| `NationalSquadMember.Player` | B | `5DC3+0x0F` | Satz 5, Poolindex |
| `FriendlyMatch.Away` | B | `D0D3` | Gast (Satzplatz, Regel 1; 0x3C260) |
| `FriendlyMatch.AwayGoals` | B | `D0C3` | Gasttore (0x3C284, unteres Byte des Ergebnisses von 0x30365) |
| `FriendlyMatch.Bonus` | U | – | Wertebonus des Spiels nicht im HOK: 0 `[APPROX]` (`026C` sind die Kosten je Nationalteam, im Remake Formel) |
| `FriendlyMatch.Home` | B | `D0DB` | Heim (Satzplatz, Regel 1; 0x3C252) |
| `FriendlyMatch.HomeGoals` | B | `D0CB` | Heimtore (0x3C273, oberes Byte) |
| `NationalBooking.Amount` | N | – | siehe `GameState.NationalBookings` |
| `NationalBooking.Kind` | N | – | Carryover |
| `NationalBooking.Season` | N | – | laufende Saison |
| `SeasonRecord.FinalTables` | N | – | nur in `CurrentRecord` (null) |
| `SeasonRecord.LeftOberliga` | N | – | wie oben |
| `SeasonRecord.Managers` | N | – | wie oben |
| `SeasonRecord.PromotedInto` | N | – | wie oben |
| `SeasonRecord.RelegatedInto` | N | – | wie oben |
| `SeasonRecord.Season` | N | – | wie oben |
| `SeasonRecord.Titles` | A | `D0E6`, `0387`, `0388` | Titel der laufenden Saison im `CurrentRecord` (ab dem 14. April): Meister `D0E6` (geprüft gegen die Serien), Pokal- und Europacupsieger aus den Wettbewerben |
| `ManagerSeasonResult.Club` | N | – | wie `SeasonRecord` |
| `ManagerSeasonResult.Events` | N | – | wie `SeasonRecord` |
| `ManagerSeasonResult.League` | N | – | wie `SeasonRecord` |
| `ManagerSeasonResult.Manager` | N | – | wie `SeasonRecord` |
| `ManagerSeasonResult.Premium` | N | – | wie `SeasonRecord` |
| `ManagerSeasonResult.Rank` | N | – | wie `SeasonRecord` |
| `ManagerSeasonHistory.Club` | N | – | Verlauf beginnt mit dem Import |
| `ManagerSeasonHistory.Deb` | N | – | wie `Club` |
| `ManagerSeasonHistory.DebRound` | N | – | wie `Club` |
| `ManagerSeasonHistory.Europe` | N | – | wie `Club` |
| `ManagerSeasonHistory.EuropeFinalRound` | N | – | wie `Club` |
| `ManagerSeasonHistory.League` | N | – | wie `Club` |
| `ManagerSeasonHistory.LeagueOutcome` | N | – | wie `Club` |
| `ManagerSeasonHistory.Rank` | N | – | wie `Club` |
| `ManagerSeasonHistory.Season` | N | – | wie `Club` |
| `EternalEntry.Championships` | U | – | wie `GameState._eternal` |
| `EternalEntry.Club` | U | – | wie `GameState._eternal` |
| `EternalEntry.CupWins` | U | – | wie `GameState._eternal` |
| `EternalEntry.EuropeanCupWins` | U | – | wie `GameState._eternal` |
| `EternalEntry.Points` | U | – | wie `GameState._eternal` |

### Import im Spielkern (Teil 2)

`GameState.ImportOriginal(save, datenbank, saat)` in `Core/OriginalImport.cs`; Tests `OriginalImportTests` (alle Stände
FILE01–FILE41, Rundreise, Mehrsaisonprobe). Ablauf in drei Schritten:

1. **Spielstart** wie `NewGame.Create`, aber Ligen und Satzfolge aus den Satzplätzen 0–35 (Regeln 1/2), Managervereine aus
   `965B+0x14`, kein Platztausch und keine Startmischung; Setup (`0118`, `D0B9`) und Optionen (`021D`, `0262`) aus dem HOK.
2. **Nachspielen** der Zeitleiste von Tag 0 bis vor den Tag `03B7 − 1` (der Tag selbst ist im Original noch nicht
   verarbeitet). Nach jedem Ligaspieltag ersetzt der Import die berechneten Ergebnisse durch die aus `A0CB`, so dass Tabelle,
   Play-off-Setzliste und Aufstiegsrunden dem Original folgen. Steht ein ganzer Spieltag in allen Ligen auf 0:0, hat das
   Original ihn nicht eingetragen (FILE16A, vorgestellter Tageszähler: Tabellensätze mit 2 Spielen bei 43 Spieltagen); seine
   Ergebnisse entfallen `[APPROX]`.
3. **Übernahme** der belegten Felder: Teamwerte aller Vereinssätze (`far:9657+0x17…0x1D`), je Manager Konto, Eintritt,
   Stadion, Bankdarlehen, Liga-Stufe, Werbebewertung, Talentpreis, Zinsdrittel, Jugend; Kader der Managervereine aus `5DC3`
   und dem Pool (Werte, Statistik, Vertrag, Gehalt, Versicherung, Verletzung, Aufstellung Reihe 1–3, Eingesetzte); Manager
   am Zug `A70E`. Gleichnamige Spieler gleicher Rolle behalten Objekt und ID aus dem Nachspielen.

**Spielplan und Ergebnisse (belegt in Teil 2):** Die Buchstaben der festen Tabelle 0x4E7C7 (`Schedule`, re-nachtrag.md §4)
sind Plätze in der **Satzfolge** der Liga, nicht im Vereinsindex. Ergebnis von Paarung k am Spieltag d der Liga L:
`A0CB + 0x210 · L + 12 · (d − 1) + 2 · k` (Heimtore, Gasttore). Probe: die daraus gerechnete Tabelle gleicht den
Tabellensätzen `8257` (Punkte, Tore, Gegentore, Spiele, Siege, Niederlagen) an FILE05, FILE16A, FILE24, FILE31, FILE36,
FILE37A, FILE40. Das Remake rechnet den Plan nach dem Import über die Satzfolge; ab der nächsten Saison wieder nach
`Schedule.Rounds` (Vereinsindex) – Abweichung des Remakes, nicht Teil dieser Spur.

**Noch nicht übernommen** (Stand aus dem Nachspielen nach Remake-Regeln, `[APPROX]`): frühere DEB-Pokal-Runden (das Original
führt nur die laufende), Auslosung Europapokal `A763`. Teil 4a–4d übernehmen die übrigen Bereiche (Abschnitte unten). Weiter offen aus Teil 1: Pokalzeilen `5C4B`,
`A89A`, Ewige Tabelle, Managerkopf, Leihe; Zählung des Bundestrainer-Satzes. Ohne Entsprechung im Remake (Status N/U, Review
R14HI-04): Vorjahres-Titelträger `0387`/`0388`/`D0E6` vor dem jeweiligen Endspiel (das Remake führt sie nur in `History`, die
leer bleibt), Marken (+30) der Play-off-Spiele in `CCB2`, Wertebonus der Freundschaftsspiele, Kosten je Nationalteam `026C`
(im Remake Formel), offene Meldung je Poolspieler `far:8253+0x1E` (Nachrichten kommen aus `D294`), Platzliste `CD88` (ergibt
sich aus der Tabelle, geprüft).

### Restfelder (Teil 4a)

`OriginalImport.Rest.cs` (Aufruf am Ende von Schritt 3), Tests `OriginalImportRestTests`. Je Manager:

| Bereich | HOK | Import | Beleg |
|---|---|---|---|
| Bank, Sparbuch | `965B+0x1AB` (Eintrag i = 4) | negativ → Kredit, positiv → Sparbuch `[APPROX]`, / 3 | FILE08 (Kredit 701.000 DM) |
| Managerdarlehen | `965B+0x19B` + 4 · i (i = 0–3) | negativer Eintrag i im Satz m → Darlehen Gläubiger i, Schuldner m, / 3 `[APPROX]` | Gegenbuchung beim Bankkredit FILE08 (Satz 4 Eintrag 0 = +2.103.000); Managerdarlehen an keinem Stand |
| Börse | `D0F4`, `01FD`, `A891`, `A895` | Verlauf 36 Wörter / 3, Kurs = letzter Wert; Basiswert, Restdauer, Tendenz | FILE01: 84 / 119 / 135 / 147 DM |
| Aktienbesitz | `D214` + 32 · m + 8 · i | +0 Kaufpreis / 3, +4 Stück | an keinem Stand belegt (alle 0) |
| Werbeangebote | `C313` + 0x1F4 · m + 0x64 · k + 10 · p | Art k 0 Bande, 1 Trikot, 2 Fernsehen, 3/4 Eis; Betrag 0 = kein Angebot `[APPROX]`; +4 Banden, +5 Laufzeit, +6 Option, +7 Logo (= p), +8 Bewertung | FILE05 |
| Werbeverträge | `C15B` + 0x6E · m + 10 · Fläche | Flächen 0–6 Bande (gleicher Partner, Betrag, Restlaufzeit = ein Vertrag `[APPROX]`), 7 Trikot, 8 Fernsehen, 9/10 Eis; +6 ohne Bit 0x80 („Kündbar.“, im Remake nicht geführt) | finanzen-rekonstruktion.md; an keinem Stand ein Vertrag |
| Training | `A8A1` + 28 · m | Einheit Tag · 4 + Einheit; Preisindex `02BC` (überall 100) | FILE05 = Standardplan, FILE08 Einheit 8 = 1, FILE09 alles frei |
| Taktik | `A6FB`, `A707`, `A701`, `CAE9` + 0x30 · m, `CC43` + 8 · m | Modi; Einsatz Block · 6 + Spielstand, Angriff + 0x18; Minuten 4 · Teil + Reihe | FILE24/FILE40 (Block 0 mit Automatikwerten) |
| automatisch aufstellen | `0375` + 2 · m | ≠ 0 → `AutoLineupEnabled` | FILE34–41 = 0 (Handmodus), FILE05 = 1 |
| Über-/Unterzahl | `965B+0x177` | sechs Wörter | FILE36: 12 1 1 24 71 81 |
| Talentsuche | `965B+0xEC` | Restzähler, 0 = keine | an keinem Stand ≠ 0 |
| neue Nachricht | `5A50` + m | Kennzeichen | FILE20/21/48: Manager 0 = 1 |

Je Satzplatz 0–63 (Verein nach Regel 1; Sätze, deren Verein im Remake keinen Index unter 64 hat, entfallen – nicht beobachtet):
laufende Serien `AB7B+0xA00` + 21 · Satz, Rekorde `AB7B+0x1194` + 0x10 · Satz + 2 · Art mit Gegner `AB7B+0xF94` + 8 · Satz + Art
(Gegner als Satzplatz, Regel 1), Kopf-an-Kopf `AB7B` + 0x280 · m + 10 · Satz (Managerplätze 0–3; 4–5 leer), Strafminuten
DGROUP 0x82A1 + 0x50 · Satz (Wort), Platzfelder DGROUP 0x828B + 0x50 · Satz: 44 Halbbytes, Feld i im Byte i / 2, gerades i im
unteren Halbbyte (FILE03: Feld 2 = Platz in der Tabelle `5CCB` nach dem 3. Spieltag; FILE36). Rekordserien der Manager
`AB7B+0xF40` + 21 · m. Nationalkonto: Satz 5 `+0x18F` / 3 als eine Übertragsbuchung (FILE18: 1.000.000 DM).

### Pool, Markt, Nationalkader, Nachrichten (Teil 4b)

`OriginalImport.Market.cs`, `.National.cs`, `.Messages.cs` (Aufruf nach Teil 4a), Tests `OriginalImportPoolTests`.

| Bereich | HOK | Import | Beleg |
|---|---|---|---|
| Pool | `far:8253` + 33 · i, i = 1–199 | Name, Talent, Alter, Grundwerte `+0x15`–`+0x17`, Art aus `+0x18`, Verein `+0x1D` (Regel 1); Besitzer `+0x1A` 0–3 → vergeben (derselbe Spieler wie im Managerkader), 4 → Markt, sonst frei | FILE05: 25 vergeben, 7 Markt; Besitzer an allen Ständen nur 0, 1, 4, 6 |
| Transferliste | `5DC3` Satz 4 in Platzfolge | `+0x0F` Poolindex, Preis `+0x2C` / 3, Sperre `+0x1A`, Werte `+0x10`–`+0x14`, `+0x19`; Besitzer 4 → Poolangebot (Herkunft = Poolverein), 0–3 → Angebot des Managers, Spieler nur auf dem Markt-Platz, nicht im Kader (A380, #676); Poolindex 0 = Rest des aufgerückten Satzes, kein Eintrag `[APPROX]` | FILE05 (KÜNAST 435.000 DM … D.HEGEN), FILE41 (HOHENADL Platz 7, Besitzer 0, 316.000 DM; Platz 11 mit Index 0) |
| Managerkader | `5DC3` Satz m + Marktplätze mit Besitzer m | Angebote des Managers folgen hinter den eigenen Plätzen | FILE40 → FILE41 (L7) |
| Nationalkader | `5DC3` Satz 5 (29 Plätze) | Poolindex → Spieler; Werte `+0x10`–`+0x13` für Pool- und Marktspieler `[APPROX]` (ein Wertesatz je Spieler); Reihe `+0x15` (Bit 0x80 Sturm), eingesetzt `+0x0A`; Bundestrainer aus dem Nachspielen `[APPROX]` (`D0BA` an allen Ständen 0) | FILE18–FILE20, FILE47–FILE49A (Pool `+0x1D` der Nominierten = Land 54) |
| Taktik Bundestrainer | `A6FB`/`A707`/`A701` + 5, `CAE9` + 0xF0, `CC43` + 0x28 | wie Teil 4a | FILE18 |
| Nachrichten | `D294` + 0x71 · k, k < 24 | `+0` Bits 0–2 Manager, Bit 3 ohne Spieler, oberes Halbbyte Restlaufzeit; `+1` Schlüssel = Tag + 1; `+2` Poolspieler; ab `+3` Text („^“ trennt Zeilen); Nachrichten des Nachspielens entfallen | FILE20 (zwei Verletzungsmeldungen), FILE48; Satz mit Spieler an keinem Stand |

Jeder Name nur einmal (Entscheidung Nutzer 01.11.2026): Poolnamen weichen in den Computerkadern einem nicht mehr gebrauchten
Poolspieler des Nachspielens gleicher Art, sonst einem neuen Spieler mit den Werten des alten und einem freien Datenbanknamen
`[APPROX]`; Gebote und Vertragslose des Nachspielens entfallen (Feldkarte U). Gleichnamige Poolspieler gleicher Art aus dem
Nachspielen behalten Objekt und ID.

### Pokale und WM (Teil 4c)

`OriginalImport.Cups.cs` (Aufruf nach Teil 4b), Tests `OriginalImportCupTests`. Zuordnung aus em.exe (Datei-Offsets):

- **Plätze** DGROUP `0x5CFB` + 32 · Wettbewerb, **Runde** `0x5CF3` + Wettbewerb. Auslosung 0x341AB (Parameter Wettbewerb) setzt die
  Runde auf 1; 0x3489D liest die Zahl der Sieger aus DGROUP 0x3B1[Runde], schreibt Platz 2k → k, dahinter `0xFF`, tauscht 20-mal
  und zählt die Runde hoch (bei 5 Abbruch). Hinter dem `0xFF` stehen Reste älterer Runden (FILE36 `9 2 1 3 8 7 0 5 FF …` =
  Viertelfinale, Runde 3).
- **DEB-Pokal-Termin** 0x34B76: `0x5C4B` = Tore des laufenden Tages je Platz (Heimbyte + 30 je Marke), 0x34B3E sichert sie
  nach `0xD014` + 32 · Spiel (Spiel = `0xD074`, 0–2), dann je Paarung Eintrag über 0x50283, Sieg an Platz 2k bei Heimtoren ≥
  Gasttoren (Datei 0x34C76 „jnc“), Siege `0xD075` + Platz; danach `0x5C4B` = 0 (deshalb an jedem Stand 0). Zwei Siege → nächstes
  Spiel 100 (nicht ausgetragen). Nach Spiel 1 und 2 tauschen die Vereine einer Paarung Platz, Siege und gesicherte Tore (die
  Einträge folgen also immer dem Verein); nach Spiel 3 Sieger auf Platz 2k, `0xD074` und Siege auf 0, Auslosung der nächsten Runde.
  Nach dem Endspiel: Sieger `0x387`, `0xD074` = 2, Plätze wie vor dem dritten Termin (0x34ED6–0x34EFC).
- **Import DEB-Pokal:** laufende Runde aus `5CF3`, Paarungen aus den Plätzen (ungerader Spielzähler = getauscht), Spiele aus
  `D014` mit Heimrecht nach Spielnummer, Marke = Heimbyte div 30, Tore mod 30, Marke 1 → Penaltysieger; Endspiel fertig, wenn
  `D074` = 2 und alle Siege 0. Belege FILE38/39 (Viertelfinale vor und nach Spiel 1), FILE44 (Endspiel 4:4 und 3:3 mit zwei
  Marken, 1:4, Sieger `0387`). `[APPROX]` frühere Runden aus dem Nachspielen; Ziehungsfolge vor dem Heimrechttausch 0x347EF
  nicht gesichert.
- **Gruppen** 0x30427: Teilnehmer DGROUP `0xCFF4` + 8 · Wettbewerb + 4 · Gruppe (Europapokal Wettbewerb 2 = `CFFC`+8, Endrunde
  Wettbewerb 3 = `CFFC`+16), Paarungen `CED8` und Tore `CDB8` + 0x48 · Wettbewerb + 0x24 · Gruppe + 6 · Termin (drei Paare je
  Termin; Europapokal mit einem Füllpaar). WM = Wettbewerb 0, Gruppe g = Sätze 52 + 6g … (die Tausche 0x35EDD vertauschen die
  Sätze, Regel 1). Termine gespielt: Europapokal `07A9`+6 (0x7AF), WM `07A9`+5 (0x7AE). Belege FILE36 (Europapokal nach Termin 7),
  FILE38 (nach Termin 12, Sieger = `0388` von FILE43), FILE48 (WM nach Termin 1), FILE20 (nach dem Halbfinale).
- **WM-K.-o.:** Plätze `5CFB`+0x60 = DGROUP 0x5D5B (24 Plätze wie das Remake, Sieger vorn, Weiterverteilung 0x12C). Tore der
  K.-o.-Spiele sind nicht gesichert: Import 1:0 für den Sieger `[APPROX]`. In Spielständen mit Vereinssaison belegen Endrunde und
  Aufstiegsrunde dieselben Plätze (FILE36, FILE44); der Import liest sie nur nach dem fünften WM-Termin.
- **Deutsche Europapokal-Teilnehmer** `A776`: Folge der Liste, Zahl (DGROUP 0xA775) nicht im HOK → Teilnehmer = Vereine der
  Auslosung unter 64 `[APPROX]`.

### Play-offs und Aufstiegsrunden (Teil 4d, Review R14HI-01)

`OriginalImport.Playoffs.cs` (Aufruf nach Teil 4c), Tests `OriginalImportPostseasonTests` (FILE44 → FILE46, FILE22 → FILE23 nach dem
Saisonwechsel). Zuordnung aus em.exe (Overlay 0xE8, Datei-Offsets):

- **Play-off-Plätze** DGROUP 0x5D3B (`5CFB`+0x40, 24 Plätze): Setzung 0x319AC–0x319D4 schreibt Platz 2i = `5CCB`[i], 2i + 1 =
  `5CCB`[7 − i]; Viertelfinale Plätze 0–7, Halbfinale 8–11, Endspiel 12–13. Tabelle DGROUP 0x144 beim Rundenwechsel
  (0x31BED–0x31C4D): Sieger von Platz 0 → 8, 2 → 10, 4 → 9, 6 → 11, 8 → 12, 10 → 13, Verlierer → Platz 23 (Halbfinale also 1–8
  gegen 3–6 und 2–7 gegen 4–5, Heimrecht in Spiel 1 beim kleineren Platz; seit #676 paart das Remake eigene Spiele ebenso, `[ORIGINAL]` in
  `Playoffs.cs`, Serien in Platzfolge). Runde DGROUP 0x7B0, Spiel der Runde 0xCCA3 (0–4; 5 nach dem Endspiel), Anfang der Runde 0xCFFA (0, 8, 12).
- **Spiele** DGROUP 0xCCB2 + 14 · Spiel + Platz (0x3173A–0x317AE): Heimbyte + 30 je Marke (die Schleife 0x31622–0x31631 zieht sie
  vor der Wertung ab), 100 = nicht ausgetragen (0x31800 für das nächste Spiel nach dem dritten Sieg). Sieger nach Toren, Gleichstand
  zählt für den Gast (0x3166E `jnc` → 0xCCA5). Siege 0xCCA4 + Platz; hat beim fünften Termin der Gast mehr als zwei Siege, steht dort 0x14
  (0x3168B–0x316BF), danach wird getauscht – auch wenn die Serie schon vorher entschieden war (FILE22 Endspiel 3:0; R14HN-02). Nach jedem der ersten vier Spiele tauschen Heim und Gast samt Siegen und bisherigen Spielen (0x31505, Aufruf
  0x316C9); beim Rundenwechsel stellt 0x31C3A–0x31C46 die Setzreihenfolge wieder her. Im Import: Paarung (2k, 2k + 1) in
  Setzreihenfolge, in der laufenden Runde nach ungerader Spielzahl getauscht, im fertigen Endspiel bei Siegbyte 0x14. Meister
  `D0E6` (0x31CBF). `[APPROX]` Die Marken stehen an FILE22/FILE44 auch bei klaren Ergebnissen (4:2) und werden nicht gedeutet.
- **Aufstiegsrunden** = Wettbewerb 1 der Gruppentabellen (0x30852, Aufruf 0x38691): Teilnehmer `CFFC` + 4 · Gruppe (Gruppe 0
  Bundesliga/2. Liga, 1 = 2. Liga/Oberliga: die zwei Letzten der oberen, dann die zwei Ersten der unteren Liga, 0x30511),
  Paarungen `CED8` und Tore `CDB8` + 0x48 + 0x24 · Gruppe + 6 · Termin (zwei Paare, drittes Paar Füllwert), Punkte `CD58`,
  Tore `CD28`, Gegentore `CCF8`, Platzliste `CD88` je + 12 + 6 · Gruppe + Platz. Gespielte Termine = Punktsumme / 4 (kein
  Terminzähler im HOK). Belege FILE44 (Platzliste 3 2 0 1: EC Kassel und ES Weißwasser steigen auf, wie FILE46) und FILE22
  (ERC Schwenningen, wie FILE23). Der Saisonrückblick (Meister, Auf-/Absteiger vom 13. April) wird nach dem Import neu
  eingetragen.

### Feldbelege aus der DOSBox-Aufnahme `runde-13` (re-runde-14.md, Leitstelle L7)

Belegt in `docs/analyse/re-runde-14.md` (Spur `re-dosbox`), hier für die Feldkarte zusammengefasst; die Umsetzung im Spielkern
liegt bei den genannten Spuren.

- **DGROUP 0x9744 + 0x1AF · m** (`965B+0xE9`): 1 bei „eine Saison, ein Manager“ (Spielende 1994, ein Manager; em.exe Datei
  0x29CFA–0x29D23), sonst 30 = „nicht im Wettbewerb“. FILE29/30/31: HOK 0x00F70 = 1; FILE01/05/09/10/27/28: 30 (U15-c).
- **DGROUP 0x8288 + 0x50 · Satz** (`8257`, Tabellensatz +0x2E): Tabellenplatz für die Zuschauerformel 0x3F185 (Datei
  0x54608–0x5464A), 0 = Platz 1. Vor dem ersten Spieltag des Spiels 0, in späteren Saisons der Wert vom Ende der Vorsaison.
  HOK 0x03C5F + 0x31 + 0x50 · i: FILE29/30/01/12 alle 0, FILE31 gesetzt, FILE14/17/23 Werte der Vorsaison (A046). Im Remake
  noch kein Feld (Schema bei `kader-start`); der Import übernimmt es deshalb nicht.
- **DGROUP 0x375/0x376 + 2 · m** (`0375`): 0x375 = „automatisch aufstellen“ (Knopf oben links im Taktikbildschirm, kein
  Torwartwechsel), 0x376 sichert den Wert, während der Spiele des Tages ist 0x375 = 0 (Datei 0x54FCE–0x54FE8, 0x552F9–0x5530B,
  0x54D8C–0x54D9E). FILE29–31/05: 1/1; FILE32/33: 0/1; FILE34–41: 0/0. Der Import liest 0x375.
- **Kaderplatz +0x03–0x08, +0x1C–0x26** (`5DC3`): Tore +0x03/+0x04/+0x05 und Spiele +0x06/+0x07/+0x08 je Wettbewerb (Liga,
  DEB-Pokal, Europapokal); beim jetzigen Verein Spiele +0x1C/+0x1E/+0x20 und Tore +0x22/+0x24/+0x26 (Wörter). SP und TO der
  Kaderliste zeigen die Summe (FILE38 → FILE39 Pokal, FILE39 → FILE40 Liga; A135). Der Import summiert wie bisher (Remake
  zählt nicht je Wettbewerb).
- **Markt-Kaderplatz bei Transferliste** (`5DC3` Satz 4): Ein vom Manager auf die Liste gesetzter Spieler verlässt den Kader,
  sein Kaderplatz wandert in den Marktsatz, die Plätze rücken auf; der Pool-Satz behält Besitzer = Manager (`far:8253+0x1A`)
  und Verein (`+0x1D`). FILE40 → FILE41: HOHENADL Markt Platz 7, Pool-Satz 28 `+0x1A` = 0, `+0x1D` = 2 (A380). Für den Import
  des Transfermarkts (Teil 4b): Marktplatz mit Pool-Besitzer 0–3 = Angebot dieses Managers; der Spieler steht seit #676 auch im
  importierten Remake-Stand nur auf dem Markt-Platz, nicht im Kader (FILE26 HOCK, FILE41 HOHENADL; Test `PlayoffSemifinalTests`).

### Stand Integration Runde 14 (Schema 43) und Fixlauf #676

- Neu seit der Karte (Schema 40/41): `GameOptions.ManagerDayMinuteLoop` (nur Remake, Import `false`) und „automatisch aufstellen“
  des Bundestrainers (`0375`, 0x37F/0x380; der Import übernimmt 0x380, Test `OriginalImportRunde14Tests`).
- Review R14HI-01 bis R14HI-04 (09.11.2026): Play-offs und Aufstiegsrunden (Teil 4d), laufende Ausbauten (`965B+0x117`, `+0x128`),
  Zuschauerstatistik, Freundschaftsspiele, Tendenzwert `5DC3+0x0E`, Ankündigung `5DC3+0x18`, Bundestrainer-Amt aus dem Vereinssatz
  des Landes, Taktikanzeige des Nationalteams, Titelträger `0387`/`0388` nach dem Endspiel; `OriginalImportFieldMapTests` prüft
  seitdem, dass jede Zeile mit Status B/A ihren Block (und ihre Versätze) im Importcode hat.
- Schema 42/43 aus `kader-start` (#669) ist in der Karte und im Import: Start-Liga `0118+2`, Tabellenfeld `8257` (A046), Spiele und
  Tore je Wettbewerb samt Überträgen (A135, Kaderplatz +0x03–0x08 und +0x1C–0x26). Die Überträge prüft
  `PlayoffSemifinalTests.Import_CarriesPerCompetition_AllStands` an allen Ständen (Pokalüberträge ab der zweiten Saison,
  R14KS9-01); Europapokal ist an keinem Stand belegt, nur aus Datei 0x5494C–0x5499E.
- Fixlauf #676: Play-off-Halbfinale nach Tabelle DGROUP 0x144 auch im Remake (`Playoffs.cs` `[ORIGINAL]`), Serien in Platzfolge;
  Feldkartenprüfung ohne Kommentare (R14HN-01), Tests für eine laufende Play-off-Runde und eine teilweise gespielte Aufstiegsrunde
  (R14HN-03, synthetisch aus FILE44), eigener Spieler auf dem Markt-Platz nicht im Kader (A380).
- Schema 44 (R683-01/-04): `PlayoffSeries.HomeClub`/`AwayClub` statt `HigherSeed`/`LowerSeed` (erster Verein = Platz 2k, ab dem
  Halbfinale nicht immer der besser Platzierte); die Migration ordnet ein laufendes Viertelfinale älterer Stände in Platzfolge.
- Schema 45 (#688, R688-04): `GameState.DayOpened` – der Tageswechsel (Tagesabschluss, Takt 0x07169, Listenwechsel) läuft vor dem
  Halt; ältere Stände wurden vor ihm gesichert (Migration: false, die Zeitleiste holt ihn vor den Spielen des Tages nach).
- Schema 46 (#688, R688S-03, Nachprüfung #691): `Manager.DebtDays` entfällt – em.exe kennt keine Entlassung wegen Schulden
  (`SeasonDismissal.cs`); die Migration streicht das Feld. R688S-02: `Player.OnLoan`/`Player.Loan` aus Kaderplatz `+0x0C`
  (Leihe, Verleiher = Vereinssatz + 0x80), bisher U.
- Schema 47 (Spur `pruefung-karte` Teil 5, Entscheidung Nutzer 09.11.2026 C): `GameState.LoadCounterOffset` – Ladeeigenheit des
  Markierungstags (`GameState.LoadQuirk.cs`); FILE44 → Saisonende bucht wie `file46_2` den April-Abschluss am 30. April. Migration:
  ältere Stände 0.

### Zugang (Teil 3)

- **Werkzeug:** `emtool hok import <hok> <ziel> [--gamedata ordner] [--seed n] [--force]` schreibt den importierten Stand als
  Remake-Spielstand (`*.ems`; ohne Endung wird sie ergänzt). Saat ohne `--seed` fest 1993 `[APPROX]` (wiederholbar).
  Rückgabe 0 = geschrieben, 1 = kein Original-Spielstand oder nicht importierbar, 3 = Prüfsumme falsch (nichts geschrieben).
- **Laden-Dialog** (`UI/Screens/SaveLoad/`): listet `*.HOK` mit gültigem 8.3-Namen; beim Laden gelb und wählbar, beim
  Sichern grau (das Remake schreibt kein HOK; Export Runde 15). Getippter Name: der Remake-Stand `*.ems` geht vor, sonst
  der gleichnamige HOK; ein Klick auf den HOK-Eintrag importiert immer diesen. Kern: `OriginalImport.TryImport` in
  `Core/OriginalImport.Access.cs` (keine Ausnahme nach außen; Saat `ScreenHost.Seed`, sonst die Uhr wie beim neuen Spiel).
- **Falsche Prüfsumme:** Bildschirm „Der kleine Mogelguru“ (`MogelguruScreen`, Texte und Bild oben), es wird nichts geladen,
  Klick oder Taste führt zurück in den Dialog `[APPROX]` (Ablauf nach dem Bildschirm im Original offen, siehe unten).
- **Bildschirmfoto-Ablauf** `hok-import` (`Tools/Scenarios/HokImportAblauf.cs`): Dialog mit FILE05, FILE29, KAPUTT; Mogelguru;
  Hauptmenü nach Import von FILE05 und FILE29. Sichtvergleich mit `file05_1_hauptmenue.png`, `file29_4_hauptmenue.png`,
  `kaputt_mogelguru.png` (08.11.2026): Datum, Manager, Verein, Gegner, Spieltag, Tabellenkurve, Konto und „Vor dem ersten
  Spieltag.“ stimmen. Abweichungen: Managerkopf (nicht im HOK belegt, Feldkarte U; das Original zeigt in beiden Ständen den
  Kopf mit Sonnenbrille) und bei FILE05 eine Nachricht aus dem Nachspielen („EC RATINGEN ist an J.SCHULZ interessiert.“),
  wo das Original keine zeigt (seit Teil 4b behoben: Nachrichten aus `D294`).

## Export Remake → HOK (Spur `hok-export`, Runde 15)

`OriginalExport.Export(spiel, kb23)` in `Core/OriginalExport*.cs`, Schreiber `Formats/OriginalSaveWriter.cs` (alle Blöcke mit
Nullbytes vorbelegt, Kopf, Verschlüsselung und Prüfsummen über `OriginalSavePatcher.Encode`). Umkehrung der Feldkarte (Regeln 1–6
rückwärts); der Export ändert den Spielstand nicht (Test `OriginalExportTests.Export_IsPure`).

- **Satzfolge:** Ligen in der Folge, deren Plan 0x4E7C7 dem Spielplan des Remakes gleicht (nach Import die HOK-Folge, im neuen
  Remake-Spiel die Indexfolge), dann deutsche Vereine ohne Liga, Leerplätze, Länder in WM-Reihenfolge, übrige nach Index.
- **Neu belegt beim Export** (an den Ständen geprüft): Vereinssatz +0x34 Logo (`LogoNumber`, Managerverein + 0x80), +0x3B/+0x3C
  Farbcode (`kb\23`, niederes Byte zuerst), Namen als Rohbytes der Datenbank; Tabellensätze `8257` (Sicherung der Siege/Niederlagen
  nur im Heimfeld), Bilanz „--------“ vor dem ersten Spiel; Paarungen des Spieltags `5D7B` (Satzplätze); Saisonjahr `03BB`;
  Highscore-Schlüssel `0256`; Kosten der Freundschaftsspiele `026C` in Satzfolge; Gegner eines leeren Rekords = Satz des Vereins
  mit Index 0; Managersatz +0xE6 Pokalrunde, +0xE9 (1 bei „eine Saison, ein Manager“); unbenutzte Managersätze wie FILE01.
- **Nach der DOSBox-Probe (Review 2026-11-10 B1–B4):** Teamwerte `far:9657+0x18–0x1B` absolut (Datenbankwert + Abweichung; vor dem
  ersten Spiel zieht der Export sie wie das erste Spiel und stellt Zufall und Teamwerte danach zurück); Umlaute wie Datenbank und
  Stände (Ö = 0x99, Ä/Ü/ß = `[ ] ~`; Spielernamen der Datenbank roh, BRANNSTRÖM mit 0x5C; Nachrichten wie FILE44); Managerkopf
  `0178+m` und Kopie `965B+0x13`; Spielstand-ID, Füllbyte und Startschlüssel aus SHA-256 über Kopf und Klartext (gleicher Stand →
  gleiche Datei, verschiedene Stände → verschiedene IDs; das Original lehnt eine in der Sitzung schon geladene ID ab).
- **Nach der zweiten DOSBox-Probe (R727P-04, Nachprüfung N1):** Jedes Poolnamensfeld wird ganz geschrieben (Name, Endnull, Rest 0).
  Ursache der Namensreste: Das Remake führt die Managerkader neben 199 Poolspielern, das Original in denselben 199 Plätzen (FILE01:
  25 Sätze mit Besitzer 0). Ein Kaderspieler ohne Poolplatz (im neuen Spiel alle 25) belegt deshalb einen fremden Platz; er bekommt
  jetzt wie im Original einen freien Ligaplatz seiner Art ab dem zweiten (FILE01/05/09: 2…, 22…, 98…; bisher die höchsten Plätze,
  also Nationalspieler 180–199) bzw. den Platz eines Poolsatzes gleichen Namens (FILE41: Kaderplatz 24 → Pool 179 auf dem Markt).
  `[APPROX]` Der verdrängte freie Poolspieler fehlt im Export (der Pool fasst nicht mehr); Markt-, vergebene und Nationalspieler
  bleiben (Test `PoolNames_WholeField_OnePlayerPerIndex`).
- **Vermutet, im Original zu prüfen** (`[APPROX]`): `A70D` letzter Zugteilnehmer, `A8A0` Monat, `07A9`+4 laufende Pokalrunde.
- **Vorgabe des Spielstarts** (an allen Ständen gleich): `0120`, `017C`, `D0E4`, `022D`, `0246`, `02C0`, `021D`+13.
- **Regeln der Rundreise (B5):** jede Regel ist an Block, Satzbereich und – wo die Begründung es verlangt – an Merkmal, Satz oder
  Bedingung gebunden (Poolname nur bei anderem Text bzw. hinter dem Nullbyte, Kopf `+0x13` nur im Bundestrainer-Satz); der Test
  `AllowedRules_EachCoversADifference` gibt die Treffer je Regel aus und wird rot bei einer Regel ohne Treffer. Der Kopf (Liga-Stufe,
  Weltkugel, Name, Vereinsname) wird byte-gleich verglichen.
- **Rundreise (Teil 2, `OriginalExportRoundTripTests`):** (1) FILE01–FILE49A → Import → Export gleicht dem Klartext des Originals bis auf
  die Felder der Liste im Test (je Zeile Grund: ungedeutet, Feldkarte N/U, Rundung Drittel-DM, Satzfolge außerhalb der Ligen,
  Vorsaison, „nur Weltkugel“); FILE22/FILE44 (Ladeeigenheit des Markierungstags) prüft nur Lesbarkeit. (2) Remake → HOK → Remake →
  HOK ohne Abweichung (Spielstart, 3/22/44 Spieltage, zwei Manager, vier Saisonwechsel passiv und aktiv mit Play-offs und WM); bekannt
  verloren: erreichte Pokalrunde +0xE6 (frühere Pokalrunden aus dem Nachspielen), „eingesetzt“ bei gesperrtem Spieler (Sperre U),
  ab Saison 2 Titel und Startordnung der Vorsaison (History N).
- **Import, für die Rundreise angepasst:** Managerkopf aus `0178+m` (`OriginalImport.cs`); Teamwerte Originalwert − Datenbankwert
  (#729); Eingesetzte genau
  wie +0x0A (zuerst die nur von der Automatik Eingesetzten herausnehmen). Play-off-Spiel mit Gleichstand und Penaltysieger: der
  Export zählt den Penaltytreffer zum Ergebnis (im Original kein Gleichstand).
- **Zugang:** `emtool hok export <spielstand.ems> <ziel.hok> [--gamedata ordner] [--force]`; Prüfhilfe für die DOSBox-Probe:
  `emtool hok export --neu <name> <vereinsindex> <ziel.hok> [--spieltage n] [--kopf n] [--ems datei] [--seed n]` (neues Spiel mit Saat n, Vorgabe 1993, n Ligaspieltage über
  `GameState.Continue`). Ergebnis mit `emtool hok dump` lesbar.

## Offene Punkte

- **Pokale (nach Teil 4c):** Auslosung Europapokal `A763` (18 Byte) und `CFF8`, `5CF0` ungedeutet; Tore der WM-K.-o.-Spiele
  nicht im HOK.
- Deutung der Blöcke ohne Eintrag in der Tabelle, besonders `8257` (0x1400), `AB7B` (0x1594), `D294` (0xA98),
  `C313` (0x7D0), `D0F4`, `5D7B` (Permutation der 36 Vereine, ändert sich mit dem Spieltag; nicht die Tabelle).
- Vereinsdatensatz: in FILE03 ändern sich `+0x17`–`+0x3A` (Form, Teamwerte, weitere Statistik?). Punkte und Tore der
  Tabelle stehen dort nicht als einfache Zahl; vermutlich berechnet das Original die Tabelle aus `0xA0CB`.
- Kaderplatz: offene Bytes (Liste oben). Bei „Transfer“ steht E direkt hinter TO ("133" = TO 1, E 33), keine dreistellige Zahl.
- Kauf: Annahmeregel für Gebote unter/über Listenpreis (em.exe); wo der Sponsorzuschuss steht.
- `0xCC73`, `0x375`, Vereinsfeld `+0x26` (ändern sich mit der Aufstellung, FILE06).
- Saisonwechsel, DEB-Pokal, Bundestrainer mit Vereinsmanagern.
- Ablauf nach dem Mogelguru-Bildschirm (Rückkehr, geladener Stand behalten?).
