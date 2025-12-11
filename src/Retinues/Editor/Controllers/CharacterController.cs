using System;
using Retinues.Engine;
using Retinues.Model.Characters;
using Retinues.Model.Factions;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Editor.Controllers
{
    public class CharacterController : BaseController
    {
        public static void ChangeName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                Notifications.Popup(
                    L.T("invalid_name_title", "Invalid Name"),
                    L.T("invalid_name_body", "The name cannot be empty.")
                );
                return;
            }

            if (State.Character == null)
                return;

            if (newName == State.Character.Name)
                return; // No change.

            State.Character.Name = newName;
            EventManager.Fire(UIEvent.Name, EventScope.Local);
        }

        public static void ChangeCulture(WCulture newCulture)
        {
            var troop = State.Character;
            if (troop == null)
                return;

            if (newCulture == null)
                return;

            var co = troop.Base;
            if (co == null)
                return;

            // Skip heroes for now (keep culture in sync, but no visual work).
            if (co.HeroObject != null)
            {
                troop.Culture = newCulture;
                EventManager.Fire(UIEvent.Culture, EventScope.Local);
                return;
            }

            // No change requested.
            if (newCulture == troop.Culture)
                return;

            // 1) Update logical culture (wrapper + persistence).
            troop.Culture = newCulture;

            // 2) Apply appearance from that culture.
            ApplyPropertiesFromCulture(troop);

            // 3) Notify the UI so the tableau refreshes.
            EventManager.Fire(UIEvent.Culture, EventScope.Local);
        }

        /* ━━━━━━━━━━━━━━━━━━━━━━ Helpers ━━━━━━━━━━━━━━━━━━━━━━ */

        /// <summary>
        /// Port of old BodyHelper.ApplyPropertiesFromCulture for non-heroes,
        /// but wired to the new wrappers.
        /// </summary>
        static void ApplyPropertiesFromCulture(WCharacter troop)
        {
            var co = troop?.Base;
            var culture = troop?.Culture?.Base;

            if (co == null || culture == null)
                return;

            // Heroes handled later via Hero.BodyProperties.
            if (co.HeroObject != null)
                return;

            // Choose a template for body envelope.
            var template = culture.BasicTroop ?? culture.EliteBasicTroop;
            if (template == null)
                return;

            // Template body envelope.
            var minTroop = template.GetBodyPropertiesMin();
            var maxTroop = template.GetBodyPropertiesMax();

            // ───── Race (wrapper → MAttribute) ─────
            troop.Race = template.Race;

            // ───── Dynamic ranges (all via MAttribute) ─────

            // Age range
            troop.AgeMin = minTroop.Age;
            troop.AgeMax = maxTroop.Age;

            // Weight range
            troop.WeightMin = minTroop.Weight;
            troop.WeightMax = maxTroop.Weight;

            // Build range
            troop.BuildMin = minTroop.Build;
            troop.BuildMax = maxTroop.Build;

            // Actual Age: mid-point of the range, through wrapper.
            var midAge = (minTroop.Age + maxTroop.Age) * 0.5f;
            troop.Age = midAge;

            // Tags (hair/beard/tattoo) – also all via wrapper.
            ApplyTagsFromCulture(troop);
        }

        /// <summary>
        /// Re-applies hair/beard/tattoo tags from culture templates.
        /// </summary>
        static void ApplyTagsFromCulture(WCharacter troop)
        {
#if BL13
            try
            {
                var co = troop?.Base;
                var culture = troop?.Culture?.Base;

                if (co == null || culture == null)
                    return;

                if (co is not BasicCharacterObject basic)
                    return;

                // Heroes handled later.
                if (basic.IsHero)
                    return;

                // Pick template for tags.
                CharacterObject templateForTags = null;

                if (troop.IsFemale)
                    templateForTags = culture.VillageWoman;

                if (templateForTags == null)
                    templateForTags = culture.BasicTroop ?? culture.EliteBasicTroop;

                if (templateForTags == null)
                    return;

                var templateRange = templateForTags.BodyPropertyRange as MBBodyProperty;
                if (templateRange == null)
                    return;

                var hairTags = templateRange.HairTags ?? string.Empty;
                var beardTags = templateRange.BeardTags ?? string.Empty;
                var tattooTags = templateRange.TattooTags ?? string.Empty;

                bool hasHair = !string.IsNullOrEmpty(hairTags);
                bool hasBeard = !string.IsNullOrEmpty(beardTags);
                bool hasTattoo = !string.IsNullOrEmpty(tattooTags);

                if (!hasHair && !hasBeard && !hasTattoo)
                    return;

                bool different =
                    (hasHair && !string.Equals(troop.HairTags, hairTags, StringComparison.Ordinal))
                    || (
                        hasBeard
                        && !string.Equals(troop.BeardTags, beardTags, StringComparison.Ordinal)
                    )
                    || (
                        hasTattoo
                        && !string.Equals(troop.TattooTags, tattooTags, StringComparison.Ordinal)
                    );

                if (!different)
                    return;

                if (hasHair)
                    troop.HairTags = hairTags;

                if (hasBeard)
                    troop.BeardTags = beardTags;

                if (hasTattoo)
                    troop.TattooTags = tattooTags;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
#endif
        }
    }
}
