# Erstreview PR #1002 – Spur `bedienung` (B01, B07, B11, B12, B18)

Reviewer: Claude Opus 5.5, geplanter Lauf, eigene Sitzung ohne Implementierungskontext. Geprüft: Head `bdf8313`
(`git rev-parse` = `bdf831399a3d…`, `git status --porcelain` leer), Base `0c1811f`.

## 1. Merge-Empfehlung

**Ja** für genau `bdf8313`. Alle fünf Gegenbeispiele sind auf dem alten Code rot und auf dem Head grün; die Vorgaben aus
`re-abweichungen.md` B12 und B18 sind umgesetzt (Rechtsklick wie links, kein Abwählen per Rechtsklick, Umriss 24 an
(X+4|Y+4)–(X+67|Y+52), Kopf-Klick ohne Abwählen, A302/A331 nur Marker). B11 = A ist richtig gelesen: die drei Bildschirme mit
„jede Taste schließt“ (A348, A351, A354) schließen nur noch auf Maus und Escape; Escape/Rechtsklick der übrigen B11-Stellen bleiben
mit Marker. Das Hauptmenü ändert beim Aufbau weiter nur über `GameState.OnMainMenu`; `_newsBarOverTiles` ist reiner
Anzeigezustand. Alle drei Läufe von `tools/pruefen.sh` grün, CI `build` auf dem Head grün. Nur P3 offen.

## 2. Befunde

**BD-01 (P3) – „Gegner-/Vereinsname nur links“ (B12) ohne Regressionsschutz.**
[`InfoBarWindows.cs:44`](https://github.com/kaaasi/eishockey-manager-redone/blob/bdf831399a3dbcf4741cdba6f1750fe44bd19bd0/src/EishockeyManager.UI/Screens/InfoBarWindows.cs#L44), `:49`. Der Code ist richtig (nur
`input.Clicked`, vom PR unberührt), aber kein Test hält die Vorgabe fest. Beleg: Mutation `input.Clicked(OpponentName)` →
`… || input.RightClicked(OpponentName)` bzw. dasselbe für `OwnClubName`, dann
`dotnet test tests/EishockeyManager.UI.Tests --filter "FullyQualifiedName~Vereinsfenster|FullyQualifiedName~BedienungHauptmenue|FullyQualifiedName~MainMenu"`
→ beide Mutationen **Passed 49/49** (überleben). Vorschlag: in `BedienungHauptmenueTests` ein Rechtsklick auf beide Namen → kein
Fenster (Zeile „rechts Gegnername … –“ der B12-Tabelle). Mitnehmen bei `pruefung-karte` oder einem ohnehin laufenden Fixlauf.

Keine weiteren Befunde. Geprüft und ohne Abweichung:

