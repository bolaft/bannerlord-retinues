using Retinues.Framework.Model.Attributes;

namespace Retinues.Domain.Characters.Wrappers
{
    public partial class WCharacter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Stubs                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public const string CustomTroopPrefix = "retinues_custom_";

        MAttribute<bool> IsActiveStubAttribute => Attribute(false);

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

                // Mark as active.
                wc.IsActiveStub = true;

                // Found a free stub.
                return wc;
            }

            return null; // No free stubs.
        }
    }
}
