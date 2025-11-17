using System;
using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Helpers;
using Retinues.Utils;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop.List
{
    /// <summary>
    /// ViewModel for a single troop row in troop lists.
    /// </summary>
    [SafeClass]
    public sealed class TroopRowVM(WCharacter rowTroop, string placeholderText = null)
        : BaseListElementVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public readonly WCharacter RowTroop = rowTroop;
        private readonly string PlaceholderText = placeholderText;

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
                    nameof(ShowPlayerIcon),
                    nameof(ShowRulerIcon),
                    nameof(ShowClanLeaderIcon),
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
                [UIEvent.Equip] = [nameof(FormationClassIcon)],
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
        public StringItemWithHintVM TierIconData =>
            RowTroop?.IsHero == true ? null : Icons.GetTierIconData(RowTroop);

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

        [DataSourceProperty]
        public bool ShowPlayerIcon => RowTroop == Player.Character;

        [DataSourceProperty]
        public bool ShowRulerIcon => RowTroop?.IsRuler == true;

        [DataSourceProperty]
        public bool ShowClanLeaderIcon =>
            RowTroop?.IsClanLeader == true && !ShowRulerIcon && !ShowPlayerIcon;

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string NameText
        {
            get
            {
                if (IsPlaceholder)
                    return PlaceholderText
                        ?? L.T("troop_list.placeholder", "No Troops Available").ToString();

                if (RowTroop?.Parent == null && RowTroop?.IsMercenary == false)
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

        /// <summary>
        /// Select this troop as the current troop in state.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteSelect() => State.UpdateTroop(RowTroop);
    }
}