- **B12 Reihenfolge** ([`MainMenuScreen.cs:170–252`](https://github.com/kaaasi/eishockey-manager-redone/blob/bdf831399a3dbcf4741cdba6f1750fe44bd19bd0/src/EishockeyManager.UI/Screens/MainMenuScreen.cs#L170)): Vereins-/
  Gegnerfenster (links) → Infofile (links, (319|0)) → Credits (beide) → Escape → Kopf → Logo → große Kacheln → Einträge, jeweils
  `MouseClicked` = links oder rechts. Rechtsklick außerhalb von Logo/Kopf/Kacheln/Einträgen fällt durch, Auswahl bleibt – wie
  Tabelle B12 (leere Fläche, Gegnername rechts). Rechtsklick auf „und tschüß“ öffnet die Frage wie links (Kachel = Auswahl).
- **B18/A297:** Leiste über den kleinen Kacheln, Einträge darunter bleiben anklickbar (`NewsBar.Update` wird dabei nicht
  gerufen, Klicks gehen an die Einträge) – entspricht „Leistenfeld → Schleife, Eintrag 1 → Ende 0x58a57“.
- **B18/A301 Pixelvergleich:** `screens … pixel-verein`, Bild 01 gegen `folge01_00600` (Kachel 7 gewählt). Gelbanteil
  (R+G−2B) entlang des Umrisses, Remake/Video: oben 358/155, links 358/108, rechts 358/90; übrige Kacheln −16/−2…−41. Umriss im
  Video an allen vier Seiten, im Remake an denselben Linien. Farbe 30 der abgewählten Kachel = `Colors.TileBackground`.
- **B18/A302, A331:** Bundestrainer ohne Kurve und ohne Achsen (Zweig leer), Lampe 0 rot bei unvollständiger Aufstellung, 1–5
  grün – passt zur Ausgabe von `b18_hauptmenue_aufbau.py` im Bericht.
- **B07:** Probe über drei Saisons (passiver Manager, alle Nachrichten gesammelt, Breite mit `Assets.Mini`): höchstens 2 Zeilen,
  breiteste Zeile 155 px (< 287 px Feld 0x10–0x12F). Code-Suche aller `Notify`/`NotifyAbout`/`Lines =` in `Core`: höchstens
  zwei Zeilen; die Texte in em.exe (Werbepartner 0x1BE01, Verletzung 0x19AC6, Talentspäher 0x19721/0x196D7, Stadion 0x19D19)
  sind eigene Zeichenketten, nur 0x19787 „$ ist an $^interessiert.^“ trennt mit „^“ – genau diese stellt der PR um.
- **Fremde Datei / fremde Tests (R15-E2 = C):** `Transfer.Unsolicited.cs` nur Text in `MessageText` gezogen (minimal, begründet).
  `SeasonUiTests` (Klick statt Enter), `NewsBarHintTests`, `AddManagerScreenTests` (Hinweis entfällt mit A778) sachlich nötig.

**Erwartet offen – bestätigt:**

- Datumszeile importierter Meldungen (HOK D294) steht als eigene Textzeile; mit zwei Textzeilen läge eine dritte auf dem
  unteren Rand (y 234). Bleibt Restlücke für `hok-format`.
- Rotes Datum A321 bei weiterer Nachricht nicht umgesetzt (nicht Teil dieses PR).
- Szenario `pixel-verein-bundestrainer` bricht auf Head **und** Base `0c1811f` ab (`PixelVerein.CoachRows`, PixelVerein.cs:91) –
  kein Fehler dieses PR (Aufräumkonzept U14).
- Neue `[APPROX]` Escape als Komfort (Fair-Play, Bestenliste, Vereinsstatistik, Hauptmenü) nach B11 = A.

## 3. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| Rückschaltprobe: Head-Tests + `src/` von `0c1811f` (nur `Transfer.Unsolicited.cs` vom Head, sonst kein Build) | rot: `Music_PlaysOnceThenStaysSilent`; `NewsBarDosboxTests.SingleLine_IsNotWrapped`, `Question_WithSeveralMessages_ShowsNoHintAndNoCounter`; `BedienungTastenTests` 3/3; `BedienungHauptmenueTests` 4/5 (`Escape_StillDeselects` grün wie angegeben) |
| Mutationen `MainMenuScreen.cs`: Kopf wählt ab; Leiste nicht über Kacheln; Umriss nur unten; Umriss 1 px schmaler; Rechtsklick wählt ab; Einträge nur links | alle rot (1–2 Tests) |
| Mutationen `InfoBarWindows.cs` (rechts öffnet Fenster) | überleben → BD-01 |
| Probe Nachrichtenbreite (3 Saisons, temporärer Test, verworfen) | max. 2 Zeilen, 155 px |
| `screens gamedata … pixel-verein` + Vergleich `folge01_00600` | Umriss deckungsgleich (s. o.) |
| `screens … pixel-verein-bundestrainer` auf Head und Base | beide Abbruch (erwartet) |
| CI Head `bdf8313` | `build` success |

`tools/pruefen.sh` (alle drei Läufe, Commit `bdf831399a3d…`, SDK 10.0.402, Ergebnis: bestanden):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3862 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2716 | 0 | 634 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2717 | 1145 | 0 |

**Akzeptierte `[APPROX]`:** Escape als Komfort (B11 = A) in Fair-Play, Bestenliste, Vereinsstatistik und Hauptmenü; Vereinsstatistik
schließt per Klick statt sofort zurückzukehren (A348, Rest). **Annahmen (nicht selbst nachgebildet):** Lesarten von em.exe zu B01
(0x431CB, 0x15DB0) und B07 (0x4B6F0–0x4B76D) aus `re-vor-dosbox.md` bzw. PR übernommen; B12/B18: `b12_hauptmenue_rechtsklick.py` und
`b18_hauptmenue_aufbau.py` mit `~/referenz/original/gamedata/em.exe` erneut ausgeführt, Ausgaben gleich `re-abweichungen.md`.
