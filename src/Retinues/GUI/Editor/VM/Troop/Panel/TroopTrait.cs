using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Game.Wrappers;
using Retinues.GUI.Helpers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop.Panel
{
    /// <summary>
    /// Single hero trait row (name, value, +/-).
    /// </summary>
    [SafeClass]
    public sealed class TroopTraitVM(TraitObject trait) : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly TraitObject _trait = trait;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override Dictionary<UIEvent, string[]> EventMap =>
            new()
            {
                [UIEvent.Troop] =
                [
                    nameof(Value),
                    nameof(ValueText),
                    nameof(Sprite),
                    nameof(SpriteColor),
                    nameof(CanIncrement),
                    nameof(CanDecrement),
                ],
            };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WHero Hero => State.Troop as WHero;
        public int Value => Hero?.GetTrait(_trait) ?? 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Name => _trait.Name.ToString();

        [DataSourceProperty]
        public string ValueText => Value.ToString("+#;-#;0");

        [DataSourceProperty]
        public string Sprite
        {
            get
            {
                int v = Value;
                int spriteValue = v == 0 ? 1 : v; // 0 uses 1
                string id = _trait.StringId; // e.g. "calculating"
                return $"SPGeneral\\SPTraits\\{id.ToLower()}_{spriteValue}";
            }
        }

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
        public BasicTooltipViewModel Hint =>
            Tooltip.MakeTooltip(null, _trait.Description?.ToString() ?? string.Empty);

        [DataSourceProperty]
        public bool CanIncrement => Value < _trait.MaxValue;

        [DataSourceProperty]
        public bool CanDecrement => Value > _trait.MinValue;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteIncrement() => Change(+1);

        [DataSourceMethod]
        public void ExecuteDecrement() => Change(-1);

        private void Change(int delta)
        {
            if (delta > 0)
                if (!CanIncrement)
                    return;
            if (delta < 0)
                if (!CanDecrement)
                    return;

            int current = Hero?.GetTrait(_trait) ?? 0;
            int next = current + delta;

            Hero?.SetTrait(_trait, next);

            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(ValueText));
            OnPropertyChanged(nameof(Sprite));
            OnPropertyChanged(nameof(SpriteColor));
            OnPropertyChanged(nameof(CanIncrement));
            OnPropertyChanged(nameof(CanDecrement));
        }
    }
}
