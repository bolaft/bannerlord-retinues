using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using TaleWorlds.Core;

namespace Retinues.Editor.MVC.Common.TopPanel.Helpers
{
    /// <summary>
    /// Kind of target for top panel actions.
    /// </summary>
    public enum TargetKind
    {
        Faction,
        Troop,
    }

    /// <summary>
    /// Target for top panel actions.
    /// </summary>
    public sealed class Target
    {
        public TargetKind Kind { get; }
        public WCharacter Troop { get; }

        private Target(TargetKind kind, WCharacter troop)
        {
            Kind = kind;
            Troop = troop;
        }

        /// <summary>
        /// Create a target for the selected faction.
        /// </summary>
        public static Target ForFaction() => new(TargetKind.Faction, null);

        /// <summary>
        /// Create a target for the given troop.
        /// </summary>
        public static Target ForTroop(WCharacter troop) => new(TargetKind.Troop, troop);
    }

    /// <summary>
    /// Helper methods for building top panel targets.
    /// </summary>
    public static class TargetsHelper
    {
        /// <summary>
        /// Build InquiryElements for: selected faction + all of its troops.
        /// </summary>
        public static List<InquiryElement> BuildFactionAndTroopsElements(
            IBaseFaction faction,
            Func<IBaseFaction, bool> isFactionEnabled,
            string factionDisabledHint,
            Func<WCharacter, bool> isTroopEnabled,
            Func<WCharacter, string> troopDisabledHint
        )
        {
            var elements = new List<InquiryElement>();

            if (faction == null)
                return elements;

            var factionEnabled = isFactionEnabled?.Invoke(faction) ?? false;

            elements.Add(
                new InquiryElement(
                    identifier: Target.ForFaction(),
                    title: faction.Name,
                    imageIdentifier: faction.ImageIdentifier,
                    isEnabled: factionEnabled,
                    hint: factionEnabled ? null : factionDisabledHint
                )
            );

            foreach (var wc in faction.Troops)
            {
                if (wc == null)
                    continue;

                var enabled = isTroopEnabled?.Invoke(wc) ?? false;

                elements.Add(
                    new InquiryElement(
                        identifier: Target.ForTroop(wc),
                        title: wc.Name,
                        imageIdentifier: wc.GetImageIdentifier(),
                        isEnabled: enabled,
                        hint: enabled ? null : (troopDisabledHint?.Invoke(wc))
                    )
                );
            }

            return elements;
        }
    }
}
