# Review PR #849: Abschluss Runde 18 (Archiv, Kopf Runde 19, Doku-Nachzug)

Geprüft: Base `c069d4e4103704709192ac303ab23ecd0fdb2a7a`, Head `30544897e0dd41d15a559424da09c6d1cafcc04a` (ein Commit, 5 Dateien, nur `docs/**`).
Reviewer: Claude (frische Sitzung, geplanter Lauf, keine Mitarbeit am PR). Ablage nach `docs/vorlagen/review-anfrage.md`.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Nur Doku; die drei Schwerpunkte stimmen mit Base-Stand, gemergten PRs, Issues und Code überein.
Kein Build, keine Tests, kein `tools/pruefen.sh` (reine Doku, laut Kurzaufruf entfallen) – **kein Testnachweis nach `zusammenarbeit.md` §6 Nr. 4**; die Empfehlung beruht auf Doku-Abgleich und Codelesen.

## 2. Befunde

Keine Befunde P0–P2. Zwei Hinweise P3 (Verdacht, kein Beleg für einen Fehler, nicht blockierend):

- **P3-1 (Verdacht):** `inventur.md` A318/A319/A320 behalten Spalten „Stelle“ (`InboxScreen.cs:7`, `:11`, `NewsBar.cs:21`) und
  „Remake“-Text („Kachel „Neuer“ öffnet Posteingang“) unverändert; nur der Stand-Vermerk ist angehängt. Das ist laut Kopfzeile der
  Inventur („Stelle/Karte bleiben auf 2d0b081“) so vorgesehen und der Vermerk ist eindeutig – aber der Zeilentext allein liest sich
  weiterhin falsch. Kein Handlungsbedarf, falls die Inventur-Konvention (Vermerk statt Umschreiben) gilt.
- **P3-2 (Verdacht):** Die Doku nennt als Fundstelle „Datei 0x45755 → 0x28165“; im Code (`SetupScreen.cs:~503`, `GameState.AddManager.cs:9`)
  steht „Datei 0x4574B–0x4576D (Aufruf 0x28165, Argument 0)“. 0x45755 liegt in diesem Bereich, ist also kein Widerspruch, aber im
  Code nicht wörtlich zu finden (`git grep 0x45755 -- src` leer). Belegbarkeit der Einzeladresse nicht neu disassembliert.

## 3. Besonders zu prüfende Punkte

**(1) Archiv unverändert.** Probe (Python, Base `git show c069d4e:docs/arbeitspakete.md`, Abschnitt von `## Aktuelle Runde: Runde 18`
bis vor `### Zurückgestellt`, 13 782 Zeichen): ist **wörtlich Teilstring** von `docs/archiv/runde-18.md` auf dem Head
(`True`). Davor stehen nur Titel, Kopfabsatz, „Ergebnis“, „Abschluss“, „Entscheidungen während der Runde“ und `---`; danach nichts.
„Zurückgestellt“ und „Vormerkungen“ sind nicht im Archiv, sondern bleiben in `arbeitspakete.md` (nachgezogen auf „nach Runde 18“).
Die Statuszeilen der Spuren zeigen bewusst den Stand vor dem Merge (im Kopf des Archivs gesagt). Die alte Überschrift
„Aktuelle Runde: Runde 18 …“ bleibt im Archiv als Abschnittsüberschrift stehen – wörtlich wie verlangt.
`git diff --stat c069d4e..30544897`: genau die fünf genannten Dateien, 196 +/162 −.

**(2) Ergebnis/Abschluss/Entscheidungen gegen GitHub.**
- Gemergt (Liste der geschlossenen PRs): #811, #812, #813, #814, #830, #833, #842; Reviews #815, #816, #822, #827, #831, #843, #844,
  #846, Gesamtreview #848 – alle `merged: true`, #848 zuletzt (07:47 UTC), Base von #849 = Merge von #848.
- Gesamtreview-Aussage im Archiv („Ja für `f6679b8`, keine neuen Befunde, Testlücke Speichern/Laden in angehaltener Live-Paarung“)
  stimmt mit `docs/reviews/2026-11-14-runde-18-gesamt.md` (§1, §2) überein.
