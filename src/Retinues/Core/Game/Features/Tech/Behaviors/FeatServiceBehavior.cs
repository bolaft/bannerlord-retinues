using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Core.Game.Features.Tech.Behaviors
{
    public interface IFeatService
    {
        bool IsFeatMet(string featId);        // e.g., "defeat_king_party"
        string DescribeFeat(string featId);   // for UI
        bool IsEligible(Hero hero, SkillObject skill, int minValue);
    }

    // Feat IDs to avoid stringly-typed mistakes.
    public static class FeatIds
    {

    }

    // Tracks feat completion flags and exposes eligibility checks for companions.
    public sealed class FeatServiceBehavior : CampaignBehaviorBase, IFeatService
    {
        // Singleton convenience for UI code that doesn't use DI
        public static FeatServiceBehavior Instance { get; private set; }

        // Persisted state: feat -> completed?
        private Dictionary<string, bool> _flags = new();

        // Texts for UI
        private static readonly Dictionary<string, string> _descriptions = new()
        {
            
        };

        public FeatServiceBehavior()
        {
            Instance = this;
        }

        // ---------------------------------------------------------------------
        // IFeatService
        // ---------------------------------------------------------------------

        public bool IsFeatMet(string featId)
            => !string.IsNullOrEmpty(featId) && _flags.TryGetValue(featId, out var v) && v;

        public string DescribeFeat(string featId)
            => _descriptions.TryGetValue(featId ?? "", out var d) ? d : "Complete the associated deed.";

        public bool IsEligible(Hero hero, SkillObject skill, int minValue)
        {
            if (hero == null || hero.IsDead || hero.IsPrisoner) return false;
            if (skill == null || minValue <= 0) return true; // no specific requirement
            return hero.GetSkillValue(skill) >= minValue;
        }

        // ---------------------------------------------------------------------
        // Event wiring
        // ---------------------------------------------------------------------

        public override void RegisterEvents()
        {
            
        }

        // ---------------------------------------------------------------------
        // Sync
        // ---------------------------------------------------------------------

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("Retinues_FeatFlags", ref _flags);
            _flags ??= new Dictionary<string, bool>();
        }
    }
}
