using System;
using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

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
                    nameof(ImageId),
                    nameof(ImageAdditionalArgs),
#if BL13
                    nameof(ImageTextureProviderName),
#else
                    nameof(ImageTypeCode),
#endif
                    nameof(IsPlaceholder),
                    nameof(NameText),
                    nameof(TierText),
                    nameof(IsSelected),
                    nameof(FormationClassIcon),
                ],
                [UIEvent.Equipment] =
                [
                    nameof(FormationClassIcon),
                ],
                [UIEvent.Equip] =
                [
                    nameof(FormationClassIcon),
                    nameof(ImageId),
                    nameof(ImageAdditionalArgs),
#if BL13
                    nameof(ImageTextureProviderName),
#else
                    nameof(ImageTypeCode),
#endif
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

        /* ━━━━━━━━━ Icon ━━━━━━━━━ */

        [DataSourceProperty]
        public string FormationClassIcon
        {
            get
            {
                return (RowTroop?.FormationClass) switch
                {
                    FormationClass.Infantry => @"General\TroopTypeIcons\icon_troop_type_infantry",
                    FormationClass.Ranged => @"General\TroopTypeIcons\icon_troop_type_bow",
                    FormationClass.Cavalry => @"General\TroopTypeIcons\icon_troop_type_cavalry",
                    FormationClass.HorseArcher => @"General\TroopTypeIcons\icon_troop_type_horse_archer",
                    _ => @"General\TroopTypeIcons\icon_troop_type_infantry",
                };
            }
        }

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

        [DataSourceProperty]
        public string TierText => RowTroop == null ? string.Empty : $"T{RowTroop.Tier}";

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
