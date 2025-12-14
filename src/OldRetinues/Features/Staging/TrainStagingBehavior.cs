using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Game.Menu;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor;
using Retinues.GUI.Helpers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
# if BL12
using TaleWorlds.CampaignSystem.Overlay;
# endif

namespace OldRetinues.Features.Staging
{
    [Serializable]
    public class PendingTrainData : IPendingData
    {
        [SaveableField(1)]
        public string TroopId;

        [SaveableField(2)]
        public int Remaining;

        [SaveableField(3)]
        public float Carry;

        [SaveableField(4)]
        public string SkillId;

        [SaveableField(5)]
        public int PointsRemaining;

        [SaveableField(6)]
        public float PointsPerHour;

        string IPendingData.TroopId
        {
            get => TroopId;
            set => TroopId = value;
        }
        int IPendingData.Remaining
        {
            get => Remaining;
            set => Remaining = value;
        }
        float IPendingData.Carry
        {
            get => Carry;
            set => Carry = value;
        }
    }

    /// <summary>
    /// Staged training jobs with a unified public API (Stage/Unstage/Get/Clear).
    /// </summary>
    [SafeClass]
    public class TrainStagingBehavior : BaseStagingBehavior<PendingTrainData>
    {
        private const int BaseTrainingTime = 1; // hours per skill point

