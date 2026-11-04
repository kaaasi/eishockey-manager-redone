# Prüfaufträge Runde 12 (Spur `re-pruefung`)

Stand: abgeschlossen (03.11.2026, geplanter Lauf Claude Opus 5.5). Quelle: `em.exe` aus `kaaasi/eishockey-manager-referenz`
(`original/gamedata/em.exe`), Disassembly mit `tools/reverse-engineering/disasm.py`; Spielstände FILE13/FILE14 mit
`emtool hok dump … far:9657 5CCB`. Adressen sind Dateioffsets; DGROUP = Segment 0x14FD (Datei 0x16DD0 + Versatz).
Texte aus DGROUP stehen hier in Remake-Schreibweise (der Font kodiert ä/ö/ü als `{`/`|`/`}`).

**Selbstprüfung** (`funktionskarte.py gamedata/em.exe --repo . --csv <scratch>` ohne `--write`): 679 Funktionen, 4811
Zuordnungen; Spielcode unverändert umgesetzt 238, genähert 4, dokumentiert 121, unbekannt 1 (Lader `tore\`, zurückgestellt bei `livespiel`); diese Datei
ist Fundstelle in 36 Funktionen. Den Stand in `re-abdeckung.md` schreibt `pixel-marker` (Welle 2).

Punkte: [E2](#e2--kurzmeldungen-der-aufstellung-datei-0x40b55), [E4](#e4--was-das-hauptmenü-beim-anzeigen-ändert),
[E5/S-K1](#e5--s-k1-konto-am-29-april), [S-K6](#s-k6--umordnung-der-vereinssätze), [UM-4](#um-4--aufrufer-der-kurzmeldung-datei-0x0c0b8),
[UM-2](#um-2--vereinsfenster-datei-0x4d385), Zusammenfassung [Umsetzung](#umsetzung).

## E2 – Kurzmeldungen der Aufstellung (Datei 0x40B55)

Aufstellung Datei 0x40364 (Ov 0x108, 4559 B). Kaderplatz des Managers = `[0xA70E] · 0x618 + Zeile · 0x34 + 0x5DC3`;
Status = Kaderplatz +0x09 & 3 (1 gesperrt, 2 verletzt), Restdauer +0x0D (Sperre bzw. Verletzung, 0xFF = heilt nicht).
Die Kurzmeldung Datei 0x0C0B8 bekommt `(Zeile 1, Zeile 2)` als Fernzeiger und gibt immer 1 zurück (Datei 0x0C176); der
Aufrufer bricht damit die Aktion ab.

**Zwei Wege mit unterschiedlichem Text:**

1. **Klick auf einen belegten Platz im Aufstellungsfeld** (rechts, x 0x101–0x137, Reihen im Abstand 42; Datei 0x40967–0x40AC4):
   - Ist die Automatik an (DGROUP 0x375 + 2 · Manager ≠ 0), wird sie zuerst ausgeschaltet und „Auto-Aufstellung / ist
     deaktiviert !“ gezeigt (Datei 0x40ACF–0x40AF3).
   - Dann Datei 0x40B37–0x40B55: **+0x0D ≠ 0 und (+0x09 & 3) ≠ 0** → Kurzmeldung **„Spieler ist“ / „noch gesperrt !“**
     (Zeiger DGROUP 0x1255 → DGROUP 0x40AE, 0x1251 → DGROUP 0x409E), Sprung ans Ende (Datei 0x40D7B), der Spieler bleibt auf seinem Platz.
     Die Bedingung trifft **gesperrte und verletzte** Spieler gleichermaßen; Zeile 1 ist fester Text, kein Name.
2. **Klick auf einen Spieler in der Kaderliste** (Hinzufügen zur gewählten Reihe, Datei 0x40D8D–0x40F0A):
   - Status `s` = +0x09 & 3, Rolle = Datei 0x04180 (+0x0F; 0 Torhüter, 1 Abwehr, 2 Sturm). Zuerst wieder Automatik aus mit
     Meldung (Datei 0x40DD5–0x40DF9).
   - `s` = 1 oder 2 (Datei 0x40E01–0x40E27): Kurzmeldung **„Spieler ist“ / „Gesperrt.“** (s = 1) bzw. **„Spieler ist“ /
     „Verletzt.“** (s = 2); Zeile 2 = DGROUP 0xB84 + 4 · (2 − s) (0xB84 „Verletzt.“, 0xB88 „Gesperrt.“). Hier ohne Prüfung
     von +0x0D. Aufruf über die gemeinsame Stelle Datei 0x40EB9.
   - Sonst in dieser Reihenfolge (Datei 0x40E2A–0x40EB9): Argument bp+6 = 0, Spieler nicht eingesetzt (+0x0A = 0) und schon
     22 Eingesetzte (Datei 0x4BE17 Art 3 = 0x16) → „Schon 22 Mann / im Team !“. Nur bei Linksklick auf einen Spieler ohne
     Reihe (+0x15 = 0): keine Reihe gewählt ([bp−9] = 0xFF) → **„Keine Reihe / ausgewählt !“** (DGROUP 0x1221/0x1225);
     Torhüter in Reihe 0–3 oder Feldspieler in die Torhüterreihe 4 → „Der darf da / nicht rein !“.

**Abgleich Remake** (`UI/Screens/Team/LineupScreen.Report`, `Core/Squad.Lineup.AssignToLineup`):

| Fall | Original | Remake |
|---|---|---|
| Gesperrter aus der Liste in eine Reihe | Kurzmeldung „Spieler ist“ / „Gesperrt.“ | Kurzmeldung „<Name>“ / „noch gesperrt !“ |
| Verletzter aus der Liste in eine Reihe | Kurzmeldung „Spieler ist“ / „Verletzt.“ | Meldungsfenster „<Name>“ / „ist verletzt.“ (`[APPROX]`) |
| Gesperrter/Verletzter auf dem Feld angeklickt (+0x0D ≠ 0) | Kurzmeldung „Spieler ist“ / „noch gesperrt !“, keine Aktion | nicht belegt (Verletzte nimmt `PlayerInjuries.RefillLineup` heraus) |
| Automatik an | erst „Auto-Aufstellung / ist deaktiviert !“, dann ggf. die zweite Meldung | nicht abgeglichen; Abgleich und Test bei `kader-transfer` Teil 4 |
| Keine Reihe gewählt | „Keine Reihe / ausgewählt !“ | nicht abgeglichen; Abgleich und Test bei `kader-transfer` Teil 4 |

Der `[APPROX]`-Kommentar in `LineupScreen.Report` („‚ist verletzt.‘ steht in keinem der sechs Aufrufe“) ist damit
aufgelöst: Der Weg ist der gemeinsame Aufruf Datei 0x40EB9, der Text „Verletzt.“ kommt aus DGROUP 0xB84.

## E4 – Was das Hauptmenü beim Anzeigen ändert

Hauptmenü Datei 0x44E22 (Ov 0x110, 3205 B, einziger Aufrufer 0x54C9F). Der Rumpf hat drei Einstiege: Funktionsanfang
(einmal je Managerzug), Datei 0x44E6D (nach Laden 0x5C3E3, Saisonbilanz 0x28165 und Infofile 0x432EC; Datei 0x4576D,
0x4590F, 0x453FB) und **Datei 0x44FBD nach jeder Rückkehr aus einem Unterbildschirm** (gemeinsamer Rücksprung Datei
0x4592F → 0x45961/0x4596C). Alle Schreibzugriffe auf DGROUP bis zur Eingabeschleife und beim Rücksprung (`disasm.py 0x44E22
0x45AA7`, alle `mov`/`inc` auf DGROUP; Schreiber in den Klickzweigen der Eingabeschleife nicht aufgeführt):

| Datei | Schreibt | Wann | Bedeutung |
|---|---|---|---|
| 0x44E32 | 0xD0C2 = 0 | Funktionsanfang | Wettbewerbscode (nur Anzeige, `TeamInfoWindow`) |
| **0x44E3F/0x44E46** | **0xE1A4 = random(0x55, 0x73)** (0x0C037) | Funktionsanfang, also je Managerzug | **Spielzustand und Zufall beim Anzeigen:** Faktor der Gehaltsforderung (85–115), einziger Leser die Vertragsverhandlung Datei 0x4A832 (`--xref 0xE1A4 0xE1A5`). Im Remake abgeleitet: `GameState.ContractTurnFactor` (`Transfer.Contract.cs`, `[APPROX]`, rein aus Saison/Tag/Managern) |
| 0x44E49–0x44E69 | 0x5148 | Funktionsanfang | Zähler automatisches Speichern: = 10 → `autosave.hok` (0x5B9DD) und 0; Manager 0 am Zug → + 1 (Remake: `SaveLoad.AutoSave.OnMainMenu`) |
| 0x44E6D | 0x38E = 0 | ab 0x44E6D | Anzeigeschalter |
| 0x44EB1 | 0xA70D = 0xA70E | ab 0x44E6D | Manager am Zug merken (für die Rücksetzung unten) |
| 0x44EB4 | 0xE1A6 = 0xFF | ab 0x44E6D, vor 0x44A48 | Gegner-Zwischenspeicher der Infoleiste leeren |
| 0x44EBE–0x44F0C | Paarungen (0x4E7C7) und Tabellenfolge (0x50B45) je Liga mit Spieltag ≤ Ligaende | ab 0x44E6D | abgeleitete Daten, aus dem Spielstand neu gerechnet |
| 0x44F1F–0x44F2D | 0x1DB = 0 | ab 0x44E6D, nur Spieltag 44 und 0x1DB = 2 | Durchlauf ohne Halt (gibt es im Remake nicht) |
| 0x44F69, 0x44F85, 0x44FB7 | 0xE1A6/0xE1A7, über 0x44A48 | ab 0x44E6D | Gegner-Zwischenspeicher der Infoleiste |
| 0x45002–0x450A4 | 0x515E ff., 0x517A, 0x518C/0x518D | ab 0x44FBD | Kachelsperren und Lampen (nur Anzeige) |
| 0x450C3 | 0xA70E = 0xA70D | ab 0x44FBD, vor 0x426BC | Manager am Zug zurücksetzen (ein Unterbildschirm kann 0xA70E ändern) |
| **0x450C6** | **Neuaufstellung 0x426BC** des Managers am Zug | **ab 0x44FBD, also bei jeder Rückkehr ins Menü** | wirkt nur mit „automatisch aufstellen“ (Remake: `GameState.BeginManagerTurn`, nur am Zuganfang) |
| **0x4527C–0x45291** | **0x5A50 + Manager = 0**, dann Leiste 0x4B5F4(0xFF) | ab 0x44FBD, nur wenn Manager am Zug (0xA70E) < Wert DGROUP [0x386] (`cmp al,[0x386]`, Datei 0x45273) und das Kennzeichen gesetzt | „neue Nachricht“: einmal je Menüaufbau gezeigt und gelöscht |
| 0x45957 | 0xA70E = 0xA70D | gemeinsamer Rücksprung Datei 0x4592F, vor 0x45961/0x4596C | Manager am Zug zurücksetzen |
| 0x45996/0x4599C | 0xC11F/0xC121 = 0 | nur beim Verlassen des Menüs ([bp−7] ≠ 0, Zugende); die Rückkehr springt schon bei Datei 0x45961/0x4596C nach 0x44FBD | Mauszwischenspeicher |

**Nachrichtenkennzeichen:** Einziger Setzer ist das Ablegen einer Nachricht Datei 0x4B2F6 (Datei 0x4B5E6: DGROUP 0x5A50 +
Manager = 1); einziger Löscher das Hauptmenü (Datei 0x45289); Leser nur Datei 0x4527C (`disasm.py --xref 0x5A50 0x5A54`). Die
Leiste 0x4B5F4 schreibt nur lokale Puffer, sie markiert keine Nachricht als gelesen; die Nachrichtensätze (DGROUP 0xD294,
`+0 & 7` = Manager) haben kein Gelesen-Bit. Ein Klick auf den Managerkopf (Datei 0x4549D) zeigt die Leiste ohne Kennzeichen.

**Abgleich Remake:** `NewsBar.Draw` setzt `message.Read = true` bei **jedem Zeichnen** der gezeigten Nachricht, und
`MainMenuScreen.Update` öffnet die Leiste, solange irgendeine Nachricht ungelesen ist. Der Hinweis aus Review #443 (bool wird
beim bloßen Zeichnen `True`) ist dieser Schreibzugriff. Das Original ändert beim Anzeigen ebenfalls Spielzustand, aber
anders: ein Kennzeichen je Manager, gesetzt beim Ablegen, gelöscht beim Menüaufbau (nicht beim Zeichnen, nicht je Nachricht).

## E5 – S-K1: Konto am 29. April

Nachgeprüft in Datei 0x54C9F (Saisonwechsel), Schleife über die Tauschpaare Datei 0x56238–0x5636D (Bestätigung von
`re-saisonwechsel.md` Abschnitt 3, dort ausführlich):

```
aus = Platzliste 0x5CCA + 12 · Gruppe + 12 − k        // Datei 0x56238–0x56259; Gruppe 2: Letzter, Vorletzter der Oberliga
f   = verein[aus] +0x16                               // Datei 0x56296: Manager + 1, 0 = kein Manager
if f != 0 and (status(f) & 0x04) == 0:                // Datei 0x5629D–0x562B3; status = DGROUP 0x959F + 0x1AF · f
    status(f) |= 0x02                                 // Datei 0x562C1
    if gruppe >= 2:                                   // Datei 0x562C6 (cmp [bp−1], 2; jc)
        konto(f) = 0x00124F80                         // Datei 0x562D8/0x562DE: zwei mov, 1.200.000 Drittel = 400.000 DM
        tausche(aus, ein, 1)                          // Datei 0x562F2
```

- **Bedingung:** genau drei Prüfungen – Verein unter den letzten zwei der **Oberliga** (Gruppe 2), Verein gehört einem
  Manager, Bit 0x04 (Zwangsabstieg) nicht gesetzt. **Keine Prüfung des Kontos.**
- **Fälle:** hohes Guthaben → auf 400.000 DM gekürzt; Schulden → auf 400.000 DM gehoben; Bundesliga/2. Liga (Gruppen 0/1)
  → nie (dort gilt der normale Tausch). Zwei Manager auf 11/12 → beide 400.000 DM.
- **Einziger Schreiber** über diese Basis (`disasm.py --xref 0x963B 0x963D`: nur Datei 0x562D8/0x562DE); keine Buchung.
- **Abgleich Remake:** `SeasonFlow.cs` (Ereignisse am Saisonende) setzt `manager.Money = OberligaKeptMoney` für
  `KeptInOberliga` ohne Kontoprüfung – stimmt. Abweichung nur theoretisch: Das Remake prüft den Zwangsabstieg dort nicht
  (in der Oberliga nicht erreichbar, siehe `re-saisonwechsel.md` Abschnitt 2). Entlassene Manager überspringt das Remake;
  im Original hat ihr früherer Verein kein Feld +0x16 mehr.

## S-K6 – Umordnung der Vereinssätze

**Routine:** Ligamischung Datei 0x35EDD (Ov 0xF0, Stub 0xF0:0x43), aufgerufen im Saisonwechsel **Datei 0x5644E** – direkt
nach dem Auf- und Abstieg (0x562F2/0x5636D), der Ewigen Tabelle (0x5667A) und 0x09D1B, vor 0x346D5 und dem Fenster
„Saisonende“ 0x26F20 (Datei 0x5645D); außerdem beim Spielstart (0x43A17). Ablauf:

1. **Länder:** 20-mal zwei Plätze `lrandom(a, a + 11)` (a = DGROUP 0x6AD; Zufall **lrandom** Datei 0xBFD5, nicht `random` Datei 0xC037, berichtigt in Runde 13, `re-runde-13.md` S-K6-b) tauschen (auch derselbe Platz): Kostenwerte
   DGROUP 0x26C + 4 · (Platz − a), Nationalteam des Managers DGROUP 0x6AC mitgeführt, Tausch 0x36169 mit Schalter 0
   (Datei 0x35EF2–0x35FCF). Im Remake schon als WM-Tausch (`CupsWorld.cs`).
2. **Ligen 0–2:** je **55-mal** zwei Plätze `lrandom(b, b + n − 1)` (ebenfalls lrandom Datei 0xBFD5, Zustand DGROUP 0xE017 nicht im Spielstand) (b = DGROUP 0x7C0 + Liga, n = DGROUP 0x7B1 + Liga = 12),
   Tausch 0x36169 mit Schalter 1 (ganzer Vereinssatz 0x3D, Tabellensatz 0x50, Serien, Spieler +0x1D, Manager) und in der
   Platzliste DGROUP 0x5CCB + 12 · Liga die beiden Einträge vertauscht, damit sie denselben Verein behalten (Datei
   0x35FD2–0x3613F). Die Vereine bleiben in ihrer Liga, nur ihre Plätze (Indizes) wechseln.

**Beleg FILE13 (31.01.1994) → FILE14 (10.09.1994):** In allen drei Ligen sind die Indizes permutiert (je Liga nur ein
Fixpunkt: HEC Heilbronn 9, EC Kassel 16, Schalker Haie 29; Krefelder EV 27 → 24, Mannheimer ERC 5 → 11), die Ligamitglieder
stimmen bis auf Auf- und Abstieg (SB Rosenheim ↔ ES Weißwasser, EV Landsberg/Bad Nauheim, Wedemark/Miesbach). Die Vereine
36–47 ohne Liga bleiben, außer dem Nachrückertausch 35 ↔ 40. Die Länder 52–63 sind ebenfalls umgestellt. Damit ist der
Zeitpunkt der 29. April (Saisonwechsel), nicht der Sommer.

**Wirkung im Original:** Alles, was nach Index innerhalb der Liga geht, ist je Saison neu gewürfelt – der Spielplan (feste
Paarungstabelle 0x4E7C7 über die Plätze), die Fair-Play-Folge bei Gleichstand (0x2A1E0, Indexfolge, A194) und andere
Gleichstandsregeln über die Satzfolge.

**Abgleich Remake:** Die Vereine behalten feste Indizes. Den Spielplan würfelt `SeasonFlow` je Saison neu (Fisher-Yates über
die Ligamitglieder, dann Kreisverfahren) – für den Spielplan genügt das. Offen ist nur die Gleichstandsfolge: `FairPlayTable`
sortiert nach festem Remake-Index (`[APPROX]`, A194).

## UM-4 – Aufrufer der Kurzmeldung Datei 0x0C0B8

| Aufruf | Text (DGROUP) | Anlass und Bedingung | Zeitpunkt | Remake |
|---|---|---|---|---|
| 0x40EF7 | „Schon 6 Feldspieler“ / „auf dem Eis !“ (0x1231/0x1235) | Aufstellung, Linksklick auf einen **Torhüter** in der Liste bei gewählter Torhüterreihe (4), wenn Reihe 1 schon **sechs Feldspieler** hat und kein Torhüter steht ([bp−0xE], Datei 0x404E8–0x404FD: Zahl in Reihe 1 = 6 und Torhüterplatz leer) und vorher keine andere Meldung kam | Aufstellungsbildschirm | ohne Entsprechung: das Remake kennt keine sechs Feldspieler |
| 0x414C6 | „Kein“ / „Platz mehr !“ (DGROUP 0x50A7/0x50AC, direkt) | Aufstellung, Spieler in die gewählte Reihe: Abwehr- und Sturmhälfte voll (zweiter Versuch mit der anderen Hälfte, Datei 0x414D3); in der Torhüterreihe sofort; in Reihe 1 nur, wenn schon sechs Feldspieler oder ein Torhüter steht – sonst kommt er als **sechster Feldspieler** dazu (Datei 0x413E8–0x414B4, [bp−0xE] = 1) | Aufstellungsbildschirm | ohne Entsprechung (Verhalten des Remakes bei voller Reihe nicht abgeglichen; Abgleich bei `ui-meldungen` Teil 3) |
| 0x3DE9F | „Die Baufirma“ / „ist noch beleidigt...“ (0x1239/0x123D) | Stadion, Projekt p gewählt und DGROUP 0xCC09 + 11 · Manager + p ≠ 0; Auswahl danach 0xFF | Stadionbildschirm | Hinweistext `StadiumScreen.Select` (`_notice`), keine Kurzmeldung |
| 0x46DEF | „Dieser Spieler hört“ / „am Saisonende auf.“ (0x12BD/0x12C1) | Transferliste, Linksklick auf einen Spieler des eigenen Kaders (Zeile < 30; Rechtsklick öffnet Spielerdaten 0x041AC) mit Kaderplatz +0x18 Bit 0x80 (Ruhestand angekündigt); sonst weiter zu Angebot/Verkauf (Datei 0x46DFA ff.) | Transferbildschirm | ohne Entsprechung; das Bit hat das Remake als `ContractNotice.Retiring` |
| 0x5C559 | „Spielstand aus“ / „falscher Version !“ (0x97C/0x980) | Laden 0x5C3E3: Kennung „KRON_0.99“ stimmt (sonst Meldungsfenster 0x4BCCC „Kein / Eishockey Manager / Spielstand. / !!!“, DGROUP 0x1441–0x144D), die folgenden 4 Byte aber nicht „EH0“ (DGROUP 0x55E7); Datei schließen, zurück zur Auswahl | Laden | Remake-Format eigen; Hinweis bei fremder Version nicht abgeglichen (Abgleich bei `ui-meldungen` Teil 3) |
| 0x5C59A | „Keinen heutigen“ / „Spielstand laden !“ (0x12B1/0x12B5) | Laden: Spielstand-ID steht in der Liste DGROUP 0xDD4E (50 Einträge) | Laden | **nicht übernehmen** (Entscheidung Nutzer, `docs/formate/spielstand-original.md` „Sperren“) |

Dauer wie alle Kurzmeldungen: 75 Takte (25 BIOS-Ticks ≈ 1,4 s), bei Zeile 1 „Auto-…“ 45 Takte; keine Eingabe.

## UM-2 – Vereinsfenster Datei 0x4D385

Argumente `(Team, Seite, a, b)`; Hauptmenü ruft `(eigener Verein, 2, 0, 0)` beim Klick auf das eigene Logo (Datei
0x453C0–0x453DF). Ursprung **x 93 (si = 0x5D), y 44** (Datei 0x4D3FA). Der Länderzweig ist als `TeamInfoWindow` umgesetzt
(Klassenkommentar, `reverse-engineering.md` „Gegner-/Vereinsfenster“); hier die **Unterschiede im Vereinszweig** (Team < 0x40
und kein Land, [bp−0x14] = 0):

- **Kopf:** zwei Bilder über 0x0C17D (Nr. 0x21 und Team & 0x3F, Länder 0x8F; Zuordnung nicht geprüft), Rahmen
  (x + 165 | y + 8)–(x + 204 | y + 47); Name (Vereinssatz DGROUP 0x9657 + 0x3D · Team) über 0x0B6A5.
- **Kurve:** **0x445E0(x + 30, y − 30, Team, 0xFF, 0, 0)** (Datei 0x4D56D–0x4D588) – Ursprung X = 123, Y = 14, nur der
  eigene Verein (Gegner 0xFF), nur für Team < 0x40. Mit der Regel aus `re-meldungen.md` Abschnitt E: Achsen (125 | 62)–(125 | 85)
  Farbe 6 und (126 | 86)–(221 | 86) Farbe 8, Plätze auf y 63 + 2 p, Schritt ⌊85 / (n − 1)⌋, vor dem 2. Spieltag „Vor dem
  ersten Spieltag.“; für Länder zeichnet 0x445E0 nichts.
- **Bilanzzeichen** 0x51AAF über (x + 25 | y + 45)–(x + 156 | y + 45) wie im Länderzweig.
- **Knöpfe** auf y + 171: „Reicht mir“ (x + 10, DGROUP 0xE84); **„Spielplan“** (x + 80, DGROUP 0xF89) nur wenn der Tageszähler
  DGROUP 0x3B7 ≠ 0 und Team < DGROUP 0x6AD (kein Land) – öffnet den Spielplan des Gegners 0x05CA0 (`re-abdeckung.md` Nr. 12);
  dritter Knopf (x + 150) bei Wettbewerbscode 0 wie im Länderzweig (Einschätzung, Schreiber 0x0AE2B: Vereinsfeld 0x34 Bits
  6–7 und DGROUP 0xE1B6 – **ändert Spielzustand** per Klick, Datei 0x4E440–0x4E4B0).
- **Zeile 1** (y + 58, zentriert zwischen x und x + 216, Farbe 30; Datei 0x4D7F2–0x4D8DB): „<Platz>.“ + „ Platz in der “ + Liganame (DGROUP 0x9E0 + 4 · Liga aus 0x34FD5)
  + „, Stärke: “ + Wert 0x4D316; Platz = Tabellensatz DGROUP 0x8257 + 0x50 · Team, +0x31, + 1.
- **Teams ab 0x40** (Europapokal-Gegner): statt der Statistik die drei Zeilen „Leider sind aus vielerlei Gründen keine“ /
  „Informationen verfügbar...“ / „Ähm... der Grund ist der begrenzte Speicher...“ (DGROUP 0xF7D–0xF87, y + 76/84/94) und
  sofort die Eingabeschleife (Datei 0x4D6CC–0x4D8E9).
- **Statistik** ab y + 109 (+ 0x41, Datei 0x4D8EC), Zeilen je + 6: „Heimbilanz:“ (Länder „Bilanz:“) und **„Auswärtsbilanz:“**
  (nur Vereine) als „n Siege, n Unent., n Nied.“ (Tabellensatz +0x29/+0x2D bzw. +0x2A/+0x2E), „Aus d. letzten Spielen:“,
  „Tore / Spiel“, Serien, Rekorde, „Historische Ergebnisse gegen …“ (nur Code 0) wie im Länderzweig.
- **Ewige Tabelle** (nur Vereine, Datei 0x4E348–0x4E3CE, + 8): „Punkte in der Ewigen Tabelle: n“ (Tabellensatz +0x4C/+0x4E,
  Farbe 1) bzw. „Punkte in der Ewigen Tabelle: Noch keine...“ (Farbe 6), x + 5.

## Umsetzung

| Punkt | Ziel | Dateien | Testidee | Spur (Runde 12) |
|---|---|---|---|---|
| E2 | Liste → Reihe: „Spieler ist“ / „Gesperrt.“ bzw. „Verletzt.“ als Kurzmeldung (statt Name / „noch gesperrt !“ bzw. Fenster „ist verletzt.“); Feld: Klick auf einen Gesperrten/Verletzten mit Restdauer → „Spieler ist“ / „noch gesperrt !“, keine Aktion; Automatik-Meldung davor; „Keine Reihe / ausgewählt !“: der ausführende Teil stellt fest, ob das Remake den Fall (keine Reihe gewählt) kennt, und setzt die Meldung oder vermerkt „ohne Entsprechung“. `[APPROX]` in `Report` auf `[ORIGINAL]` (Datei 0x40B37, 0x40E01). **Umfang:** über „Text angleichen“ hinaus (Feldklick sperren, Automatik-Meldung, Keine Reihe) – Erweiterung von Teil 4 durch die Leitstelle | `UI/Screens/Team/LineupScreen.cs`; braucht der Feldklick ein Ergebnis aus `Core/Squad.Lineup.cs` (Dateigrenze `wettbewerbe-spiel`): Anschluss melden, keine eigene Änderung | Verletzten bzw. Gesperrten aus der Liste wählen → Kurzmeldung mit genau diesen Zeilen, kein Knopf, Aufstellung unverändert; mit Automatik zwei Meldungen nacheinander | `kader-transfer` Teil 4 |
| E4 | Kennzeichen „neue Nachricht“ je Manager statt Gelesen-Bit beim Zeichnen: setzen beim Ablegen (`FinanceInbox.Post`, alle Nachrichtenarten; `Notify` ist nur die Info-Variante; 0x4B5E6), löschen beim Menüaufbau (`MainMenuScreen.Enter`, 0x45289); Leiste nur dann. Neuaufstellung 0x426BC auch bei jeder Rückkehr ins Menü (0x450C6), nicht nur am Zuganfang. Ausnahme in `AGENTS.md` §4 vorschlagen: „Der Aufbau des Hauptmenüs löscht das Nachrichtenkennzeichen und stellt bei ‚automatisch aufstellen‘ neu auf (Original 0x45289, 0x450C6)“; ausdrücklich ausgenommen bleibt der Zufallszug 0xE1A4 je Managerzug (0x44E3F), weil das Remake ihn rein ableitet (`ContractTurnFactor`, kein Zustand, kein Zug aus dem Spielzufall) | `UI/Screens/MainMenuScreen.cs` (minimal), `Core/GameState.HotSeat.cs` (`BeginManagerTurn`: Neuaufstellung bei jeder Rückkehr; Anschluss melden, falls fremde Dateigrenze), `UI/Screens/Manager/NewsBar.cs` (Dateigrenze `geld`: Anschluss melden), neues Feld in eigener Datei (Schema + Migration), Abstimmung mit `pixel-marker` Teil 3 (R-K6, `FinanceInbox.cs`) | Zeichnen ändert den `StateFingerprint` nicht; Nachricht ablegen → erster Menüaufbau zeigt die Leiste und löscht das Kennzeichen, der zweite nicht; Rückkehr aus der Aufstellung mit Automatik stellt neu auf | `ui-meldungen` Teil 4 |
| E5 | Kein Code nötig: Remake stimmt. Marker in `SeasonFlow.cs`/`SeasonFlow.Oberliga.cs` mit Bedingung ergänzen („ohne Kontoprüfung, Datei 0x562C6–0x562DE“); Tests für die Fälle | `Core/SeasonFlow.Oberliga.cs`, Tests | Managerverein Platz 12 Oberliga mit 2.000.000 DM bzw. −500.000 DM → 400.000 DM (zuzüglich Prämien des Fensters); Platz 12 der 2. Liga → Konto unverändert; zwei Manager auf 11/12 → beide 400.000 DM | `saisonwechsel` Teil 3 |
| S-K6 | Satzfolge je Liga als eigene Permutation im Spielzustand, beim Spielstart und am 29. April neu gewürfelt (55 Tausche, 0x35EDD); Gleichstandsregeln nach Index (Fair-Play 0x2A1E0, A194) darüber; Spielplan wie bisher je Saison gewürfelt. Alternativ begründet `[APPROX]` lassen (Wirkung nur bei Gleichstand) – Entscheidung Leitstelle | `Core/SeasonFlow.cs`, neue Datei für die Permutation (Schema + Migration), `Core/StatisticsClub.cs` (A194, Anschluss melden) | Fair-Play-Gleichstand um Platz 1: Sieger wechselt mit der Permutation; Permutation ändert sich am Saisonwechsel, Ligamitglieder nicht; Mehrsaisonprobe | `saisonwechsel` Teil 3 |
| UM-4 | Bausteine je Meldung (Texte oben): „Schon 6 Feldspieler / auf dem Eis !“ und „Kein / Platz mehr !“ nur, wenn das Remake sechs Feldspieler bzw. volle Reihen abbildet (sonst als „ohne Entsprechung“ vermerken); „Die Baufirma / ist noch beleidigt...“ als Kurzmeldung statt Hinweistext; „Dieser Spieler hört / am Saisonende auf.“ beim Klick auf einen eigenen Spieler mit `ContractNotice.Retiring` in der Transferliste; „Spielstand aus / falscher Version !“ beim Laden einer fremden Version; 0x5C59A nicht | Bausteine in `UI/Screens/Team/TeamUi.cs`/eigener Datei; Aufrufer `LineupScreen.cs`, `TransferScreen.cs` (`kader-transfer`), `StadiumScreen.cs` (`geld`), Laden – im PR listen | je Meldung: Auslöser herstellen → Kurzmeldung mit genau diesen Zeilen, kein Knopf, schließt nach 75 Takten; Zustand unverändert | `ui-meldungen` Teil 3 |
| UM-2 | Vereinsfenster als neue Datei nach dem Vereinszweig oben (Kurve über `RankCurve` mit (123 \| 14), Zeile „Platz in der“, Heim-/Auswärtsbilanz, Ewige Tabelle, Knopf „Spielplan“, Text für Teams ab 0x40), Aufruf aus dem Hauptmenü per Klick auf das eigene Logo; dritter Knopf weiter offen (ändert Zustand, Bild 0xC12F nicht zugeordnet) | neue Datei in `UI/Screens/…`, `MainMenuScreen.cs` (Klick) | Bildschirmfoto gegen Video, Kurve: Achsen bei (125 \| 62)–(125 \| 85) und (126 \| 86)–(221 \| 86); Fenster öffnen ändert den `StateFingerprint` nicht | `ui-meldungen` Teil 2 |
