namespace EishockeyManager.Core;

/// <summary>Nationalteams in der Minutenrechnung (Spur wettbewerbe-spiel, Teil 3; <c>docs/spielberechnung.md</c> §20).</summary>
public static partial class MatchEngine
{
    /// <summary>
    /// Mannschaft eines Nationalteams für die Minutenrechnung.
    /// <list type="bullet">
    /// <item>[ORIGINAL] Teamwerte und Tagesform: <see cref="GameState.NationalTeamValues"/> (Vereinsfelder 0x17–0x1D des
    ///   Vereinsplatzes 52–63, Lader Datei 0x2F0CC–0x2F120 ohne Streuung). Reihen, Energie und Abschlüsse wie bei jedem
    ///   Verein (0x977F, 0x368B1).</item>
    /// <item>[ORIGINAL] Länder ohne Manager (Vereinsfeld 0x16 = 0, auch Deutschland ohne Bundestrainer): Einsatz und
    ///   Angriff aus der Automatik der Spielvorbereitung 0x8365 „ohne Blick auf die Modi“ (R8-B04).</item>
    /// <item>[ORIGINAL] Land des Bundestrainers (0x16 = 6): Managerverein mit Kader (<see cref="NationalSquad"/>), Taktik
    ///   des Nationalteams (<see cref="GameState.NationalTactics"/>; Einsatz 0x977F/0x98F8, Taktik 0x939A, Reihenwechsel
    ///   0x8EF2), Strafspieler aus dem Kader (0x378C5, R8W3I-06), unter 8 fitten Kaderspielern keine Strafen (0x358FF).
    ///   [ORIGINAL] Managerwert der Strafen (A084, Spur wettbewerbe-rest): <see cref="NationalPenaltyManagerValue"/>.</item>
    /// <item>[ORIGINAL] Satz des Bundestrainers (Manager 5, A424/A097, Spur national Teil 1; <see cref="NationalCoach.ManagerRecord"/>):
    ///   Verletzungen 0x38B8F mit Satz 5 (Datei 0x38BE3–0x38BF0: nur mit Verein DGROUP 0x9EDA ≤ 0x3F, also im Amt; nach den
    ///   Vereinsmanagern), eigener Managerwert DGROUP 0x974B + 5 · 0x1AF aus 0x952C mit Manager 5 (<see cref="NationalManagerValue"/>)
    ///   und die Zähler der Über-/Unterzahl im Satz 5 (<see cref="NationalCoach.PowerPlayCounts"/>, Vereinsfeld 0x16 = 6).
    ///   [ORIGINAL] Verletzt wird der Kaderplatz in Satz 5 (DGROUP 0x5DC3 + 5 · 0x618, Datei 0x38D18–0x38D2C), der beim
    ///   Saisonwechsel entfällt (Datei 0x557B0; eigener Platz seit Teil 3, A412).</item>
    /// <item>[ORIGINAL] Einsatz 0x26 und Angriff 0x2C setzt die Spielvorbereitung 0x8365 auch für Länder (A494,
    ///   <see cref="PrepareNationalMatch"/>); hier stehen die eigenen Werte beim Stand 0:0.</item>
    /// </list>
    /// </summary>
    /// <param name="ownStrength">Stärke 0x368B1 des Landes für die Automatik (0x8365, doppelt verglichen).</param>
    /// <param name="opponentStrength">Stärke 0x368B1 des Gegners.</param>
    /// <param name="managerMatch">Managerpaarung (0x130:0x4D): Kader und Taktik je Minute.</param>
    internal static LiveTeam NationalLiveTeam(GameState game, int nation, int ownStrength, int opponentStrength, bool managerMatch)
    {
        var (defense, attack, dayForm) = game.NationalTeamValues(nation);
        var coach = game.NationalCoach is { } c && c.Nation == nation ? c : null;
        var managed = coach is not null;
        var tactics = managed ? coach!.Tactics : new Tactics();
        var squad = managed ? NationalSquad(game, coach!) : null;
        var squadOfManager = managerMatch ? squad : null;
        var team = new LiveTeam
        {
            Defense = defense,
            Attack = attack,
            ClubForm = dayForm,
            Effort = tactics.EffectiveEffort(0, 0, ownStrength, opponentStrength),
            AttackLevel = tactics.EffectiveAttack(0, 0, ownStrength, opponentStrength),
            Energy = PreparedEnergy,
            Tactics = managerMatch ? tactics : null,
            Managed = managed,
            LineChange = managed ? tactics.LineChange : null,
            Squad = managerMatch ? squad : null,
            // A837/A840: [ORIGINAL] 0x35AB8–0x35B2C wählt den Strafspieler (0x378C5) auch ohne Reihe (Vereinsfeld 0x16 = 6).
            PenaltySquad = managerMatch ? null : squad,
            PenaltyManagerValue = managed ? NationalPenaltyManagerValue(game) : 20,
            TacticsStrength = ownStrength,
            TacticsOpponentStrength = opponentStrength,
            FewFitPlayers = managed && coach!.Squad.Count(m => m.Player.Available) < MinimumFitForPenalties,
            ManagerValue = squadOfManager is null ? 20 : NationalManagerValue(squadOfManager),
            InjuryManager = squadOfManager is null ? null : NationalCoach.ManagerRecord,
            PowerPlayCounts = managed ? coach!.PowerPlayCounts : null,
        };
        // [ORIGINAL] 0x35691 zählt beim Freiwerden neu (0x38A36); wie bei den Managervereinen (LiveTeamOf).
        if (managed)
            team.Box.Released = () => RecountSkaters(team);
        return team;
    }

