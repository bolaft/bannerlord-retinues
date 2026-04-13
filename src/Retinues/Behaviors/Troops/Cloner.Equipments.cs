using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Services.Random;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Settings;

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
                        return;

                    case Configuration.EquipmentMode.SingleSet:
                        clone.EquipmentRoster.Copy(
                            template.EquipmentRoster,
                            EquipmentCopyMode.FirstOfEach
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
        }
    }
}
