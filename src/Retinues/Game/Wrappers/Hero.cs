using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Game.Helpers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
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
        /// Hero name (full name as string).
        /// </summary>
        public override string Name
        {
            // Display just the first name if present, otherwise fall back to full name
            get => _hero.FirstName?.ToString() ?? _hero.Name?.ToString();
            set
            {
                if (_hero == null || string.IsNullOrWhiteSpace(value))
                    return;

                var first = new TextObject(value);
                var full = new TextObject(
                    _hero
                        .Name.Value.Replace(_hero.FirstName?.ToString() ?? "", first.ToString())
                        .Trim()
                );

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
                    return TroopSkills.ToDictionary(s => s, _ => 0);

                return TroopSkills.ToDictionary(s => s, s => _hero.GetSkillValue(s));
            }
            set
            {
                if (_hero == null)
                    return;

                foreach (var skill in TroopSkills)
                {
                    var v = (value != null && value.TryGetValue(skill, out var val)) ? val : 0;
                    _hero.SetSkillValue(skill, v);
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
