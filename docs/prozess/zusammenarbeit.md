# Zusammenarbeit mehrerer Agenten

Gilt für alle Agenten (Claude Code, ChatGPT, andere) und ergänzt [AGENTS.md](../../AGENTS.md). Ziel: Jeder geeignete
Agent kann ein Arbeitspaket, ein Review oder – nach Freigabe des Nutzers – die Leitstelle übernehmen, **nur anhand
dessen, was in GitHub steht**. Kein Schritt darf einen bestimmten Chatverlauf voraussetzen.

## 1. Rollen

| Rolle | Wer | Aufgaben | Darf nicht |
|---|---|---|---|
| **Nutzer** | Projektinhaber | Ziele, Freigaben, Ernennung der Leitstelle, letzte Entscheidung bei Uneinigkeit | – |
| **Leitstelle** | ein vom Nutzer ernannter Agent (aktuell siehe unten) | Pakete schneiden und verteilen, PRs integrieren, Merge-Prüfung, Review-Anfragen formulieren, Plan pflegen | eigene Umsetzungen (auch Integrations-Commits) ohne unabhängiges Review mergen |
| **Implementierer** | beliebiger Agent | ein Arbeitspaket auf **einem** Branch umsetzen, PR mit Pflichtangaben | eigenen PR freigeben oder mergen; fremde Branches ohne Übergabe ändern |
| **Reviewer** | anderer Agent **oder** frische Sitzung ohne Implementierungskontext | unabhängiges Review mit festen SHAs, Ergebnis als Datei in `docs/reviews/` | Code oder den geprüften PR ändern |

**Aktuelle Leitstelle:** Claude Code (seit 25.10.2026). **Aktueller Standard-Reviewer:** ChatGPT.
Ein Wechsel wird vom Nutzer entschieden und hier per PR eingetragen (Datum, neue Leitstelle, Grund).

## 2. Arbeitspaket übernehmen oder beginnen

Ein Arbeitspaket ist übernahmefähig, wenn in `docs/arbeitspakete.md` (oder im Draft-PR) steht:

- Ziel und Befundnummern, **Abnahmekriterien** mit Regressionstest
- Branch, **Base-SHA**, Dateigrenzen, Abhängigkeiten zu anderen Spuren
- Testanforderungen (Originaldaten nötig? Mehrsaisonprobe?)
- aktueller Stand: Draft-PR-Nummer, letzter Head-SHA, Bearbeiter

Ablauf für den übernehmenden Agenten:

1. `AGENTS.md`, diesen Text und den Abschnitt der Spur in `docs/arbeitspakete.md` lesen (Kurzaufruf und Ablauf:
   `docs/vorlagen/spur-start.md`).
2. Prüfen, ob es zum Branch einen offenen PR gibt. **Gibt es einen Bearbeiter ohne Übergabekommentar, nicht anfangen**
   – den Nutzer fragen.
3. Einrichten (AGENTS.md, Abschnitt 3), Branch auschecken, `git log -1` mit dem Head-SHA aus der letzten Übergabe
   vergleichen, `tools/pruefen.sh --schnell` ausführen. Erst wenn der Stand übereinstimmt und grün ist, weiterarbeiten.
4. Neues Paket: Branch von aktuellem `main`, sofort einen **Draft-PR** öffnen (Titel `Spur <name>: …`), in der
   Beschreibung `Bearbeiter: <Agent>` und Base-SHA eintragen. Der Draft-PR ist die Sperre für den Branch.
5. Fertig: PR-Beschreibung nach Vorlage ausfüllen, Draft-Status aufheben („Ready for review“), Leitstelle informieren.

## 3. Übergabe zwischen Agenten

