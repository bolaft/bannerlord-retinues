using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.Core;
using System.Text;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Core.Game.Features.Doctrines;
using Retinues.Core.Utils;

namespace Retinues.Core.Editor.UI.VM.Doctrines
{
    public sealed class DoctrineVM : ViewModel
    {
        private readonly string _id;
        private readonly DoctrineServiceBehavior _svc;
        private readonly DoctrineDef _def;
        private readonly string _name;

        public DoctrineVM(string doctrineId)
        {
            _id = doctrineId;
            _svc = Campaign.Current?.GetCampaignBehavior<DoctrineServiceBehavior>();
            _def = _svc?.GetDoctrine(_id);
            _name = _def?.Name ?? _id;

            Log.Debug($"Created DoctrineVM for {_id} - {_name}");
            
            Refresh();
        }

        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public DoctrineStatus Status
        {
            get { return DoctrineAPI.GetDoctrineStatus(_id); }
        }

        [DataSourceProperty]
        public string ButtonText
        {
            get
            {
                if (!IsEnabled) return _name;

                int total = _def?.Feats?.Count ?? 0;
                int complete = 0;
                if (total > 0 && _svc != null)
                    complete = _def.Feats.Count(f => _svc.IsFeatComplete(f.Key));
                return $"{_name} ({complete}/{total})";
            }
        }

        [DataSourceProperty]
        public string Description
        {
            get { return _def?.Description ?? string.Empty; }
        }

        [DataSourceProperty]
        public bool IsEnabled
        {
            get { return Status != DoctrineStatus.Locked && Status != DoctrineStatus.Unlocked; }
        }

        [DataSourceProperty]
        public string ButtonBrush
        {
            get { return Status == DoctrineStatus.Unlocked ? "ButtonBrush1" : "ButtonBrush2"; }
        }

        [DataSourceProperty]
        public int GoldCost
        {
            get { return _def?.GoldCost ?? 0; }
        }

        [DataSourceProperty]
        public int InfluenceCost
        {
            get { return _def?.InfluenceCost ?? 0; }
        }

        // =========================================================================
        // Action Bindings
        // =========================================================================

        [DataSourceMethod]
        public void ExecuteShowPopup()
        {
            var svc = Campaign.Current?.GetCampaignBehavior<DoctrineServiceBehavior>();
            var def = svc?.GetDoctrine(_id);

            if (svc == null || def == null)
                return;

            // Build feats text from real data
            var feats = def.Feats ?? [];
            int total = feats.Count;
            int complete = 0;

            var sb = new StringBuilder();
            foreach (var f in feats)
            {
                bool done = svc.IsFeatComplete(f.Key);
                if (done) complete++;

                string status = done ? "■" : "□";
                int prog = svc.GetFeatProgress(f.Key);
                int target = svc.GetFeatTarget(f.Key);

                if (target > 0)
                    sb.Append("    ").Append(status).Append("  ").Append(f.Description).Append(" (")
                    .Append(prog).Append('/').Append(target).Append(")\n");
                else
                    sb.Append("    ").Append(status).Append("  ").Append(f.Description).Append('\n');
            }

            string featsText = total == 0 ? L.S("feats_no_reqs", "No requirements.") : $"{L.S("feats_reqs","Requirements")}:\n\n{sb}";

            var costs = L.T("doctrine_costs", "Cost: {GOLD} Gold, {INFLUENCE} Influence.")
                .SetTextVariable("GOLD", GoldCost)
                .SetTextVariable("INFLUENCE", InfluenceCost)
                .ToString();

            var text = $"{Description}\n\n{featsText}\n\n{costs}";

            bool allComplete = total == 0 || complete == total;
            bool alreadyUnlocked = svc.IsDoctrineUnlocked(_id);

            if (allComplete && !alreadyUnlocked)
            {
                // Show Cancel / Unlock
                InformationManager.ShowInquiry(new InquiryData(
                    _name,
                    text.ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("unlock_btn", "Unlock"),
                    negativeText: GameTexts.FindText("str_cancel").ToString(),
                    affirmativeAction: () =>
                    {
                        if (DoctrineAPI.TryAcquireDoctrine(_id, out var reason))
                        {
                            Column?.Refresh(); // update bindings (Status, ButtonText, costs if you vary them post-unlock)
                        }
                        else
                        {
                            InformationManager.DisplayMessage(new InformationMessage(
                                string.IsNullOrEmpty(reason) ? L.S("unlock_failed", "Cannot unlock.") : reason));
                        }
                    },
                    negativeAction: () => { }
                ), true);
            }
            else
            {
                // Show OK only
                InformationManager.ShowInquiry(new InquiryData(
                    _name,
                    text.ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: false,
                    affirmativeText: GameTexts.FindText("str_ok").ToString(),
                    negativeText: null,
                    affirmativeAction: null,
                    negativeAction: null
                ), true);
            }
        }

        // =========================================================================
        // Public API
        // =========================================================================

        public DoctrineColumnVM Column { get; set; }

        public void Refresh()
        {
            OnPropertyChanged(nameof(ButtonBrush));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(ButtonText));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(IsEnabled));
            OnPropertyChanged(nameof(GoldCost));
            OnPropertyChanged(nameof(InfluenceCost));
        }
    }
}
