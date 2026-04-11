using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Characters.Services.Skills;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Model;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Localization;
#if BL12
using Retinues.Utilities;
#endif

namespace Retinues.Domain.Characters.Wrappers
{
    /// <summary>
    /// Wrapper for Hero providing convenience accessors and helpers.
    /// </summary>
    public class WHero(Hero @base) : WBase<WHero, Hero>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Resolver                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets all WParty instances in the current campaign.
        /// </summary>
        public static new IEnumerable<WHero> All
        {
            get
            {
                var campaign = Campaign.Current;
                if (campaign == null)
                    yield break;

                var heroes = campaign.AliveHeroes;
                if (heroes == null)
                    yield break;

                foreach (var h in heroes)
                {
                    if (h != null)
                        yield return Get(h);
                }
            }
        }

        /// <summary>
        /// Static constructor that registers the WHero resolver.
        /// </summary>
        static WHero() => RegisterResolver(ResolveHero);

        /// <summary>
        /// Resolves a Hero by its string id from alive or dead hero lists.
        /// </summary>
        static Hero ResolveHero(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            var campaign = Campaign.Current;
            if (campaign == null)
                return null;

            var alive = campaign.AliveHeroes;
            if (alive != null)
            {
                for (int i = 0; i < alive.Count; i++)
                {
                    var h = alive[i];
                    if (h != null && h.StringId == id)
                        return h;
                }
            }

            var dead = campaign.DeadOrDisabledHeroes;
            if (dead != null)
            {
                for (int i = 0; i < dead.Count; i++)
                {
                    var h = dead[i];
                    if (h != null && h.StringId == id)
                        return h;
                }
            }

            return null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Name                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Name
        {
            get => Base.FirstName?.ToString() ?? Base.Name?.ToString() ?? string.Empty;
            set => ApplyName(value);
        }

        /// <summary>
        /// Applies a new display name to the hero, preserving template formatting.
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

        /// <summary>
        /// Internal helper to apply the IsFemale flag in a version-safe way.
        /// </summary>
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
        //                         Skills                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private HeroSkills _skills;

        public HeroSkills Skills
        {
            get
            {
                _skills ??= new HeroSkills(this);
                return _skills;
            }
        }

        /// <summary>
        /// Clears the cached skills wrapper so it will be rebuilt.
        /// </summary>
        public void ClearSkillsCache() => _skills = null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Traits                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static TraitObject[] PersonalityTraits =>
            [
                DefaultTraits.Mercy,
                DefaultTraits.Valor,
                DefaultTraits.Honor,
                DefaultTraits.Generosity,
                DefaultTraits.Calculating,
            ];

        /// <summary>
        /// Returns the trait level for the provided trait.
        /// </summary>
        public int GetTrait(TraitObject trait)
        {
            if (trait == null)
                return 0;

            return Base.GetTraitLevel(trait);
        }

        /// <summary>
        /// Sets the trait level for the provided trait.
        /// </summary>
        public void SetTrait(TraitObject trait, int value)
        {
            if (trait == null)
                return;

            Base.SetTraitLevel(trait, value);
        }

        public Dictionary<TraitObject, int> Traits
        {
            get => PersonalityTraits.ToDictionary(t => t, Base.GetTraitLevel);
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
        //                        Surname                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// The suffix/title portion of the hero's full name (e.g. "the Golden").
        /// Empty string when no surname is set.
        /// </summary>
        public string Surname
        {
            get
            {
                var firstName = Base.FirstName?.ToString();
                var fullName = Base.Name?.ToString();
                if (string.IsNullOrEmpty(fullName))
                    return string.Empty;
                if (!string.IsNullOrEmpty(firstName) && fullName.StartsWith(firstName))
                    return fullName.Substring(firstName.Length).TrimStart();
                return string.Empty;
            }
            set
            {
                var firstName = Base.FirstName?.ToString() ?? string.Empty;
                var first = new TextObject(firstName);
                TextObject full = string.IsNullOrWhiteSpace(value)
                    ? new TextObject(firstName)
                    : new TextObject($"{firstName} {value.Trim()}");
                Base.SetName(full, first);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Volunteers                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public List<WCharacter> Volunteers => [.. Base.VolunteerTypes.Select(WCharacter.Get)];
    }
}
