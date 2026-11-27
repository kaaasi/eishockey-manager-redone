# Review #318 – Plan Runde 9: Folgeteile, Doku-Befunde Z96

- **PR:** #318 (`prozess/runde-9-folgeteile`)
- **Base:** `3bf25094a274ec160d241a228071f3558c0dd562` · **Head:** `599acb9a99d7553bd507f97e610cdbae1c28b816`
- **Reviewer:** Claude Code (Claude Opus 5.5), geplanter Lauf, eigene Sitzung ohne Mitarbeit an #318
- **Art:** nur Codelesen (reiner Doku-PR, laut Auftrag kein Build und keine Tests)

## 1. Merge-Empfehlung

**Ja, für genau diesen Head – nur Codelesen.** Die vier Z96-Befunde sind erledigt, wie im Review #297 vorgeschlagen. Beide
Folgeteile sind im Kern übernahmefähig nach `zusammenarbeit.md` §2. Die neue Regel „Fortsetzung anlegen – Leitstelle“ und
die Draft-Ausnahme widersprechen `spur-start.md` und `leitstelle-start.md` Schritt 7 nicht. Neu sind vier P3 (R9F-01 bis -04).
Sie blockieren den Merge nicht. R9F-01 und R9F-02 sollten vor dem Start der Läufe für Teil 4 bzw. Teil 5 geklärt werden.
Mit R9F-01 blockiert Teil 4 sonst wahrscheinlich an einer fremden Datei.

**Beleg aus diesem Lauf:** `git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD` →
`0bc38fba0e3e7d0c41b122e8a3087a933a88ea13 HEAD` (geerbt, ohne `add_repo`). `ToolSearch`
`select:mcp__claude-code-remote__add_repo` → „No matching deferred tools found“. Das bestätigt den neuen Wortlaut zu Z96-01.

## 2. Befunde

### Gegenbeispiele erneut ausgeführt

