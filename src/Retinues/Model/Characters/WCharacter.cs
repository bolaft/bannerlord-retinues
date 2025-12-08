using System;
using System.Collections.Generic;
using System.Reflection;
using Retinues.Model.Equipments;
using Retinues.Model.Factions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
# if BL13
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
# endif

namespace Retinues.Model.Characters
{
    public sealed class WCharacter : Wrapper<WCharacter, CharacterObject>
    {
        public const string CustomTroopPrefix = "retinues_custom_";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int Tier => Base.Tier;

        public int Level
        {
            get => Base.Level;
            set => Base.Level = value;
        }

        public bool IsHero => Base.IsHero;
        public bool IsElite => Root == Culture.RootElite;
        public bool IsCustom => StringId.StartsWith(CustomTroopPrefix);
        public bool IsVanilla => !IsCustom;

        public WCulture Culture => WCulture.Get(Base.Culture);

        [Persistent]
        [Reflected(setterName: "SetName")]
        public TextObject Name
        {
            get
            {
                var stored = Get<TextObject>();
                if (stored != null)
                    return stored;

                // Fallback: whatever the game currently has.
                return Base.Name;
            }
            set { SetRef(value); }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Formation Class                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public FormationClass FormationClass => Base.GetFormationClass();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Hierarchy                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Logical invalidator name for all hierarchy-derived caches.
        private const string HierarchyInvalidator = "Hierarchy";

        /// <summary>
        /// Troops this one can upgrade into (children in the upgrade tree).
        /// Backed by CharacterObject.UpgradeTargets via reflection.
        /// </summary>
        [Cached(HierarchyInvalidator)]
        public IReadOnlyList<WCharacter> UpgradeTargets
        {
            get =>
                GetCached<IReadOnlyList<WCharacter>>(
                    () =>
                    {
                        var raw = UpgradeTargetsRaw;
                        if (raw == null || raw.Length == 0)
                            return [];

                        var list = new List<WCharacter>(raw.Length);
                        for (int i = 0; i < raw.Length; i++)
                        {
                            var co = raw[i];
                            if (co == null)
                                continue;

                            var child = Get(co);
                            if (child != null)
                                list.Add(child);
                        }

                        return list;
                    },
                    nameof(UpgradeTargets)
                );
            set
            {
                CharacterObject[] raw;

                if (value == null)
                {
                    raw = [];
                }
                else if (value is IReadOnlyList<WCharacter> roList)
                {
                    raw = new CharacterObject[roList.Count];
                    for (int i = 0; i < roList.Count; i++)
                        raw[i] = roList[i]?.Base;
                }
                else
                {
                    var tmp = new List<CharacterObject>();
                    foreach (var w in value)
                    {
                        if (w?.Base != null)
                            tmp.Add(w.Base);
                    }

                    raw = [.. tmp];
                }

                // Push back into CharacterObject via reflection.
                UpgradeTargetsRaw = raw;

                // Invalidate this line's cached hierarchy values.
                InvalidateCachesFor(HierarchyInvalidator, StringId);

                // Rebuild global upgrade sources map (currently global; can be
                // optimized to per-line if needed).
                UpgradeCache.Recompute(this);
            }
        }

        /// <summary>
        /// Troops that can upgrade into this one (parents / sources).
        /// Backed by a global UpgradeMap, computed once.
        /// </summary>
        public IReadOnlyList<WCharacter> UpgradeSources => UpgradeCache.GetSources(this);

        [Cached(HierarchyInvalidator)]
        public int Depth =>
            GetCached(() =>
            {
                int depth = 0;
                var visited = new HashSet<WCharacter>();
                var queue = new Queue<(WCharacter character, int currentDepth)>();

                visited.Add(this);
                queue.Enqueue((this, 0));

                while (queue.Count > 0)
                {
                    var (current, currentDepth) = queue.Dequeue();
                    depth = System.Math.Max(depth, currentDepth);

                    var sources = current.UpgradeSources;
                    for (int i = 0; i < sources.Count; i++)
                    {
                        var s = sources[i];
                        if (s != null && visited.Add(s))
                            queue.Enqueue((s, currentDepth + 1));
                    }
                }

                return depth;
            });

        [Cached(HierarchyInvalidator)]
        public bool IsRoot =>
            GetCached(() =>
            {
                var sources = UpgradeSources;
                return sources == null || sources.Count == 0;
            });

        [Cached(HierarchyInvalidator)]
        public WCharacter Root =>
            GetCached(() =>
            {
                // BFS upwards using UpgradeSources to find any ancestor with no sources.
                var visited = new HashSet<WCharacter>();
                var queue = new Queue<WCharacter>();

                visited.Add(this);
                queue.Enqueue(this);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    var sources = current.UpgradeSources;

                    if (sources == null || sources.Count == 0)
                        return current;

                    for (int i = 0; i < sources.Count; i++)
                    {
                        var s = sources[i];
                        if (s != null && visited.Add(s))
                            queue.Enqueue(s);
                    }
                }

                // Fallback (should only happen on cycles where everyone has sources)
                return this;
            });

