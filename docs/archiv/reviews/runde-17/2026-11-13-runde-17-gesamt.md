# Gesamtreview Runde 17 – 13.11.2026

## 1. Merge-Empfehlung

**Ja für Head `20e739c1cc8b36f4f794c5388a4364256f1b6fc4`.** Base: `3a8070c`.
Reviewer: ChatGPT/Codex, unabhängige Sitzung ohne Implementierungskontext.
Risikoorientierte Ausführung und Codeprüfung am exakt verifizierten, sauberen Checkout; kein reines Codelesen.
Keine neuen nachgewiesenen Produktfehler oder blockierenden Testlücken in den untersuchten Übergängen.
Die ausdrücklich zurückgestellten Punkte bleiben bestehen; dies ist keine Vollabnahme der Originaltreue.

## 2. Befunde

Keine neuen P0–P3-Befunde. Produktfehler und Testlücken werden nicht aus bloß ungeprüften Bereichen abgeleitet.

**Erwartet offen, bestätigt – keine neuen Befunde:**
- #730 (P2, Belastung) und #773 (P3, Strafminuten): GitHub offen; DOSBox-Aufnahme `runde-17` bleibt ausstehend.
- #803 (P3, Testlücke Börsen-Wachstumsrate), #804 (P3, HOK-Felder 0x26/0x2C), #805 (P3, Belastungsanzeige): GitHub offen.
- A037/A421 bleibt `[APPROX]`: Wettbewerbsteilnahme nach dem letzten Europapokal-/Aufstiegstermin ungeklärt.
- Live-Spiel L1–L8 bleibt Runde 18; die Analyse dieser Runde ersetzt dessen Umsetzung nicht.

## 3. Ausgeführte Prüfungen und akzeptierte `[APPROX]`

**Prüfstand:** `git rev-parse HEAD` = oben genannter Head, `git status --porcelain` leer, vor und nach der Prüfung.
SDK `10.0.402`; Originaldaten/FILE-Stände aus Referenz-Repo, Stand `a529b60eccf88dffb974ac06a3f1324580b15c3f`.
`PATH=/workspace/.dotnet:$PATH tools/pruefen.sh --schnell`: Release-Build mit Warnungen als Fehler.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3044 | 0 | 0 |

**Verlangte Gegenbeispiele, selbst erneut ausgeführt:**
Befehl jeweils `EM_REQUIRE_GAMEDATA=1 dotnet test tests/<Projekt> -c Release --filter '<Filter>'`.
Die Rückmutationen liefen getrennt vom geprüften Checkout, mit eigenem Build; danach vollständig zurückgesetzt.

| Risiko / Testfilter | Head | gezielte Rückmutation | Ergebnis |
|---|---|---|---|
| Core: `File30To31_ComputerClubs_ExactBaseEffortLikeOriginal` | 3 grün | `MatchEngine.Preparation.cs`: Gast vergleicht wieder mit altem Heim-Einsatz | 3 rot, jeweils 6 Gäste falsch |
| Core: `NewGame_LeaguePlan_FollowsSlotOrder` und `MultiSeason_FourChanges_PlanFollowsSlotOrder` | 4 grün | `NewGame.cs` und `GameState.Spielplan.cs`: wieder `Schedule.Rounds(members, rounds: 4)` | 4 rot, auch aktiv/passiv nach Saisonwechsel |
| UI: `Icons_YouthJoinedEvents_Symbol9Last` und `SummaryWindow_DrawsYouthJoinedFromCore` | 2 grün | `SeasonEndSummaryScreen.YouthJoined(...) => 0` | 2 rot; Zeichenprobe 494 abweichende Pixel |