    /// <summary>
    /// Managerwert des Bundestrainers (A424). [ORIGINAL] 0x952C mit Manager 5 schreibt DGROUP 0x974B + 5 · 0x1AF wie für jeden
    /// Manager (<see cref="ManagedTeamValues"/>: clamp(d / 5 + 20, 0, 40), bei weniger als neun aufgestellten und eingesetzten
    /// Kaderplätzen 100, Datei 0x9690–0x96A8). [APPROX] Wie bei den Vereinen aus dem Stand des Spiels gerechnet (zwischen
    /// Tageslauf und Spiel ändert sich die Aufstellung nicht; R8W3I-05).
    /// </summary>
    public static int NationalManagerValue(LiveSquad squad) =>
        ManagedTeamValues(squad, nineOrMore: OriginalMinutes.LineupPlayers(squad) >= GameState.MinimumPlayers).ManagerValue;

    /// <summary>
    /// Spielvorbereitung 0x8365 eines Länderspiels (A494, Spur national Teil 1). [ORIGINAL] Der Spieltagsablauf 0x36B5D ruft
    /// 0x8365 für jede Paarung der Wettbewerbe (Datei 0x36F27 Heim, 0x36F5A Gast; WM-Gruppe, WM-Endrunde und
    /// Freundschaftsspiel 0x30365 laufen über 0x36B5D) und nimmt Länder nicht aus – Ablauf wie bei den Vereinen
    /// (<see cref="PrepareMatch"/>, #772/#784):
    /// <list type="number">
    /// <item>Stärke 0x368B1 beider Länder mit dem stehenden Einsatz 0x26 aus ihrem letzten Spiel
    ///   (<see cref="GameState.NationalMatchStrength"/>; Datei 0x9C66), zuerst Gast, dann Heim; der Gast vergleicht danach mit
    ///   dem schon geschriebenen Einsatz des Heimlandes.</item>
    /// <item>Ohne Bundestrainer (Vereinsfeld 0x16 = 0) oder bei Einsatz „AUTOM.“: 0x26 = Grundwert (60 bzw. 40); Angriff „AUTOM.“
    ///   bzw. ohne Bundestrainer: die Bits 4–6 von 0x2C bleiben aus dem letzten Spiel (<see cref="Tactics.MatchAttack"/>).</item>
    /// <item>Zeile der Automatik für die Anzeige „AUTOM.“ (<see cref="Tactics.RecordAutoLine"/>, #192).</item>
    /// </list>
    /// Beleg: DOSBox FILE18 → FILE19 (Freundschaftsspiel DEUTSCHLAND – SCHWEDEN, Sätze 54 und 59: 0x2D = 40 bzw. 60, Zuschläge und
    /// Angriffszeilen wie oben; Test <c>NationalTeil1Tests.File18_FriendlyAgainstSweden_PreparesBothNationsLikeFile19</c>).
    /// [APPROX] Wie bei den Vereinen behält der Bundestrainer mit „SEMI“/„MANUELL“ die eigenen Werte beim Stand 0:0.
    /// </summary>
    internal static void PrepareNationalMatch(GameState game, Club home, Club away, LiveTeam homeTeam, LiveTeam awayTeam)
    {
        var homeTactics = game.NationFieldTactics(home.Index);
        var awayTactics = game.NationFieldTactics(away.Index);
        var awayStrength = game.NationalMatchStrength(away.Index);
        var homeWritten = PrepareNation(game, home.Index, homeTactics, homeTeam, game.NationalMatchStrength(home.Index), awayStrength);
        var homeField = homeWritten ? homeTeam.Effort : homeTactics.MatchEffort;
        PrepareNation(game, away.Index, awayTactics, awayTeam, awayStrength, game.NationalMatchStrength(home.Index, homeField));
    }

