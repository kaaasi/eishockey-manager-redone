# Plan-Review PR #1073 – Spur `hok-patch-tabellen` (Runde 27, Nachtrag)

Prüfer: Claude Sonnet 5.5 (frische Sitzung, nicht am PR beteiligt) · Base `77719af` · Head `e230199c97af1ff66396d9f841fc9aeba2c65c76`
Checkout: `git rev-parse HEAD` = Head, `git status --porcelain` leer. Nur Doku (+37 Zeilen); kein Build, kein `tools/pruefen.sh`
(Auftrag). Unbeaufsichtigter Lauf.

## 1. Merge-Empfehlung

**ja** (Plan-PR, nur Doku). Der Plan setzt H1 = D vollständig um, die Teile sind ausführbar und in sinnvoller Folge. Die Befunde
P2-1/P2-2 sollten beim Spurstart (im Spur-Kurzaufruf) beachtet oder im Abschnitt nachgezogen werden; sie blockieren den Merge nicht.

## 2. Befunde

**P2-1 – Vorlage FILE75 passt nicht zum Zielstand.** Der Abschnitt (Teil 3, `docs/arbeitspakete.md` Zeile 189 im Head) nennt „Vorlage
FILE75 oder ein Stand mit zwei Bundesliga-Managern“. FILE75 ist laut Referenz-README der Start eines neuen Spiels am 10.09.1993
(`original/spielstaende/README.md` Zeile 114: „Sonntag 10.09.1993, … im ersten Hauptmenü gesichert“): keine Ergebnisse, keine
Abschlusstabelle, kein Meister, keine Aufstiegsrunde. Beleg: `grep -n "FILE75" original/spielstaende/README.md`. Die Aufstiegsrunde
(`CFFC`, `CED8`/`CDB8` ab +0x48) und die Sonderfall-Voraussetzungen (kein Meister/Europapokalsieger, `Status & 0x28 = 0`,
`docs/reverse-engineering.md` Zeile 946) hängen aber an einer abgelaufenen Saison. Teil 2 listet nur Tabelle und Aufstiegsrunde;
Ergebnisse `A0CB` (1584 Byte), Spieltagszähler `07A9`, Playoff-/Meisterfelder (`D0E6`) fehlen als zu patchende oder zu prüfende
Blöcke. Vorschlag: Teil 1 legt die Vorlage fest (bevorzugt ein vorhandener Stand um den 13.–29. April mit belegter Aufstiegsrunde,
z. B. FILE44-Reihe, auf zwei Bundesliga-Manager geprüft) und führt `07A9`, `A0CB`, Meister/Status als Pflichtblöcke der Konsistenz.

**P2-2 – Teil 1 ändert die Basis der Funktionskarte von `aufraeumen-doku`.** Teil 1 liegt dateimäßig disjunkt (neue Dateien), aber
`docs/analyse/hok-tabellen.md` enthält em.exe-Adressen; `funktionskarte.py` (`scan_docs`) zählt jede Doku außer `inventur.md` und
`re-abdeckung.md` als Fundstelle (`tools/reverse-engineering/README.md` Zeile 31; Plan-Absatz `arbeitspakete.md` Zeile 42). Die
Abnahme von `aufraeumen-doku` verlangt „`funktionskarte.py <em.exe> --csv` vor/nach, Statusverteilung gleich“. Läuft Teil 1 zwischen
„vor“ und „nach“ dieser Spur ein, ändert sich die Verteilung ohne Fehler der Spur. Vorschlag: Reihenfolge festlegen (Teil 1 erst
nach Merge von `aufraeumen-doku`, oder „vor“-Lauf der Doku-Spur nach Merge von Teil 1 neu) und Teil 1 beim Merge `--csv` vor/nach
vermerken lassen.

**P3-1 – Falsche Anfangsadresse.** Plan Zeile 173: „Datei 0x55F22–0x55F87“; `docs/reverse-engineering.md` Zeile 946 nennt
0x55F17–0x55F87 (Bit 0x04, Konto < −2.000.000 Drittel). Beleg: `grep -rn "55F22" docs` trifft nur den Plan. 

**P3-2 – Kein früher Beleg gegen em.exe.** Teil 1 ist Nachbildung aus den Bytes (kein Lauf des Originals); ob em.exe die Tabellen
beim Laden neu rechnet, zeigt erst die Pflichtprobe nach Teil 2/3. Falsche Annahme aus Teil 1 würde erst nach dem Patcherbau
auffallen. Vorschlag: nach Teil 1 eine Mini-Probe des Nutzers (ein von Hand gepatchter Stand, nur Tabellenfelder) vor Teil 2.

**P3-3 – Teil 2/3 hängen an einer ungestarteten Spur.** `aufraeumen-doku` ist Welle 5 („zuletzt“, Status „nicht gestartet“, Zeile 164).
Für den Code des Patchers (Formats/Tools/Tests) besteht keine Überschneidung außer Pfadkommentaren; nur die Doku-Dateien
(`spielstand-original.md`, `dosbox-aufnahme.md`) kollidieren. Plan könnte Teil 2 (Code) früher erlauben und nur die Doku-Einträge
nachziehen; sonst blockiert Welle 5 die Aufnahme `runde-22` Sitzung 5. Zudem verlangt Teil 2 neue `[ORIGINAL]`/`[APPROX]`-Marker,
`markerliste.py --write` ist aber nur in `aufraeumen-doku` erlaubt (A6 = A): Plan sagt nicht, wie die Markerliste nachzieht.

**P3-4 – Spur fehlt in Folge und Spurtabelle** (`arbeitspakete.md` Zeilen 54–70); Modell steht nur im Spurabschnitt. Formal.

## 3. Geprüft

| Punkt | Ergebnis |
|---|---|
| H1 = D gegen PR-Text | Ligatabelle (C) plus Gruppentabelle (D) im Ziel, Teil 1–3; A/B/E nicht vermischt – stimmt |
| Teile in sinnvoller Folge | Beleg → Patcher (nur belegte Felder, Widersprüche abgewiesen) → Stand/Anleitung; ja |
| Blöcke gegen `docs/formate/spielstand-original.md` | `8257`, `5CCB`, `A0CB`, `CD88`, `CD58`, `CD28`, `CCF8`, `CED8`, `CDB8`, `CFFC`, Offsets 0x825A/0x8288/0x828B/0x30C36/0x55F93 stimmen (Zeilen 107–117, 137, 147, 162, 1030–1036); `07A9` fehlt (P2-1) |
| Dateigrenzen zu `aufraeumen-doku` | Teil 1 nur neue Dateien: disjunkt, aber siehe P2-2; Teil 2/3 nach Merge: ausreichend, siehe P3-3 |
| `hok patch` Bestand | `konto`, `datum`, `zaehler`, `spieltag` vorhanden (`OriginalSaveGamePatcher.cs`, `HokPatch.cs`); Tabellenfelder fehlen wie behauptet |
| Abnahme | `--schnell`, Rundreise, Prüfsummen, Rest bytegleich, `OriginalImport`, Pflichtprobe des Nutzers: prüfbar |

Akzeptierte `[APPROX]`: keine. Annahmen (nicht geprüft): Inhalt von em.exe zu den Tabellenblöcken, Verfügbarkeit
eines Aprilstandes mit zwei Bundesliga-Managern im Referenz-Repo, Ablauf der Wellen.
