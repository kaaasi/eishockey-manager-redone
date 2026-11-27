# Review #355 – Abschluss Runde 9: Vorlagen (R9F-02 bis -04), #349 nach v1.0, Liste für Runde 11

- **PR:** #355 (`prozess/runde-9-abschluss`)
- **Base:** `6b52a4fda2a73f8c6241a78006206d68a6b3c6a7` · **Head:** `328c932527f642c276b804591d875e242fe2f893` (per REST gelesen)
- **Reviewer:** Claude Code (Claude Opus 5.5), geplanter Lauf, eigene Sitzung ohne Mitarbeit an #355
- **Art:** nur Codelesen (reiner Doku-PR, 4 Dateien, +22/−5; kein Build, keine Tests)

## 1. Merge-Empfehlung

**Ja, inhaltlich für genau diesen Head – nur Codelesen. Gemergt werden kann aber erst nach einem Rebase auf `main`.**
R9F-02, R9F-03 und der erste Punkt von R9F-04 sind so umgesetzt, wie das Review #318 es vorgeschlagen hat. Die Zeilenlänge ist
behoben, R9F-01 und der Torszenen-Halbsatz sind mit #321 gegenstandslos. Gegen das aktuelle `main` (`acc01a3`) entsteht ein
Konflikt in `docs/anforderungen/zusatzfunktionen.md` (AR9-01). Er ist leicht aufzulösen. Neu sind außerdem drei P3
(AR9-02 bis -04).

Erwartet offen und bestätigt:

- #349 nach v1.0: Eintrag in `zusatzfunktionen.md` Abschnitt C und in `arbeitspakete.md` vorhanden. Aber `Fixes #349` schließt
  das Issue beim Merge, siehe AR9-02.
- P3 für Runde 11: #327, #329, #342, #344, #347, #348, #354 sowie #352 und #353 sind offen und mit `P3` gekennzeichnet. Die Liste
  ist aber unvollständig, siehe AR9-03.

## 2. Befunde

### Besonders geprüft

**Vorlagen gegen R9F-02 bis -04** (Vorschläge aus `docs/reviews/2026-11-01-plan-runde-9-folgeteile.md`):

- **R9F-02:** `spur-start.md` Z. 23–24 bringt die Variante „Einzelner Folgeteil ohne Fortsetzung“, und Z. 49–50 erweitert den
  Geltungsbereich („… oder einen einzelnen Folgeteil mit Base nennt (dann ohne Schritt 5)“). Beide Vorschläge des Reviews
  sind übernommen. Das Gegenbeispiel aus R9F-02 greift damit nicht mehr: Stapelregel und Rebase-Ziel gelten über Schritt 2,
  Schritt 5 entfällt. **Erledigt**, mit einer Unschärfe im Wortlaut (AR9-04).
- **R9F-03:** Die Leitstelle muss den Prompt aus der Übergabe vorher gegen den Kurzaufruf „Geplanter Lauf mit Teilen“ prüfen.
  Abweichen dürfen nur Spur, Modell, Zähler und PR-Nummer (`leitstelle-start.md` Z. 31–32). In `spur-start.md` Z. 78 heißt es
  jetzt „setzt das Modell laut vom Nutzer bestätigtem Plan“. Beide Punkte stehen wörtlich wie vorgeschlagen. **Erledigt.**
- **R9F-04:** Die Draft-Zuständigkeit ist in beiden Dateien gleich formuliert („der Nutzer; die Leitstelle nur, wenn ihr ein
  Werkzeug dafür zur Verfügung steht“). Die frühere Zeile 20 mit 252 Zeichen ist umbrochen. Den Torszenen-Halbsatz braucht es
  nicht mehr: Teil 4 ist mit #321 gemergt (`05a2070`). **Erledigt bzw. gegenstandslos.**
- **R9F-01 (`Fixes #331`):** Auf `main` nutzt `src/EishockeyManager.Tools/Scenarios/Einstellungen.cs` Z. 18 jetzt
  `Row.Fullscreen` statt `Row.GoalScenes` (`b1a15eb`, Teil 4). Damit ist der Befund gegenstandslos. Das Schließen ist richtig.

**#349 nach v1.0:** Der neue Eintrag in Abschnitt C beschreibt den Merker 0x36FEA richtig: Ist `[bp−5] ≠ 0`, geht es bei
0x3702C direkt in die Minutenschleife 0x37088. Er nennt den Preis der Einstellung (Ergebnisse und Zufallsverbrauch). Zwei
Halbsätze sind durch #360 überholt (AR9-04).

### AR9-01 (P2, Merge-Voraussetzung) – Konflikt mit `main` in `zusatzfunktionen.md`

`git merge-tree --write-tree origin/main 328c932` → `CONFLICT (content): Merge conflict in docs/anforderungen/zusatzfunktionen.md`.
Auf `main` kamen seit der Base in Abschnitt C der Eintrag „Verjüngung des Spielerpools“ und eine Neufassung von
„Annahmewahrscheinlichkeit“ dazu. Die Neufassung betrifft genau die Zeilen, an die #355 anhängt. `docs/arbeitspakete.md` lässt
sich automatisch zusammenführen. GitHub meldet `mergeable_state: clean` noch gegen den alten Stand. **Auflösung:** Den Stand von
`main` übernehmen und den #349-Eintrag am Ende von Abschnitt C anhängen. Inhaltlich hängt nichts davon ab.