    /// <summary>Vorbereitung eines Landes; true, wenn 0x8365 den Grundwert in 0x26 schreibt (ohne Bundestrainer oder „AUTOM.“).</summary>
    private static bool PrepareNation(GameState game, int nation, Tactics tactics, LiveTeam team, int own, int opponent)
    {
        var unmanaged = game.NationalCoach?.Nation != nation;
        tactics.RecordAutoLine(own, opponent, unmanaged);
        team.TacticsStrength = own;
        team.TacticsOpponentStrength = opponent;
        var writesEffort = unmanaged || tactics.EffortMode == TacticMode.Auto;
        if (writesEffort)
            team.Effort = Tactics.PreparedEffort(own, opponent);
        if (unmanaged || tactics.AttackMode == TacticMode.Auto)
            team.AttackLevel = tactics.MatchAttack;
        return writesEffort;
    }

    /// <summary>
    /// Managerwert der Strafen des Bundestrainer-Teams. [ORIGINAL] Die Strafen lesen den Wert über Vereinsfeld 0x16
    /// (Datei 0x35883–0x358A0: DGROUP 0x974B + 0x1AF · Feld 0x16, dazu −20, begrenzt auf 0 … 100 mit 0x4BD35). Das Land des
    /// Bundestrainers hat 0x16 = 6 (Manager 5 + 1, wie bei den Vereinen der „nächste“ Satz), gelesen wird also
    /// DGROUP 0x974B + 6 · 0x1AF = 0xA165 – hinter den sechs Managersätzen (0x965B + 6 · 0x1AF = 0xA075) und den
    /// Jugendteams (0xA075, 84 Byte) im Ergebnisblock 0xA0CB: 0xA165 − 0xA0CB = 0x9A = 12 · (13 − 1) + 2 · 5, also die
    /// <b>Heimtore der sechsten Paarung des 13. Bundesliga-Spieltags</b> (Schreiber 0x369C7, Datei 0x36A54; Lage wie
    /// <c>OriginalExport.WriteLeague</c>). Ein Querverweis auf 0xA160–0xA170 fehlt im ganzen Programm (disasm.py --xref),
    /// der Wert entsteht nur über diese Adressrechnung. Nicht gespielt (oder nach dem Saisonwechsel geleert, Datei 0x565C2):
    /// 0. [APPROX] Ergebnismarken des Originals (z. B. 0x5A bei Wertung, 0x36A05) führt das Remake nicht; es nimmt die Tore
    /// aus <see cref="GameState.Results"/>, begrenzt auf ein Byte.
    /// </summary>
    public static int NationalPenaltyManagerValue(GameState game)
    {
        const int matchDay = 13, pairing = 5;
        if (!game.Schedules.TryGetValue(League.Bundesliga, out var plan) || plan.Count < matchDay
            || plan[matchDay - 1].Matches.Count <= pairing)
            return 0;
        var (home, away) = plan[matchDay - 1].Matches[pairing];
        var result = game.Results.FirstOrDefault(r =>
            r.League == League.Bundesliga && r.MatchDay == plan[matchDay - 1].Number && r.Home == home && r.Away == away);
        return result is null ? 0 : Math.Clamp(result.HomeGoals, 0, 255);
    }

    /// <summary>
    /// Kader des Nationalteams für die Minutenrechnung. [ORIGINAL] Kaderplätze des Bundestrainers (Manager 5): Platz +0x15
    /// aus der Aufstellung (alle vier Blöcke, Block b = Reihe b; 0x8E5F/0x91FF zählen eine vierte Reihe, A022), eingesetzt
    /// +0x0A wie im Aufstellungsbildschirm (<see cref="NationalCoach.IsDressed"/>), Technik + Länderspiele / 8 im Spiel
    /// (0x98F8, <see cref="LiveSquad.NationalCoach"/>). [ORIGINAL] Ohne „automatisch aufstellen“ spielt eine leere Aufstellung leer
    /// (A009, FILE48 0:17). Mit der Einstellung und leerer Aufstellung die Aufstellung der Neuaufstellung 0x426BC
    /// (<see cref="NationalCoach.StandInFormation"/>, A024); „5 − Level“ (DGROUP 0xED8)
    /// aus dem Level des Managers, der Bundestrainer ist. [ORIGINAL] Reihenfolge der Kaderplätze wie der WM-Kader: nach „auto“
    /// Plätze 0–2 Torhüter, 3–12 Verteidiger, 13–28 Stürmer, je Art absteigend nach Summe (A085, DOSBox FILE18 Satz 5;
    /// <see cref="GameState.AutoNominate"/>).
    /// </summary>
    public static LiveSquad NationalSquad(GameState game, NationalCoach coach)
    {
        var manager = coach.Manager < game.Managers.Count ? game.Managers[coach.Manager] : null;
        var squad = new LiveSquad
        {
            Difficulty = manager is null ? 2 : TrainingRules.LevelValue(manager.Level),
            Tactics = coach.Tactics,
            NationalCoach = true,
        };
        var positions = new Dictionary<Player, int>();
        // A024: leere Formation mit Einstellung → Aufstellung der Neuaufstellung 0x426BC (Plätze und eingesetzt wie dort).
        var standIn = coach.UsesStandInLineup ? coach.StandInFormation() : default;
        var formation = standIn.Formation ?? coach.Formation;
        foreach (var slot in LineupSlot.All)
            if (formation[slot] is { } p && coach.Contains(p))
                positions[p] = LiveSquad.PositionOf(slot);
        foreach (var member in coach.Squad)
        {
            var p = member.Player;
            squad.Players.Add(new LivePlayer
            {
                Player = p,
                Position = positions.GetValueOrDefault(p),
                Dressed = p.Available && (standIn.Dressed is { } dressed ? dressed.Contains(p) : coach.IsDressed(p)),
                Energy = PreparedPlayerEnergy(p),
            });
        }
        return squad;
    }