        public readonly struct TrainChange(SkillObject skill, int points = 1)
        {
            public readonly SkillObject Skill = skill;
            public readonly int Points = Math.Max(1, points);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override string SaveFieldName { get; set; } = "Retinues_Train_Pending";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Instance-level API (enforced by base)

        protected override PendingTrainData GetStagedChange(WCharacter troop, string objectKey)
        {
            if (troop == null || string.IsNullOrEmpty(objectKey))
                return null;
            return GetPending(troop.StringId, objectKey);
        }

        protected override List<PendingTrainData> GetStagedChanges(WCharacter troop)
        {
            if (troop == null)
                return [];
            return GetPending(troop.StringId);
        }

        protected override void StageChange(WCharacter troop, object payload)
        {
            if (troop == null)
                return;

            if (payload is not TrainChange tc || tc.Skill == null)
            {
                Log.Warn("TroopTrainBehavior.StageChange called with invalid payload.");
                return;
            }

            if (Config.TrainingTroopsTakesTime == false || ClanScreen.IsStudioMode)
            {
                ApplyChange(troop.StringId, tc.Skill, tc.Points);
                return;
            }

            var hoursPerPoint = Math.Max(1, BaseTrainingTime * Config.TrainingTimeMultiplier);
            var pph = 1f / hoursPerPoint;

            var troopId = troop.StringId;
            var skillId = tc.Skill.StringId;
            var v = GetPending(troopId, skillId);

            if (v != null)
            {
                v.Remaining += hoursPerPoint * tc.Points;
                v.PointsRemaining += tc.Points;
                v.PointsPerHour = (float)v.PointsRemaining / Math.Max(1, v.Remaining);
                if (v.PointsPerHour <= 0f)
                    v.PointsPerHour = pph; // defensive
            }
            else
            {
                SetPending(
                    troopId,
                    skillId,
                    new PendingTrainData
                    {
                        TroopId = troopId,
                        Remaining = hoursPerPoint * tc.Points,
                        SkillId = skillId,
                        PointsRemaining = tc.Points,
                        PointsPerHour = pph,
                        Carry = 0f,
                    }
                );
            }

            if (IsInManagedMenu(out _))
                RefreshManagedMenuOrDefault();
        }

        protected override void UnstageChange(WCharacter troop, string objectKey)
        {
            if (troop == null || string.IsNullOrEmpty(objectKey))
                return;
            RemovePending(troop.StringId, objectKey);
        }

        protected override void UnstageChanges(WCharacter troop)
        {
            if (troop == null)
                return;
            foreach (var kvp in troop.Skills)
                RemovePending(troop.StringId, kvp.Key.StringId);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Static Accessors                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static PendingTrainData Get(WCharacter troop, SkillObject skill) =>
            ((TrainStagingBehavior)Instance).GetStagedChange(troop, skill?.StringId);

        public static List<PendingTrainData> Get(WCharacter troop) =>
            ((TrainStagingBehavior)Instance).GetStagedChanges(troop);

        public static void Stage(WCharacter troop, SkillObject skill, int points = 1) =>
            ((TrainStagingBehavior)Instance).StageChange(troop, new TrainChange(skill, points));

        public static void Unstage(WCharacter troop, SkillObject skill) =>
            ((TrainStagingBehavior)Instance).UnstageChange(troop, skill?.StringId);

        public static void Unstage(WCharacter troop) =>
            ((TrainStagingBehavior)Instance).UnstageChanges(troop);

        /// <summary>
        /// Applies skill point changes immediately (used by timers and instant training).
        /// </summary>
        public static void ApplyChange(string troopId, SkillObject skill, int delta)
        {
            if (string.IsNullOrEmpty(troopId) || skill == null)
                return;

            var w = new WCharacter(troopId);
            if (delta > 0)
            {
                for (int i = 0; i < delta; i++)
                {
                    var current = w.GetSkill(skill);
                    w.SetSkill(skill, current + 1);
                }
            }
            else if (delta < 0)
            {
                var v = ((TrainStagingBehavior)Instance).GetPending(troopId, skill.StringId);

                // First, cancel staged points if any
                if (v != null && v.PointsRemaining > 0)
                {
                    int toCancel = Math.Min(-delta, v.PointsRemaining);
                    v.PointsRemaining -= toCancel;
                    v.Remaining -= (int)(toCancel / Math.Max(1e-6f, v.PointsPerHour));

                    if (v.PointsRemaining <= 0)
                        ((TrainStagingBehavior)Instance).Pending[troopId].Remove(skill.StringId);

                    delta += toCancel;
                    if (delta >= 0)
                        return;
                }

                // Then, remove actual skill points
                for (int i = 0; i < -delta; i++)
                {
                    var current = w.GetSkill(skill);
                    if (current > 0)
                        w.SetSkill(skill, current - 1);
                    else
                        break;
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override string OptionId => "ret_train_pending";
        protected override string OptionText => L.S("upgrade_train_pending_btn", "Train troops");
        protected override string InquiryTitle =>
            L.S("upgrade_train_select_troop", "Select troops to train");
        protected override string InquiryDescription =>
            L.S("upgrade_train_choose_troop", "Choose one or more troops to start training now.");
        protected override string InquiryAffirmative =>
            L.S("upgrade_train_begin", "Begin training");
        protected override string InquiryNegative => L.S("cancel", "Cancel");
        protected override string ActionString => L.S("action_modify", "modify");
        protected override GameMenuOption.LeaveType LeaveType => GameMenuOption.LeaveType.Recruit;

        protected override string BuildElementTitle(WCharacter troop, PendingTrainData data)
        {
            var skill = MBObjectManager.Instance.GetObject<SkillObject>(data.SkillId);
            return $"{troop.Name}\n+{data.PointsRemaining} {skill.Name} ({data.Remaining}h)";
        }

        private readonly Dictionary<WCharacter, List<SkillObject>> _batchedActions = [];

        protected override void FinalModalSummary()
        {
            if (_batchedActions.Count == 0)
                return;

            var summaryLines = new List<string>();
            foreach (var entry in _batchedActions)
            {
                var troop = entry.Key;
                var skills = entry.Value;
                var skillCounts = new Dictionary<string, int>();
                foreach (var skill in skills)
                {
                    if (!skillCounts.ContainsKey(skill.Name.ToString()))
                        skillCounts[skill.Name.ToString()] = 0;
                    skillCounts[skill.Name.ToString()] += 1;
                }

                var skillSummaries = new List<string>();
                foreach (var kvp in skillCounts)
                {
                    skillSummaries.Add($"{kvp.Value} {kvp.Key}");
                }

                var line = L.T("equip_complete_summary_line", "{TROOP}: {SKILLS}")
                    .SetTextVariable("TROOP", troop.Name)
                    .SetTextVariable("SKILLS", string.Join(", ", skillSummaries));

                summaryLines.Add(line.ToString());
            }

            var summary = L.T(
                    "train_complete_summary",
                    "The following troops have completed their training:\n\n{SUMMARY}"
                )
                .SetTextVariable("SUMMARY", string.Join("\n", summaryLines));

            Notifications.Popup(L.T("train_complete", "Training Complete"), summary);

            _batchedActions.Clear();
        }

        protected override void StartWait(
            CampaignGameStarter starter,
            string troopId,
            string objId,
            PendingTrainData data,
            Action onAfterCompleted = null
        )
        {
            var troop = new WCharacter(troopId);
            var skill = MBObjectManager.Instance.GetObject<SkillObject>(objId);

            TimedWaitMenu.Start(
                starter,
                idSuffix: $"train_{troopId}_{objId}",
                title: L.T("upgrade_train_progress", "Training {NAME}...")
                    .SetTextVariable("NAME", troop.Name)
                    .ToString(),
                durationHours: data.Remaining,
                onCompleted: () =>
                {
                    while (data.PointsRemaining > 0)
                    {
                        ApplyChange(troopId, skill, +1);
                        data.PointsRemaining -= 1;
                    }

                    RemovePending(troopId, objId);

                    var message = L.T(
                            "training_complete_text",
                            "{TROOP} has completed their {SKILL} training."
                        )
                        .SetTextVariable("TROOP", troop.Name)
                        .SetTextVariable("SKILL", skill.Name);

                    if (!_batchActive)
                        Notifications.Popup(L.T("training_complete", "Training Complete"), message);
                    else
                    {
                        if (!_batchedActions.ContainsKey(troop))
                            _batchedActions[troop] = [];
                        _batchedActions[troop].Add(skill);

                        Log.Message(message.ToString()); // Log instead of popup in batch mode
                    }

                    onAfterCompleted?.Invoke();
                },
                onAborted: () =>
                {
                    // Keep staged; continue so the batch doesn't stall
                    onAfterCompleted?.Invoke();
                },
# if BL13
                overlay: GameMenu.MenuOverlayType.SettlementWithBoth,
# else
                overlay: GameOverlays.MenuOverlayType.SettlementWithBoth,
# endif
                onWholeHour: _ =>
                {
                    if (data.Remaining > 0 && data.PointsRemaining > 0)
                    {
                        data.Remaining -= 1;
                        data.Carry += data.PointsPerHour;

                        int steps = Math.Min((int)Math.Floor(data.Carry), data.PointsRemaining);
                        if (steps > 0)
                        {
                            ApplyChange(troopId, skill, steps);
                            data.PointsRemaining -= steps;
                            data.Carry -= steps;
                        }
                    }
                }
            );
        }
    }
}