### AR9-02 (P3) – `Fixes #349` schließt einen Befund, der offen bleiben soll

PR-Beschreibung: `Fixes #349`. #349 ist ein `[APPROX]`, das bewusst bleibt („Remake-Verhalten bleibt; Original als Einstellung
nach v1.0“). Im Code (`MatchEngine.Live.cs`, nach #360) und in `docs/spielberechnung.md` (Head #360, Z. 259–263) steht es weiter mit
Verweis auf #349. Beim Merge schließt GitHub das Issue, und die Verweise zeigen dann auf ein geschlossenes Issue ohne
Kennzeichnung. **Vorschlag:** `Refs #349` statt `Fixes #349` und das Issue mit einem Label wie „nach v1.0“ offen lassen. Wenn
der Nutzer den Eintrag in `zusatzfunktionen.md` als Nachverfolgung genügen lässt, ist auch bewusstes Schließen in Ordnung. Dann
sollte das Issue aber mit „not planned“ und einem Verweis auf Abschnitt C geschlossen werden.

### AR9-03 (P3) – Liste für Runde 11 ohne #357 und #358

Stelle: [`arbeitspakete.md` Z. 154–155](https://github.com/kaaasi/eishockey-manager-redone/blob/328c932527f642c276b804591d875e242fe2f893/docs/arbeitspakete.md#L154-L155).
#357 (R9T-P3-03) und #358 (R9S-07) wurden um 18:49 angelegt, also nach #355 (18:35). Beide tragen im Text „Runde 11
(Entscheidung Nutzer 01.11.2026: P3 nach Runde 11)“, fehlen aber in der Liste. **Vorschlag:** Beim Rebase (AR9-01) ergänzen.
#357 gehört zur Probenrobustheit (neben #352, #353), #358 zu den P3 der Integration.

### AR9-04 (P3) – Wortlaut

- [`spur-start.md` Z. 23–24](https://github.com/kaaasi/eishockey-manager-redone/blob/328c932527f642c276b804591d875e242fe2f893/docs/vorlagen/spur-start.md#L23-L24):
  „statt des Satzes zur Fortsetzung“. Im Kurzaufruf (Z. 11–12) lautet der Satz „Teile und Selbstfortsetzung nach Abschnitt …;
  das Anlegen einer Fortsetzung per `create_trigger` ist autorisiert (höchstens 3 je Spur).“ Ersetzt man nur den zweiten
  Halbsatz, steht „Selbstfortsetzung“ weiter da. Eindeutiger wäre: „… statt ‚Teile und Selbstfortsetzung … je Spur).‘ den Satz
  ‚Teile nach Abschnitt „Teile und Fortsetzung“; nur Teil <n>, keine Fortsetzung anlegen.‘“
- [`zusatzfunktionen.md` Z. 61–65](https://github.com/kaaasi/eishockey-manager-redone/blob/328c932527f642c276b804591d875e242fe2f893/docs/anforderungen/zusatzfunktionen.md#L61-L65):
  „an einem **Pokal**spieltag“ – nach #360 gilt der Merker auch für Play-offs und Aufstiegsrunde (Code 1). „sonst alle über den
  schnellen Weg“ stimmt nur bei ausgeschaltetem Schalter des Wettbewerbs (0x3702E–0x37085). Vorschlag: „an einem Spieltag
  von Pokal, Play-offs oder Aufstiegsrunde … sonst je nach Anzeigeschalter des Wettbewerbs“; „Pokalspiele ohne Manager“ → „Spiele
  ohne Manager“.

## 3. Ausgeführte Prüfungen

```
git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD   → 0bc38fba0e3e7d0c41b122e8a3087a933a88ea13 HEAD
git fetch origin pull/355/head → 328c932527f642c276b804591d875e242fe2f893 (= Head laut REST)
git merge-base --is-ancestor 6b52a4f 328c932 → ja ; git diff --stat 6b52a4f..328c932 (4 Dateien)
git merge-tree --write-tree origin/main 328c932 → Konflikt zusatzfunktionen.md (AR9-01)
Review #318 (R9F-01 bis -04) auf main gelesen; Issues #327–#358 per GitHub-Connector (Labels, Status, Anlagezeit)
Einstellungen.cs und SettingsScreen.cs auf main (R9F-01); ndisasm 0x36FE0 ff. (Merker 0x36FEA / 0x3702C)
```

Kein Build und keine Tests (reiner Doku-PR, nur Codelesen). Das Review liefert keinen Testnachweis nach `zusammenarbeit.md`
§6 Nr. 4. Für Doku ist er nicht nötig.

**Akzeptierte `[APPROX]`:** #349 (je Paarung statt je Spieltag) als Entscheidung des Nutzers, Einstellung nach v1.0.
**Annahmen:** Die Zuordnung der Liste für Runde 11 zur Entscheidung des Nutzers ist aus den Issue-Texten übernommen und nicht
gesondert nachgefragt.
