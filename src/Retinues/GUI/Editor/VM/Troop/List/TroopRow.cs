using System;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop.List
{
    [SafeClass]
    public sealed class TroopRowVM : BaseRow<TroopListVM, TroopRowVM>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public readonly TroopScreenVM Screen;
        public readonly WCharacter Troop;

        public TroopRowVM(TroopScreenVM screen, WCharacter troop)
        {
            Log.Info("Building TroopRowVM...");

            Screen = screen;
            Troop = troop;
        }

        public void Initialize()
        {
            Log.Info("Initializing TroopRowVM...");

            // Subscribe to events
            EventManager.NameChange.RegisterProperties(this, nameof(NameText));
            EventManager.TierChange.RegisterProperties(this, nameof(TierText));

            void RefreshImage()
            {
                OnPropertyChanged(nameof(ImageId));
                OnPropertyChanged(nameof(ImageAdditionalArgs));
#if BL13
                OnPropertyChanged(nameof(ImageTextureProviderName));
#else
                OnPropertyChanged(nameof(ImageTypeCode));
#endif
            }

            EventManager.EquipmentChange.Register(RefreshImage);
            EventManager.EquipmentItemChange.Register(RefreshImage);
            EventManager.GenderChange.Register(RefreshImage);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Image ━━━━━━━━ */

        [DataSourceProperty]
        public string ImageId => Troop?.Image?.Id;

        [DataSourceProperty]
        public string ImageAdditionalArgs => Troop?.Image?.AdditionalArgs;

#if BL13
        [DataSourceProperty]
        public string ImageTextureProviderName => Troop?.Image?.TextureProviderName;
#else
        [DataSourceProperty]
        public int ImageTypeCode => Troop?.Image?.ImageTypeCode ?? 0;
#endif

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool IsPlaceholder => Troop == null;

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string NameText
        {
            get
            {
                if (IsPlaceholder)
                    return L.S("acquire_fief_to_unlock", "Acquire a fief to unlock clan troops.");

                if (Troop?.IsRetinue == true || Troop?.IsMilitia == true)
                    return Troop?.Name;

                // Indent troops by tier
                int n = Math.Max(0, Troop?.Tier - 1 ?? 0);
                var indent = new string(' ', n * 4);

                return $"{indent}{Troop?.Name}";
            }
        }

        [DataSourceProperty]
        public string TierText => Troop == null ? string.Empty : $"T{Troop.Tier}";

        /* ━━━━━━━━ Enabled ━━━━━━━ */

        [DataSourceProperty]
        public override bool IsEnabled
        {
            get
            {
                if (IsPlaceholder)
                    return false; // Placeholder row is never enabled

                return true; // All checks passed
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override TroopListVM List => Screen.TroopList;

        protected override void OnSelect()
        {
            if (Screen == null)
                return; // On construction

            EventManager.TroopChange.Fire();
        }

        /// <summary>
        /// Updates the visibility of the row based on the given filter text.
        /// </summary>
        public override bool FilterMatch(string filter)
        {
            if (Troop == null)
                return true;

            var search = filter.Trim().ToLowerInvariant();
            var name = Troop.Name?.ToString().ToLowerInvariant() ?? string.Empty;

            return name.Contains(search);
        }
    }
}