**Jede Arbeitssitzung endet mit einem Übergabekommentar** im PR nach [docs/handoffs/VORLAGE.md](../handoffs/VORLAGE.md) –
nach erledigter Abarbeitung (Teil fertig, Befunde abgearbeitet, Warten auf Review) genauso wie bei einem Abbruch
(Kontingent erschöpft, Sitzung endet, Paket wird umverteilt). Bei einem absehbaren Abbruch wird er **vorher**
geschrieben. So kann jede neue Sitzung ohne Chatverlauf weitermachen. Inhalt:

- alles committet und gepusht; der Kommentar nennt den **Head-SHA** – lokale, nicht gepushte Änderungen gibt es nicht
- erledigt / offen / nächster konkreter Schritt
- Testtabelle aus `tools/pruefen.sh` zum genannten SHA
- bekannte Risiken, Annahmen, neue `[APPROX]`-Stellen
- `Bearbeiter: frei` (oder der vom Nutzer genannte Nachfolger)

Gestapelte oder zusammengehörige PRs einer Spur dürfen eine gemeinsame Übergabe im obersten PR haben; die übrigen PRs
verweisen mit einem kurzen Kommentar (Head-SHA, `Bearbeiter: frei`) darauf.

Kommentare statt Dateien, weil sie nicht mit anderen Branches kollidieren und am PR hängen bleiben. Ohne PR (z. B. reine
Analyse) wird die Übergabe als `docs/handoffs/<datum>-<thema>.md` auf einem eigenen Branch abgelegt.

Kann ein Agent nicht mehr übergeben (Abbruch ohne Kommentar), gilt der letzte gepushte Stand. Der Nachfolger
schreibt zuerst einen Übernahmekommentar („übernommen ab SHA …, Tests …“) und arbeitet erst dann weiter.

## 4. Parallele Arbeit ohne Überschreiben

- **Ein Branch – ein Bearbeiter.** Zwei Agenten am selben Paket arbeiten auf getrennten Branches
  (`paket/<spur>-<teil>`) mit getrennten PRs.
- Kein `git push --force` auf fremde Branches; auf eigenen nur `--force-with-lease` nach Rebase.
- Dateigrenzen der Spuren einhalten; Berührungen gemeinsamer Dateien im PR begründen.
- `docs/arbeitspakete.md` nur im eigenen Statusabschnitt ändern; Planabschnitte ändert die Leitstelle.

## 5. Unabhängiges Review

- Die Leitstelle gibt die Review-Anfrage als ausgefüllten **Kurzaufruf** aus `docs/vorlagen/review-anfrage.md`: PR,
  **Base- und Head-SHA**, zu wiederholende Gegenbeispiele, erwartet offene Befunde, Ablageort. Der Ablauf für den
  Reviewer steht in der Vorlage und wird nicht jedes Mal neu geschrieben.
- Reviewer ist nie die Sitzung, die umgesetzt hat. Hat derselbe Agent umgesetzt (z. B. ChatGPT), läuft das Review
  in einer **neuen** Sitzung, die den Implementierungsverlauf nicht kennt.
- Ergebnis: `docs/reviews/JJJJ-MM-TT-<thema>.md` auf Branch `review/<thema>`, PR mit nur dieser Datei. Ohne
  Schreibzugriff: Datei an den Nutzer, die Leitstelle legt sie ab.
- Ändert sich der Head nach dem Review, ist eine **Nachprüfung** des Deltas nötig (`…-nachpruefung.md`), außer im
  Fall „nur P2/P3“ der sparsamen Kette (unten).
