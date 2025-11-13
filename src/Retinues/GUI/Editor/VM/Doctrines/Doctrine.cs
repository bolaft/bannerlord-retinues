using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Game.Wrappers;
using Retinues.Troops;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Doctrines
{
    /// <summary>
    /// ViewModel for a doctrine. Handles display, unlock logic, popup, and UI refresh.
    /// </summary>
    [SafeClass]
    public sealed class DoctrineVM : BaseVM
    {
        protected override Dictionary<UIEvent, string[]> EventMap => [];

        private readonly string _id;
        private readonly DoctrineServiceBehavior _svc;
        private readonly DoctrineDefinition _def;
        private readonly string _name;

        /// <summary>
        /// Construct a DoctrineVM for the given doctrine id.
        /// </summary>
        public DoctrineVM(string doctrineId)
        {
            try
            {
                _id = doctrineId;
                _svc = Campaign.Current?.GetCampaignBehavior<DoctrineServiceBehavior>();
                _def = _svc?.GetDoctrine(_id);
                _name = _def?.Name?.ToString() ?? _id;

                Refresh();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Button ━━━━━━━━ */

        [DataSourceProperty]
        public string ButtonText
        {
            get
            {
                if (!IsEnabled)
                    return _name;

                if (Config.DisableFeatRequirements)
                    return _name; // Show name only if feats are disabled

                int total = _def?.Feats?.Count ?? 0;
                int complete = 0;
                if (total > 0 && _svc != null)
                    complete = _def.Feats.Count(f => _svc.IsFeatComplete(f.Key));
                return $"{_name} ({complete}/{total})";
            }
        }

        [DataSourceProperty]
        public string ButtonBrush
        {
            get { return Status == DoctrineStatus.Unlocked ? "ButtonBrush1" : "ButtonBrush2"; }
        }

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool IsEnabled
        {
            get { return Status != DoctrineStatus.Locked && Status != DoctrineStatus.Unlocked; }
        }

        /* ━━━━━━ Properties ━━━━━━ */

        [DataSourceProperty]
        public DoctrineStatus Status
        {
            get { return DoctrineAPI.GetDoctrineStatus(_id); }
        }

        [DataSourceProperty]
        public string Description
        {
            get { return _def?.Description?.ToString() ?? string.Empty; }
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        [SafeMethod]
        /// <summary>
        /// Show doctrine details popup and allow unlocking if requirements are met.
        /// </summary>
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
                if (done)
                    complete++;

                string status = done ? "■" : "□";
                int prog = svc.GetFeatProgress(f.Key);
                int target = svc.GetFeatTarget(f.Key);

                if (target > 0)
                    sb.Append("    ")
                        .Append(status)
                        .Append("  ")
                        .Append(f.Description)
                        .Append(" (")
                        .Append(prog)
                        .Append('/')
                        .Append(target)
                        .Append(")\n");
                else
                    sb.Append("    ")
                        .Append(status)
                        .Append("  ")
                        .Append(f.Description)
                        .Append('\n');
            }

            string featsText =
                total == 0
                    ? L.S("feats_no_reqs", "No requirements.")
                    : $"{L.S("feats_reqs", "Requirements")}:\n\n{sb}";

            var costs = L.T("doctrine_costs", "Cost: {GOLD} Gold, {INFLUENCE} Influence.")
                .SetTextVariable("GOLD", GoldCost)
                .SetTextVariable("INFLUENCE", InfluenceCost)
                .ToString();

            var text = Config.DisableFeatRequirements
                ? $"{Description}\n\n{costs}"
                : $"{Description}\n\n{featsText}\n\n{costs}";

            bool allComplete = total == 0 || complete == total;
            bool alreadyUnlocked = svc.IsDoctrineUnlocked(_id);

            if (allComplete && !alreadyUnlocked)
            {
                // Show Cancel / Unlock
                InformationManager.ShowInquiry(
                    new InquiryData(
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
                                Column?.Refresh();

                                if (State.Faction is WFaction f)
                                {
                                    // Trigger rebuild and refresh if needed
                                    TroopBuilder.EnsureTroopsExist(f);
                                    State.UpdateFaction();
                                }
                            }
                            else
                            {
                                InformationManager.DisplayMessage(
                                    new InformationMessage(
                                        string.IsNullOrEmpty(reason)
                                            ? L.S("unlock_failed", "Cannot unlock.")
                                            : reason
                                    )
                                );
                            }
                        },
                        negativeAction: () => { }
                    ),
                    true
                );
            }
            else
            {
                // Show OK only
                InformationManager.ShowInquiry(
                    new InquiryData(
                        _name,
                        text.ToString(),
                        isAffirmativeOptionShown: true,
                        isNegativeOptionShown: false,
                        affirmativeText: GameTexts.FindText("str_ok").ToString(),
                        negativeText: null,
                        affirmativeAction: null,
                        negativeAction: null
                    ),
                    true
                );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Parent column that contains this doctrine VM.
        /// </summary>
        public DoctrineColumnVM Column { get; set; }

        /// <summary>
        /// Refresh doctrine display properties.
        /// </summary>
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
