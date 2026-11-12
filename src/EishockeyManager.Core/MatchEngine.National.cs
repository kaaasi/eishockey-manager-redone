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
    ///   [ORIGINAL] Managerwert der Strafen (A084, Spur wettbewerbe-rest): <see cref="NationalPenaltyManagerValue"/>.
    ///   [APPROX] Eigener Managerwert 20 (wirkt nur bei Verletzungen, die das Nationalteam nicht hat); keine Verletzungen und
    ///   keine Über-/Unterzahl-Zähler (Satz des Bundestrainers nicht geführt).</item>
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
            PenaltyManagerValue = managed ? NationalPenaltyManagerValue(game) : 20,
            TacticsStrength = ownStrength,
            TacticsOpponentStrength = opponentStrength,
            FewFitPlayers = managed && coach!.Squad.Count(m => m.Player.Available) < MinimumFitForPenalties,
            ManagerValue = 20,
        };
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
}
