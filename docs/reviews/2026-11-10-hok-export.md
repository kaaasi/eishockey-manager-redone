# Review PR #727 – Spur hok-export (Remake → HOK)

Reviewer: Claude Opus 5.5 (geplanter Lauf, frische Sitzung ohne Implementierungskontext) · Base `3af4ec9` · Head `b84b94b`
(`git rev-parse HEAD` = Head, `git status --porcelain` leer) · Originaldaten aus `kaaasi/eishockey-manager-referenz` (`fd3656e`).

## 1. Merge-Empfehlung

**Nein** für Head `b84b94b`. Schreiber, Verschlüsselung und Prüfsummen stimmen, Build und alle drei Läufe sind grün, und die
Rundreise Remake → HOK → Remake ist belastbar. Die DOSBox-Probe des Nutzers (R15-D5 = D) ist aber gelaufen und hat
R727P-01…03 gefunden; alle drei sind hier mit Bytes belegt. Dazu kommen zwei Befunde aus dem Review (B4, B5). B2 zeigt außerdem,
dass die Liste `Allowed` mindestens einen Exportfehler verdeckt. Merge erst nach den Fixes und einer erneuten Probe von EXPORT01/02.

## 2. Befunde

**B1 = R727P-03 (P1) Teamwerte der Computervereine: Export schreibt Abweichung bzw. 0 statt Absolutwert.**
[`OriginalExport.League.cs:37–46`](https://github.com/kaaasi/eishockey-manager-redone/blob/b84b94baa8222c67ca97a898b2dbe5733fdace7e/src/EishockeyManager.Core/OriginalExport.League.cs#L37-L46)
schreibt `club.TeamValues` direkt. Im Remake sind Kondition und Technik dort aber Abweichungen zu `ComputerTeamValue`
(`MatchEngine.ClubFieldValues`: `value + v.DefenseCondition` …), und vor dem ersten Spiel ist `TeamValues` `null`.
Probe (Python, Klartext far:9657 + 0x18–0x1D, (Σ)/6 je Satz 0–35):
- FILE01 (Spielstart des Originals): 65, 77, 70, 67 … 30–77
- FILE39: 61, 80, 66 … 32–80
- **EXPORT01: alle 36 Sätze 0** (auch +0x17 und die Form = 0)
- **EXPORT02: 15–24** (z. B. Satz 0 `0,0,11,2,48,54`; Satz 9 `0,0,0,0,46,48`)

Damit kann im Original schon EXPORT01 kein Computerverein spielen, nicht nur EXPORT02. Die Rundreise-Tests sehen das nicht:
Original → Import → Export reicht die Bytes unverändert durch, und Remake → HOK → Remake ist in sich konsistent (Gegenstück zu
#729). Fehlender Test: „Stärke jedes Computervereins nach dem Export im Bereich der Originalstände (z. B. ≥ 25)“ für einen neuen
Spielstand und nach n Spieltagen. Beim Export vorher `EnsureTeamValues` ziehen, ohne den Spielzustand zu ändern (`Export_IsPure`).

**B2 = R727P-02 (P2) Umlaute: Export kodiert Ö als `\`, das Original als 0x99 (CP437); die Liste `Allowed` verdeckt das.**
Spielernamen gehen über `GameText.Encode`
([`OriginalExport.Squads.cs:149`](https://github.com/kaaasi/eishockey-manager-redone/blob/b84b94baa8222c67ca97a898b2dbe5733fdace7e/src/EishockeyManager.Core/OriginalExport.Squads.cs#L149)),
der Vereinsname im Kopf genauso
([`OriginalSaveWriter.cs:82`](https://github.com/kaaasi/eishockey-manager-redone/blob/b84b94baa8222c67ca97a898b2dbe5733fdace7e/src/EishockeyManager.Formats/OriginalSaveWriter.cs#L82)).
Vereinssätze nehmen dagegen die Rohbytes der Datenbank. Belege:
- Pool FILE01 `N\x99TZEL`, EXPORT01 `N\\TZEL`. Ü steht in Datenbank und Original als `]` (`K]HN`), deshalb fällt es nicht auf.
- Kopf +0x25 FILE01 `K\x99LNER EC`, Export `K\\LNER EC` (FILE01, FILE05, FILE44B).
- Rundreise per `emtool hok import`/`export`: FILE01 NÖTZEL, HÖLSCHER; FILE14 GÖTZMANN, KÖNIGER, NÖTZEL, PÖPEL; FILE29 SÖDERSTRÖM u. a.;
  FILE44B FRÖHLICH, GÖBEL u. a. – überall `0x99 → 0x5C`.

Gegenprobe zur Liste: Regel
[`far:8253 0x00–0x11`](https://github.com/kaaasi/eishockey-manager-redone/blob/b84b94baa8222c67ca97a898b2dbe5733fdace7e/tests/EishockeyManager.Core.Tests/OriginalExportRoundTripTests.cs#L85)
in einer temporären Kopie auf leer gesetzt → **50 von 52 Ständen rot**, mit 166 Abweichungen `Original 0x99, Export 0x5C`. Die Regel
ist für umbenannte Poolspieler begründet, gilt aber für jedes Namensbyte. Fix: Namen aus den Rohbytes der Datenbank, wo der
Name unverändert ist (wie bei `DatabaseClubName`), und die Regel auf Sätze mit geändertem Namen beschränken. Dazu ein Test über
alle Umlaute.

**B3 = R727P-01 (P2) Managerkopf: `965B+0x13` ist nicht der Kopf; der Import übernimmt die widerlegte Vermutung.**
Die Bytes widerlegen die Vermutung unabhängig von der Probe:
- 965B+0x13 je Satz ist in FILE01, FILE05 und FILE29 `[6,2,3,4,0,0]` und in **EXPORT01 identisch** `[6,2,3,4,0,0]`.
- Das Original zeigt aber für FILE05 den Kopf mit Sonnenbrille (`bilder/file05_1_hauptmenue.png`) und für EXPORT01 eine Frau
  (`export01_1.png`).

Der Kopf steht also woanders, z. B. in einem der ungedeuteten Felder `0120`/`017C`/`D0E4`. Folgen im Head:
- Der Import ([`OriginalImport.cs:180`](https://github.com/kaaasi/eishockey-manager-redone/blob/b84b94baa8222c67ca97a898b2dbe5733fdace7e/src/EishockeyManager.Core/OriginalImport.cs#L180))
  gibt jedem Einzelmanager-Original jetzt Kopf 6 statt der bisherigen markierten Vorgabe 0. Das ist keine Testverschlechterung,
  aber eine belegt falsche Zuordnung mit `[APPROX]`-Text „vermutlich“.
- Der Export schreibt den Kopf in das falsche Feld
  ([`OriginalExport.Managers.cs:51`](https://github.com/kaaasi/eishockey-manager-redone/blob/b84b94baa8222c67ca97a898b2dbe5733fdace7e/src/EishockeyManager.Core/OriginalExport.Managers.cs#L51)).

Fix: Feld suchen, z. B. FILE09 mit zwei Managern und bekannten Köpfen gegen Bilder diffen. Bis dahin Import auf 0 zurück und +0x13
wie FILE01 vorbelegen.

**B4 (P2) Spielstand-ID, Füllbyte und Startschlüssel hängen nur an der Saat → jeder Export mit Vorgabe-Saat hat dieselbe ID.**
[`OriginalExport.cs:44`](https://github.com/kaaasi/eishockey-manager-redone/blob/b84b94baa8222c67ca97a898b2dbe5733fdace7e/src/EishockeyManager.Core/OriginalExport.cs#L44)
ruft `writer.Randomize(new Random(seed))` auf, und `emtool` nimmt die Vorgabe-Saat 1993. Beleg (`hok dump`): EXPORT01 **und** EXPORT02
haben `Spielstand-ID -2625, Füllbyte 0x99, Startschlüssel 0x47`. Das Original weist das Laden einer ID ab, die es in dieser
Sitzung schon geladen oder gesichert hat („Keinen heutigen Spielstand laden !“, `0x5C572`; vom Nutzer bestätigt 30.10.2026, siehe
`spielstand-original.md` „Sperren“). Wer zwei Exporte nacheinander in einer DOSBox-Sitzung lädt, scheitert also am zweiten. Fix:
ID aus dem Inhalt ableiten, z. B. einem Hash über den Klartext, damit der Export wiederholbar bleibt.

**B5 (P3) Regeln ohne Bedingung gelten breiter als ihre Begründung.**
[`965B +0x13`](https://github.com/kaaasi/eishockey-manager-redone/blob/b84b94baa8222c67ca97a898b2dbe5733fdace7e/tests/EishockeyManager.Core.Tests/OriginalExportRoundTripTests.cs#L70)
ist mit „nur Weltkugel und Bundestrainer-Satz“ begründet, gilt aber ohne `When` für alle Stände und alle Sätze.
`5DC3 +0x0A` („Automatik neu berechnet“) gilt ebenfalls immer, obwohl der Import „eingesetzt“ jetzt genau übernimmt. Verdacht:
Weitere Pauschalregeln wie `5DC3 0x03–0x08`, `AB7B` ganz und `CED8`/`CDB8` ganz können ähnlich verdecken. Vorschlag: Regeln an
Bedingung oder Satz binden und je Regel die Zahl der Treffer ausgeben, damit tote oder zu breite Regeln auffallen.

Zu „Besonders prüfen“ (1): Verschlüsselung `(klar XOR k) + k`, `k += 0xD7`, laufende Summe ab Startschlüssel und zwei
unverschlüsselte Prüfsummen (0x8F09, 0xBB4C) entsprechen `spielstand-original.md` und dem Leser, der alle Originale gültig liest.
Mein eigener Python-Decoder liest EXPORT01/02 identisch. Der Kopf entspricht der Tabelle bis auf Umlaute (B2) und den
`[APPROX]`-Stapelrest. Zur ID-Formel siehe B4. Zu (3): siehe Abschnitt 3.

## 3. Ausgeführte Prüfungen, akzeptierte `[APPROX]`

`tools/pruefen.sh` (alle drei Läufe, Head `b84b94b`, SDK 10.0.402):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2687 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2081 | 0 | 457 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2082 | 605 | 0 |

**Gegenbeispiele**
- `emtool hok export --neu Joerg 5 EXPORT01.HOK --kopf 6 --ems export01.ems` → SHA-256 `f7b2f943…1803f2`, gleich mit dem
  PR-Kommentar und dem Referenz-Repo.
- `… EXPORT02.HOK --kopf 6 --spieltage 22 …` → `468517a0…473d1f`, gleich.
- `emtool hok dump` auf beide: Prüfsummen gültig, 141 Zeilen; Datum, Konto und Kader wie im PR-Kommentar.
- Rundreise `hok import` → `hok export` für FILE05, FILE14, FILE29, FILE44B (dazu FILE01), Klartext blockweise verglichen. Prüfsummen
  gültig; Managerzahl und 0x0F gleich. Bekannte Abweichungsblöcke nach Liste, dazu die Umlaute aus B2 in Pool und Kopf. Die
  Testtheorie selbst ist grün (in `pruefen.sh` enthalten).

**Gegenproben (Mutation in temporärer Kopie, danach `git checkout`)**
- Undress-Schleife im Import (`OriginalImport.cs`, „Eingesetzte genau wie +0x0A“) ausgeschaltet → `RemakeStand_SecondRoundTrip`
  und beide `MultiSeason_SecondRoundTrip` werden rot (3 von 162). Die Importtests bleiben grün; die Änderung wirkt also nur über
  die Rundreise.
- Pool-Namensregel leer → 50 von 52 Ständen rot (B2).
- Import-Tests (`--filter OriginalImport`): ohne Mutation alle grün. Durch die Import-Änderungen ist kein Test schlechter
  geworden; zum Kopf siehe B3.

**Erwartet offen – bestätigt:** Die Probe ist inzwischen gelaufen und hat R727P-01…03 ergeben. Die Kopf-Vermutung `965B+0x13` ist
widerlegt (B3). `A70D`, `A8A0`, `07A9+4` und `+0xE6` bleiben unbelegt, weil die Probe dazu nichts zeigt.

**Akzeptierte `[APPROX]`:**
- Stapelrest im Kopf als Nullbytes
- ungedeutete Felder mit Spielstart-Vorgabe (`0120`, `017C`, `D0E4`, `022D`, `0246`, `021D+13`, Vereinssatz `+0x1E–0x25`, Kaderplatz `+0x1B`)
- Rollenwert je Art
- Drittel-DM-Rundung

Nicht akzeptiert: Kopf `+0x13` (B3) und die ID aus der Saat (B4).

**Annahmen:**
- Die Stärkeanzeige ist (K+T+F)/6 nach `docs/spielberechnung.md` §15; nicht selbst disassembliert.
- Die Ablehnung gleicher IDs stammt aus der Doku bzw. der Bestätigung des Nutzers; nicht selbst in DOSBox geprüft.
