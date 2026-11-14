using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace Retinues.Compatibility
{
    /// <summary>
    /// Veteran guards have no CultureObject slot. Resolve them from the actual caravan
    /// templates, without depending on editable names, culture, levels or equipment.
    /// </summary>
    internal static class CaravanTroopResolver
    {
        internal static CharacterObject FindVeteran(CultureObject culture)
        {
            if (culture == null)
                return null;

            var candidates = new Dictionary<string, CharacterObject>(StringComparer.Ordinal);
#if BL12
            Collect(culture.CaravanPartyTemplate);
            Collect(culture.EliteCaravanPartyTemplate);
#else
            if (culture.CaravanPartyTemplates != null)
                foreach (var template in culture.CaravanPartyTemplates)
                    Collect(template);
            if (culture.EliteCaravanPartyTemplates != null)
                foreach (var template in culture.EliteCaravanPartyTemplates)
                    Collect(template);
#endif

            // Prefer the vanilla identity when an overhaul adds other caravan specialists.
            if (candidates.TryGetValue("veteran_caravan_guard_" + culture.StringId, out var native))
                return native;

            // A renamed overhaul veteran is safe to identify only if the role is unambiguous.
            if (candidates.Count == 1)
                foreach (var troop in candidates.Values)
                    return troop;
            return null;

            void Collect(PartyTemplateObject template)
            {
                if (template?.Stacks == null)
                    return;

                foreach (var stack in template.Stacks)
                {
                    var troop = stack.Character;
                    // Template membership defines the caravan role. The editor can change
                    // a troop's appearance culture without changing which caravans use it.
                    if (troop == null || troop.IsHero
                        || troop.Occupation != Occupation.CaravanGuard
                        || troop == culture.CaravanGuard || troop == culture.CaravanMaster
                        || troop == troop.Culture?.CaravanGuard || troop == troop.Culture?.CaravanMaster
                        || string.IsNullOrEmpty(troop.StringId)
                        || troop.StringId.StartsWith("armed_trader_", StringComparison.Ordinal))
                        continue;
#if BL12
                    if (troop == culture.ArmedTrader || troop == troop.Culture?.ArmedTrader)
                        continue;
#endif
                    candidates[troop.StringId] = troop;
                }
            }
        }
    }
}