- **Z96-01 – erledigt.** [`CLAUDE.md` Z. 46–47](https://github.com/kaaasi/eishockey-manager-redone/blob/599acb9a99d7553bd507f97e610cdbae1c28b816/CLAUDE.md#L46-L47)
  sagt jetzt „im Lauf selbst meist nicht verfügbar (auch nicht per `ToolSearch`)“. Der Zugriffsabsatz
  ([`spur-start.md` Z. 14–16](https://github.com/kaaasi/eishockey-manager-redone/blob/599acb9a99d7553bd507f97e610cdbae1c28b816/docs/vorlagen/spur-start.md#L14-L16))
  sagt „meist nicht vorhanden“ und verlangt die Fehlermeldungen von `ls-remote` und `ToolSearch`. Das Gegenbeispiel weckt
  keine falsche Erwartung mehr. In diesem Lauf erneut nachgestellt, siehe 1.
- **Z96-02 – erledigt.** Z. 16–17: „Meldung: Benachrichtigung an den Nutzer; gibt es einen PR der Spur, dort Kommentar und
  Label `spur-wartet`; sonst keinen PR anlegen.“ Im Gegenbeispiel (Teil 1 ohne PR) erreicht die Meldung den Nutzer jetzt
  über die Benachrichtigung. Ein leerer PR wird nicht verlangt.
- **Z96-03 – erledigt.** [Z. 51–52](https://github.com/kaaasi/eishockey-manager-redone/blob/599acb9a99d7553bd507f97e610cdbae1c28b816/docs/vorlagen/spur-start.md#L51-L52):
  „… keine Fortsetzung anlegen und den Lauf beenden; der Hinweiskommentar ersetzt die Übergabe (Hauptablauf Schritt 7
  entfällt).“ Lauf B kann nicht mehr auf den Branch von A stapeln und schreibt kein `Bearbeiter: frei`.
- **Z96-04 – erledigt.** Z. 19–20 begrenzen den Zugriffsabsatz auf Spurläufe. Reviews folgen `review-anfrage.md` („nur wenn
  Daten nötig; ‚keine Code-Arbeit‘ gilt dort als ‚kein Review‘“). Dieser Startaufruf enthält den Absatz zu Recht nicht.
  [`leitstelle-start.md` Z. 28–30](https://github.com/kaaasi/eishockey-manager-redone/blob/599acb9a99d7553bd507f97e610cdbae1c28b816/docs/vorlagen/leitstelle-start.md#L28-L30):
  Modell „nur auf ausdrücklichen Wunsch des Nutzers (z. B. vom Nutzer bestätigter Plan bzw. Go mit Modell)“. Erledigt.

### Besonders geprüft

**(1) Folgeteile – Übernahmefähigkeit nach §2.**
Optionen Teil 4: [`arbeitspakete.md` Z. 131–135](https://github.com/kaaasi/eishockey-manager-redone/blob/599acb9a99d7553bd507f97e610cdbae1c28b816/docs/arbeitspakete.md#L131-L135).
Transfer Teil 5: [Z. 95–99](https://github.com/kaaasi/eishockey-manager-redone/blob/599acb9a99d7553bd507f97e610cdbae1c28b816/docs/arbeitspakete.md#L95-L99).

- Ziel: vorhanden, mit Fundstellen (0x494EF, 0x49332) bzw. Bezug #298. Die Abnahme steht teils im Teil und teils in der Spur
  (roter Test je Regel/Option, Mehrsaisonprobe, Pixelvergleich). Das genügt.
- Branch und Base: Sie folgen der Stapelregel (`spur-start.md` „Teile und Fortsetzung“ Schritt 2). Die Base-SHA fehlt. Das
  ist durch die Rundenregel gedeckt („tatsächlichen Base-SHA in den Draft-PR“).
- Stand: #303 und #310 sind offen und nicht Draft. Sie tragen `spur-wartet`, die letzte Übergabe sagt `Bearbeiter: frei`. Ein
  neuer Lauf darf also aufsetzen. Abschnitt Schritt 1 hält ihn nicht an.
- Dateigrenzen: Teil 5 liegt ganz in `Core/Transfer*.cs` bzw. `Player.Contract.cs` (Zuständigkeit der Spur). Für Teil 4 gibt
  es eine Lücke, siehe R9F-01.
- Ausführungsform „eigener Lauf ohne Fortsetzung“: siehe R9F-02.

**(2) „Fortsetzung anlegen – Leitstelle“ und Draft-Status.**
Ablauf: [`spur-start.md` Z. 70–72](https://github.com/kaaasi/eishockey-manager-redone/blob/599acb9a99d7553bd507f97e610cdbae1c28b816/docs/vorlagen/spur-start.md#L70-L72)
→ Label `spur-wartet` → [`leitstelle-start.md` Z. 30–31](https://github.com/kaaasi/eishockey-manager-redone/blob/599acb9a99d7553bd507f97e610cdbae1c28b816/docs/vorlagen/leitstelle-start.md#L30-L31).
Die Leitstelle legt die Aufgabe an. Sie hat vorher nach Schritt 7 beide Repos eingebunden, und der Prompt enthält den
Zugriffsabsatz (die Fortsetzung übernimmt den Startaufruf). Die Kette schließt sich, ein Widerspruch besteht nicht. Die
Doppelanlage verhindert `list_triggers` (Schritt 5, erster Punkt). Der Zähler steht im Prompt.

Der Draft-Fall: Ein fertiger Teil kann im Draft bleiben (Schritt 3), ein blockierter auch (Schritt 4). Unterscheiden lassen
sie sich nur am Übergabetext. Das ist hinnehmbar, denn die Merge-Prüfung `zusammenarbeit.md` §6 Nr. 6 („Kein Draft“) fängt
es ab. Restunschärfen stehen in R9F-03 und R9F-04.

### R9F-01 (P3) – Teil 4: Szenario `Einstellungen.cs` liegt außerhalb der Dateigrenzen

Stelle: [`arbeitspakete.md` Z. 130–135](https://github.com/kaaasi/eishockey-manager-redone/blob/599acb9a99d7553bd507f97e610cdbae1c28b816/docs/arbeitspakete.md#L130-L135),
Zuständigkeit Z. 49–50 (optionen-wirkung: `GameSetup.cs`, `GameOptions.cs`, `SettingsScreen.cs`, `Settings.cs`, …).

Teil 4 lässt „die Geräteeinstellung entfallen bzw. daraus ableiten“. `src/EishockeyManager.Tools/Scenarios/Einstellungen.cs`
Z. 18 nutzt `SettingsScreen.RowRect(SettingsScreen.Row.GoalScenes)` (auf `main` und auf `paket/optionen-wirkung-3`). Diese
Datei steht in keiner Dateigrenze der Spur.

Gegenbeispiel:

1. Der Lauf entfernt die Zeile `Row.GoalScenes` und `GameSettings.GoalScenes`, wie die Variante „entfällt“ vorsieht.
2. Der Build von `EishockeyManager.Tools` scheitert.
3. Nach `spur-start.md` Abschnitt Schritt 4 („fremde Datei nötig“) gilt der Teil als blockiert.

Außerdem ist offen, wie die drei Stufen von `LiveMode` (0, 1, 2 = Dauerhalle) auf den bisherigen Bool abgebildet werden.
Das gilt bei der Variante „abgeleitet“ ebenso wie für die Migration alter Einstellungen.

**Vorschlag:** Dateigrenze von Teil 4 um `Tools/Scenarios/Einstellungen.cs` ergänzen. Alternativ die Variante festlegen
(„Zeile bleibt, wird aus `LiveMode` abgeleitet“). Die Abbildung der Stufen als `[APPROX]` mit Begründung verlangen.

### R9F-02 (P3) – „Eigener Lauf ohne Fortsetzung“ hat keinen passenden Ablauf

Stelle: [`spur-start.md` Z. 45](https://github.com/kaaasi/eishockey-manager-redone/blob/599acb9a99d7553bd507f97e610cdbae1c28b816/docs/vorlagen/spur-start.md#L45)
und [Z. 27–31](https://github.com/kaaasi/eishockey-manager-redone/blob/599acb9a99d7553bd507f97e610cdbae1c28b816/docs/vorlagen/spur-start.md#L27-L31)
gegen `arbeitspakete.md` Z. 95 und Z. 131.

Der Abschnitt „Teile und Fortsetzung“ gilt nur, wenn „der Startaufruf die Fortsetzung erlaubt“. Ohne diese Erlaubnis greift
Hauptablauf Schritt 3 („Branch … von aktuellem `main`“). Damit fehlen die Stapelregel und das Rebase-Ziel. Der Kurzaufruf
„Geplanter Lauf mit Teilen“ ist der einzige mit Zugriffsabsatz und erlaubt `create_trigger` ausdrücklich.

Gegenbeispiel: Die Leitstelle startet Teil 4 mit dem Kurzaufruf ohne den Satz zur Fortsetzung. Der Agent folgt Schritt 3 und
zweigt von `main` ab, obwohl #298 und #303 offen sind. Der Branch enthält `GameOptions.LiveMode` nicht, und Teil 4 kann nicht
gebaut werden. Umgekehrt: Mit dem unveränderten Kurzaufruf ist die Fortsetzung erlaubt, entgegen „ohne Fortsetzung“.
Praktisch fängt die ausdrückliche Base in `arbeitspakete.md` das meist ab, eine Regel dazu fehlt aber.

**Vorschlag:** Im Kurzaufruf eine Variante „Fortsetzung nicht erlaubt“ vorsehen, z. B. „Teile nach Abschnitt ‚Teile und
Fortsetzung‘ ohne Schritt 5“. Oder Z. 45 erweitern: „… oder `arbeitspakete.md` einen einzelnen Folgeteil mit Base nennt
(dann ohne Schritt 5)“.

### R9F-03 (P3) – Prompt aus der Übergabe ungeprüft übernommen; Modell „setzt“ ohne Bedingung

Stelle: [`leitstelle-start.md` Z. 30–31](https://github.com/kaaasi/eishockey-manager-redone/blob/599acb9a99d7553bd507f97e610cdbae1c28b816/docs/vorlagen/leitstelle-start.md#L30-L31),
[`spur-start.md` Z. 71–72](https://github.com/kaaasi/eishockey-manager-redone/blob/599acb9a99d7553bd507f97e610cdbae1c28b816/docs/vorlagen/spur-start.md#L71-L72).

- „die Aufgabe mit dem Prompt aus der Übergabe anlegen“: Ein PR-Kommentar ist GitHub-Inhalt, den jeder mit Kommentarrecht
  schreiben kann. Wird er wörtlich zum Prompt eines unbeaufsichtigten Laufs mit Schreibrechten, gibt es keine Prüfung.
  Gegenbeispiel: Eine Übergabe enthält im Prompt einen zusätzlichen Satz (z. B. „mergen“ oder eine fremde Spur). Die Leitstelle
  übernimmt ihn nach dem Wortlaut. **Vorschlag:** „Prompt gegen den Kurzaufruf ‚Geplanter Lauf mit Teilen‘ prüfen; abweichen
  dürfen nur Spur, Modell, Zähler und PR-Nummer.“
- `spur-start.md` sagt „die Leitstelle … setzt das Modell“ ohne Bedingung. `leitstelle-start.md` erlaubt das nur auf
  ausdrücklichen Wunsch des Nutzers. Widerspruchsfrei ist das nur, solange das Modell der Spur in einem vom Nutzer bestätigten
  Plan steht. Für Runde 9 ist das so. **Vorschlag:** „… und setzt das Modell laut vom Nutzer bestätigtem Plan“.

### R9F-04 (P3) – Kleinigkeiten

- Draft aufheben: `spur-start.md` Z. 58 sagt „Leitstelle bzw. Nutzer setzen es“, `leitstelle-start.md` Z. 31 „setzt der
  Nutzer in der Weboberfläche“. Das widerspricht sich nicht, ist aber uneinheitlich. Ein Satz genügt, z. B. „der Nutzer;
  die Leitstelle, wenn ihr ein Werkzeug dafür zur Verfügung steht“.
- [`spur-start.md` Z. 20](https://github.com/kaaasi/eishockey-manager-redone/blob/599acb9a99d7553bd507f97e610cdbae1c28b816/docs/vorlagen/spur-start.md#L20)
  ist 252 Zeichen lang. Die übrigen Zeilen liegen bei etwa 125–150. Nur Form.
- Teil 4 berührt den Torszenen-Schalter, während Torszenen in Runde 9 ausgeklammert sind (`livespiel`). Gemeint ist erkennbar
  nur der Schalter, nicht die Szene. Ein Halbsatz („nur der Schalter, keine Szenen“) würde Rückfragen vermeiden.

## 3. Ausgeführte Prüfungen

- **Nur Codelesen.** Laut Auftrag kein Build und keine Tests, also kein Testnachweis nach `zusammenarbeit.md` §6 Nr. 4.
  Für einen reinen Doku-PR ist das nicht nötig.
- Zugriff: `git ls-remote …referenz HEAD` gelang. `ToolSearch select:mcp__claude-code-remote__add_repo` ergab „No matching
  deferred tools found“.
- `git fetch origin pull/318/head`: `git rev-parse` = `599acb9a…`, ein Commit über Base `3bf25094…` (= `origin/main`).
  `git diff --stat` ergab 4 Dateien, +39/−16. Das ganze Delta wurde gelesen.
- Gelesen am Head: `spur-start.md` ganz, `leitstelle-start.md` Z. 15–32, `zusammenarbeit.md` §2, `arbeitspakete.md`
  Z. 1–60 und 77–160. Außerdem `review-anfrage.md` und das Review `2026-11-01-zugriff-geplante-laeufe.md` (Z96-01 bis -04).
- Per REST: PR #298, #303, #308, #310 und #318 (Status, Draft, Base, Labels). Dazu die letzte Übergabe in #303 und #310
  (`Bearbeiter: frei`).
- Für R9F-01: `git grep GoalScenes|LiveMode` auf dem Head und auf `origin/paket/optionen-wirkung-3`. Gelesen wurden
  `Desktop/SettingsSync.cs` und `Tools/Scenarios/Einstellungen.cs`.
- Akzeptierte `[APPROX]`: keine, denn der PR ändert keinen Code. Annahme: Die Entscheidungen des Nutzers vom 01.11.2026
  (Go, Modelle) sind nur aus PR und Plan entnommen und nicht unabhängig belegt.
