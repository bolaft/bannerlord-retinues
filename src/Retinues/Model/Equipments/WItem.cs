using Retinues.Model.Characters;
using Retinues.Model.Factions;
using TaleWorlds.Core;
#if BL13
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
#endif

namespace Retinues.Model.Equipments
{
    public class WItem(ItemObject @base) : WBase<WItem, ItemObject>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Name => Base.Name.ToString();

        public bool IsCivilian => Base.IsCivilian;

        public int Value => Base.Value;

        public ItemCategory Category => Base.ItemCategory;

        public ItemObject.ItemTypeEnum Type => Base.ItemType;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Image                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

#if BL13
        public ItemImageIdentifierVM Image => new(Base);
#else
        public ImageIdentifierVM Image => new(Base);
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Culture                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCulture Culture => WCulture.Get(Base.Culture.StringId);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Skill Requirement                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public SkillObject RelevantSkill => Base.RelevantSkill;
        public int Difficulty => Base.Difficulty;

        public bool EquippableBy(WCharacter character)
        {
            if (RelevantSkill is null)
                return true;

            if (Difficulty <= 0)
                return true;

            return character.Skills.Get(RelevantSkill) >= Difficulty;
        }
    }
}
