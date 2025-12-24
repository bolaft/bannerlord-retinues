using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers.Character;
using Retinues.Helpers;
using Retinues.Model.Characters;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Panel.Character
{
    /// <summary>
    /// Single hero trait row (name, value, +/-).
    /// </summary>
    [SafeClass]
    public sealed class CharacterTraitVM : BaseVM
    {
        private readonly TraitObject _trait;

        public CharacterTraitVM(TraitObject trait)
        {
            _trait = trait;

            RefreshTraitIncrease();
            RefreshTraitDecrease();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Convenience                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WHero Hero => State.Character.Hero;
        private int Value => Hero?.GetTrait(_trait) ?? 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Name => _trait.Name.ToString();

        [EventListener(UIEvent.Trait)]
        [DataSourceProperty]
        public string Sprite
        {
            get
            {
                int v = Value;
                int spriteValue = v == 0 ? 1 : v; // 0 uses 1
                string id = _trait.StringId;
                return $"SPGeneral\\SPTraits\\{id.ToLower()}_{spriteValue}";
            }
        }

        [EventListener(UIEvent.Trait)]
        [DataSourceProperty]
        public string SpriteColor
        {
            get
            {
                return Value switch
                {
                    2 => "#ffdb4dff", // gold
                    1 => "#f7db5eff", // light gold
                    -1 => "#ff9999ff", // light red
                    -2 => "#ff4d4dff", // deep red
                    _ => "#c0c0c0ff", // silver
                };
            }
        }

        [DataSourceProperty]
        public Tooltip Tooltip => new(_trait.Description?.ToString() ?? string.Empty);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Increase                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool CanIncrement { get; set; }

        [DataSourceProperty]
        public Tooltip TooltipIncrement { get; set; }

        [EventListener(UIEvent.Trait)]
        private void RefreshTraitIncrease()
        {
            CanIncrement = TraitController.TraitIncrease.Allow(_trait);
            TooltipIncrement = TraitController.TraitIncrease.Tooltip(_trait);

            OnPropertyChanged(nameof(CanIncrement));
            OnPropertyChanged(nameof(TooltipIncrement));
        }

        [DataSourceMethod]
        public void ExecuteIncrement() => TraitController.TraitIncrease.Execute(_trait);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Decrease                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool CanDecrement { get; set; }

        [DataSourceProperty]
        public Tooltip TooltipDecrement { get; set; }

        [EventListener(UIEvent.Trait)]
        private void RefreshTraitDecrease()
        {
            CanDecrement = TraitController.TraitDecrease.Allow(_trait);
            TooltipDecrement = TraitController.TraitDecrease.Tooltip(_trait);

            OnPropertyChanged(nameof(CanDecrement));
            OnPropertyChanged(nameof(TooltipDecrement));
        }

        [DataSourceMethod]
        public void ExecuteDecrement() => TraitController.TraitDecrease.Execute(_trait);
    }
}