**Untersuchte Risiken und Übergänge:**
- A113/A142 (#790): Liga-Plan über Satzplätze, Aufstiegsgruppe in Teilnehmerfolge, Rückspiele und Kalenderanschluss gelesen.
  `WettbewerbeRestTeil3Tests`: 6 grün, darunter vier Saisonwechsel aktiv/passiv und die Termine 6.2./10.2./20.2./25.2./22.3./29.3.
- Zusätzliche temporäre Probe `ReviewR17ProbeTests.FourSeasons_PromotionAndYouth_SaveLoadContinuation`: 2 grün.
  Zwei Manager (Vereine 2/14), Saat 17, vier Saisonwechsel aktiv/passiv: beide Aufstiegsgruppen vollständig (je 6 Runden,
  12 Ergebnisse), Ergebnisse in den vorgesehenen Paarungen; Speichern/Laden an beobachteten Rundenübergängen byteidentisch.
  An jedem Saisonende Original und geladene Kopie weitergespielt: nach dem Wechsel byteidentisch, einschließlich Kader/Geld/Zufall.
  Zeit ausschließlich über `Continue()`; automatisch übersprungene Tage anhand sämtlicher Gruppenergebnisse mitgeprüft.
- Saisonende/Jugend (#791): Aufruf vor dem Fenster, Jugendteam-Wechsel nur einmal, ungealterte Neuzugänge und Symbol je Zugang;
  Altern erst im folgenden Kaderwechsel. Schema 50, Migration 49→50 und Nachholmarke samt Löschung nachvollzogen.
  `KaderRestSaisonendeTests`, `KaderRestTests.SaveGame_PoolForm_RoundTripAndMigration` sowie die täglichen Roundtrip-Tests
  sind im Standardlauf enthalten; die Mehrsaison-Jugendprobe umfasst vier Wechsel, aktiv/passiv und Saaten 3/5/7.
- Transfers (#791): Besitzgrenze 29 einschließlich eigener Marktspieler und ausgeliehener eigener Spieler gelesen;
  erneute Prüfung bei Vertragsabschluss und Verkäuferantwort, ohne Geld-/Spielerbewegung bei Ablehnung.
  `KaderRestTransferTests` umfasst diese Grenzen sowie Verkauf aller Torhüter und vier Saisonwechsel ohne Mindestbesetzung.
- Finanzen: geänderte Kontoleiste und HOK-Export saldieren je Darlehenspartner; keine neue direkte Buchung am Finanzbuch vorbei.
  Spielvorbereitung (#794): Heim→Gast, gespeicherte Einsatz-/Angriffsfelder und erneuter Stärkevergleich am Aufrufer geprüft.
- Bedienung: Liga↔Play-Off/Aufstieg, gemeinsame Seitenwahl, Vereinsfenster und zustandsfreies Zeichnen der neuen Seiten gelesen;
  `LigapaarungenSeitenTests`, Vertragsabsage- und Saisonende-Anzeigetests im Standardlauf.

**Originalbelege, gezielt selbst geprüft:**
`objdump -D -b binary -m i8086 --start-address=<von> --stop-address=<bis> gamedata/em.exe`:
- `0x4E7C7–0x4E89A`: Buchstabe minus `'a'`, Satzbasis und Heimtausch nach n−1 Tagen → A113.
- `0x31A2C–0x31A65`: Termin nur bei 0/2 im Fünferblock → A142.
- `0x36CE0–0x36D62`: Aufruf 0x8365 zuerst Heimbyte 0x5D7B, danach Gastbyte 0x5D7C → #794.
- `0x2728C–0x27320`: Jahresgrenze, Jugendzugang, Symbol 9 nur bei Erfolg.
- `0x42CF7–0x42D45` und `0x46FD4–0x470E5`: Besitzprüfung mit Vereinsausnahme bzw. Verkaufszweig ohne Mindestbestand je Art.
`dotnet run --project src/EishockeyManager.Tools -c Release --no-build -- screens gamedata <ziel> pixel-aufstieg`:
Übersicht `folge07_03307` visuell gegen das Originalbild geprüft: Paarungen, Hin-/Rücktermine und Tabelle stimmen überein.
Der erste Versuch mit `disasm.py` war mangels `ndisasm` blockiert; obige Prüfung erfolgte anschließend mit `objdump`.

**Akzeptierte `[APPROX]`:** A037/A421; erhaltene Zufallszüge der früheren Planmischung (A422); Computer-Kaderregeln;
Poolform älterer Stände = Kaderform; Nachholen alter Saisonende-Stände ohne nachträgliches Ereignissymbol;
bekannte Grenzen der Spielvorbereitung (Reihenwahl/Manageraufstellung) und Seitenwahl nach dem Laden.
**Grenzen/Annahmen:** Keine neue DOSBox-Aufnahme, kein vollständiger Pixelabgleich aller Seiten, keine Vollinventur oder
Einzelrückführung sämtlicher Saaten/Abdrücke; Live-Spiel-Analyse L1–L8 und übrige unveränderte Regeln nicht voll nachgeprüft.
Prämien-/Kontoprüfungen und Mehrsaisonläufe belegen die untersuchten Übergänge, nicht bitgenaue Original-Zufallsfolgen.
