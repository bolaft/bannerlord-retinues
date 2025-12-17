using System.Collections.Generic;
using System.Linq;
using Retinues.Model.Equipments;
using Retinues.Model.Factions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Localization;
#if BL12
using Retinues.Utilities;
#endif

namespace Retinues.Model.Characters
{
    /// <summary>
    /// Wrapper for Hero.
    /// </summary>
    public class WHero(Hero @base) : WBase<WHero, Hero>(@base), IEditableUnit
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Name                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Name
        {
            get => Base.FirstName?.ToString() ?? Base.Name?.ToString() ?? string.Empty;
            set => ApplyName(value);
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
                        newDisplay = value + oldDisplay.Substring(oldFirst.Length);
                    else
                        newDisplay = value;

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
        public bool IsClanLeader => Base.IsClanLeader;
        public bool IsFactionLeader => Base.IsFactionLeader;
        public bool IsNotable => Base.IsNotable;
        public bool IsPartyLeader => Base.IsPartyLeader;
        public bool IsDead => Base.IsDead;

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

        public WCulture Culture
        {
            get => WCulture.Get(Base.Culture);
            set => Base.Culture = value?.Base;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Visuals                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsFemale
        {
            get => Base.IsFemale;
            set => ApplyIsFemaleInternal(Base, value);
        }

        static void ApplyIsFemaleInternal(Hero hero, bool value)
        {
            if (hero == null)
                return;

#if BL13
            hero.IsFemale = value;
#else
            Reflection.SetPropertyValue(hero, "IsFemale", value);
#endif
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Level                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int Level
        {
            get => Base.Level;
            set => Base.Level = value;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equipment                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public List<MEquipment> Equipments => [BattleEquipment, CivilianEquipment];

        public MEquipment BattleEquipment => new(Base.BattleEquipment, Character);
        public MEquipment CivilianEquipment => new(Base.CivilianEquipment, Character);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Skills                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private HeroSkills _skills;
        public HeroSkills Skills => _skills ??= new HeroSkills(this);

        IEditableSkills IEditableUnit.Skills => Skills;

        public class HeroSkills(WHero wh) : IEditableSkills
        {
            public int Get(SkillObject skill)
            {
                if (skill == null)
                    return 0;

                return wh.Base.GetSkillValue(skill);
            }

            public void Set(SkillObject skill, int value)
            {
                if (skill == null)
                    return;

                wh.Base.SetSkillValue(skill, value);
            }

            public void Modify(SkillObject skill, int amount)
            {
                if (skill == null)
                    return;

                Set(skill, Get(skill) + amount);
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
        }

        public Dictionary<TraitObject, int> Traits
        {
            get => PersonalityTraits.ToDictionary(t => t, t => Base.GetTraitLevel(t));
            set
            {
                if (value == null)
                    return;

                foreach (var pair in value)
                {
                    if (pair.Key == null)
                        continue;

                    Base.SetTraitLevel(pair.Key, pair.Value);
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Volunteers                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public List<WCharacter> Volunteers => [.. Base.VolunteerTypes.Select(WCharacter.Get)];
    }
}
