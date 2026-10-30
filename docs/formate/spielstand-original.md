# Original-Spielstand `*.HOK` (DOS)

Spur `hok-format`, nur Analyse (Lesen). Leser: `src/EishockeyManager.Formats/OriginalSaveGame.cs`,
Werkzeug `emtool hok dump <datei> [block …]`. Belegt an drei Spielständen des Nutzers (DOSBox, Version DOS/DE;
im Referenz-Repo unter `original/spielstaende/` mit Bildern und README, nie ins Code-Repo kopieren): **FILE01** Neustart (ein Manager, Kölner EC, Bundesliga, Level 3, Dauer 3, Stärke Mittelfeld,
10.09.1993), **FILE02** wie FILE01, nur Eintritt 13 → 17 DM, **FILE03** = FILE02 nach drei Spieltagen (20.09.1993).
Alle drei: 47.949 Byte, beide Prüfsummen gültig.

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
Managers (`0x5B556`–`0x5B57F`); ob das Original ihn danach zurückschreibt, ist offen (kein Stand in diesem Modus).

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
| `0x000D2` | `020D` | 16 |  |
| `0x000E2` | `0118` | 4 | Setup-Regler Level, Dauer, Start-Liga, Start-Stärke |
| `0x000E6` | `0120` | 4 |  |
| `0x000EA` | `D214` | 128 |  |
| `0x0016A` | `D0F4` | 288 |  |
| `0x0028A` | `01FD` | 16 |  |
| `0x0029A` | `A89A` | 6 |  |
| `0x002A0` | `A891` | 4 |  |
| `0x002A4` | `A895` | 4 |  |
| `0x002A8` | `0178` | 4 |  |
| `0x002AC` | `017C` | 1 |  |
| `0x002AD` | `A707` | 6 | Taktikmodus Angriff je Manager |
| `0x002B3` | `A701` | 6 |  |
| `0x002B9` | `A6FB` | 6 | Taktikmodus Einsatz je Manager |
| `0x002BF` | `CCB2` | 70 |  |
| `0x00305` | `CCA4` | 14 |  |
| `0x00313` | `CCA3` | 1 | Meisterrunde |
| `0x00314` | `D0B9` | 1 | Spielmodus Fahne/Weltkugel |
| `0x00315` | `CC73` | 48 |  |
| `0x00345` | `CC43` | 48 |  |
| `0x00375` | `CD28` | 48 |  |
| `0x003A5` | `CCF8` | 48 |  |
| `0x003D5` | `D074` | 1 |  |
| `0x003D6` | `D014` | 96 |  |
| `0x00436` | `D075` | 32 |  |
| `0x00456` | `CFFC` | 24 | DEB-Pokal (Spiele) |
| `0x0046E` | `CFF8` | 4 |  |
| `0x00472` | `CED8` | 288 | Play-Off-/Aufstiegsrunden-Gruppen (ab 0xCF20) |
| `0x00592` | `CDB8` | 288 |  |
| `0x006B2` | `CD88` | 48 |  |
| `0x006E2` | `CD58` | 48 |  |
| `0x00712` | `026C` | 48 | Kosten Freundschaftsspiele je Nationalteam |
| `0x00742` | `D0BA` | 4 | Bundestrainer-Ernennung je Manager |
| `0x00746` | `06AC` | 1 | Nationalteam (Vereinsindex) |
| `0x00747` | `D0C1` | 1 | Zahl der Freundschaftsspiele |
| `0x00748` | `D0DB` | 8 |  |
| `0x00750` | `D0D3` | 8 |  |
| `0x00758` | `D0CB` | 8 |  |
| `0x00760` | `D0C3` | 8 | Ergebnisse Freundschaftsspiele |
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
| `0x00844` | `0375` | 12 |  |
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
| `0x03C5F` | `8257` | 5120 |  |
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
| `0x08F0C` | `5CFB` | 128 | DEB-Pokal-Paarungen |
| `0x08F8C` | `5CF3` | 8 | DEB-Pokal-Runde |
| `0x08F94` | `5CF0` | 3 |  |
| `0x08F97` | `5CCB` | 36 | Tabelle: Vereinsindex je Liga × 12 + Platz |
| `0x08FBB` | `5C4B` | 128 |  |
| `0x0903B` | `07A9` | 8 | 0x7A9–0x7AB nächster Spieltag je Liga; 0x7AF Europapokal-Termin, 0x7B0 Runden |
| `0x09043` | `AB7B` | 5524 |  |
| `0x0A5D7` | `021D` | 16 | Wettbewerbsschalter (0x227 DEB-Pokal) |
| `0x0A5E7` | `C313` | 2000 |  |
| `0x0ADB7` | `C15B` | 440 | Werbeflächen (0x6E je Manager) |
| `0x0AF6F` | `0ED8` | 1 | 5 − Spiel-Level |
| `0x0AF70` | `5A14` | 2 | Spielende (Jahr) |
| `0x0AF72` | `D294` | 2712 |  |
| `0x0BA0A` | `0262` | 2 |  |
| `0x0BA0C` | `03BD` | 2 | Saisonzähler |
| `0x0BA0E` | `A969` | 2 | Tageszähler (wie 0x3B7, Rolle offen) |
| `0x0BA10` | `0246` | 16 |  |
| `0x0BA20` | `A8A0` | 1 |  |
| `0x0BA21` | `5A50` | 5 |  |
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

## Offene Punkte

- Deutung der Blöcke ohne Eintrag in der Tabelle, besonders `8257` (0x1400), `AB7B` (0x1594), `D294` (0xA98),
  `C313` (0x7D0), `D0F4`, `5D7B` (Permutation der 36 Vereine, ändert sich mit dem Spieltag; nicht die Tabelle).
- Vereinsdatensatz: in FILE03 ändern sich `+0x17`–`+0x3A` (Form, Teamwerte, weitere Statistik?). Punkte und Tore der
  Tabelle stehen dort nicht als einfache Zahl; vermutlich berechnet das Original die Tabelle aus `0xA0CB`.
- Spielerpool `far:8253` (200 × 33) und Kaderplätze `5DC3` (6 × 0x618, Platz 0x34) gegen die Spielerstruktur in
  `reverse-engineering.md` prüfen (braucht einen Stand mit bekanntem Kader, z. B. Bild „Mannschaft“).
- Kopf im Modus „nur Weltkugel“, Stände mit mehreren Managern, Saisonwechsel, Bundestrainer: noch kein Stand.
- Text und Ablauf der Meldung bei falscher Prüfsumme (Fehlercode 5).
- `0xD0B9` = 4 bei „Fahne und Weltkugel“ (bisherige Notizen nennen 1 = nur Weltkugel, 2 = nur Fahne).
- Spielende `0x5A14` = 22222 bei Dauer 3 (unbegrenzt?).
