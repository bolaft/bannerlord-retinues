using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for Hero that reuses WCharacter but redirects hero-specific state
    /// (name, culture, skills, gender, clan/kingdom) to Hero instead of the template.
    /// </summary>
    [SafeClass]
    public class WHero(Hero hero) : WCharacter(hero?.CharacterObject)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Base                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Hero _hero = hero ?? throw new ArgumentNullException(nameof(hero));

        /// <summary>
        /// Underlying hero.
        /// </summary>
        public Hero Hero => _hero;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Identification / Faction             //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Use hero string id rather than character template id.
        /// </summary>
        public override string StringId => _hero.StringId;

        /// <summary>
        /// Hero name
        /// </summary>
        public override string Name
        {
            // Show first name if present, else full name
            get => _hero.FirstName?.ToString() ?? _hero.Name?.ToString();
            set
            {
                if (_hero == null || string.IsNullOrWhiteSpace(value))
                    return;

                var first = new TextObject(value);

                var template = _hero.Name;
                TextObject full;

                if (template != null)
                {
                    // Work on a copy so we preserve all existing attributes
                    full = template.CopyTextObject();

                    var oldFirst = _hero.FirstName?.ToString();
                    var templateValue = full.Value ?? string.Empty;

                    if (templateValue.Contains("{FIRSTNAME}"))
                    {
                        // Template-based hero ("{FIRSTNAME} the Bard") – just swap the variable
                        full.SetTextVariable("FIRSTNAME", first);
                    }
                    else
                    {
                        // Non-templated – try to replace the old first name in the *display* string
                        var oldDisplay = template.ToString();
                        string newDisplay;

                        if (
                            !string.IsNullOrEmpty(oldFirst)
                            && !string.IsNullOrEmpty(oldDisplay)
                            && oldDisplay.StartsWith(oldFirst)
                        )
                        {
                            // Preserve suffix like " the Bard"
                            newDisplay = value + oldDisplay.Substring(oldFirst.Length);
                        }
                        else
                        {
                            // No recognizable pattern, just use the new name as-is
                            newDisplay = value;
                        }

                        full = new TextObject(newDisplay);
                    }
                }
                else
                {
                    full = new TextObject(value);
                }

                _hero.SetName(full, first);
                NeedsPersistence = true;
            }
        }

        /// <summary>
        /// Hero culture wrapper.
        /// </summary>
        public override WCulture Culture
        {
            get => _hero.Culture == null ? null : new WCulture(_hero.Culture);
            set
            {
                var newCulture = value?.Base;
                if (newCulture == null || newCulture == _hero.Culture)
                    return;

                _hero.Culture = newCulture;
                NeedsPersistence = true;
            }
        }

        public override WFaction Clan => _hero.Clan == null ? null : new WFaction(_hero.Clan);

        public override WFaction Kingdom =>
            _hero.Clan?.Kingdom == null ? null : new WFaction(_hero.Clan.Kingdom);

        public bool IsPartyLeader => _hero.IsPartyLeader;

        public bool IsCompanion => _hero.IsPlayerCompanion;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Skills                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// For heroes, skills live on Hero, not on CharacterObject.DefaultCharacterSkills.
        /// Override the WCharacter skill API to hit Hero.GetSkillValue / SetSkillValue.
        /// </summary>
        public override int GetSkill(SkillObject skill)
        {
            if (_hero == null || skill == null)
                return 0;

            return _hero.GetSkillValue(skill);
        }

        public override void SetSkill(SkillObject skill, int value)
        {
            if (_hero == null || skill == null)
                return;

            _hero.SetSkillValue(skill, value);
            NeedsPersistence = true;
        }

        /// <summary>
        /// Shortcut for the troop-relevant skill set, mapped to hero skills.
        /// </summary>
        public override Dictionary<SkillObject, int> Skills
        {
            get
            {
                if (_hero == null)
                    return AllSkills.ToDictionary(s => s, _ => 0);

                return AllSkills.ToDictionary(s => s, s => _hero.GetSkillValue(s));
            }
            set
            {
                if (_hero == null)
                    return;

                foreach (var skill in AllSkills)
                {
                    var v = (value != null && value.TryGetValue(skill, out var val)) ? val : 0;
                    _hero.SetSkillValue(skill, v);
                }

                NeedsPersistence = true;
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

        /// <summary>
        /// Gets the hero's level in the given trait.
        /// </summary>
        public int GetTrait(TraitObject trait)
        {
            if (_hero == null || trait == null)
                return 0;

            return _hero.GetTraitLevel(trait);
        }

        /// <summary>
        /// Sets the hero's level in the given trait (clamped by the game).
        /// </summary>
        public void SetTrait(TraitObject trait, int value)
        {
            if (_hero == null || trait == null)
                return;

            _hero.SetTraitLevel(trait, value);
            NeedsPersistence = true;
        }

        /// <summary>
        /// Mirrors the WCharacter Skills API: expose traits as a dictionary.
        /// </summary>
        public Dictionary<TraitObject, int> Traits
        {
            get
            {
                if (_hero == null)
                    return [];

                return PersonalityTraits.ToDictionary(tr => tr, tr => _hero.GetTraitLevel(tr));
            }
            set
            {
                if (_hero == null || value == null)
                    return;

                foreach (var tr in PersonalityTraits)
                {
                    value.TryGetValue(tr, out int v);
                    _hero.SetTraitLevel(tr, v);
                }

                NeedsPersistence = true;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Visuals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override bool IsFemale
        {
            get => _hero?.IsFemale ?? base.IsFemale;
            set
            {
                if (_hero != null)
                {
#if BL13
                    _hero.IsFemale = value;
#else
                    // BL12: IsFemale has a private setter; use reflection/backing field.
                    Reflector.SetPropertyValue(_hero, "IsFemale", value);
#endif
                }
                else
                {
                    base.IsFemale = value;
                }

                NeedsPersistence = true;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Convenience                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override int Level
        {
            get => _hero.Level;
            set
            {
                _hero.Level = value;
                NeedsPersistence = true;
            }
        }
    }
}
