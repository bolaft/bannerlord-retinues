using System;
using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Game.Wrappers;
using Retinues.GUI.Helpers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop.List
{
    /// <summary>
    /// ViewModel for a single troop row in troop lists.
    /// </summary>
    [SafeClass]
    public sealed class TroopRowVM(WCharacter rowTroop) : BaseListElementVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly WCharacter RowTroop = rowTroop;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override Dictionary<UIEvent, string[]> EventMap =>
            new()
            {
                [UIEvent.Troop] =
                [
                    nameof(IsPlaceholder),
                    nameof(NameText),
                    nameof(TierIconData),
                    nameof(IsSelected),
                    nameof(FormationClassIcon),
                ],
                [UIEvent.Equipment] = [nameof(FormationClassIcon)],
                [UIEvent.Appearance] =
                [
                    nameof(ImageId),
                    nameof(ImageAdditionalArgs),
#if BL13
                    nameof(ImageTextureProviderName),
#else
                    nameof(ImageTypeCode),
#endif
                ],
                [UIEvent.Equip] =
                [
                    nameof(FormationClassIcon),
                ],
            };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Image ━━━━━━━━ */

        [DataSourceProperty]
        public string ImageId => RowTroop?.Image.Id;

        [DataSourceProperty]
        public string ImageAdditionalArgs => RowTroop?.Image.AdditionalArgs;

#if BL13
        [DataSourceProperty]
        public string ImageTextureProviderName => RowTroop?.Image.TextureProviderName;
#else
        [DataSourceProperty]
        public int ImageTypeCode => RowTroop?.Image.ImageTypeCode ?? 0;
#endif

        /* ━━━━━━━━━ Icons ━━━━━━━━ */

        [DataSourceProperty]
        public StringItemWithHintVM TierIconData => Icons.GetTierIconData(RowTroop);

        [DataSourceProperty]
        public string FormationClassIcon => Icons.GetFormationClassIcon(RowTroop);

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool IsPlaceholder => RowTroop == null;

        [DataSourceProperty]
        public bool IsTroop => RowTroop != null;

        [DataSourceProperty]
        public override bool IsSelected => RowTroop == State.Troop;

        [DataSourceProperty]
        public override bool IsEnabled => RowTroop != null;

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string NameText
        {
            get
            {
                if (IsPlaceholder)
                    return L.S("acquire_fief_to_unlock", "Acquire a fief to unlock clan troops.");

                if (RowTroop?.IsRetinue == true || RowTroop?.IsMilitia == true)
                    return RowTroop?.Name;

                // Indent troops by tier
                int n = Math.Max(0, RowTroop?.Tier - 1 ?? 0);
                var indent = new string(' ', n * 4);

                return $"{indent}{RowTroop?.Name}";
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Determine whether this row matches the provided filter.
        /// </summary>
        public override bool FilterMatch(string filter)
        {
            if (RowTroop == null)
                return true; // Always show placeholder

            var search = filter.Trim().ToLowerInvariant();
            var name = RowTroop.Name.ToString().ToLowerInvariant();

            return name.Contains(search);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        /// <summary>
        /// Select this troop as the current troop in state.
        /// </summary>
        public void ExecuteSelect() => State.UpdateTroop(RowTroop);
    }
}
