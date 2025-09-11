using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game;
using Retinues.Core.Game.Features.Tech;
using Retinues.Core.Game.Features.Tech.Behaviors;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI.VM.Tech
{
    // =========================================================================
    // Interfaces
    // =========================================================================

    public interface IDoctrineRepository
    {
        IEnumerable<DoctrineDef> All();
        DoctrineState Get(string id);
        void Put(DoctrineState state);
    }

    // Simple in-memory fallback so the UI runs before you wire behaviors.
    public sealed class InMemoryDoctrineRepository : IDoctrineRepository
    {
        private readonly Dictionary<string, DoctrineState> _states = new();
        public IEnumerable<DoctrineDef> _defs = Enumerable.Empty<DoctrineDef>();
        public IEnumerable<DoctrineDef> All() => _defs;
        public DoctrineState Get(string id) => _states.TryGetValue(id, out var s) ? s : null;
        public void Put(DoctrineState s) => _states[s.Id] = s;
    }

    public sealed class TechTreeVM : ViewModel, IView
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private readonly IDoctrineRepository _repo;
        private readonly IFeatService _feats;

        private readonly Dictionary<string, TechNodeVM> _nodesById = new();

        public TechTreeVM(IDoctrineRepository repo, IFeatService feats)
        {
            _repo = repo;
            _feats = feats;

            // 4 columns
            Columns = new MBBindingList<TechColumnVM>
            {
                new(0), new(1), new(2), new(3)
            };

            BuildFromRepo();
        }

        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty] public MBBindingList<TechColumnVM> Columns { get; private set; }

        [DataSourceProperty] public string Title => "Doctrines"; // rename if you like

        // =========================================================================
        // Public API
        // =========================================================================

        public void Refresh()
        {
            foreach (var c in Columns) c.Refresh();
            OnPropertyChanged(nameof(Columns));
        }

        public void RecomputeAllStatuses()
        {
            foreach (var vm in _nodesById.Values)
                vm.RecomputeStatus();
            Refresh();
        }

        // ---------------------------------------------------------------------
        // Status helpers (used by TechNodeVM)
        // ---------------------------------------------------------------------

        public bool IsCompleted(string doctrineId)
            => _repo.Get(doctrineId)?.Status == DoctrineStatus.Completed;

        public bool IsFeatMet(string featId) => string.IsNullOrEmpty(featId) || _feats?.IsFeatMet(featId) == true;

        public string GetFeatText(string featId) => _feats?.DescribeFeat(featId) ?? "Feat required";

        public string GetTitle(string doctrineId)
            => _repo.All().FirstOrDefault(d => d.Id == doctrineId)?.Title ?? doctrineId;

        public bool CanStartResearch(DoctrineDef def, DoctrineState state)
        {
            if (state.Status == DoctrineStatus.Completed || state.Status == DoctrineStatus.Researching)
                return false;

            bool prereqOk = string.IsNullOrEmpty(def.PrerequisiteId) || IsCompleted(def.PrerequisiteId);
            bool featOk = string.IsNullOrEmpty(def.FeatId) || IsFeatMet(def.FeatId);
            if (!prereqOk || !featOk) return false;

            if (Player.Gold < def.GoldCost) return false;

            // Need at least one eligible companion
            var hero = SelectEligibleCompanion(def);
            return hero != null;
        }

        public Hero SelectEligibleCompanion(DoctrineDef def)
        {
            // Simple default rule: companions in player's clan party that meet skill
            var heroes = Clan.PlayerClan?.Companions ?? Enumerable.Empty<Hero>();
            if (def.RequiredSkill == null || def.RequiredSkillValue <= 0)
                return heroes.FirstOrDefault(); // any companion will do
            return heroes.FirstOrDefault(h => _feats?.IsEligible(h, def.RequiredSkill, def.RequiredSkillValue) == true);
        }

        public void SetState(DoctrineState s) => _repo.Put(s);

        public Hero ResolveHeroById(string heroId)
            => Hero.FindFirst(h => h.StringId == heroId);

        public void OnDoctrineCompleted(DoctrineDef def)
        {
            // Hook: later you’ll attach real effects via behaviors/patches.
            Log.Debug($"Doctrine completed: {def.Title}");
        }

        // =========================================================================
        // Internals
        // =========================================================================

        private void BuildFromRepo()
        {
            _nodesById.Clear();
            var defs = _repo.All() ?? Enumerable.Empty<DoctrineDef>();

            var nodeGroups = defs
                .GroupBy(d => d.Column)
                .ToDictionary(g => g.Key, g => g.OrderBy(d => d.Row).ToList());

            foreach (var col in Columns)
            {
                var defsInCol = nodeGroups.TryGetValue(col.Index, out var list) ? list : new List<DoctrineDef>();
                var nodes = new List<TechNodeVM>();
                foreach (var def in defsInCol)
                {
                    var state = _repo.Get(def.Id) ?? new DoctrineState { Id = def.Id, Status = DoctrineStatus.Locked };
                    var vm = new TechNodeVM(def, state, this);
                    nodes.Add(vm);
                    _nodesById[def.Id] = vm;
                }
                col.SetItems(nodes);
            }

            RecomputeAllStatuses();
        }
    }
}
