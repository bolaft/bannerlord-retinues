using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;

namespace Retinues.Compatibility.Interops.BanditMilitias
{
    /// <summary>
    /// Filters custom troops from BanditMilitias recruit/bandit pools and militia rosters.
    /// </summary>
    internal static class BanditMilitiasTroopsPatcher
    {
        private const string HelperTypeName = "BanditMilitias.Helper";
        private const string GlobalsTypeName = "BanditMilitias.Globals";

        private static FieldInfo _recruitsField;
        private static FieldInfo _basicInfantryField;
        private static FieldInfo _basicRangedField;
        private static FieldInfo _basicCavalryField;

        /// <summary>
        /// Attempts to apply Harmony patches to BanditMilitias if available.
        /// </summary>
        public static void TryPatch(Harmony harmony)
        {
            try
            {
                var helperType = AccessTools.TypeByName(HelperTypeName);
                var globalsType = AccessTools.TypeByName(GlobalsTypeName);

                if (helperType == null || globalsType == null)
                {
                    Log.Warning(
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
                    Log.Warning(
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
                    Log.Warning(
                        "BanditMilitias.Helper.InitMilitia not found; militia roster filter skipped."
                    );
                }

                Log.Debug("BanditMilitias compatibility patch applied (InitMap/InitMilitia).");
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

        /// <summary>
        /// Postfix executed after InitMap to filter recruit and bandit pools.
        /// </summary>
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

        /// <summary>
        /// Removes custom troops from BanditMilitias recruit dictionaries.
        /// </summary>
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
                    if (list[i] is CharacterObject co && WCharacter.Get(co).IsCustom)
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

        /// <summary>
        /// Removes custom troops from basic bandit pool lists.
        /// </summary>
        private static void FilterBasicBanditPools()
        {
            FilterBasicList(_basicInfantryField, "BasicInfantry");
            FilterBasicList(_basicRangedField, "BasicRanged");
            FilterBasicList(_basicCavalryField, "BasicCavalry");
        }

        /// <summary>
        /// Removes custom troops from the provided basic pool field.
        /// </summary>
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
                if (value[i] is CharacterObject co && WCharacter.Get(co).IsCustom)
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

        /// <summary>
        /// Prefix executed before InitMilitia to strip custom troops from rosters.
        /// </summary>
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

        /// <summary>
        /// Removes custom troops from the given TroopRoster in-place.
        /// </summary>
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

                    if (!WCharacter.Get(co).IsCustom)
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
    }
}
