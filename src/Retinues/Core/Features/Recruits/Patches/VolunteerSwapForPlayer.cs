using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Core.Features.Recruits;
using Retinues.Core.Game;
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
    private static readonly Dictionary<Hero, CharacterObject[]> _backup = [];
    private static Settlement _settlement;

    public static bool IsActive => _settlement != null;

    public static void BeginIfNeeded()
    {
        if (!Config.GetOption<bool>("RecruitAnywhere"))
            return;

        if (IsActive)
            End();

        var playerClan = Player.Clan;
        if (playerClan == null)
            return;

        if (playerClan.BasicTroops.Count == 0 || playerClan.EliteTroops.Count == 0)
            return;

        var s = Hero.MainHero?.CurrentSettlement ?? Settlement.CurrentSettlement;
        if (s == null)
            return;

        Log.Debug(
            $"VolunteerSwapForPlayer: Attempting swap at {s.Name} for clan {playerClan.Name}."
        );

        _settlement = s;

        foreach (var notable in s.Notables)
        {
            try
            {
                var arr = notable?.VolunteerTypes;
                if (arr == null)
                    continue;

                // backup
                _backup[notable] = (CharacterObject[])arr.Clone();

                for (int i = 0; i < arr.Length; i++)
                {
                    var vanilla = arr[i];
                    if (vanilla == null)
                        continue;

                    var wrapped = new Retinues.Core.Game.Wrappers.WCharacter(vanilla);

                    if (TroopSwapHelper.IsFactionTroop(playerClan, wrapped))
                        continue;

                    var root = TroopSwapHelper.GetFactionRootFor(wrapped, playerClan);
                    if (root == null)
                        continue;
                    var replacement = TroopSwapHelper.MatchTier(root, wrapped.Tier);

                    if (replacement != null && replacement.IsActive)
                        arr[i] = replacement.Base;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        Log.Debug($"Temporary swap applied at {s.Name}.");
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
                Log.Exception(e);
            }
        }

        _backup.Clear();
        Log.Debug($"Swap restored at {_settlement?.Name}.");
        _settlement = null;
    }
}
