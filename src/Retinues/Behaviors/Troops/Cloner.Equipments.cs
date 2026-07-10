using System.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Services.Random;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Settings;
using TaleWorlds.Core;

namespace Retinues.Behaviors.Troops
{
    /// <summary>
    /// Cloner utilities for creating troop clones from templates.
    /// </summary>
    public static partial class Cloner
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Equipment Strategy                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Applies the configured starter equipment strategy to a cloned character.
        /// </summary>
        private static void ApplyStarterEquipments(
            WCharacter template,
            WCharacter clone,
            WCulture cultureContext,
            bool createCivilianSet,
            RandomEquipmentReuseContext reuseContext = null,
            bool forceRandom = false,
            int maxItemTierOverride = -1,
            int minItemTierOverride = -1
        )
        {
            if (template == null || clone == null)
                return;

            if (!forceRandom)
            {
                switch (Configuration.StarterEquipment.Value)
                {
                    case Configuration.EquipmentMode.AllSets:
                        clone.EquipmentRoster.Copy(template.EquipmentRoster, EquipmentCopyMode.All);
                        if (createCivilianSet)
                            EnsureClothedCivilianSet(
                                clone,
                                cultureContext ?? template.Culture,
                                reuseContext,
                                maxItemTierOverride,
                                minItemTierOverride
                            );
                        return;

                    case Configuration.EquipmentMode.SingleSet:
                        clone.EquipmentRoster.Copy(
                            template.EquipmentRoster,
                            EquipmentCopyMode.FirstOfEach
                        );
                        if (createCivilianSet)
                            EnsureClothedCivilianSet(
                                clone,
                                cultureContext ?? template.Culture,
                                reuseContext,
                                maxItemTierOverride,
                                minItemTierOverride
                            );
                        return;

                    case Configuration.EquipmentMode.EmptySet:
                        clone.EquipmentRoster.Copy(
                            template.EquipmentRoster,
                            EquipmentCopyMode.Reset
                        );
                        return;

                    case Configuration.EquipmentMode.RandomSet:
                    default:
                        break;
                }
            }

            var culture = cultureContext ?? template.Culture;

            MEquipment srcBattle = null;
            MEquipment srcCivil = null;

            var tplEquipments = template.EquipmentRoster?.Equipments;
            if (tplEquipments != null)
            {
                for (int i = 0; i < tplEquipments.Count; i++)
                {
                    var e = tplEquipments[i];
                    if (e == null)
                        continue;

                    if (e.IsCivilian)
                    {
                        if (srcCivil == null)
                            srcCivil = e;
                    }
                    else
                    {
                        if (srcBattle == null)
                            srcBattle = e;
                    }

                    if (srcBattle != null && srcCivil != null)
                        break;
                }
            }

            if (srcBattle == null && srcCivil != null)
                srcBattle = srcCivil;

            if (srcBattle == null)
            {
                clone.EquipmentRoster.Copy(template.EquipmentRoster, EquipmentCopyMode.Reset);
                return;
            }

            if (srcCivil == null)
                srcCivil = srcBattle;

            var battle = EquipmentRandomizer.CreateRandomEquipment(
                owner: clone,
                source: srcBattle,
                civilian: false,
                acceptableCultures: culture != null ? [culture] : null,
                acceptNeutralCulture: true,
                requireSkillForItem: true,
                itemFilter: null,
                fromStocks: false,
                pickBest: false,
                enforceLimits: true,
                reuseContext: reuseContext,
                preferUnlocked: true,
                maxItemTierOverride: maxItemTierOverride,
                minItemTierOverride: minItemTierOverride
            );

            MEquipment civil = null;

            if (createCivilianSet)
            {
                civil = EquipmentRandomizer.CreateRandomEquipment(
                    owner: clone,
                    source: srcCivil,
                    civilian: true,
                    acceptableCultures: culture != null ? [culture] : null,
                    acceptNeutralCulture: true,
                    requireSkillForItem: true,
                    itemFilter: null,
                    fromStocks: false,
                    pickBest: false,
                    enforceLimits: true,
                    reuseContext: reuseContext,
                    preferUnlocked: true,
                    maxItemTierOverride: maxItemTierOverride,
                    minItemTierOverride: minItemTierOverride
                );
            }

            clone.EquipmentRoster.Equipments = createCivilianSet ? [battle, civil] : [battle];

            if (createCivilianSet)
                EnsureClothedCivilianSet(
                    clone,
                    culture,
                    reuseContext,
                    maxItemTierOverride,
                    minItemTierOverride
                );
        }

        /// <summary>
        /// Guarantees the clone has a civilian set with body clothing. Many troop templates ship no
        /// civilian set (or an empty one), so copying them — or mirroring an empty civilian source —
        /// leaves the troop naked in town scenes. When no clothed civilian set exists, one is
        /// synthesized from the battle set (which reliably has body armor to mirror into civilian
        /// clothing). No-op if a clothed civilian set is already present or none can be produced.
        /// </summary>
        private static void EnsureClothedCivilianSet(
            WCharacter clone,
            WCulture culture,
            RandomEquipmentReuseContext reuseContext,
            int maxItemTierOverride,
            int minItemTierOverride
        )
        {
            if (clone?.Base == null)
                return;

            // Already clothed for town scenes? Nothing to do.
            var existingCivil = clone.FirstCivilianEquipment;
            if (existingCivil != null && existingCivil.Get(EquipmentIndex.Body)?.Base != null)
                return;

            var battle = clone.FirstBattleEquipment;
            if (battle == null)
                return;

            var civil = EquipmentRandomizer.CreateRandomEquipment(
                owner: clone,
                source: battle,
                civilian: true,
                acceptableCultures: culture != null ? [culture] : null,
                acceptNeutralCulture: true,
                requireSkillForItem: true,
                itemFilter: null,
                fromStocks: false,
                pickBest: false,
                enforceLimits: true,
                reuseContext: reuseContext,
                preferUnlocked: true,
                maxItemTierOverride: maxItemTierOverride,
                minItemTierOverride: minItemTierOverride
            );

            // Only replace if we actually produced clothing (a culture with no civilian items is
            // left as-is rather than given an equally-empty set).
            if (civil == null || civil.Get(EquipmentIndex.Body)?.Base == null)
                return;

            var list = clone
                .EquipmentRoster.Equipments.Where(e => e != null && !e.IsCivilian)
                .ToList();
            list.Add(civil);
            clone.EquipmentRoster.Equipments = list;
        }
    }
}
