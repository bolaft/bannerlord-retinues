using System;
using Retinues.Configuration;
using Retinues.Game.Menu;
using Retinues.Game.Wrappers;
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

namespace Retinues.Features.Upgrade.Behaviors
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
    /// Adds menu option in settlement for training troops.
    /// </summary>
    [SafeClass]
    public sealed class TroopTrainBehavior : BaseUpgradeBehavior<PendingTrainData>
    {
        private const int BaseTrainingTime = 3; // hours per skill point

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override string SaveFieldName { get; set; } = "Retinues_Train_Pending";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void StageTraining(WCharacter troop, SkillObject skill)
        {
            if (Config.TrainingTakesTime == false)
            {
                ApplyChange(troop.StringId, skill, 1);
                return;
            }

            var hours = Math.Max(1, BaseTrainingTime * Config.TrainingTimeModifier);
            var pph = 1 / (float)hours;

            var troopId = troop.StringId;
            var skillId = skill?.StringId;

            var v = Instance.GetPending(troopId, skillId);

            if (v != null)
            {
                if (v != null)
                {
                    v.Remaining += hours;
                    v.PointsRemaining += 1;
                    v.PointsPerHour = (float)v.PointsRemaining / Math.Max(1, v.Remaining);
                    if (v.PointsPerHour <= 0f)
                        v.PointsPerHour = pph; // defensive
                }
            }
            else
            {
                Instance.SetPending(
                    troopId,
                    skillId,
                    new PendingTrainData
                    {
                        TroopId = troopId,
                        Remaining = hours,
                        SkillId = skillId,
                        PointsRemaining = 1,
                        PointsPerHour = pph,
                        Carry = 0f,
                    }
                );
            }

            if (IsInManagedMenu(out _))
                RefreshManagedMenuOrDefault();
        }

        /// <summary>Applies skill point changes.</summary>
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
                var v = Instance.GetPending(troopId, skill.StringId);

                // First, cancel staged points if any
                if (v != null && v.PointsRemaining > 0)
                {
                    // How many points can we cancel?
                    int toCancel = Math.Min(-delta, v.PointsRemaining);

                    // Cancel them
                    v.PointsRemaining -= toCancel;
                    v.Remaining -= (int)(toCancel / v.PointsPerHour); // decrease Remaining hours accordingly

                    // If no more staged points, remove entry
                    if (v.PointsRemaining <= 0)
                        Instance.Pending[troopId].Remove(skill.StringId);

                    // Adjust delta
                    delta += toCancel;

                    if (delta >= 0)
                        return; // All done
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
            L.S("upgrade_train_select_troop", "Select a troop to train");
        protected override string InquiryDescription =>
            L.S("upgrade_train_choose_troop", "Choose one pending troop to start training now.");
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

        protected override void StartWait(
            CampaignGameStarter starter,
            string troopId,
            string objId,
            PendingTrainData data
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
                    if (Pending.Count == 0)
                        RefreshManagedMenuOrDefault();

                    Popup.Display(
                        L.T("training_complete", "Training Complete"),
                        L.T(
                                "training_complete_text",
                                "{TROOP} has completed their {SKILL} training."
                            )
                            .SetTextVariable("TROOP", new WCharacter(troopId).Name)
                            .SetTextVariable("SKILL", skill?.Name)
                    );
                },
                onAborted: () => { /* keep pending */
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
