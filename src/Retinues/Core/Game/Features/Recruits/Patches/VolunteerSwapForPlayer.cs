using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using HarmonyLib;
using Retinues.Core.Game;
using Retinues.Core.Game.Helpers;
using Retinues.Core.Utils;

[HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "game_menu_recruit_volunteers_on_consequence")]
internal static class VolunteerSwapForPlayer_Begin
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        VolunteerSwapForPlayerSession.BeginIfNeeded();
    }
}

[HarmonyPatch(typeof(TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment.RecruitmentVM), "Deactivate")]
internal static class VolunteerSwapForPlayer_End_Deactivate
{
    [HarmonyPostfix] private static void Postfix() => VolunteerSwapForPlayerSession.End();
}

[HarmonyPatch(typeof(TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment.RecruitmentVM), "OnFinalize")]
internal static class VolunteerSwapForPlayer_End_Finalize
{
    [HarmonyPostfix] private static void Postfix() => VolunteerSwapForPlayerSession.End();
}

internal static class VolunteerSwapForPlayerSession
{
    private static readonly Dictionary<Hero, CharacterObject[]> _backup = [];
    private static Settlement _settlement;

    public static bool IsActive => _settlement != null;

    public static void BeginIfNeeded()
    {
        // Only if config is set that way
        if (!Config.GetOption<bool>("VolunteerSwapForPlayer")) return;

        if (IsActive) End(); // safety

        var clan = Player.Clan;
        if (clan == null) return;

        var s = Hero.MainHero?.CurrentSettlement ?? Settlement.CurrentSettlement;
        if (s == null) return;
        _settlement = s;

        // Stash originals and swap to clan equivalents (player only)
        foreach (var notable in s.Notables)
        {
            if (notable?.VolunteerTypes == null) continue;

            var original = (CharacterObject[])notable.VolunteerTypes.Clone();
            _backup[notable] = original;

            for (int i = 0; i < notable.VolunteerTypes.Length; i++)
            {
                var vanilla = notable.VolunteerTypes[i];
                if (vanilla == null) continue;

                // Skip if already a clan troop
                if (CharacterObjectHelper.IsFactionTroop(clan, vanilla)) continue;

                var root = CharacterObjectHelper.GetFactionRootFor(vanilla, clan);
                if (root == null) continue;

                notable.VolunteerTypes[i] = CharacterObjectHelper.TryToLevel(root, vanilla.Tier);
            }
        }

        Log.Debug($"[VolunteerSwapForPlayer] Temporary swap applied at {s.Name}.");
    }

    public static void End()
    {
        if (!IsActive) return;

        foreach (var kv in _backup)
        {
            var notable = kv.Key;
            var orig = kv.Value;
            if (notable?.VolunteerTypes == null || orig == null) continue;

            var n = System.Math.Min(notable.VolunteerTypes.Length, orig.Length);
            for (int i = 0; i < n; i++)
                notable.VolunteerTypes[i] = orig[i];
        }

        _backup.Clear();
        Log.Debug($"[VolunteerSwapForPlayer] Swap restored at {_settlement?.Name}.");
        _settlement = null;
    }
}
