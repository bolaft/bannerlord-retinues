using System;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.VM.Equipment;
using Retinues.GUI.Editor.VM.Troop.Panel;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop.List
{
    /// <summary>
    /// ViewModel for a troop row in the troop list. Handles display, search filtering, and selection refresh logic.
    /// </summary>
    [SafeClass]
    public sealed class TroopRowVM(WCharacter troop) : BaseRow<TroopListVM, TroopRowVM>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Troop                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter Troop => troop;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Image ━━━━━━━━ */

#if BL13
        [DataSourceProperty]
        public string AdditionalArgs => Troop?.Image.AdditionalArgs;

        [DataSourceProperty]
        public string Id => Troop?.Image.Id;

        [DataSourceProperty]
        public string TextureProviderName => Troop?.Image.TextureProviderName;
#else
        [DataSourceProperty]
        public string ImageId => Troop?.Image.Id;

        [DataSourceProperty]
        public int ImageTypeCode => Troop?.Image.ImageTypeCode ?? 0;

        [DataSourceProperty]
        public string ImageAdditionalArgs => Troop?.Image.AdditionalArgs;
#endif

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        public bool IsPlaceholder => Troop == null;

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string NameText
        {
            get
            {
                if (IsPlaceholder)
                    return L.S("acquire_fief_to_unlock", "Acquire a fief to unlock clan troops.");

                if (Troop.IsRetinue || Troop.IsMilitia)
                    return Troop.Name;

                // Indent troops by tier
                int n = Math.Max(0, Troop.Tier - 1);
                var indent = new string(' ', n * 4);

                return $"{indent}{Troop.Name}";
            }
        }

        [DataSourceProperty]
        public string TierText => $"T{Troop.Tier}";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override TroopListVM List => Editor.TroopScreen.TroopList;

        protected override void OnSelect()
        {
            OnPropertyChanged(nameof(Editor.TroopScreen.TroopPanel));
            OnPropertyChanged(nameof(Editor.EquipmentScreen));
            OnPropertyChanged(nameof(Editor.Model));
        }

        /// <summary>
        /// Updates the visibility of the row based on the given filter text.
        /// </summary>
        public override bool FilterMatch(string filter)
        {
            if (Troop == null)
                return true;

            var search = filter.Trim().ToLowerInvariant();
            var name = Troop.Name.ToString().ToLowerInvariant();

            return name.Contains(search);
        }
    }
}
