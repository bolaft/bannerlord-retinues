using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers;
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
    public sealed class CharacterTraitVM(TraitObject trait) : BaseVM()
    {
        private readonly TraitObject _trait = trait;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Convenience                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WHero Hero => State.Character.Hero;
        public int Value => Hero?.GetTrait(_trait) ?? 0;

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
                string id = _trait.StringId; // e.g. "calculating"
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

        [EventListener(UIEvent.Trait)]
        [DataSourceProperty]
        public bool CanIncrement => TraitController.CanIncrement(_trait, Value);

        [EventListener(UIEvent.Trait)]
        [DataSourceProperty]
        public bool CanDecrement => TraitController.CanDecrement(_trait, Value);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteIncrement() => TraitController.Change(_trait, Value, +1);

        [DataSourceMethod]
        public void ExecuteDecrement() => TraitController.Change(_trait, Value, -1);
    }
}
