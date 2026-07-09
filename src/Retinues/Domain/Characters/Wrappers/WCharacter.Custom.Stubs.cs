using Retinues.Domain.Characters.Services.Caches;
using Retinues.Framework.Model.Attributes;
using TaleWorlds.CampaignSystem;

namespace Retinues.Domain.Characters.Wrappers
{
    public partial class WCharacter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Stubs                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public const string CustomTroopPrefix = "retinues_custom_";

        MAttribute<bool> IsActiveStubAttribute => Attribute(false, name: "IsActiveStubAttribute");

        /// <summary>
        /// Whether this WCharacter is currently allocated as an active stub for custom troop creation.
        /// </summary>
        public bool IsActiveStub
        {
            get => IsActiveStubAttribute.Get();
            set => IsActiveStubAttribute.Set(value);
        }

        /// <summary>
        /// Allocates a free stub WCharacter for new custom troop creation.
        /// </summary>
        public static WCharacter GetFreeStub()
        {
            foreach (var wc in All)
            {
                if (!wc.IsCustom)
                    continue;

                if (wc.IsActiveStub)
                    continue;

                // Guard against a lost IsActiveStub flag: if the stub is still referenced by a
                // faction roster (a tree node, militia, caravan, villager, retinue, ...), it is NOT
                // free — handing it out would cross-link it into a second tree (e.g. an imported
                // elite tree reusing a militia troop). Re-mark it active to repair the flag and skip.
                if (wc.SourceFlags != TroopSourceFlags.None)
                {
                    wc.IsActiveStub = true;
                    continue;
                }

                // Mark as active.
                wc.IsActiveStub = true;

                // Record the campaign day so the statistics popup can show service time.
                wc.CreationDay = Campaign.Current != null ? CampaignTime.Now.ToDays : 0.0;

                // Found a free stub.
                return wc;
            }

            return null; // No free stubs.
        }
    }
}
