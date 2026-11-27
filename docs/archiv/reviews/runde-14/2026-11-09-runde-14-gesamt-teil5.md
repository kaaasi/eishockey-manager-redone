# Gesamtreview Runde 14 – produktorientierter Prüfstand

## Gegenstand und Empfehlung

Geprüft wird das Rundendelta von Base `401efa7` bis Head `b13046422b285404ad6ddbe7b5f1b18e22069eea`. Auf Nutzerwunsch vom 10.11.2026 konzentriert sich dieses Review auf **konkrete Produktfehler und relevante Abweichungen vom Original**. Die zuvor verlangte vollständige Rückmutation jedes Tests, Sollwerts und jeder Saat sowie der lückenlose Nachweis aller Originalstellen und Bildschirmzustände werden auf eine abschließende Prüfung nach der Gesamtumsetzung verschoben.

**Merge-Empfehlung: noch offen.** Die bis jetzt ausgeführten Prüfungen haben keinen neuen Produktfehler nachgewiesen; sie rechtfertigen aber noch keine pauschale Freigabe des gesamten Deltas. Für eine abschließende Empfehlung sind die verbleibenden risikoreichen Produktpfade gezielt zu prüfen. Die bloße Existenz nicht durchmutierter Tests oder nicht einzeln nachgelesener Belege ist nach dem geänderten Auftrag kein automatischer Ablehnungsgrund.

## Konkrete Ergebnisse

- **R683:** Die geprüfte Halbfinalpaarung ist Sieger 1–8 gegen Sieger 3–6 und Sieger 2–7 gegen Sieger 4–5. Die zugehörigen Import- und Migrationstests bestanden.
- **R688S-01:** Die geprüfte normale Saison endet mit dem letzten Tagesabschluss am 29. April. Die Sommerpause beginnt am 1. Mai und enthält vier Monatsabschlüsse bis zum 9. September; ein regulärer Tagesabschluss am 30. April wird nicht gerechnet.
- **R688S-03:** In den ausgeführten Schuldenläufen blieb der Manager im Spiel und ein Zwangsabstieg trat ein. Eine Gegenprobe mit abgeschaltetem Zwangsabstieg wurde für beide geprüften Saaten rot.
- **FILE44 → FILE46:** Die Originalstände ergeben **−2.139.514 DM**, das Remake in der geprüften Konstellation **−2.106.011 DM**, also eine Abweichung von **33.503 DM**. Die Differenz ist ein tatsächliches Vergleichsergebnis; ihre einzelnen Ursachen wurden nicht vollständig zugeordnet. Die gesondert bekannte April-Abweichung von 115.000 DM bleibt als `[APPROX]` offen und wurde hier nicht als neuer Befund bewertet.
- **Markierungstag, Nutzerentscheidung C:** Der Import erkennt die einschlägigen Stände und setzt den Ladeversatz im Spielkern. Der gezielte FILE44-Test und die Gegenprobe ohne Versatz bestätigen den untersuchten Pfad; weitere Stände sind damit nicht pauschal bewiesen.
- **Originalbilder:** Einzelne Pixel von Historie und VALLIERE-Kauffenster wurden anhand der DOSBox-Bilder unabhängig nachgemessen. Die untersuchten Positionen und Farben stimmen mit den im Bericht beschriebenen Bildpunkten überein; dies ist kein vollständiger Bildvergleich aller Oberflächen.

Die erwarteten offenen Punkte [#620](https://github.com/kaaasi/eishockey-manager-redone/issues/620) (Rest), [#698](https://github.com/kaaasi/eishockey-manager-redone/issues/698), [#699](https://github.com/kaaasi/eishockey-manager-redone/issues/699), [#703](https://github.com/kaaasi/eishockey-manager-redone/issues/703) und der `[APPROX]` April-Abschluss wurden im ersten Bericht bestätigt; sie werden nicht als neue Befunde gezählt.

## Produktbefunde und verbleibende Prüfung

**Keine neuen reproduzierten Produktfehler festgestellt.** Die bisherigen Befunde R14G2-01/02, R14G3-01 bis -03, R14G4-01 bis -08 und R14G5-01 betreffen nach den vorliegenden Gegenproben die Aussagekraft von Tests. Die Mutationen liefen in einer isolierten Kopie; sie belegen für sich keinen Fehler des unveränderten Heads. Besonders relevant für spätere gezielte Tests sind die Lücken bei Import der Ranghistorie, vorzeitigem Spielende in Mehrsaisonproben und dem WM-Ablauf nach dem ersten Termin. Details bleiben in den früheren Berichten und im unten verlinkten ausführlichen Prüfstand erhalten.

Für das zugeschnittene Review bleiben diese produktbezogenen Arbeiten:

1. Die Änderungen an Originalimport und Spielstandmigration auf konkrete Datenverluste oder falsche Zustandsübergänge prüfen, insbesondere bei Markierungstag, Liga-/Play-off-Zuordnung und Kader/Leihe.
2. Den Tages- und Saisonwechsel einschließlich Sommerpause, Finanzen, Trainings- und Managerwechsel auf konkrete Fehler untersuchen; die FILE44-Kontodifferenz nur soweit weiter zerlegen, wie es zur Einordnung einer relevanten Abweichung nötig ist.
3. Aufstellungs-, Taktik- und wesentliche Menüabläufe auf tatsächliche Änderungen des Spielzustands und erlebbare Abweichungen prüfen. Bei begründetem Verdacht Originaldaten oder Screenshots gezielt heranziehen.
4. Anschließend die Merge-Empfehlung anhand konkreter Befunde und ihrer Auswirkungen neu bewerten. Verbleibende Unsicherheiten und akzeptierte Näherungen benennen.

## Nachweise

Der Standardlauf auf dem unveränderten Head war erfolgreich: mit Originaldaten **2.625 bestanden, 0 fehlgeschlagen**; ohne Originaldaten **2.076 bestanden, 0 fehlgeschlagen, 452 übersprungen**. Der Pflichtlauf ohne Daten scheiterte wie vorgesehen. Die gezielte Ausführung der angeforderten Gegenbeispiele bestand mit **20 Tests**. Einzelheiten, Prüfumgebung und Grenzen stehen im [ersten Bericht](https://github.com/kaaasi/eishockey-manager-redone/blob/a9ef4ebc07f5165f85df084fef3dc084eed7ff8b/docs/reviews/2026-11-09-runde-14-gesamt.md).

Die bisherigen ausführlichen Inventuren, 1.450 Gegenprobenergebnisse, Originalstellen und Einzeltabellen werden nicht verworfen: Sie sind im [gesicherten ausführlichen Prüfstand von Teil 5](https://github.com/kaaasi/eishockey-manager-redone/blob/88cc769cd33452364f1db29d3609d92abdb8f77a/docs/reviews/2026-11-09-runde-14-gesamt-teil5.md) und in den Vorberichten dokumentiert. Sie können für die spätere Abschlussprüfung verwendet werden.
