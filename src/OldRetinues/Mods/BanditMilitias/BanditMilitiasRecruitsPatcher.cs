using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;

namespace OldRetinues.Mods.BanditMilitias
{
    /// <summary>
    /// Harmony compatibility patch for the BanditMilitias mod.
    /// Filters out custom troops from Bandit Militias recruit and bandit pools,
    /// and strips them from newly initialized militia rosters.
    /// </summary>
    [SafeClass]
    internal static class BanditMilitiasTroopsPatcher
    {
        private const string HelperTypeName = "BanditMilitias.Helper";
        private const string GlobalsTypeName = "BanditMilitias.Globals";

        private static FieldInfo _recruitsField;
        private static FieldInfo _basicInfantryField;
        private static FieldInfo _basicRangedField;
        private static FieldInfo _basicCavalryField;

        public static void TryPatch(Harmony harmony)
        {
            try
            {
                var helperType = AccessTools.TypeByName(HelperTypeName);
                var globalsType = AccessTools.TypeByName(GlobalsTypeName);

                if (helperType == null || globalsType == null)
                {
                    Log.Debug(
                        "BanditMilitias not found; skipping BanditMilitias compatibility patch."
                    );
                    return;
                }

                _recruitsField = AccessTools.Field(globalsType, "Recruits");
                _basicInfantryField = AccessTools.Field(globalsType, "BasicInfantry");
                _basicRangedField = AccessTools.Field(globalsType, "BasicRanged");
                _basicCavalryField = AccessTools.Field(globalsType, "BasicCavalry");

                // Patch Helper.InitMap (builds recruit & bandit pools)
                var initMap = AccessTools.Method(helperType, "InitMap", Type.EmptyTypes);
                if (initMap != null)
                {
                    var postfix = new HarmonyMethod(
                        typeof(BanditMilitiasTroopsPatcher).GetMethod(
                            nameof(InitMapPostfix),
                            BindingFlags.Static | BindingFlags.NonPublic
                        )
                    );

                    harmony.Patch(initMap, postfix: postfix);
                }
                else
                {
                    Log.Debug(
                        "BanditMilitias.Helper.InitMap not found; recruit pool filter skipped."
                    );
                }

                // Patch Helper.InitMilitia (initializes a militia party from rosters)
                var initMilitia = AccessTools.Method(
                    helperType,
                    "InitMilitia",
                    [typeof(MobileParty), typeof(TroopRoster[]), typeof(Vec2)]
                );

                if (initMilitia != null)
                {
                    var prefix = new HarmonyMethod(
                        typeof(BanditMilitiasTroopsPatcher).GetMethod(
                            nameof(InitMilitiaPrefix),
                            BindingFlags.Static | BindingFlags.NonPublic
                        )
                    );

                    harmony.Patch(initMilitia, prefix: prefix);
                }
                else
                {
                    Log.Debug(
                        "BanditMilitias.Helper.InitMilitia not found; militia roster filter skipped."
                    );
                }

                Log.Info("BanditMilitias compatibility patch applied (InitMap/InitMilitia).");
            }
            catch (Exception e)
            {
                // Never let BM compat crash the game
                Log.Exception(e, "Failed to apply BanditMilitias compatibility patches.");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        InitMap                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Called after BanditMilitias.Helper.InitMap() builds its recruit & bandit pools.
        private static void InitMapPostfix()
        {
            try
            {
                FilterRecruitPools();
                FilterBasicBanditPools();
            }
            catch (Exception e)
            {
                Log.Exception(e, "BanditMilitias.InitMapPostfix failed.");
            }
        }

        private static void FilterRecruitPools()
        {
            if (_recruitsField == null)
                return;

            var value = _recruitsField.GetValue(null);
            if (value is not IDictionary dict)
                return;

            int removedTotal = 0;

            foreach (DictionaryEntry entry in dict)
            {
                if (entry.Value is not IList list)
                    continue;

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i] is CharacterObject co && IsCustom(co))
                    {
                        list.RemoveAt(i);
                        removedTotal++;
                    }
                }
            }

            if (removedTotal > 0)
            {
                Log.Debug(
                    $"[BanditMilitiasCompat] Removed {removedTotal} custom troops from Globals.Recruits."
                );
            }
        }

        private static void FilterBasicBanditPools()
        {
            FilterBasicList(_basicInfantryField, "BasicInfantry");
            FilterBasicList(_basicRangedField, "BasicRanged");
            FilterBasicList(_basicCavalryField, "BasicCavalry");
        }

        private static void FilterBasicList(FieldInfo field, string name)
        {
            if (field == null)
                return;

            var value = field.GetValue(null) as IList;
            if (value == null)
                return;

            int removed = 0;

            for (int i = value.Count - 1; i >= 0; i--)
            {
                if (value[i] is CharacterObject co && IsCustom(co))
                {
                    value.RemoveAt(i);
                    removed++;
                }
            }

            if (removed > 0)
            {
                Log.Debug(
                    $"[BanditMilitiasCompat] Removed {removed} custom troops from Globals.{name}."
                );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      InitMilitia                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Called before BanditMilitias.Helper.InitMilitia initializes the party.
        // We strip any custom troops from the member roster used to create the militia.
        private static void InitMilitiaPrefix(TroopRoster[] rosters)
        {
            try
            {
                if (rosters == null || rosters.Length == 0)
                    return;

                var members = rosters[0];
                if (members == null)
                    return;

                StripCustomFromRoster(members);
            }
            catch (Exception e)
            {
                Log.Exception(e, "BanditMilitias.InitMilitiaPrefix failed.");
            }
        }

        private static void StripCustomFromRoster(TroopRoster roster)
        {
            try
            {
                for (int i = roster.Count - 1; i >= 0; i--)
                {
                    var element = roster.GetElementCopyAtIndex(i);
                    var co = element.Character;
                    if (co == null)
                        continue;

                    if (!IsCustom(co))
                        continue;

                    roster.AddToCounts(co, -element.Number, woundedCount: -element.WoundedNumber);

                    Log.Debug(
                        $"[BanditMilitiasCompat] Removed custom troop '{co.StringId}' from militia roster."
                    );
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "StripCustomFromRoster failed.");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Helpers                           //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool IsCustom(CharacterObject character)
        {
            if (character == null)
                return false;

            var id = character.StringId;
            if (string.IsNullOrEmpty(id))
                return false;

            // Use the same prefixes as WCharacter for custom troops.
            return id.StartsWith(WCharacter.CustomIdPrefix, StringComparison.Ordinal)
                || id.StartsWith(WCharacter.LegacyCustomIdPrefix, StringComparison.Ordinal);
        }
    }
}