        [Cached(HierarchyInvalidator)]
        public List<WCharacter> Tree =>
            GetCached(() =>
            {
                var result = new List<WCharacter>();
                var visited = new HashSet<WCharacter>();
                var stack = new Stack<WCharacter>();

                // Start from this character.
                result.Add(this);

                // Seed with direct children.
                var directChildren = UpgradeTargets;
                for (int i = 0; i < directChildren.Count; i++)
                {
                    var child = directChildren[i];
                    if (child != null && visited.Add(child))
                        stack.Push(child);
                }

                while (stack.Count > 0)
                {
                    var node = stack.Pop();
                    result.Add(node);

                    var children = node.UpgradeTargets;
                    for (int i = 0; i < children.Count; i++)
                    {
                        var c = children[i];
                        if (c != null && visited.Add(c))
                            stack.Push(c);
                    }
                }

                return result;
            });

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Equipments                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly FieldInfo EquipmentRosterField =
            typeof(BasicCharacterObject).GetField(
                "_equipmentRoster",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

        private static readonly FieldInfo EquipmentsField = typeof(MBEquipmentRoster).GetField(
            "_equipments",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        [Cached]
        public IReadOnlyList<MEquipment> Equipments =>
            GetCached(() =>
            {
                var roster = GetEquipmentRoster();
                if (roster == null)
                    return [];

                var all = roster.AllEquipments;
                if (all == null || all.Count == 0)
                    return [];

                var list = new List<MEquipment>(all.Count);

                for (int i = 0; i < all.Count; i++)
                {
                    var equipment = all[i];
                    if (equipment == null)
                        continue;

                    var wrapped = MEquipment.Wrap(equipment);
                    if (wrapped != null)
                        list.Add(wrapped);
                }

                return (IReadOnlyList<MEquipment>)list;
            });

        private MBEquipmentRoster GetEquipmentRoster()
        {
            if (EquipmentRosterField == null)
                return null;

            return EquipmentRosterField.GetValue(Base) as MBEquipmentRoster;
        }

        private List<Equipment> GetMutableEquipments()
        {
            var roster = GetEquipmentRoster();
            if (roster == null || EquipmentsField == null)
                return null;

            return EquipmentsField.GetValue(roster) as List<Equipment>;
        }

        public int EquipmentCount => Equipments.Count;

        public int AddEquipment(Equipment equipment, bool cloneBase = true)
        {
            if (equipment == null || Base.IsHero)
                return -1;

            var list = GetMutableEquipments();
            if (list == null)
                return -1;

            var entry = cloneBase ? equipment.Clone() : equipment;
            list.Add(entry);

            InvalidateCache(nameof(Equipments));

            return list.Count - 1;
        }

        public bool RemoveEquipmentAt(int index)
        {
            if (Base.IsHero)
                return false;

            var list = GetMutableEquipments();
            if (list == null)
                return false;

            if ((uint)index >= (uint)list.Count)
                return false;

            list.RemoveAt(index);
            InvalidateCache(nameof(Equipments));

            return true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Image                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

# if BL13
        public CharacterCode GetCharacterCode(bool civilian = false) =>
            CharacterCode.CreateFrom(
                Base,
                civilian ? Base.FirstCivilianEquipment : Base.FirstBattleEquipment
            );

        public CharacterImageIdentifierVM GetImage(bool civilian = false) =>
            new(GetCharacterCode(civilian));

        public ImageIdentifier GetImageIdentifier(bool civilian = false) =>
            new CharacterImageIdentifier(GetCharacterCode(civilian));
#else
        public CharacterCode GetCharacterCode(bool civilian = false) =>
            CharacterCode.CreateFrom(Base); // 1.2.x does not support setting civilian boolean.

        public ImageIdentifierVM GetImage(bool civilian = false) => new(GetCharacterCode(civilian));

        public ImageIdentifier GetImageIdentifier(bool civilian = false) =>
            new(GetCharacterCode(civilian));
# endif

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

        public float Age
        {
            get => Base.Age;
            set => Base.Age = value;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Upgrades                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [Reflected(memberName: nameof(CharacterObject.UpgradeTargets))]
        private CharacterObject[] UpgradeTargetsRaw
        {
            get => GetRef<CharacterObject[]>() ?? [];
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
