using System;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop
{
    /// <summary>
    /// ViewModel for a troop row in the troop list. Handles display, search filtering, and selection refresh logic.
    /// </summary>
    [SafeClass]
    public sealed class TroopRowVM(TroopListVM list, WCharacter troop)
        : BaseRow<TroopListVM, TroopRowVM>(list)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter Troop = troop;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Image ━━━━━━━━ */

#if BL13
        [DataSourceProperty]
        public string AdditionalArgs => Troop.Image.AdditionalArgs;

        [DataSourceProperty]
        public string Id => Troop.Image.Id;

        [DataSourceProperty]
        public string TextureProviderName => Troop.Image.TextureProviderName;
#else
        [DataSourceProperty]
        public string ImageId => Troop.Image.Id;

        [DataSourceProperty]
        public int ImageTypeCode => Troop.Image.ImageTypeCode ?? 0;

        [DataSourceProperty]
        public string ImageAdditionalArgs => Troop.Image.AdditionalArgs;
#endif

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string NameText
        {
            get
            {
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

        [DataSourceProperty]
        public string PlaceholderText =>
            L.S("acquire_fief_to_unlock", "Acquire a fief to unlock clan troops.");

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        public bool IsPlaceholder => Troop == null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override void OnSelect()
        {
            EditorVM.Instance.Troop = Troop;
        }
    }
}
