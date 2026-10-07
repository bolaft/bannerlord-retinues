using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Behaviors.Unlocks
{
    /// <summary>
    /// Invalidates the static item caches when a new item is crafted in the smithy, so newly
    /// crafted weapons show up in the editor's equipment lists (with "Show weapons from the
    /// Smithy" enabled) without restarting the game.
    /// </summary>
    public sealed class CraftedItemsCacheBehavior : BaseCampaignBehavior
    {
        protected override void RegisterCustomEvents()
        {
            CampaignEvents.OnNewItemCraftedEvent.AddNonSerializedListener(this, OnNewItemCrafted);
        }

        internal static void OnNewItemCrafted(
            ItemObject item,
            ItemModifier modifier,
            bool isCraftedByPlayer
        )
        {
            WItem.ClearStaticCaches();
            Log.Debug($"Cleared item caches after crafting '{item?.StringId}'.");
        }
    }
}
