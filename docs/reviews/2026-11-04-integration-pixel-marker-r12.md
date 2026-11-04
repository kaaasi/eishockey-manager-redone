# Unabhängiges Review: Integration pixel-marker, Runde 12 (PR #545)

Base `abdc378f96cb15d19956cb39cfdd8ecc521bfc94` · Head `f1e6dd398a97d0356061b74b20cad737a30b161d` · Reviewer: Claude Code
(geplanter Lauf, Claude Opus 5.5), eigene Sitzung ohne Mitarbeit an #542/#543/#544/#545.

## 1. Merge-Empfehlung

**Nein für `f1e6dd3`.** R-K6 (Nachrichtenspeicher), D8 (#537) sowie Schema 35 → 36 mit Migration sind in Ordnung. Am echten
Schema-35-Stand vom Base geprüft, alle drei Läufe von `tools/pruefen.sh` am exakten Head bestanden. Drei P2-Befunde
verhindern den Merge:

1. Die Bannerdauer aus R-K3 ist um den Faktor 3 zu lang. 0x130EE teilt durch 3, also sind es 20 Takte und nicht 60.
   Die Videobilder bestätigen das.
2. Teil 2 hat die Inventurzeilen der übrigen Spuren nicht übertragen.
3. R-K5 ist zu `[ORIGINAL]` hochgestuft, obwohl das Video innerhalb der Seiten eine andere Reihenfolge zeigt.

Alle drei lassen sich mit kleinen Änderungen beheben (eine Konstante mit Test, Doku, ein Marker). Danach ist eine
Nachprüfung nötig.

## 2. Befunde

- **P2 – R12PM-01: Banner steht ≈ 3,3 s statt ≈ 1,1 s (R-K3, falsches `[ORIGINAL]`).**
  [`MatchDayScreen.cs:18`](https://github.com/kaaasi/eishockey-manager-redone/blob/f1e6dd398a97d0356061b74b20cad737a30b161d/src/EishockeyManager.UI/Screens/MatchDayScreen.cs#L18) setzt `ShortMessage.FramesFor(180)`,
  das sind 60 Takte bzw. 198 Bilder. Der Test [`PixelMarkerTests.cs:78`](https://github.com/kaaasi/eishockey-manager-redone/blob/f1e6dd398a97d0356061b74b20cad737a30b161d/tests/EishockeyManager.UI.Tests/PixelMarkerTests.cs#L78)
  schreibt 198 fest.
  - Das Banner (Datei 0x35E1D–0x35E22) übergibt `0x3C` = 60 an `0x50:0x19E`. Die Wartefunktion (Datei 0x130EE) teilt ihr
    Argument zuerst durch 3 (Datei 0x130F1–0x130FE: `push 0:3`, `call 0:0x47C` = Laufzeit-Division) und wartet dann
    auf die Takte. Dieselbe Funktion ist in [`re-meldungen.md:142`](https://github.com/kaaasi/eishockey-manager-redone/blob/f1e6dd398a97d0356061b74b20cad737a30b161d/docs/analyse/re-meldungen.md#L142) und
    `ShortMessage.FramesFor(mode)` als „m / 3 Takte“ abgebildet. Richtig ist also `FramesFor(60)`, das sind 20 Takte,
    ≈ 1,1 s bzw. 66 Bilder.
  - Fehlerquelle ist [`re-rest.md:185`](https://github.com/kaaasi/eishockey-manager-redone/blob/f1e6dd398a97d0356061b74b20cad737a30b161d/docs/analyse/re-rest.md#L185) („60 Takte warten (0x130EE, ≈ 3,3 s)“).
  - Video (`bundestrainer`, 1 Bild/s): Die Banner „Freundschaftsspiel“ 00120, 00157 und 00246 stehen auf genau einem
    Bild, „Weltmeisterschaft“ 00612–00613 auf zwei. Gemessen mit dem Mittelwert der Differenz im Bannerfeld: Nachbarbilder
    47–54, Bannerbild 0. Bei 3,3 s wären es mindestens 3 Bilder. Der alte Wert von 75 Bildern (1,25 s) lag näher.
  - Gegenbeispiel: `Assert.InRange(MatchDayScreen.BannerFrames, 60, 72)` ist am Head rot (198).
  - Korrektur: `FramesFor(60)` mit Test auf 66 Bilder. Dazu `re-rest.md` §E und Inventur F077 berichtigen.
- **P2 – R12PM-02: Inventur nicht „Stand je Zeile“ auf den Rundenstand (Teil 2).**
  [`arbeitspakete.md:41`](https://github.com/kaaasi/eishockey-manager-redone/blob/f1e6dd398a97d0356061b74b20cad737a30b161d/docs/arbeitspakete.md#L41) legt `inventur.md` allein in die Hand von `pixel-marker`. Die
  übrigen Spuren haben ihre Zeilen im PR genannt:
  - #481: A193
  - #482: A070, A089
  - #483: F122
  - #491: A184, A369
  - #492: A341, F135, A305, A310, A327, F121, F040, F108
  - #495: A174
  - #503: A013
  - #511/#527: A016, F106

  Am Head ist keine dieser Zeilen nachgezogen. Beispiele:
  - [A184](https://github.com/kaaasi/eishockey-manager-redone/blob/f1e6dd398a97d0356061b74b20cad737a30b161d/docs/analyse/inventur.md#L461) und [A174](https://github.com/kaaasi/eishockey-manager-redone/blob/f1e6dd398a97d0356061b74b20cad737a30b161d/docs/analyse/inventur.md#L451) stehen noch auf „Stand Runde
    11: offen, Entscheidung … Runde 12“, obwohl die Integrationen #508/#526 sie umgesetzt haben.
  - F122 steht noch auf „genähert“ (#483: jetzt `[ORIGINAL]`).
  - A013 und A341/F135 sind ohne Stand.

  Auch der Hinweis zum Stand ([`inventur.md:257`](https://github.com/kaaasi/eishockey-manager-redone/blob/f1e6dd398a97d0356061b74b20cad737a30b161d/docs/analyse/inventur.md#L257)) nennt nur Runde 11. Nachgezogen sind
  nur die eigenen Zeilen (R-K2…R-K6, M-K10). Die Integrationen #505/#519/#535 betreffen außer den genannten Zeilen
  keine weiteren. Korrektur: Die genannten Zeilen in derselben Form wie in Runde 11 übertragen (fett „Stand Runde 12“
  mit PR-Nummer) und den Hinweis ergänzen.
- **P2 – R12PM-03: R-K5 – Reihenfolge innerhalb der Seite als `[ORIGINAL]` markiert, Video widerspricht.**
  [`SetupScreen.cs:289`](https://github.com/kaaasi/eishockey-manager-redone/blob/f1e6dd398a97d0356061b74b20cad737a30b161d/src/EishockeyManager.UI/Screens/SetupScreen.cs#L289) behauptet „Satzreihenfolge – das ist die
  Reihenfolge der Datenbank (Vereinsindex)“. `ClubAt` ist gegenüber dem Base unverändert, nur der Marker ist von
  `[APPROX]` („im Original nicht nach Index sortiert“) auf `[ORIGINAL]` gewechselt. A347 und F058 stehen auf „erledigt“.
  - Ich habe jedes 38 × 38-Feld der Videobilder `oberflaeche_0128` (Seite 0) und `_0133` (Seite 1) gegen alle 48 Felder
    des Heads abgeglichen. Ergebnis: dieselben 12 Vereine je Seite, aber in anderer Reihenfolge. Seite 0 zeigt die
    Remake-Plätze 5, 3, 1, 4, 9, 0, 8, 6, 2, 7, 10, 11. Seite 3 (`_0136`) ist identisch.
  - Belegt sind also die Zuordnung von 12 Sätzen je Seite (0x28030) und das zyklische Blättern (0x28E4B). Die
    Gleichsetzung „Satzreihenfolge = Vereinsindex der Datei“ ist nicht belegt. Möglicherweise hängt sie mit der
    Satzumordnung S-K6/A194 zusammen.
  - Korrektur: Für die Reihenfolge innerhalb der Seite wieder `[APPROX]` mit Verweis auf die Videobilder. A347 bleibt
    offen.
- **P3 – R12PM-04: A309 „erledigt (Pixel-Marker Teil 1)“, obwohl Teil 1 die Eistafeln nicht berührt.**
  [`inventur.md:601`](https://github.com/kaaasi/eishockey-manager-redone/blob/f1e6dd398a97d0356061b74b20cad737a30b161d/docs/analyse/inventur.md#L601): Geändert wurde nur das Trikot (Bit 1). Die Eistafel-Stelle
  trägt weiter `[APPROX]` („Im Video ist die Eisfläche nicht belegt“, `AdvertisingScreen.cs` `DrawContracts`). Laut
  `re-rest.md` §D stimmte sie schon. Den Status entweder mit Begründung „stimmte bereits (re-rest §D)“ führen oder
  offen lassen.
- Hinweis ohne Priorität: `NewsBar.Shown` ([`NewsBar.cs:44`](https://github.com/kaaasi/eishockey-manager-redone/blob/f1e6dd398a97d0356061b74b20cad737a30b161d/src/EishockeyManager.UI/Screens/Manager/NewsBar.cs#L44))
  setzt den Zeiger nur zurück, wenn sich die Anzahl ändert. Fällt an einem Tag eine Meldung weg und kommt eine neue hinzu,
  zeigt der gespeicherte Index auf eine andere Nachricht als zuvor. Das ist nur Anzeige und ändert keinen Zustand.

**Erwartet offen, bestätigt:**

- Farben 18/12/22: am Video `oberflaeche_1542` teilweise nachgemessen.
  - Grün (80, 94, 16) passt zu 12 (81, 97, 16), nicht zu 13 (97, 113, 32).
  - Rot erreicht 223–232 und liegt damit über 238 (211, 0, 0), stützt also 18 (243, 0, 0).
  - Blau ist unscharf (≤ 164) und nicht entscheidbar.
- Sortierschlüssel-Vorrang 0 bleibt `[APPROX]`.

## 3. Ausgeführte Prüfungen

Klon `git rev-parse HEAD` = `f1e6dd3…`, `git status --porcelain` leer, SDK 10.0.402. Base-Stand ebenfalls als Worktree
`abdc378`. Proben lagen in temporären, nicht committeten Dateien.

`tools/pruefen.sh` (alle drei Läufe) am Head:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2159 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1818 | 0 | 305 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1819 | 340 | 0 |

Ergebnis: bestanden.

**Gegenbeispiele (rot am Base, grün am Head):**

- **R-K6:** eigene Probe mit öffentlicher API, an beiden Ständen gleich.
  - 20 + 5 Meldungen auf zwei Manager ergeben 24 Plätze. Am Base sind es 25.
  - Eine Meldung ist nach 3 Tagen weg. Am Base bleibt sie.
  - Der Tageslauf sortiert (Tag 50/10 → 10, 50). Am Base wird nicht sortiert.
  - Am Head außerdem `MessageStoreTests` (8 Tests) grün. Löschen je Spieler (4B270h) gibt es am Base nicht; am Head
    deckt es `PlayerMessage_StaysUntilRemoved` ab.
- **D8/#537:** `InboxOpenTests` (beide Tests) unverändert auf den Base kopiert. Beide sind rot, weil `Enter` „gelesen“
  setzt. Am Head grün.
- **R-K2…R-K5:** `PixelMarkerTests` auf den Base kopiert (nur `NameOffset` → 8): alle 4 rot, am Head grün.
- **Abgleich mit den Referenzbildern:**
  - R-K2: Zeichen bei x = 20/120/220, Name ab x ≈ 28 (`oberflaeche_1542`) – passt zum Head.
  - R-K3: Text der Banner in den Zeilen 111–130 wie im Video (`bundestrainer_00120`; Base 110–129), Schatten ohne
    seitlichen Versatz – passt zum Head.
  - R-K4: Template-Abgleich `oberflaeche_0650` gegen kb\11. Beste Lage ist das Head-Feld (70 | 130) bei (30 | 120) mit
    mittlerem Fehler 29,8; das Base-Feld liegt am besten bei (31 | 116) mit 40,7.
  - R-K5: siehe R12PM-03.
- **Funktionskarte:** `python3 tools/reverse-engineering/funktionskarte.py gamedata/em.exe --repo . --write` am Head
  zweimal ausgeführt, beide Male ohne Diff. Am Base ändert derselbe Lauf `docs/re-abdeckung.md` (14+/16−).

**Schema 35 → 36 mit echtem Base-Stand:** Mit dem Base-Code (Schema 35) eine Synthetik-Datenbank mit zwei Managern (ein
aktiver) über einen Saisonwechsel gespielt. Dabei 5 Stände gespeichert, mit bis zu 49 Nachrichten (A 39, B 10), alle
mit `"Read"`. Am Head:

- Jeder Stand lädt (`Success`).
- Speichern → Laden → Speichern ist bytegleich.
- Die Posteingänge sind sortiert.
- Nach 4 Tagen ist der Speicher geräumt (0 ≤ 24), und `Notify` legt wieder ab.

Der dokumentierte Übergang (> 24 Nachrichten → neue Meldungen entfallen bis zum Räumen; alte Angebote ohne `Player`
altern nach 3 Tagen) ist so zu beobachten. Spielerverweise werden über die ID geschrieben (`SaveGame.Writer`); die
Identität für `RemoveMessagesOf` bleibt nach dem Laden erhalten (Mehrsaisonprobe bytegleich).

**Gegenproben (Mutation → Test rot):**

| Mutation | roter Test |
|---|---|
| `MessageCapacity` 25 | `Store_…` |
| ohne `DaysLeft--` | `InfoMessage_…` |
| ohne `SortInbox` | `DailyRun_…` |
| tägliches Aufräumen der Spielermeldungen aus | `PlayerMessage_…` |
| `RemoveMessagesOf` leer | `PlayerMessage_…` |
| Spalten 21/121/221 | `SeasonEnd_…` |
| `ArrowRed` 238 | `SeasonEnd_…` |
| Schatten x + 1 | `Banner_…` |
| Seitenwahl geklemmt | `ClubPicker_…` |
| `InboxScreen.Enter` ändert Geld | beide `InboxOpenTests` |
| `NewsBar` setzt `Done` beim Zeichnen | `OpeningInboxAndDrawingNewsBar_…` |

**Akzeptierte `[APPROX]`:**

- Sortierschlüssel-Vorrang 0 mit Saison im Schlüssel.
- Tägliches Nachräumen der Spielermeldungen statt Löschen an jeder Stelle.
- Migration: `DaysLeft` = 3 und `Player` = null für alte Nachrichten; mehr als 24 alte Nachrichten bleiben bis zum
  Räumen.

**Annahmen (nicht geprüft):**

- Dass `0x50:0x19E` auf Datei 0x130EE abbildet, ist aus `re-meldungen.md` und dem Review Runde 10 übernommen
  (Overlay-Stub nicht selbst aufgelöst).
- Der Takt von 18,2 Hz ist nicht neu geprüft. Die Videobilder stützen aber die kürzere Dauer unabhängig davon.
