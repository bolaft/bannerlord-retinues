using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Core.Game;
using Retinues.Core.Game.Features.Tech;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI.VM.Tech
{
    public sealed class TechNodeVM : ViewModel, IView
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private readonly TechTreeVM _tree;
        private readonly DoctrineDef _def;
        private DoctrineState _state;

        public TechNodeVM(DoctrineDef def, DoctrineState state, TechTreeVM tree)
        {
            _def = def;
            _state = state ?? new DoctrineState { Id = def.Id, Status = DoctrineStatus.Locked };
            _tree = tree;
        }

        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty] public string Title => _def.Title;
        [DataSourceProperty] public string Description => _def.Description;
        [DataSourceProperty] public string IconId => _def.IconId;
        [DataSourceProperty] public int Column => _def.Column;
        [DataSourceProperty] public int Row => _def.Row;
        [DataSourceProperty] public int GoldCost => _def.GoldCost;

        [DataSourceProperty] public bool IsLocked => _state.Status == DoctrineStatus.Locked;
        [DataSourceProperty] public bool IsAvailable => _state.Status == DoctrineStatus.Available;
        [DataSourceProperty] public bool IsResearching => _state.Status == DoctrineStatus.Researching;
        [DataSourceProperty] public bool IsCompleted => _state.Status == DoctrineStatus.Completed;

        [DataSourceProperty]
        public string StatusText =>
            IsCompleted   ? "Unlocked"
          : IsResearching ? $"Researching ({RemainingTimeText})"
          : IsAvailable   ? "Available"
          : "Locked";

        [DataSourceProperty]
        public float Progress01
        {
            get
            {
                if (!IsResearching) return IsCompleted ? 1f : 0f;
                var totalH = (float)(_def.Duration.ToHours);
                var leftH = (float)(_state.EndTime - CampaignTime.Now).ToHours;
                var doneH = MathF.Max(0f, totalH - MathF.Max(0f, leftH));
                return totalH <= 0.01f ? 1f : MathF.Clamp(doneH / totalH, 0f, 1f);
            }
        }

        [DataSourceProperty]
        public string RemainingTimeText
        {
            get
            {
                if (!IsResearching) return string.Empty;
                var left = _state.EndTime - CampaignTime.Now;
                var hours = Math.Max(0, (int)left.ToHours);
                if (hours >= 24) return $"{hours/24}d {hours%24}h";
                return $"{hours}h";
            }
        }

        [DataSourceProperty]
        public string RequirementText
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrEmpty(_def.PrerequisiteId))
                    parts.Add($"Requires: {_tree.GetTitle(_def.PrerequisiteId)}");
                if (!string.IsNullOrEmpty(_def.FeatId))
                    parts.Add(_tree.GetFeatText(_def.FeatId));
                if (_def.RequiredSkill != null && _def.RequiredSkillValue > 0)
                    parts.Add($"Companion: {_def.RequiredSkill.Name} ≥ {_def.RequiredSkillValue}");
                return string.Join("\n", parts);
            }
        }

        [DataSourceProperty]
        public string AssignedCompanionName
        {
            get
            {
                if (!IsResearching || string.IsNullOrEmpty(_state.AssignedHeroId)) return "None";
                var hero = _tree.ResolveHeroById(_state.AssignedHeroId);
                return hero?.Name?.ToString() ?? "None";
            }
        }

        [DataSourceProperty]
        public bool CanStartResearch => _tree.CanStartResearch(_def, _state);

        [DataSourceProperty]
        public bool CanCancelResearch => IsResearching;

        [DataSourceProperty]
        public bool CanCollect => IsResearching && CampaignTime.Now >= _state.EndTime;

        // =========================================================================
        // Action Bindings
        // =========================================================================

        [DataSourceMethod]
        public void ExecuteStartResearch()
        {
            try
            {
                if (!CanStartResearch) return;

                // Select a valid companion (you can replace this with a proper picker later)
                var hero = _tree.SelectEligibleCompanion(_def);
                if (hero == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("No eligible companion."));
                    return;
                }

                if (Player.Gold < _def.GoldCost)
                {
                    InformationManager.DisplayMessage(new InformationMessage("Not enough gold."));
                    return;
                }
                Player.ChangeGold(-_def.GoldCost);

                _state.Status = DoctrineStatus.Researching;
                _state.AssignedHeroId = hero.StringId;
                _state.StartTime = CampaignTime.Now;
                _state.EndTime = CampaignTime.Now + _def.Duration;

                _tree.SetState(_state);
                Refresh();
                _tree.Refresh();
            }
            catch (Exception e) { Log.Exception(e); }
        }

        [DataSourceMethod]
        public void ExecuteCancelResearch()
        {
            if (!CanCancelResearch) return;
            // Optional: partial refund, release companion, etc.
            _state.Status = DoctrineStatus.Available;
            _state.AssignedHeroId = null;
            _state.StartTime = CampaignTime.Zero;
            _state.EndTime = CampaignTime.Zero;
            _tree.SetState(_state);
            Refresh();
            _tree.Refresh();
        }

        [DataSourceMethod]
        public void ExecuteCollect()
        {
            if (!CanCollect) return;
            _state.Status = DoctrineStatus.Completed;
            _state.AssignedHeroId = null;
            _state.StartTime = CampaignTime.Zero;
            _state.EndTime = CampaignTime.Zero;
            _tree.SetState(_state);
            _tree.OnDoctrineCompleted(_def);
            Refresh();
            _tree.Refresh();
        }

        // =========================================================================
        // Public API
        // =========================================================================

        public DoctrineDef Definition => _def;

        public void RecomputeStatus()
        {
            if (IsCompleted || IsResearching) { OnPropertyChanged(nameof(StatusText)); return; }

            bool prereqOk = string.IsNullOrEmpty(_def.PrerequisiteId) || _tree.IsCompleted(_def.PrerequisiteId);
            bool featOk = string.IsNullOrEmpty(_def.FeatId) || _tree.IsFeatMet(_def.FeatId);

            _state.Status = (prereqOk && featOk) ? DoctrineStatus.Available : DoctrineStatus.Locked;
            _tree.SetState(_state);
            Refresh();
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(IconId));
            OnPropertyChanged(nameof(GoldCost));

            OnPropertyChanged(nameof(IsLocked));
            OnPropertyChanged(nameof(IsAvailable));
            OnPropertyChanged(nameof(IsResearching));
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(StatusText));

            OnPropertyChanged(nameof(AssignedCompanionName));
            OnPropertyChanged(nameof(Progress01));
            OnPropertyChanged(nameof(RemainingTimeText));
            OnPropertyChanged(nameof(CanStartResearch));
            OnPropertyChanged(nameof(CanCancelResearch));
            OnPropertyChanged(nameof(CanCollect));
            OnPropertyChanged(nameof(RequirementText));
        }
    }
}