- Als behoben genannte Issues geschlossen: #773, #804, #803, #805, #823, #832, #817–#821, #824, #825, #828, #829, #834–#841, #845 –
  alle `closed`. (R758-01 und #730 Spieltag 1 nur aus den Reviews #827/#848 übernommen, nicht einzeln nachgeprüft.)
- Als offen genannte Punkte: offene Issues sind genau **#730 (P2), #826 (P3), #847 (P2)** – deckt sich mit Abschluss und
  „Vormerkungen“; #838 ist geschlossen, das Archiv führt nur den `[APPROX]`-Restwert (⅓ s je Minute), konsistent mit dem Gesamtreview.
  A093/A053 stehen im Gesamtreview als erwartet offen; Torszenen Runde 19 steht in beiden Dateien.
- Entscheidungen R18-E1…E5 (A5 im Kern, Spur `nutzer-befunde`, Kachel „Neuer“, Optionsfenster, Platztausch Runde 19) stimmen mit den
  Spurabschnitten (A5/#814, #842 Teil 3, #836/#841, #847) überein; R18-E5 → #847 ✔.

**(3) Doku-Nachzug gegen Code auf der Base.**
- `bildschirme.md` Z. 67/100: `MenuTiles.cs:27` hat die Kachel „Neuer“; `SetupScreen.cs:509` `[MenuEntry("Neuer")]` → `new SetupScreen(host, host.Game!, back)`
  (Spielvorbereitung, Hinzufügen-Modus); Posteingang nur `NewsBar.cs:72` (`host.Show(new InboxScreen(…))`, Klick auf die Nachricht).
  `TileLocks.cs:26` sperrt „Neuer“ bei `MaxManagers` – Text „weiterer Manager“ passt. ✔
- `inventur.md` A318–A320: `InboxScreen.cs`-Remarks und `NewsBar.QuestionHint` („(Klicken)“) bestätigen den Vermerk. ✔ (P3-1/P3-2 oben)
- `re-livespiel.md` §4 Punkt 5: `LiveMatchScreen.SpectatorLine` (Z. ~361–373) nutzt `ClubStatisticsScreen.Number(…, true)`
  (Tausenderpunkt, `Number` setzt `.` nach 3/7/11 Ziffern), Doc-Kommentar nennt `folge01_00615` „4.335“ und die Korrektur der
  Vermutung „ohne Tausenderpunkt“; Farbe 0x10 bei ausverkauft, sonst 0, „(ausw.)“ bei WM/ohne Heimspiel, zentriert 48–82 – alles wie im
  Doku-Text. Videobild selbst nicht eingesehen (Referenz-Repo nicht eingebunden). ✔

**Erwartet offen (nur bestätigt):** #730 Spieltag 2/3, #826, #847, Torszenen (Runde 19) – in `arbeitspakete.md` und Archiv als offen geführt,
Issues #730/#826/#847 offen. Keine neuen Befunde daraus.

## 4. Ausgeführte Prüfungen und Annahmen

- `git fetch`, Checkout Head: `git rev-parse FETCH_HEAD` = angegebener Head, `git status --porcelain` leer (0 Zeilen).
- `git diff --stat c069d4e..HEAD`; Python-Teilstring-Probe Archiv gegen Base (s. o.); `git grep` in `src/` für `InboxScreen`,
  `"Neuer"`, `SpectatorLine`, `0x45755`; Lesen von `InboxScreen.cs`, `NewsBar.cs`, `SetupScreen.cs`, `LiveMatchScreen.cs`.
- GitHub (REST/MCP): PR #849, Dateiliste, geschlossene PRs, offene und geschlossene Issues.
- **Nicht ausgeführt:** Build/Tests/`tools/pruefen.sh`; Gegenbeispiele (keine genannt); Originaldaten, Videos, DOSBox; Disassembly.
- **Akzeptierte `[APPROX]`:** keine neuen; `[APPROX]` zu #838 (⅓ s je Minute) und Paarungen/Zuschauer-Vorbereitung bleiben wie im Gesamtreview #848.
- **Annahmen (nicht geprüft):** Inhalte der Einzelreviews (#815…#846) über die genannten Merges hinaus; „R758-01 erledigt“ und
  „#730 Spieltag 1 behoben“ nach Aussage der Reviews.
