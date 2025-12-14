using System.Collections.Generic;
using System.Linq;
using Retinues.Model.Factions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace Retinues.Model.Characters
{
    /// <summary>
    /// Wrapper for Hero.
    /// </summary>
    public class WHero(Hero @base) : WBase<WHero, Hero>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Main                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Persisted "first name" string; setter re-applies template-aware full name.
        MAttribute<string> NameAttribute =>
            _nameAttribute ??= new MAttribute<string>(
                baseInstance: Base,
                getter: _ => Base.FirstName?.ToString() ?? Base.Name?.ToString() ?? string.Empty,
                setter: (_, value) => ApplyName(value),
                targetName: "name",
                persistent: true
            );
        MAttribute<string> _nameAttribute;

        public string Name
        {
            get => NameAttribute.Get();
            set => NameAttribute.Set(value);
        }

        /// <summary>
        /// Applies the first name while preserving the name template if any.
        /// </summary>
        void ApplyName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            var first = new TextObject(value);

            var template = Base.Name;
            TextObject full;

            if (template != null)
            {
                // Preserve existing variables/attributes where possible.
                full = template.CopyTextObject();

                var oldFirst = Base.FirstName?.ToString();
                var templateValue = full.Value ?? string.Empty;

                if (templateValue.Contains("{FIRSTNAME}"))
                {
                    full.SetTextVariable("FIRSTNAME", first);
                }
                else
                {
                    var oldDisplay = template.ToString();
                    string newDisplay;

                    if (
                        !string.IsNullOrEmpty(oldFirst)
                        && !string.IsNullOrEmpty(oldDisplay)
                        && oldDisplay.StartsWith(oldFirst)
                    )
                    {
                        newDisplay = value + oldDisplay.Substring(oldFirst.Length);
                    }
                    else
                    {
                        newDisplay = value;
                    }

                    full = new TextObject(newDisplay);
                }
            }
            else
            {
                full = new TextObject(value);
            }

            Base.SetName(full, first);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Character                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter Character => WCharacter.Get(Base.CharacterObject);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsLord => Base.IsLord;
        public bool IsCompanion => Base.IsPlayerCompanion;
        public bool IsMainHero => Base.StringId == Hero.MainHero.StringId;
        public bool IsFactionLeader => Base.IsFactionLeader;
        public bool IsNotable => Base.IsNotable;
        public bool IsPartyLeader => Base.IsPartyLeader;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Factions                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WClan Clan => WClan.Get(Base.Clan);

        public WKingdom Kingdom => WKingdom.Get(Base.Clan?.Kingdom);

        public IBaseFaction Faction
        {
            get
            {
                if (Base.MapFaction is Clan clan)
                    return WClan.Get(clan);

                if (Base.MapFaction is Kingdom kingdom)
                    return WKingdom.Get(kingdom);

                return null;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Culture                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<CultureObject> CultureAttribute => Attribute(h => h.Culture);

        public WCulture Culture
        {
            get => WCulture.Get(CultureAttribute.Get());
            set => CultureAttribute.Set(value?.Base);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Visuals                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<bool> IsFemaleAttribute =>
            _isFemaleAttribute ??= new MAttribute<bool>(
                baseInstance: Base,
                getter: _ => Base.IsFemale,
                setter: (_, value) => ApplyIsFemale(value),
                targetName: "is_female",
                persistent: true
            );
        MAttribute<bool> _isFemaleAttribute;

        public bool IsFemale
        {
            get => IsFemaleAttribute.Get();
            set => IsFemaleAttribute.Set(value);
        }

        static void ApplyIsFemaleInternal(Hero hero, bool value)
        {
            if (hero == null)
                return;

#if BL13
            hero.IsFemale = value;
#else
            // BL12: IsFemale has a private setter; use reflection.
            Reflection.SetPropertyValue(hero, "IsFemale", value);
#endif
        }

        void ApplyIsFemale(bool value) => ApplyIsFemaleInternal(Base, value);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Level                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<int> LevelAttribute => Attribute(h => h.Level);

        public int Level
        {
            get => LevelAttribute.Get();
            set => LevelAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Skills                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static IEnumerable<SkillObject> SkillList =>
            Helpers.Skills.GetSkillListForHero(includeModded: true);

        MAttribute<Dictionary<string, int>> SkillsMapAttribute =>
            _skillsMapAttribute ??= new MAttribute<Dictionary<string, int>>(
                baseInstance: Base,
                getter: _ => BuildSkillsMap(),
                setter: (_, value) => ApplySkillsMap(value),
                targetName: "skills",
                persistent: true
            );
        MAttribute<Dictionary<string, int>> _skillsMapAttribute;

        Dictionary<string, int> BuildSkillsMap()
        {
            var map = new Dictionary<string, int>();

            foreach (var s in SkillList)
            {
                if (s == null || string.IsNullOrEmpty(s.StringId))
                    continue;

                map[s.StringId] = Base.GetSkillValue(s);
            }

            return map;
        }

        void ApplySkillsMap(Dictionary<string, int> map)
        {
            if (map == null || map.Count == 0)
                return;

            var manager = MBObjectManager.Instance;
            if (manager == null)
                return;

            foreach (var pair in map)
            {
                var id = pair.Key;
                if (string.IsNullOrEmpty(id))
                    continue;

                var skill = manager.GetObject<SkillObject>(id);
                if (skill == null)
                    continue;

                Base.SetSkillValue(skill, pair.Value);
            }
        }

        public int GetSkill(SkillObject skill)
        {
            if (skill == null)
                return 0;

            return Base.GetSkillValue(skill);
        }

        public void SetSkill(SkillObject skill, int value)
        {
            if (skill == null)
                return;

            Base.SetSkillValue(skill, value);

            // Ensure persistence writes this hero's skills on next save.
            SkillsMapAttribute.Touch();
        }

        public Dictionary<SkillObject, int> Skills
        {
            get => SkillList.ToDictionary(s => s, Base.GetSkillValue);
            set
            {
                var map = new Dictionary<string, int>();

                if (value != null)
                {
                    foreach (var pair in value)
                    {
                        var skill = pair.Key;
                        if (skill == null || string.IsNullOrEmpty(skill.StringId))
                            continue;

                        map[skill.StringId] = pair.Value;
                    }
                }

                SkillsMapAttribute.Set(map);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Traits                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static TraitObject[] PersonalityTraits =>
            [
                DefaultTraits.Mercy,
                DefaultTraits.Valor,
                DefaultTraits.Honor,
                DefaultTraits.Generosity,
                DefaultTraits.Calculating,
            ];

        MAttribute<Dictionary<string, int>> TraitsMapAttribute =>
            _traitsMapAttribute ??= new MAttribute<Dictionary<string, int>>(
                baseInstance: Base,
                getter: _ => BuildTraitsMap(),
                setter: (_, value) => ApplyTraitsMap(value),
                targetName: "traits",
                persistent: true
            );
        MAttribute<Dictionary<string, int>> _traitsMapAttribute;

        Dictionary<string, int> BuildTraitsMap()
        {
            var map = new Dictionary<string, int>();

            foreach (var t in PersonalityTraits)
            {
                if (t == null || string.IsNullOrEmpty(t.StringId))
                    continue;

                map[t.StringId] = Base.GetTraitLevel(t);
            }

            return map;
        }

        void ApplyTraitsMap(Dictionary<string, int> map)
        {
            if (map == null || map.Count == 0)
                return;

            var manager = MBObjectManager.Instance;
            if (manager == null)
                return;

            foreach (var pair in map)
            {
                var id = pair.Key;
                if (string.IsNullOrEmpty(id))
                    continue;

                var trait = manager.GetObject<TraitObject>(id);
                if (trait == null)
                    continue;

                Base.SetTraitLevel(trait, pair.Value);
            }
        }

        public int GetTrait(TraitObject trait)
        {
            if (trait == null)
                return 0;

            return Base.GetTraitLevel(trait);
        }

        public void SetTrait(TraitObject trait, int value)
        {
            if (trait == null)
                return;

            Base.SetTraitLevel(trait, value);
            TraitsMapAttribute.Touch();
        }

        public Dictionary<TraitObject, int> Traits
        {
            get => PersonalityTraits.ToDictionary(t => t, t => Base.GetTraitLevel(t));
            set
            {
                var map = new Dictionary<string, int>();

                if (value != null)
                {
                    foreach (var pair in value)
                    {
                        var trait = pair.Key;
                        if (trait == null || string.IsNullOrEmpty(trait.StringId))
                            continue;

                        map[trait.StringId] = pair.Value;
                    }
                }

                TraitsMapAttribute.Set(map);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Volunteers                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public List<WCharacter> Volunteers => [.. Base.VolunteerTypes.Select(WCharacter.Get)];
    }
}
