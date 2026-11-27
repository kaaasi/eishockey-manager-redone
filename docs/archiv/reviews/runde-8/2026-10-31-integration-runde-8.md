# Review: Integration Runde 8 (#250) sowie Doku-PRs #246 und #251

- PR: #250 · Base `421d96f9c3bee5ce514fc3f4859a7cb272e1d8af` · Head `8b471845c2b1ed814249f005c2b5284dfc7b2f60`
- Enthalten: Spur-PRs #244 (befunde-p3), #240 (tabellen-original), #243 (nationalteam, Taktik), #242 (statistiken),
  #241 (spielstaerke-manager), #249 (spielerinfo), #248 (transfer-original, Teil 1), #245 (optionen-kalender, Teil 1: Belege);
  keiner davon war einzeln reviewt, ihr Inhalt wurde mitgeprüft.
- Reviewer: Claude (frische Sitzung ohne Implementierungskontext, geplanter Lauf, unbeaufsichtigt), 31.10.2026.
  Die Prüfung lief auf vier Teilprüfungen in eigenen Worktrees verteilt: A Schema/Seeds/Spielstärke, B Tabelle/Statistiken/
  Nationalteam, C Transfer/#246/#251, D Spielerinfo/befunde-p3/Dateigrenzen. Alle Aussagen unten stammen aus
  Disassembly, Ausführung oder Codelesen in dieser Sitzung.
- Checkout: Klon, `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer. Originaldaten aus
  `kaaasi/eishockey-manager-referenz` (`original/gamedata`, `SHA256SUMS` geprüft: OK). SDK 10.0.402 aus dem Release
  `claude-werkzeuge`.
- **Annahmen** (unbeaufsichtigter Lauf):
  - `gh` war in dieser Umgebung nicht nutzbar (`GH_TOKEN` ungültig). Das Release ist öffentlich und wurde ohne Anmeldung
    über die REST-Asset-URL geladen, die Pakete von Hand entpackt (wie `setup-workspace.sh`). Branch, PR und Issues liefen
    über den GitHub-Connector (REST), nicht über `gh api`. GraphQL wurde nicht benutzt.
  - Die Priorität richtet sich nach Wirkung auf Ergebnisse und Geld. Darstellung wurde nur stichprobenartig geprüft (siehe 3.3).

## 1. Merge-Empfehlung

### PR #250 – **Nein für genau diesen Head, solange R8-D01 offen ist.** Ohne R8-D01 wäre es ein Ja.

Spielregeln mit Wirkung auf Ergebnisse und Geld stimmen in allen geprüften Stellen mit dem Disassembly überein:

- Ligatabelle: Sortierung 0x50B45 und Bilanz 0x50283.
- Teamwerte: Lader 0x2EF1B und Tagesform 0x4F527.
- Ranglisten 0x25E30/0x2A1E0 und Einzelprämie (genau eine Buchung je Saison über `Book`).
- Taktik des Nationalteams.
- Transfermarkt: Annahme 0x4991D, Unfair-Grenze 0x47582, Angebote 0x072DD/0x47347.
- #216, #217, #225 und #230.

Weitere Ergebnisse:

- **Migrationen:** Echte Schema-11-Spielstände, auf der Base erzeugt, laden am Head. Danach liefen vier Saisonwechsel ab,
  Sichern und Laden war byte-gleich.
- **Seed-Wechsel:** Beide verdecken keinen Fehler. Sie zeigen aber, dass die Proben am Zufall hängen (R8-A02).
- **`tools/pruefen.sh`:** Alle drei Läufe sind am exakten Head grün, inklusive `OriginalSavePatcherTests`.

R8-D01 ist der einzige Befund ab P2. Er betrifft das Spielerdaten-Fenster:

- Die Regel ist als `[ORIGINAL]` markiert, folgt aber nicht dem Original.
- Wirksam ist sie noch nicht, weil `PlayerDataContext.Create` keinen Aufrufer hat.
- Die Leitstelle soll aber genau diese Aufrufer als Folgearbeit anschließen. Ein falsches Ausblenden würde dann im
  Transfermarkt und bei fremden Spielern sichtbar.

Nach `zusammenarbeit.md` §6 Nr. 5 braucht es eins von zwei Dingen:

- einen kleinen Fix auf dem Integrationsbranch mit anschließender Nachprüfung des Deltas, oder
- eine ausdrückliche Zurückstellung durch den Nutzer. Das ist vertretbar, solange es keinen Aufrufer gibt. Dann muss der
  Fix vor dem Anschluss der Aufrufer kommen.

### PR #246 (Fahrplan) – **Ja, nur Codelesen.**

- Der Merge geht ohne Konflikt auf `main` und auf 8b47184, auch zusammen mit #251.
- Kein Widerspruch zu `AGENTS.md`.
- Ein P3-Befund (F46-01).

### PR #251 (Idee Annahmewahrscheinlichkeit) – **Ja, nur Codelesen, aber erst nach #250.**

- Die Aussagen stimmen mit `TransferRules.Accepts` und 0x4991D überein.
- Vor #250 verweist der PR auf Dateien, die es auf `main` noch nicht gibt (W51-01).
- #246 und #251 gehen in `docs/anforderungen/zusatzfunktionen.md` konfliktfrei zusammen: #246 hängt Nr. 6 an Abschnitt A,
  #251 legt Abschnitt C an.

Für #246 und #251 liefert dieses Review keinen Testnachweis. Für reine Doku-PRs ist keiner nötig.

## 2. Befunde

Link-Basis: `https://github.com/kaaasi/eishockey-manager-redone/blob/8b471845c2b1ed814249f005c2b5284dfc7b2f60/`

