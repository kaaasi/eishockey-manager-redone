# Review: Plan Runde 9 – Nachtrag (PR #287)

- Base `737af95594b3a0eea42dc6d5439f18732e59f745` · Head `cac9012a59d8845d4c69874b56470293522d8ec9` · Vorreview
  `docs/reviews/2026-11-01-plan-runde-9-nachpruefung.md` (Befunde R9N-01 bis -03, Issues #284–#286) · Reviewer: Claude
  (geplanter Lauf, eigene Sitzung, an PR #287 nicht beteiligt).
- **Nur Codelesen** (reiner Doku-PR, laut Auftrag kein Build). Checkout: `git checkout --detach cac9012`,
  `git rev-parse HEAD` = Head, `git status --porcelain` leer. Diff `737af95..cac9012`: 3 Dateien, +15/−9.

## 1. Merge-Empfehlung

**Ja für genau diesen Head (nur Codelesen).** R9N-01 bis -03 sind umgesetzt. Neu sind zwei P3 (R9T-01, R9T-02).
Sie blockieren den Merge nicht. R9T-01 sollte aber vor dem Start von `optionen-wirkung` im Plan oder unter
„Besonderheiten“ im Startaufruf stehen, R9T-02 vor dem ersten geplanten Lauf mit Teilen.

## 2. Befunde

### Status der Vorbefunde (Gegenbeispiele erneut ausgeführt)

- **R9N-01 – erledigt.** `Season.cs` ist aus der Ausnahme gestrichen und ausdrücklich ausgeschlossen. Erlaubt sind
  Einfügungen in `MatchOn`, `ProcessDay` und `StartNextSeason` ohne Signaturänderung. Der Fall „`Cups*.cs` nötig“
  führt auf blockiert. Ab dem nächsten Saisonstart ist als `[APPROX]` festgelegt.
  Gegenbeispiel `grep -rn "DaysOf(DayEvents\.\|SeasonCalendar.Events(" src` ist unverändert. Es betrifft aber
  nur noch `Season.cs`, und die Datei ist jetzt ausgenommen.
- **R9N-02 – erledigt, mit Rest R9T-02.**
  - Punkt 1: Schritt 3 verweist für geplante Läufe auf den Abschnitt. Abschnitt Schritt 1 regelt
    „Bearbeiter ≠ frei ohne Übergabe“.
  - Punkt 2 (Doppelstart) ist damit abgedeckt.
  - Punkt 3: Die Modellzeile lautet jetzt „darf vom Lauf nicht umgestellt werden … Leitstelle bzw. Nutzer stellen es
    um“. Das passt zur Werkzeugregel.
- **R9N-03 – erledigt.**
  `diff <(git show b3a38df:docs/arbeitspakete.md | sed -n 12,336p) <(tail -n +6 docs/archiv/runde-8.md)` ergibt
  weiter `0a1,7` (Ergebnisabsatz) und `325d331` (eine Leerzeile am Ende). Der Kopf nennt jetzt den ergänzten
  Absatz. Die Leerzeile ist ohne Belang.

### Besonders geprüft: Wettbewerbsschalter nur über `SeasonFlow.cs`?

**Ja, das ist möglich, aber nur über `StartNextSeason`.** Das stützt sich auf folgende Fundstellen:

- `GameState.Competitions` ist eine öffentliche `List<ISeasonCompetition>`
  ([`SeasonFlow.cs` Z. 54](https://github.com/kaaasi/eishockey-manager-redone/blob/cac9012a59d8845d4c69874b56470293522d8ec9/src/EishockeyManager.Core/SeasonFlow.cs#L54)).
- `DebCup`, `EuropeanCup` und `WorldCup` haben implizite öffentliche Konstruktoren.
- `StartNextSeason` ruft in Z. 398–399 `StartSeason` für jeden angemeldeten Wettbewerb auf.

Ein Ab- und Anmelden direkt davor braucht also weder eine Signaturänderung noch eine Änderung an `Cups*.cs`. Dazu
zwei Grenzen:

1. `InitializeCompetitions`
   ([`Cups.GameState.cs` Z. 27–43](https://github.com/kaaasi/eishockey-manager-redone/blob/cac9012a59d8845d4c69874b56470293522d8ec9/src/EishockeyManager.Core/Cups.GameState.cs#L27-L43))
   lässt sich nicht wiederverwenden, weil es bei `Count > 0` sofort zurückkehrt. Die Anmeldebedingungen
   (`ClubCompetitions`, `DebCup`, `EuropeAndWorldCup`) müssen deshalb in `SeasonFlow.cs` dupliziert werden.
2. Die Reihenfolge DEB-Pokal → Europapokal → WM muss erhalten bleiben. `MatchOn` liefert den ersten Treffer, und
   `StartSeason` zieht der Reihe nach aus `CupRandom`.

Beides ist ohne Planänderung machbar und sollte im PR der Spur benannt werden.

### R9T-01 (P3) – Filter in `MatchOn`/`ProcessDay` widerspricht „ab dem nächsten Saisonstart“

Stelle: [`arbeitspakete.md` Z. 50–53](https://github.com/kaaasi/eishockey-manager-redone/blob/cac9012a59d8845d4c69874b56470293522d8ec9/docs/arbeitspakete.md#L50-L53)
und [Z. 117–119](https://github.com/kaaasi/eishockey-manager-redone/blob/cac9012a59d8845d4c69874b56470293522d8ec9/docs/arbeitspakete.md#L117-L119).

Die Ausnahme erlaubt „Filter der Wettbewerbe nach den Spielstand-Schaltern in `MatchOn` und `ProcessDay`“. Liest ein
solcher Filter den aktuellen Schalter, wirkt das Ausschalten sofort. Genau diesen halben Pokal will Teil 1
ausschließen.

Der Filter erfasst nur Spieltermine und Infoleiste. Unberührt bleiben:

- `GameState.DebCup`, `EuropeanCup` und `WorldCup` (`Cups.GameState.cs` Z. 20, 56, 59). Daran hängen die
  Bildschirme, `DebCupDrawFlow` und `FinanceSponsorRating.cs`.
- `FinishSeason` mit Z. 304 (Titel).

Gegenbeispiel (Gedankenablauf): DEB-Pokal läuft, Runde 1 ist gespielt. Der Schalter wird am Tag *d* ausgeschaltet.
Dann ab `ProcessDay(d+…)`:

- Es gibt keine weiteren Pokalspiele.
- `DebCup` ist nicht null, die Bildschirme zeigen also einen laufenden Pokal ohne Termine.
- `Winner` bleibt null, und es wird kein Titel vergeben.

Mit dem An- und Abmelden in `StartNextSeason` sind die Filter überflüssig.

**Vorschlag:** Den Filter streichen und nur das An- und Abmelden in `StartNextSeason` erlauben. Alternativ den Filter
ausdrücklich an einen zum Saisonstart eingefrorenen Stand binden, nicht an den laufenden Schalter.

### R9T-02 (P3) – „wie blockiert“ gibt einen fremden Lauf frei; Kriterium „ohne Übergabekommentar“ unscharf

Stelle: [`spur-start.md` Z. 38–40](https://github.com/kaaasi/eishockey-manager-redone/blob/cac9012a59d8845d4c69874b56470293522d8ec9/docs/vorlagen/spur-start.md#L38-L40)
mit [Z. 47–49](https://github.com/kaaasi/eishockey-manager-redone/blob/cac9012a59d8845d4c69874b56470293522d8ec9/docs/vorlagen/spur-start.md#L47-L49).

Mit Schritt 3 des Hauptablaufs (Z. 19–22) ist die neue Regel in der Richtung widerspruchsfrei. Wo bisher stand „sonst
den Nutzer fragen“, heißt es bei geplanten Läufen jetzt „nicht anfangen, Spur wartet“, wie in `zusammenarbeit.md` §2
Nr. 2. Im Wortlaut gibt es drei Reibungen:

1. **„nichts ändern“ und „wie blockiert (Schritt 4)“ widersprechen sich.** Schritt 4 verlangt „Übergabe“ im PR,
   Label `spur-wartet` und den Kommentar „blockiert – Entscheidung Nutzer“. Eine Übergabe nach
   `docs/handoffs/VORLAGE.md` endet mit `Bearbeiter: frei`.
   Gegenbeispiel:
   1. Lauf A arbeitet noch an `paket/<spur>-2`.
   2. Lauf B wird doppelt gestartet, sieht A als Bearbeiter und befolgt Schritt 4 mit Vorlage.
   3. Damit gibt B den Branch frei.
   4. Ein dritter Lauf oder die Leitstelle hält den Branch für frei, während A weiter pusht.

   **Vorschlag:** „nichts am Branch ändern; nur Label `spur-wartet` und einen Hinweiskommentar ohne Übergabe und ohne
   `Bearbeiter: frei`; keine Fortsetzung“.
2. **„ohne Übergabekommentar“** sollte heißen „ohne Übergabekommentar *nach* dem letzten Bearbeiter-Eintrag“.
   Sonst gilt Folgendes als „mit Übergabe“: ein PR mit älterer Übergabe, auf die eine Übernahme
   (`## Übernahme … Bearbeiter: <Agent>`) und dann ein Abbruch folgten.
3. **„statt dieses Schritts und Schritt 5“** (Z. 21) hat zwei Folgen:
   - Wörtlich entfällt auch die Pflicht aus Schritt 3, sofort einen Draft-PR mit `Bearbeiter:` und Base-SHA
     anzulegen. Auf genau diesen Eintrag stützt sich die neue Doppelstart-Erkennung. Abgedeckt ist sie nur über
     `AGENTS.md` 2.3.
   - Abschnitt Schritt 2 (Z. 42) verweist mit „Rebase vor dem Push (Schritt 5)“ auf den Schritt, der gerade ersetzt
     wurde. Im selben Abschnitt ist Schritt 5 aber „Fortsetzung anlegen“.

   **Vorschlag:** „statt der Prüfung auf vorhandene PRs (Schritt 3) und des Rebase-Ziels (Schritt 5)“, und in Z. 42
   „(Hauptablauf Schritt 5)“.

## 3. Ausgeführte Prüfungen

- `git diff --stat 737af95..cac9012` und das ganze Delta gelesen. `spur-start.md` am Head ganz gelesen.
- Code zur Ausnahme gelesen:
  - `SeasonFlow.cs` Z. 54, 86–125, 170–208, 296–310, 365–406,
  - `Cups.GameState.cs` Z. 1–60,
  - `SeasonRecord.cs` Z. 109–132 (`ISeasonCompetition`),
  - `CupsNationalMode.cs` Z. 48–60,
  - `DebCupDrawLive.cs` Z. 33–43.
- `grep` nach Konstruktoren der Pokale und nach den Lesern von `Competitions` und `EuropeanCup`.
- `reverse-engineering.md` Z. 606–627 (Index 9–12) gelesen.
- `diff` Archiv gegen `b3a38df` (siehe R9N-03).
- `zusammenarbeit.md` §2 und `handoffs/VORLAGE.md` zum Bearbeiter-Eintrag gelesen.
- Kein Build und keine Tests (Doku-PR, Auftrag). Damit kein Testnachweis nach `zusammenarbeit.md` §6 Nr. 4.
- **Akzeptierte `[APPROX]`:** „Wirkung ab dem nächsten Saisonstart, solange 0x2CD1C nichts anderes belegt“
  (`reverse-engineering.md` Z. 627: Folgewirkung nicht belegt).
- **Annahmen:**
  - Pokale werden mit dem Spielstand vollständig serialisiert. Eine nachträglich angemeldete Instanz braucht deshalb
    keine Migration. Das ist nicht geprüft und gehört in den PR der Spur.
  - Der Moduswechsel (Index 9) ist über `GameSetup.cs` und `CupsNationalMode.cs` (beide `optionen-wirkung`) und
    eine Einfügung in `StartNextSeason` erreichbar.