- **Sparsame Kette (Entscheidung Nutzer 09.11.2026, Vorschläge 1 + 2):**
  1. **Erstreview** eines PR (Paket- oder Integrations-PR): vollständig (reviewt Claude: mit Claude Opus 5.5) – alle drei Läufe von
     `tools/pruefen.sh`, Mutationen, Pixelvergleich, Fehlerfälle/Fuzzing, Fundstellen in em.exe.
  2. **Fixlauf:** Prüfung mit `tools/pruefen.sh --schnell` (den Lauf ohne Daten übernimmt die CI).
  3. **Nachprüfung** nur, wenn der Fixlauf einen **P0- oder P1-Befund** behebt: nur das Delta, nur die Gegenbeispiele und
     Rückmutationen dieser Befunde (keine erneute Vollprüfung, kein neuer Pixelvergleich außerhalb der Befunde).
  4. Behebt der Fixlauf **nur P2/P3**: keine Nachprüfung. Die Leitstelle prüft selbst Delta (`compare`/`--stat`, betroffene
     Dateien), CI auf dem Head und die Testtabelle des Fixlaufs und vermerkt das im Merge-Kommentar
     („Delta geprüft durch Leitstelle, nur P2/P3“).
  5. **P3-Befunde** bekommen keinen eigenen Lauf: Die Leitstelle legt sie als Issue (Label `P3`) an und gibt sie gesammelt
     an `pruefung-karte` bzw. den letzten Lauf der Runde. Ein Fixlauf, der ohnehin läuft, darf P3 mitnehmen.
  6. Das **Gesamtreview** am Rundenende (ChatGPT, Vollprüfung) bleibt unverändert das letzte Netz.

  **Modellwahl (Ergänzung Nutzer 09.11.2026):** Claude Opus 5.5 bleibt überall, wo es Fehler und damit Nacharbeit
  vermeidet – Spielregeln, Spielkern, Disassembly, Import, Erstreviews und Nachprüfungen. Claude Sonnet 5.5 nur für
  mechanische Arbeit: Integrationen ohne eigene Regeländerung, reine Doku-Prüfungen, Kommentare und einfache
  Testergänzungen. Im Zweifel Opus. Die Leitstelle setzt das Modell danach selbst (`docs/vorlagen/leitstelle-start.md`).
- **Befunde als Issues** (seit 28.10.2026): Beim Auswerten legt die Leitstelle je Befund ein Issue an – Titel
  `<ID>: <Kurzbeschreibung>`, Labels `befund` und `P0`–`P3`, Link auf die Review-Datei. Der PR, der den Befund behebt,
  schließt das Issue (`Fixes #<nr>` in der Beschreibung). Pakete und Status bleiben in `docs/arbeitspakete.md`.
  Agenten ohne Zugriff auf Issues nennen die Befunde im PR; die Leitstelle legt die Issues an.

## 6. Merge-Prüfung (vor jedem Merge, von Leitstelle oder Nutzer)

GitHub Free erzwingt für private Repos **keine** dieser Regeln (Branch-Schutz und Rulesets liefern HTTP 403). Die Liste
ist deshalb verbindlich und wird im Merge-Kommentar abgehakt:

1. [ ] **Head-SHA** des PR = der SHA, den das Review freigegeben hat (bei Abweichung: Nachprüfung; behebt das Delta nur
   P2/P3, genügt die Delta-Prüfung der Leitstelle nach §5 „Sparsame Kette“ Punkt 4).
2. [ ] **Base aktuell:** PR ist gegen den aktuellen `main` gebaut bzw. ohne Konflikt; bei neuem `main` seit dem
   Review: Integrationstest lokal wiederholen.
3. [ ] **CI grün auf genau diesem Head** (Check-Runs des Head-SHA, nicht eines älteren Laufs). Ändert der PR nur `docs/**`
   oder `*.md`, startet die CI nicht (`build.yml`, `paths-ignore`): dann „entfällt, nur Doku“ abhaken, nachdem
   `compare`/`--stat` das bestätigt hat.
4. [ ] **Originaldaten-Nachweis:** Testtabelle aus `tools/pruefen.sh` mit Originaldaten zum Head (in PR oder Review);
   bei Integrations-PRs alle drei Läufe einmal (Integration oder Erstreview); nach einem Fixlauf genügt dessen
   `--schnell`-Tabelle mit Originaldaten zusammen mit grüner CI auf dem Head.
   Ein Review „nur Codelesen“ bzw. an einem unverifizierten Stand (`docs/vorlagen/review-anfrage.md`, Schritt 2) zählt
   dafür nicht.
