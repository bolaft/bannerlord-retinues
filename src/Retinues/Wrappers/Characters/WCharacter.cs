using System.Collections.Generic;
using Retinues.Wrappers.Factions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
# if BL13
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
# endif

namespace Retinues.Wrappers.Characters
{
    public sealed class WCharacter : Wrapper<WCharacter, CharacterObject>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int Level
        {
            get => Base.Level;
            set => Base.Level = value;
        }

        public bool IsHero => Base.IsHero;

        [Reflected]
        public WCulture Culture
        {
            get => Base.Culture != null ? WCulture.Get(Base.Culture) : null;
            set => SetRef(value?.Base);
        }

        [Reflected(setterName: "SetName")]
        public string Name
        {
            get => Base.Name.ToString() ?? string.Empty;
            set => SetRef(new TextObject(value));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Hierarchy                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly List<WCharacter> _parents = [];

        public List<WCharacter> Parents => _parents;

        public List<WCharacter> Children
        {
            get
            {
                var result = new List<WCharacter>();
                var targets = UpgradeTargets;
                if (targets == null || targets.Length == 0)
                    return result;

                for (int i = 0; i < targets.Length; i++)
                {
                    var co = targets[i];
                    if (co == null)
                        continue;

                    var child = Get(co);
                    if (child != null)
                        result.Add(child);
                }

                return result;
            }
        }

        public WCharacter Root
        {
            get
            {
                // BFS upwards to find any ancestor with no parents
                var visited = new HashSet<WCharacter>();
                var queue = new Queue<WCharacter>();

                visited.Add(this);
                queue.Enqueue(this);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    var parents = current.Parents;

                    if (parents == null || parents.Count == 0)
                        return current;

                    for (int i = 0; i < parents.Count; i++)
                    {
                        var p = parents[i];
                        if (p != null && visited.Add(p))
                            queue.Enqueue(p);
                    }
                }

                // Fallback (should only happen on cycles where everyone has parents)
                return this;
            }
        }

        public List<WCharacter> Tree
        {
            get
            {
                var result = new List<WCharacter>();
                var visited = new HashSet<WCharacter>();
                var stack = new Stack<WCharacter>();

                // Seed with direct children
                foreach (var child in Children)
                {
                    if (child != null && visited.Add(child))
                        stack.Push(child);
                }

                while (stack.Count > 0)
                {
                    var node = stack.Pop();
                    result.Add(node);

                    var children = node.Children;
                    for (int i = 0; i < children.Count; i++)
                    {
                        var c = children[i];
                        if (c != null && visited.Add(c))
                            stack.Push(c);
                    }
                }

                return result;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Image                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

# if BL13
        public CharacterImageIdentifierVM Image => new(CharacterCode);

        public ImageIdentifier ImageIdentifier => new CharacterImageIdentifier(CharacterCode);
#else
        public ImageIdentifierVM Image => new(CharacterCode);

        public ImageIdentifier ImageIdentifier => new(CharacterCode);
# endif
        public CharacterCode CharacterCode => CharacterCode.CreateFrom(Base);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Transfer Flags                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsNotTransferableInHideouts
        {
            get => Base.IsNotTransferableInHideouts;
            set => Base.SetTransferableInHideouts(!value);
        }

        public bool IsNotTransferableInPartyScreen
        {
            get => Base.IsNotTransferableInPartyScreen;
            set => Base.SetTransferableInPartyScreen(!value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Encyclopedia Visibility                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool HiddenInEncyclopedia
        {
#if BL13
            get => Base.HiddenInEncyclopedia;
            set => Base.HiddenInEncyclopedia = value;
#else
            // 1.2.x: engine typo 'HiddenInEncylopedia' and setter is not public → use [Reflected].
            get => Base.HiddenInEncylopedia;
            set => Base.HiddenInEncylopedia = value;
#endif
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Visuals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsFemale
        {
            get => Base.IsFemale;
            set => Base.IsFemale = value;
        }

        public int Race
        {
            get => Base.Race;
            set => Base.Race = value;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Upgrades                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [Reflected]
        public CharacterObject[] UpgradeTargets
        {
            get => Base.UpgradeTargets;
            set => SetRef(value ?? []);
        }

        [Reflected(memberName: nameof(CharacterObject.UpgradeRequiresItemFromCategory))]
        public ItemCategory UpgradeItemRequirement
        {
            get => Base.UpgradeRequiresItemFromCategory;
            set => SetRef(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                           Skills                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [Reflected]
        private MBCharacterSkills DefaultCharacterSkills => GetRef<MBCharacterSkills>();

        public int GetSkill(SkillObject skill) => Base.GetSkillValue(skill);

        public void SetSkill(SkillObject skill, int value)
        {
            // MBCharacterSkills.Skills is a PropertyOwner<SkillObject> internally.
            var owner = (PropertyOwner<SkillObject>)(object)DefaultCharacterSkills.Skills;
            owner.SetPropertyValue(skill, value);
        }
    }
}