    /// <summary>
    /// Zähler nach einem WM-Spiel des Landes des Bundestrainers (A628, Spur national Teil 2). [ORIGINAL] Der WM-Lauf 0x30E7B setzt den
    /// Torindex DGROUP 0xD0BF = 0 (Datei 0x30EA6) und rechnet ab mit Wettbewerb 8 (Datei 0x30F5F/0x31025); die Abrechnung 0x544C0
    /// zählt am Kaderplatz des Satzes 5 (DGROUP 0x5DC3 + 5 · 0x618) Spiele in +0x08/+0x20 (Europapokal, Datei 0x5494C–0x5499E), das
    /// Poolfeld Spiele +0x1C nur bei Wettbewerb 0 (Datei 0x5492E–0x54948), Tore in +0x03/+0x22 (Datei 0x542F4–0x54333), Strafen in +0
    /// bzw. +1 des Platzes aus 0x378C5 (Datei 0x35B7D–0x35B8E). Die Vereinsplätze der Spieler bleiben unberührt; am Spieler selbst
    /// (Poolfelder) zählen nur Tore +0x1B (Datei 0x542C1–0x542F0) und Vorlagen +0x1F (Datei 0x5424E–0x5427A).
    /// [ORIGINAL] Seit Teil 3 (A412) führt das Remake den Satz 5 als eigene Kaderplätze (<see cref="NationalSquadMember"/>): Spiele
    /// zählen dort im Europapokal-Feld (+0x08/+0x20), Tore in der Liga (+0x03/+0x22), Strafen am Platz; Vorlagen, Gegentore und
    /// Minuten des Torhüters am Spieler im Verein/Pool (Poolfelder +0x1F). [APPROX] Das Spielerfeld Tore +0x1B führt das Remake nicht
    /// getrennt (siehe <c>CreditLineup</c>); Gegentore stehen im Remake am Spieler (Poolfeld +0x1B des Torhüters nicht gelesen).
    /// Wie jede Abrechnung 0x544C0 begrenzt sie danach die Belastung der Plätze.
    /// </summary>
    internal static void CreditWorldCupSquad(NationalCoach coach, int against, int side, MatchReport report)
    {
        // [ORIGINAL] Kaderplatzschleife Datei 0x548A8–0x549BB über alle Plätze des Satzes 5: Belastung auf 50 … 150 (0x549A2).
        TrainingRules.ClampStrain(coach.Squad.Select(m => m.Player));
        var national = coach.Lineup();
        foreach (var p in national.Players)
            p.CountGame(CompetitionSlot.Europe); // +0x08/+0x20 (Datei 0x5494C–0x5499E, Wettbewerb 8)
        if (national.Goalkeeper is { } goalie)
        {
            var keeper = coach.SourceOf(goalie);
            keeper.GoalsAgainst += against;
            keeper.GoalieMinutes += report.Overtime ? 70 : 60;
        }
        foreach (var e in report.Events)
        {
            if (e.Side != side)
                continue;
            if (e is { Kind: MatchEventKind.Goal, Player: { } scorer })
            {
                scorer.CountGoal(CompetitionSlot.League); // Torindex 0 (Datei 0x30EA6): +0x03/+0x22 (Datei 0x542F4–0x54333)
                if (e.Assist is { } assist)
                    coach.SourceOf(assist).Assists++; // Poolfeld +0x1F (Datei 0x5424E–0x5427A)
            }
            else if (e is { Kind: MatchEventKind.Penalty, Player: { } penalized })
            {
                // [ORIGINAL] Datei 0x35B7D–0x35B8E: +0 (5 Minuten) bzw. +1 (2 Minuten) des Platzes aus 0x378C5.
                if (e.PenaltyMinutes == 5)
                    penalized.Penalties5++;
                else
                    penalized.Penalties2++;
            }
        }
    }
}