### R8-D01 (P2) – Spielerdaten-Fenster blendet Gehalt und Marktwert nach Spielklasse statt nach Verein aus

[`PlayerDataWindow.cs` Z. 5–33](https://github.com/kaaasi/eishockey-manager-redone/blob/8b471845c2b1ed814249f005c2b5284dfc7b2f60/src/EishockeyManager.UI/Screens/Team/PlayerDataWindow.cs#L5-L33),
dazu [`reverse-engineering.md` Z. 442](https://github.com/kaaasi/eishockey-manager-redone/blob/8b471845c2b1ed814249f005c2b5284dfc7b2f60/docs/reverse-engineering.md#L442).

Was das Original an 0x041AC, Versatz 0x7D6–0x801, tut:

```
mov al,[0xa70e] ; imul 0x1af ; mov al,[bx-0x6991]   ; Manager +0x14 = Verein (0x966F = 0x965B + 0x14)
les bx,[0x8253] + 0x21·Spieler[+0x0F]                ; Spielerpool
cmp al,[es:bx+0x1d] ; jz 0x820                        ; Pool +0x1D = Verein
```

- Das Original vergleicht **Vereinsindizes**. Beide Felder sind in `spielstand-original.md` als „Verein“ belegt
  (Z. 185 und Z. 277).
- Ist der Verein ein anderer, wird das Gehalt immer ausgeblendet. Der Marktwert wird ausgeblendet, wenn 0xED8 < 3 ist.

Was das Remake tut:

- Es vergleicht `CupClass.Of(playerClub) == CupClass.Of(ownClub)`.
- `playerClub == null` (Transfermarkt) wertet es als „gleich“.
- Folge: Bei Spielern fremder Vereine derselben Liga und bei allen Marktspielern stehen Gehalt und Marktwert da. Im
  Videobild steht dort „-“, wie der Doku-Abschnitt selbst schreibt.
- Der Code trägt `[ORIGINAL]`. Derzeit ruft niemand `Create` auf.

Gegenbeispiel: ein temporärer Test, im Worktree rot.

- Aufbau: synthetisches Spiel, Manager bei Verein 2, Spieler von Verein 3 in derselben Klasse.
- `PlayerDataContext.Create(game, other.Players[0], other, own, 3).ShowSalary` ergibt `true`, erwartet ist `false`.
- Mit `playerClub: null` kommt dasselbe heraus.

Vorschlag:

- Vergleich auf `playerClub != ownClub` umstellen und `SameClass` in `SameClub` umbenennen.
- Doku-Abschnitt berichtigen.
- Test über `Create` mit drei Fällen: gleicher Verein, fremder Verein derselben Liga, Transfermarkt.

### R8-A01 (P3) – Spielstände aus den Spurbranches `statistiken` und `spielstaerke-manager` laden nicht

[`SaveGame.cs` Z. 77–79](https://github.com/kaaasi/eishockey-manager-redone/blob/8b471845c2b1ed814249f005c2b5284dfc7b2f60/src/EishockeyManager.Core/SaveGame.cs#L77-L79)

- Beide Branches schrieben „Schema 12“. Am Head bedeutet 12 „nach Nationalteam-Migration“, also wird der Schritt 11 → 12
  übersprungen.
- Nachgestellt wurde das an einem Head-Spielstand: Felder `_vacantNationalTactics` und `_shownAuto*` entfernt, `schema`
  auf 12 gesetzt.
- Ergebnis: „Der Spielstand ist beschädigt: spiel: Feld _vacantNationalTactics in GameState fehlt.“ Der Abbruch ist sauber,
  ein falscher Zustand entsteht nicht.
- Spielstände vom Branch `nationalteam` (ebenfalls Schema 12) laden fehlerfrei.
- Betroffen sind nur nie gemergte Zwischenstände. Ein Hinweis im Migrationskommentar genügt.

### R8-A02 (P3) – Seed-Proben hängen am Zufall; Seeds werden wiederholt verschoben

[`BoerseStartTests.cs` Z. 192–202](https://github.com/kaaasi/eishockey-manager-redone/blob/8b471845c2b1ed814249f005c2b5284dfc7b2f60/tests/EishockeyManager.Core.Tests/BoerseStartTests.cs#L192),
[`FinanceWerbungTests.cs` Z. 350](https://github.com/kaaasi/eishockey-manager-redone/blob/8b471845c2b1ed814249f005c2b5284dfc7b2f60/tests/EishockeyManager.Core.Tests/FinanceWerbungTests.cs#L350)

Der Börsen-Seed 7 → 8 ist tragfähig:

- Der Börsencode ist unverändert.
- Die Ursache ist eine Kette: Der Lader zieht aus der Hauptquelle, dadurch fallen die Spielergebnisse anders aus, dadurch
  verbraucht die Werbebewertung die Finanzquelle anders, dadurch entsteht eine andere Börsenfolge.
- Seeds 1–56 wurden auf Base und Head über vier Saisonwechsel verglichen. Bei den Seeds 17–56 verfehlen Base 9/40 und
  Head 8/40 die Bedingung „≥ 2 von 4 Basiswerten wachsen“. Die Rate ist also gleich, ein Regelfehler wird nicht verdeckt.

Das Problem liegt in den Proben selbst:

- Rund 20 % aller Seeds verfehlen die Bedingung.
- Der Werbungstest hat jetzt zum dritten Mal den Seed gewechselt (11 → 12 → 15 → 16).

Vorschlag: Die Vorbedingung gezielt herstellen, also eine Börse ohne Ereignisse bzw. einen auslaufenden Vertrag mit Option
anlegen, statt den Seed weiter zu verschieben.

### R8-C01 (P3) – Sieben ältere Core-Testfälle kaufen zum Listenpreis und sind nur wegen des Seeds grün

Betroffen sind:

- `TransferTests.cs` Z. 55, 72, 86 (`Buy_Refusals` erreicht `NotEnoughMoney` nur, wenn die Annahme vorher klappt)
- `PlayerFactoryTests.cs` Z. 55 (beide Fälle)
- `LifecycleRegressionTests.cs` Z. 136
- `WaveBReviewRegressionTests.cs` Z. 23

Gegenbeispiel:

- In `TransferRules.Accepts` die letzte Zeile ersetzen durch `Random16(random,70,120); return false;`. Das ist ein
  möglicher Ausgang mit gleich vielen Zügen.
- Genau diese 7 Testfälle werden rot.
- Zum Listenpreis nimmt das Original nur mit ≈ 59 % an. Jede Änderung der Zugfolge, etwa in Teil 2, kann sie also kippen.

Vorschlag: wie in den beiden UI-Tests 135 % bieten. Das ist im Original immer angenommen, weil der Wert über 130 % liegt
und die Unfair-Grenze beim Computermarkt nicht gilt. Oder die Annahme vom Ablehnungstest entkoppeln.

Die 135 % in `TeamScreenTests` und `LifecycleScreenRegressionTests` sind damit **begründet** und verdecken keinen Fehler.

### R8-C02 (P3) – Reihenfolge Geld/Unfair beim Gebot an einen anderen Manager

[`Transfer.cs` Z. 262–273](https://github.com/kaaasi/eishockey-manager-redone/blob/8b471845c2b1ed814249f005c2b5284dfc7b2f60/src/EishockeyManager.Core/Transfer.cs#L262-L273)

- Das Original prüft das Geld (0x474F0) vor der Unfair-Grenze (0x47546).
- Ist ein Gebot unfair und reicht das Geld nicht, meldet das Remake „unfair“, das Original „nicht genug Geld“.
- Das betrifft nur die Meldung, weder Geld noch Ergebnis. Code oder Doku angleichen.

### R8-B01 (P3) – Tabellentest prüft die Regel „weniger Spiele“ nicht

[`TabellenOriginalTests.cs` Z. 81](https://github.com/kaaasi/eishockey-manager-redone/blob/8b471845c2b1ed814249f005c2b5284dfc7b2f60/tests/EishockeyManager.Core.Tests/TabellenOriginalTests.cs#L81)

- Im Testfall haben die Vereine 3 bzw. 2 Punkte, es entscheiden also die Punkte.
- Mutation: `if (b.Games != a.Games) …` in [`Season.cs` Z. 246](https://github.com/kaaasi/eishockey-manager-redone/blob/8b471845c2b1ed814249f005c2b5284dfc7b2f60/src/EishockeyManager.Core/Season.cs#L246)
  entfernen. Alle 9 Tabellentests bleiben grün.
- Nötig ist ein Fall mit gleichen Punkten, weniger Spielen und schlechterer Tordifferenz.

### R8-B02 (P3) – Zählung der Strafminuten im Ligaspiel nur indirekt getestet

[`StatisticsClub.cs` Z. 31](https://github.com/kaaasi/eishockey-manager-redone/blob/8b471845c2b1ed814249f005c2b5284dfc7b2f60/src/EishockeyManager.Core/StatisticsClub.cs#L31)

- Mutation: die Heimzeile entfernen. Rot wird nur der Referenzabdruck `Mode4_ReferenceValues_Unchanged`.
- Vorschlag: ein Test, der prüft, dass die Summe der Spielberichte `LeaguePenaltyMinutes` ergibt und dass Pokal und
  Play-offs nicht zählen.

### R8-B03 (P3) – Heimspiel-Zweig der Nationalteam-Taktik ungetestet

[`CupsNational.Tactics.cs` Z. 48](https://github.com/kaaasi/eishockey-manager-redone/blob/8b471845c2b1ed814249f005c2b5284dfc7b2f60/src/EishockeyManager.Core/CupsNational.Tactics.cs#L48)

- Mutation: `if (false && home == …)`. Alle Core-Tests bleiben grün, auch mit Originaldaten.

### R8-B04 (P3) – Freie Nationalteam-Taktik ohne Bundestrainer behält Modus des Vorgängers

- Steht ein Modus auf „SEMI“, schreibt `RecordAutoLine` nichts. Das Original schreibt bei Vereinsfeld 0x16 = 0 immer (0x842B).
- Ob 0x16 ohne Bundestrainer 0 ist, ist nicht belegt.
- Das betrifft nur die Anzeige: `RatedNation` baut für WM-Spiele einen frischen `Club` mit Standardtaktik, die Taktik wirkt
  also nicht auf Ergebnisse.

### R8-A03 (P3) – Zwei Mutationen der Teamwerte fallen nur in Abdrucktests auf

[`MatchEngine.Live.cs` Z. 48–54](https://github.com/kaaasi/eishockey-manager-redone/blob/8b471845c2b1ed814249f005c2b5284dfc7b2f60/src/EishockeyManager.Core/MatchEngine.Live.cs#L48),
`MatchEngine.TeamValues.cs` Z. 57–62.

Zwei Mutationen bleiben in `SpielstaerkeManagerTests` grün:

- Tagesform beim kampflosen Spiel entfernt.
- Zusätzliche Ziehung zwischen Kondition und Technik.

Rot werden sie nur in `RngTwinProbeTests` und Mode4. Ob das kampflose Spiel (0x36DC5) im Original durch 0x50283 läuft, ist
nicht am Disassembly belegt. Randnotiz: `Math.Clamp(…, 0, 100)` in Z. 135–141 trägt kein `[APPROX]`. Im Original sind
Werte bis 101 möglich, das passiert in etwa 1 % der Fälle.

### R8-D02 (P3) – Grenzwert 10 der Bundestrainer-Warnung (#217) ungetestet

[`MainMenuScreen.cs` Z. 152](https://github.com/kaaasi/eishockey-manager-redone/blob/8b471845c2b1ed814249f005c2b5284dfc7b2f60/src/EishockeyManager.UI/Screens/MainMenuScreen.cs#L152)

- Mutation `< CoachMinPlayers - 1` bleibt grün.
- Es fehlt ein Fall mit 9 und 10 Spielern (Original: `cmp al,0xA; jnc`).

### R8-D03 (P3) – Anfangswert der Spielmodus-Optionen falsch zugeordnet (#245)

[`reverse-engineering.md` Z. 499](https://github.com/kaaasi/eishockey-manager-redone/blob/8b471845c2b1ed814249f005c2b5284dfc7b2f60/docs/reverse-engineering.md#L499)

- Die Bytes ab DGROUP 0x21D sind `0 × 10, 1, 0, …`. Die 1 steht also bei Index 10 (0x227), nicht bei Index 9.
- Index 9 ist 0x226 und damit der Spielmodus, wie der Abschnitt selbst sagt.

### R8-D04 (P3) – Sperre zeigt Spiele unter dem Wort „Minuten“

[`PlayerHintLine.cs` Z. 141–142](https://github.com/kaaasi/eishockey-manager-redone/blob/8b471845c2b1ed814249f005c2b5284dfc7b2f60/src/EishockeyManager.UI/Screens/Team/PlayerHintLine.cs#L141)

- Ausgabe ist „Noch 2 Minuten Gesperrt.“ bei 2 Spielen Sperre.
- Das ist als `[APPROX]` ausgewiesen und annehmbar, bis Sperrminuten geführt werden.

### R8-D05 (P3) – Dateigrenzen leicht überschritten, alle Eingriffe minimal und kommentiert

`statistiken` greift über die Spurgrenzen hinaus:

- je eine Zeile in `GameState.cs` und `GameState.Integration.cs` (gemeinsame Dateien)
- eine Zeile in `SeasonFlow.cs`
- drei Enum-Werte in `SeasonRecord.cs`
- ein Eintrag in `funktionskarte.py` (Datei von `befunde-p3`), folgerichtig

Weitere Spuren:

- `befunde-p3`: `UI/Testing/ScreenDriver.cs` geändert. Das ist Testinfrastruktur, im Plan aber nicht vorgesehen.
- `spielerinfo`: neue Datei `PlayerHintLine.cs` als Baustein.

Kein Eingriff ist inhaltlich bedenklich. Es fehlt nur die Begründung nach Regel 2.5 in den Spur-PRs.

### W51-01 (P3, #251) – Verweise auf `docs/transfer.md`/`TransferRules.Accepts` existieren erst mit #250

Deshalb erst nach #250 mergen.

### W51-02 (P3, #251) – Aussage „hängt nur von Gebot und Listenpreis ab“ zu knapp

Ergänzen, für welche Fälle sie gilt:

- Sie gilt nur für den Computermarkt.
- Bei Leihe gilt sie über Preis/3.
- Vertragslose Spieler werden immer angenommen.
- Mit der geplanten Sperre nach Ablehnung (Teil 2) gilt die Anzeige nur für einen Versuch.

### W51-03 (P3, #251) – Vor „## C. Ideen (ab v1.0)“ fehlt eine Leerzeile

### F46-01 (P3, #246) – `README.md` geändert, die Begründung nach Regel 2.5 fehlt im PR

Der Anker `#6-unabhängigkeit-von-den-originaldateien-zur-laufzeit` stimmt.

### Erwartet offen (bestätigt)

Folgendes ist laut `docs/arbeitspakete.md`, Abschnitt „Integration Runde 8 (Leitstelle)“, offen und ist es am Head
tatsächlich:

- Funktionskarte neu erzeugen.
- Aufrufe des Spielerdaten-Fensters anschließen. R8-D01 vorher beheben.
- `bildschirme.md` Z. 103 und die Vermutungen zu 0x5CCB berichtigen.
- Hinweis in `wettbewerbe.md`.
- Lader eventuell nach `NewGame.Create` verlegen.
- Teile der Spuren:
  - `tabellen-original` Teil 2
  - `spielstaerke-manager` Teil 2
  - `statistiken` „Verein“ 0x2D5AE
  - `nationalteam` #194, #195 und WM-Statistik
  - `optionen-kalender` Teil 2
  - `transfer-original` Teil 2

Folgendes ist ebenfalls offen und schon in `docs/transfer.md` geführt: Pflege 0x7C12 und das Überspringen von Spielern mit
+0x18 Bit 0x80 vor dem Tageswurf 0x072DD.

## 3. Ausgeführte Prüfungen

### 3.1 `tools/pruefen.sh` (alle drei Läufe, am exakten Head 8b47184, Originaldaten)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1265 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1050 | 0 | 191 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1050 | 215 | 0 |

- Commit 8b471845c2b1ed814249f005c2b5284dfc7b2f60, SDK 10.0.402, Ergebnis „bestanden“.
- Der Lauf mit Originaldaten enthält `OriginalSavePatcherTests`.
- Die Zahlen stimmen mit der PR-Tabelle überein, die dort an `635132c` erhoben wurde. Das Delta bis 8b47184 betrifft nur
  `docs/arbeitspakete.md`.

### 3.2 Disassembly (`ndisasm -b16` an `gamedata/em.exe`) und Ausführung

**Schema 11 → 14**

- Vier echte Schema-11-Stände auf der Base erzeugt: Modus 4 mit zwei Managern, Modus 2, Modus 1 am Saisonende, Modus 4
  nach 45 Spieltagen.
- Alle laden am Head. `TeamValues` ist null, `LeaguePenaltyMinutes` ist 0.
- Sichern und Laden byte-gleich, danach vier Saisonwechsel mit `KeepSquad`, danach erneut byte-gleich.
- Reihenfolge Nationalteam → Statistiken → Teamwerte bestätigt. `??=` bzw. `ContainsKey` sind idempotent.

**Mode4-Abdruck**

- Titel und `R375033233 C996914971` sind identisch mit dem Spur-Head ee72a59.
- Nur die Kontostände weichen ab: +100.000 DM Einzelprämie, ab Saison 3 die Werbebewertung. Die PR-Aussage stimmt.

**Lader 0x2EFDE–0x2F158**

- Tagesform random(0x2F, 0x35).
- Je Teil v − r1 + r2 mit r = random(0, v/10), in der Reihenfolge Kondition, Technik, Form.
- Form = random(0x2D, 0x37) − 5·(i > 0x40).
- Nationalteams aus DGROUP 0x4C4E.
- Ein Byte-Scan der Schreibzugriffe auf die Vereinsfelder 0x17–0x1C bestätigt `spielberechnung.md` §12: Die Werte der
  Computervereine bleiben fest.

**Tagesform 0x4F527**

- r = random(0, 2·|d|) mit Vorzeichen, begrenzt auf 0x2F…0x35.

**Tabelle**

- Sortierung 0x50D6D–0x51011, in dieser Reihenfolge:
  - Punkte
  - weniger Spiele (+0x1E, Schalter 0x6B0)
  - Tordifferenz
  - Tore
- Ansicht 0/1/2.
- Bilanz 0x50283: DGROUP „sSnNuU“, Heim klein.
- Wirkung: Nur bei vollem Gleichstand entscheidet jetzt die Reihenfolge des Vortags statt `Club.Index`. Das gilt
  deterministisch auch für Abschlusstabelle, Play-offs, Auf- und Abstieg und `TableRank`.

**Statistiken und Prämien**

- Fair-Play 0x2A1E0–0x2A2CC.
- Ranglisten 0x25F4E–0x26145: 32-Bit-Schlüssel, bei Gleichstand weniger Spiele; Torhüter ab 0x18F Minuten; Modus 2 = SCORER.
- Die Prämie wird einmal je Saison in `FinishSeason` über `Book(Praemien)` gebucht, vor dem Rücksetzen und vor dem
  Ligawechsel.
- Entlassene Manager und der Modus „nur Weltkugel“ bekommen keine Prämie.

**Nationalteam-Taktik 0x8365–0x84BD**

- Geschrieben wird nur bei Feld 0x16 = 0 oder bei Modus „AUTOM.“.
- Einsatz und Angriff werden getrennt behandelt.
- Grundwerte 60/40 bei |Differenz| < 25.
- Keine Wirkung auf WM-Ergebnisse (`RatedNation`).

**Transfer (Overlay 0x118 ab Datei 0x46000)**

- Annahme 0x4991D–0x49A3A:
  - Einziger Aufrufer 0x472C8, Preis-Argument immer 0, damit ist der Zweig über +0x28 tot.
  - Bei Leihe Preis/3.
  - Gezogen wird in der Reihenfolge 75–85 / 110–130 / 70–120, und nur so weit wie nötig.
- Unfair-Grenze 0x47546–0x475DB: 60 %/140 % des frischen Werts.
- Tageswurf 0x072DD–0x073C2.
- Angebotshöhe 0x47347–0x473DF.
- random(a,b) 0xC037.
- Kauf und Verkauf laufen weiter über `Book(…, Transfers, ±)`.

**Spielerinfo**

- Hinweiszeile 0x41683: Texttabelle 0x984 + 4·Spalte, „Status: “, Reihe, Statuszweige.
- Spielerdaten-Fenster 0x041AC: Felder 0/1/2/4 werden durch 3 geteilt, Ausblenden siehe R8-D01.
- `PlayerDataContext.Create` ändert den Spielzustand nicht (`new Random(player.Id)`).

**befunde-p3**

- #216: Bereiche des Managerkopfs und des Logos.
- #217: [0xA70E] = 5 und Anzahl < 10, Rücksprung 0x535B. [0x176] gehört zur Sicher-/Ladesperre, die Entscheidung des
  Nutzers ist korrekt umgesetzt.
- #225: `5B 0E 53` bei 0x02327.
- #230: `or` der Wörter +0x30/+0x32 bei 0x42413.

**Probemerge** 8b47184 + #246 + #251: ohne Konflikt.

**Regel 2.1**: keine Binärdateien und keine Bilder im Diff 421d96f..8b47184.

**Mutationen** (gezielte `dotnet test --filter` in eigenen Worktrees):

| Bereich | rot (Test greift) | überlebt (→ Befund) |
|---|---|---|
| Teamwerte/Tagesform | Form ohne −5; Tagesform nach allen Wettbewerben; Streuung für Managervereine | Tagesform kampflos, Zusatzzug (R8-A03) |
| Tabelle | Bilanz Groß/Klein; Gleichstand nach `Club.Index`; nur einmal sortiert | Regel „weniger Spiele“ (R8-B01) |
| Statistiken | Prämie 0; Fair-Play bei Gleichstand; Spiele-Regel der Rangliste; `ResetLeaguePenalties`; Torhüter-Schwelle | Heim-Strafminuten (R8-B02) |
| Nationalteam | – | Heim-Zweig (R8-B03) |
| Transfer | Grenze 60 → 59; `<` → `<=`; Zusatzzug; V/15 → V/16; random(0,2) entfernt | – |
| Spielerinfo/Menü | `Row` mit Floor; `SameClass` immer wahr | `CoachMinPlayers − 1` (R8-D02) |

### 3.3 Nur stichprobenartig oder nicht geprüft

**Stichprobe**

- Kein Pixelvergleich am Videobild, betroffen sind Tabelle, Statistiken, Fairplay, Spielerdaten-Fenster und Taktik.
- Bildschirmfoto-Abläufe nicht selbst ausgeführt.
- Formeln aus der Doku übernommen, nicht im Disassembly gelesen:
  - Torhüterschlüssel −(GA·6000/Min)
  - Aufnahmeregeln 0x26365
  - Prämienbuchung 0x27184 und Rücksetzen 0x5647A
  - `0x120:0x61`
- Aufrufstelle von 0x4F527 in 0x50283 und die Codes 7/8 nicht disassembliert. Ebenso offen, ob Play-offs und Relegation im
  Original Tagesform tragen.

**Nicht geprüft**

- Meisterverteilung (#204, §12): nicht nachgerechnet.
- `FinanceWerbungTests` 15 → 16 und `LifecycleRegressionTests` Seed 2 → 5: nur gelesen, kein eigener Seed-Scan.
- Spielerdaten-Fenster: Balken- und Gesichtsformeln (0x335–0x3D1), Spaltenbreiten DGROUP 0x39F und Aufrufer 0x400E0 nicht
  nachgelesen.
- Goldtest FILE03/05/07 der Transferpreise: nur über das grüne Testergebnis.
- Ein echter Schema-11-Stand mit amtierendem Bundestrainer lag nicht vor. Diesen Fall decken nur die synthetischen
  `NationalteamTests.Schema11_*` ab.
- Optionen-Doku: Klickverhalten und Leser 0x3A8EC.
- AutoEffort/AutoAttack-Tabellen 0x4912/0x491C.

### 3.4 Akzeptierte `[APPROX]`

**Teamwerte**

- Lader beim ersten Spiel aus `GameState.Random`.
- Byte 1 (Sturm) gleich Byte 0.
- Managervereine ohne Streuung und mit Form 50.
- WM ohne Nachführung der Tagesform.

**Alte Spielstände**

- Taktik des Nationalteams startet mit 0 und zeigt 0.
- Strafminuten starten mit 0.

**Tabelle**

- Startordnung jeder Saison nach Vereinsindex.
- Die Heim- und Auswärtsansicht ändert die Platzliste nicht.
- 0x6B0 = 0.

**Statistiken**

- Spielerpool = ganze Liga.
- Fair-Play-Reihenfolge nach Vereinsindex.

**Nationalteam**

- Automatik mit `NationalMatchStrength`.

**Transfer**

- Bietender Verein zufällig.
- Angebotshöhe wird beim Angebot gewürfelt.
- Antwort des verkaufenden Managers wie bisher.
- Listenpreis eigener Spieler frisch berechnet.
- Tendenz in drei Stufen.

**Spielerinfo**

- Verletzungsart, Sperrminuten und Belastung aus `Energy`.
- Versicherung „ja“.
- Zähler in Klammern.
- Unter-/Überzahl.
- `IsGoalkeeper` über den Rollenwert.
- Kachel beim Managerkopf abgewählt.
- „Na gut.“
- `RowAt` 28 + 6k.

Nicht akzeptiert: das `[ORIGINAL]` in R8-D01.
