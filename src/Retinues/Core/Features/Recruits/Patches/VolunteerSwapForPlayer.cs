using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Core.Features.Recruits;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

[HarmonyPatch(
    typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.PlayerTownVisitCampaignBehavior),
    "game_menu_recruit_volunteers_on_consequence"
)]
internal static class VolunteerSwapForPlayer_Begin
{
    [HarmonyPostfix]
    private static void Postfix() => VolunteerSwapForPlayerSession.BeginIfNeeded();
}

[HarmonyPatch(
    typeof(TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment.RecruitmentVM),
    "Deactivate"
)]
internal static class VolunteerSwapForPlayer_End_Deactivate
{
    [HarmonyPostfix]
    private static void Postfix() => VolunteerSwapForPlayerSession.End();
}

[HarmonyPatch(
    typeof(TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment.RecruitmentVM),
    "OnFinalize"
)]
internal static class VolunteerSwapForPlayer_End_Finalize
{
    [HarmonyPostfix]
    private static void Postfix() => VolunteerSwapForPlayerSession.End();
}

internal static class VolunteerSwapForPlayerSession
{
    private static readonly Dictionary<Hero, CharacterObject[]> _backup = new();
    private static Settlement _settlement;
    public static bool IsActive => _settlement != null;

    public static void BeginIfNeeded()
    {
        if (!Config.GetOption<bool>("RecruitAnywhere"))
            return;

        if (IsActive)
            End();

        var playerClan = Player.Clan;
        if (
            playerClan == null
            || (playerClan.BasicTroops.Count == 0 && playerClan.EliteTroops.Count == 0)
        )
            return;

        var s = Hero.MainHero?.CurrentSettlement ?? Settlement.CurrentSettlement;
        if (s == null)
            return;

        _settlement = s;

        foreach (var notable in s.Notables)
        {
            try
            {
                var arr = notable?.VolunteerTypes;
                if (arr == null)
                    continue;

                _backup[notable] = (CharacterObject[])arr.Clone();

                for (int i = 0; i < arr.Length; i++)
                {
                    var vanilla = arr[i];

                    // sanitize corrupt slots first
                    if (TroopSwapHelper.LooksCorrupt(vanilla))
                    {
                        var safe = TroopSwapHelper.SafeVanillaFallback(s);
                        if (TroopSwapHelper.IsValidChar(safe))
                            arr[i] = safe;
                        vanilla = safe;
                    }

                    var w = new WCharacter(vanilla);
                    if (!TroopSwapHelper.IsValid(w))
                        continue;
                    if (TroopSwapHelper.IsFactionTroop(playerClan, w))
                        continue;

                    var root = TroopSwapHelper.GetFactionRootFor(w, playerClan);
                    if (!TroopSwapHelper.IsValid(root))
                        continue;

                    var repl = TroopSwapHelper.MatchTier(root, w.Tier);
                    if (TroopSwapHelper.IsValid(repl))
                        arr[i] = repl.Base;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, $"VolunteerSwapForPlayer: notable {notable?.Name} in {s?.Name}");
            }
        }

        Log.Debug($"VolunteerSwapForPlayer: temporary swap applied at {s.Name}.");
    }

    public static void End()
    {
        if (!IsActive)
            return;

        foreach (var kv in _backup)
        {
            try
            {
                var notable = kv.Key;
                var orig = kv.Value;
                if (notable?.VolunteerTypes == null || orig == null)
                    continue;

                var n = Math.Min(notable.VolunteerTypes.Length, orig.Length);
                for (int i = 0; i < n; i++)
                    notable.VolunteerTypes[i] = orig[i];
            }
            catch (Exception e)
            {
                Log.Exception(e, "VolunteerSwapForPlayer: restore failed.");
            }
        }

        _backup.Clear();
        Log.Debug($"VolunteerSwapForPlayer: swap restored at {_settlement?.Name}.");
        _settlement = null;
    }
}