5. [ ] **Review-Freigabe** liegt als Datei in `docs/reviews/` vor; offene P0/P1/P2-Befunde behoben oder vom Nutzer
   ausdrücklich zurückgestellt.
6. [ ] Kein Draft; Pflichtangaben vollständig; Restlücken in `docs/arbeitspakete.md` übertragen.
7. [ ] Merge **mit SHA-Bindung**: REST `PUT /repos/kaaasi/eishockey-manager-redone/pulls/<nr>/merge` mit
   `sha=<freigegebener Head>` – GitHub lehnt dann ab, wenn sich der Head inzwischen geändert hat. In der
   Weboberfläche gibt es diese Bindung nicht: Head-SHA auf der PR-Seite unmittelbar vor dem Klick vergleichen.
8. [ ] Danach: CI auf `main` prüfen; Review-PR mergen; Status in `docs/arbeitspakete.md` nachziehen.

## 7. Vertretung der Leitstelle

Ist die Leitstelle nicht verfügbar (z. B. Kontingent erschöpft), gilt:

1. Der Nutzer ernennt einen anderen Agenten ausdrücklich zur Leitstelle (Nachricht an den Agenten; Eintrag in
   Abschnitt 1 per PR, sobald möglich).
2. Die neue Leitstelle liest `AGENTS.md`, diesen Text, `docs/arbeitspakete.md` (aktuelle Runde), die offenen PRs mit
   ihren Übergabekommentaren und die letzten Reviews – mehr Wissen ist nicht nötig.
3. Sie arbeitet nach denselben Regeln: Merge-Prüfung (Abschnitt 6), Review durch eine andere Sitzung oder einen anderen
   Agenten. **Hat sie selbst umgesetzt, gibt sie nicht selbst frei.** Steht kein zweiter Agent bereit, entscheidet der
   Nutzer anhand des dokumentierten Review- und Teststands.
4. Kann der Agent nicht mergen (fehlende Rechte im Werkzeug), bereitet er die Merge-Prüfung als PR-Kommentar vor und
   der Nutzer merged in der Weboberfläche (Punkt 7 der Prüfliste beachten).
5. Kehrt die alte Leitstelle zurück, übernimmt sie per Übergabekommentar bzw. Eintrag in Abschnitt 1.

Es gibt **keine automatische Integration**, nur weil ein Kontingent erschöpft ist.

## 8. GitHub Free: was gilt, was nicht

| Funktion | Lage im privaten Repo (geprüft am 27.10.2026) | Folge |
|---|---|---|
| Branch-Schutz, Rulesets, Pflicht-Checks, Pflicht-Reviews | nicht verfügbar (API: HTTP 403 „Upgrade to GitHub Pro“) | Prozessregeln (Abschnitt 6), keine technische Sperre |
| Merge per REST mit `sha` | verfügbar | einzige serverseitige Bindung an den geprüften Head |
| Draft-PRs | verfügbar (#41, #42 sind Drafts) | Sperre für Branches und Zwischenstände |
| Issues, Labels, Milestones | verfügbar | Review-Befunde als Issues (Abschnitt 5) |
| GitHub Actions | 2.000 Minuten/Monat, 500 MB Speicher für Artefakte (stündlich gemessen) | Verbrauch beobachten; Artefakte sparsam (siehe Maßnahmenplan) |
| CODEOWNERS | Datei möglich, ohne Branch-Schutz aber wirkungslos | nicht eingesetzt |

Alle Agenten schreiben über dasselbe GitHub-Konto. Wer was gemacht hat, ist nur über Commit-Trailer, PR-Beschreibung
(`Bearbeiter:`) und Übergabekommentare nachvollziehbar – diese Angaben sind deshalb Pflicht.
