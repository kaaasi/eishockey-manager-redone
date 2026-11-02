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
    ///   [APPROX] Managerwert der Strafen und eigener Managerwert 20: das Original liest DGROUP 0x974B + 6 · 0x1AF hinter
    ///   den sechs Managersätzen (nicht geprüft, A084); keine Verletzungen und keine Über-/Unterzahl-Zähler (Satz des
    ///   Bundestrainers nicht geführt).</item>
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
        var squad = managed && managerMatch ? NationalSquad(game, coach!) : null;
        return new LiveTeam
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
            Squad = squad,
            PenaltyManagerValue = 20,
            TacticsStrength = ownStrength,
            TacticsOpponentStrength = opponentStrength,
            FewFitPlayers = managed && coach!.Squad.Count(m => m.Player.Available) < MinimumFitForPenalties,
            ManagerValue = 20,
        };
    }

    /// <summary>
    /// Kader des Nationalteams für die Minutenrechnung. [ORIGINAL] Kaderplätze des Bundestrainers (Manager 5): Platz +0x15
    /// aus der Aufstellung (alle vier Blöcke, Block b = Reihe b; 0x8E5F/0x91FF zählen eine vierte Reihe, A022), eingesetzt
    /// +0x0A wie im Aufstellungsbildschirm (<see cref="NationalCoach.IsDressed"/>), Technik + Länderspiele / 8 im Spiel
    /// (0x98F8, <see cref="LiveSquad.NationalCoach"/>). [APPROX] Ohne eigene Aufstellung (passiver Bundestrainer) die
    /// Automatik <see cref="NationalCoach.Lineup"/> mit drei Reihen, alle verfügbaren eingesetzt; „5 − Level“ (DGROUP 0xED8)
    /// aus dem Level des Managers, der Bundestrainer ist; Reihenfolge der Kaderplätze wie der WM-Kader (nicht gelesen).
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
        var own = coach.Formation.Count > 0;
        if (own)
        {
            foreach (var slot in LineupSlot.All)
                if (coach.Formation[slot] is { } p && coach.Contains(p))
                    positions[p] = LiveSquad.PositionOf(slot);
        }
        else
        {
            var lineup = coach.Lineup();
            if (lineup.Goalkeeper is { } g)
                positions[g] = 1;
            for (var i = 0; i < lineup.Defenders.Count; i++)
                if (lineup.Defenders[i] is { } d)
                    positions[d] = i / Lineup.DefendersPerBlock + 1;
            for (var i = 0; i < lineup.Forwards.Count; i++)
                if (lineup.Forwards[i] is { } f)
                    positions[f] = 0x80 | (i / Lineup.ForwardsPerBlock + 1);
        }
        foreach (var member in coach.Squad)
        {
            var p = member.Player;
            squad.Players.Add(new LivePlayer
            {
                Player = p,
                Position = positions.GetValueOrDefault(p),
                Dressed = p.Available && (!own || coach.IsDressed(p)),
                Energy = PreparedPlayerEnergy(p),
            });
        }
        return squad;
    }
}
