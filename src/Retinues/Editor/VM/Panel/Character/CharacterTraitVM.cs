using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Controllers.Character;
using Retinues.Editor.Events;
using Retinues.UI.VM;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Panel.Character
{
    /// <summary>
    /// Single hero trait row (name, value, +/-).
    /// </summary>
    public sealed class CharacterTraitVM(TraitObject trait) : EventListenerVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Convenience                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WHero Hero => State.Character.Hero;
        private int Value => Hero?.GetTrait(trait) ?? 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Name => trait.Name.ToString();

        [EventListener(UIEvent.Trait)]
        [DataSourceProperty]
        public string Sprite
        {
            get
            {
                int v = Value;
                int spriteValue = v == 0 ? 1 : v; // 0 uses 1
                string id = trait.StringId;
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
        public Tooltip Tooltip => new(trait.Description?.ToString() ?? string.Empty);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Buttons                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Button<TraitObject> IncrementButton { get; } =
            new(action: TraitController.TraitIncrease, arg: () => trait, refresh: UIEvent.Trait);

        [DataSourceProperty]
        public Button<TraitObject> DecrementButton { get; } =
            new(action: TraitController.TraitDecrease, arg: () => trait, refresh: UIEvent.Trait);
    }
}
