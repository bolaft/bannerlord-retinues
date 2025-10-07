using System;
using System.Collections.Generic;
using Retinues.Core.Editor;
using Retinues.Core.Editor.UI.Helpers;
using Retinues.Core.Game.Menu;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace Retinues.Core.Features.Upgrade.Behaviors
{
    /// <summary>
    /// Adds menu option in settlement for training troops.
    /// </summary>
    [SafeClass]
    public sealed class TroopTrainingBehavior : CampaignBehaviorBase
    {
        private const float BaseTrainingTime = 2.0f;
        private static bool TrainingTakesTime => Config.GetOption<bool>("TrainingTakesTime");
        private static float TrainingTimeModifier =>
            Config.GetOption<float>("TrainingTimeModifier");

        private static bool CanTrain(WCharacter troop) =>
            Config.GetOption<bool>("RestrictEditingToFiefs") == false
            || TroopRules.IsAllowedInContext(
                troop,
                troop.Faction,
                L.S("action_modify", "modify"),
                showPopup: false
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Save-friendly pending entryprivate static readonly Dictionary<string,
        private static readonly Dictionary<
            string,
            (
                string TroopId,
                string Name,
                int Remaining,
                string SkillId,
                int PointsRemaining,
                float PointsPerHour,
                float Carry
            )
        > _pending = [];

        // DTO for saving
        [Serializable]
        public class PendingSave
        {
            [SaveableField(1)]
            public string TroopId;

            [SaveableField(2)]
            public string Name;

            [SaveableField(3)]
            public int Remaining;

            [SaveableField(4)]
            public string SkillId;

            [SaveableField(5)]
            public int PointsRemaining;

            [SaveableField(6)]
            public float PointsPerHour;

            [SaveableField(7)]
            public float Carry;
        }

        public override void SyncData(IDataStore data)
        {
            if (data.IsSaving)
            {
                var list = new List<PendingSave>(_pending?.Count ?? 0);

                foreach (var kv in _pending)
                {
                    var (TroopId, Name, Remaining, SkillId, PointsRemaining, PointsPerHour, Carry) =
                        kv.Value;
                    list.Add(
                        new PendingSave
                        {
                            TroopId = TroopId,
                            Name = Name,
                            Remaining = Remaining,
                            SkillId = SkillId,
                            PointsRemaining = PointsRemaining,
                            PointsPerHour = PointsPerHour,
                            Carry = Carry,
                        }
                    );
                }
                data.SyncData("Retinues_Training_Pending", ref list);
            }
            else if (data.IsLoading)
            {
                _pending.Clear();
                List<PendingSave> list = null;
                data.SyncData("Retinues_Training_Pending", ref list);
                _pending.Clear();
                if (list != null)
                {
                    foreach (var e in list)
                    {
                        _pending[Key(e.TroopId, e.SkillId)] = (
                            e.TroopId,
                            e.Name,
                            e.Remaining,
                            e.SkillId,
                            e.PointsRemaining,
                            e.PointsPerHour,
                            e.Carry
                        );
                    }
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            Log.Debug("TroopTrainingBehavior.RegisterEvents called.");
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void OnSessionLaunched(CampaignGameStarter starter)
        {
            Log.Debug("TroopTrainingBehavior.OnSessionLaunched called.");

            // Town example; add to others if needed ("castle", "village", etc.)
            starter.AddGameMenuOption(
                "town",
                "ret_train_pending",
                L.S("upgrade_train_pending_btn", "Train troops"),
                TrainOptionCondition,
                args => ShowTrainPicker(starter, args),
                isLeave: false,
                index: 0
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static int GetTrainingRequired(WCharacter troop)
        {
            if (troop == null)
                return 0;

            int total = 0;
            foreach (var kv in _pending)
            {
                if (kv.Value.TroopId == troop.StringId)
                    total += (int)(kv.Value.PointsRemaining / kv.Value.PointsPerHour);
            }
            return total;
        }

        public static int GetStaged(WCharacter troop, SkillObject skill)
        {
            if (troop == null || skill == null)
                return 0;

            if (_pending.TryGetValue(Key(troop, skill), out var v))
                return v.PointsRemaining;
            return 0;
        }

        public static void StageTraining(WCharacter troop, SkillObject skill)
        {
            Log.Debug($"Staging training {skill?.Name} for troop {troop?.Name} by 1.");

            if (!TrainingTakesTime)
            {
                ApplyChange(troop.StringId, skill, 1);
                return;
            }

            var name = troop?.Name?.ToString() ?? troop?.ToString() ?? "Troop";
            var hours = Math.Max(1, (int)(BaseTrainingTime * TrainingTimeModifier));
            var pph = 1 / (float)hours;

            var troopId = troop.StringId;
            var skillId = skill?.StringId;
            var key = Key(troopId, skillId);

            if (_pending.TryGetValue(key, out var v))
            {
                int newRemaining = v.Remaining + hours;
                int newPoints = v.PointsRemaining + 1;
                float newPPH = newPoints / Math.Max(1, newRemaining);

                _pending[key] = (
                    TroopId: v.TroopId,
                    Name: v.Name,
                    Remaining: newRemaining,
                    SkillId: v.SkillId,
                    PointsRemaining: newPoints,
                    PointsPerHour: newPPH,
                    Carry: v.Carry
                );
            }
            else
            {
                _pending[key] = (
                    TroopId: troopId,
                    Name: name,
                    Remaining: hours,
                    SkillId: skillId,
                    PointsRemaining: 1,
                    PointsPerHour: pph,
                    Carry: 0f
                );
            }

            if (Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId == "town")
                GameMenu.SwitchToMenu("town");
        }

        /// <summary>Applies skill point changes.</summary>
        public static void ApplyChange(string troopId, SkillObject skill, int delta)
        {
            try
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
                    var key = Key(troopId, skill.StringId);
                    // First, cancel staged points if any
                    if (_pending.TryGetValue(key, out var v) && v.PointsRemaining > 0)
                    {
                        int toCancel = Math.Min(-delta, v.PointsRemaining);
                        int pointsRemaining = v.PointsRemaining - toCancel;
                        // If no more staged points, remove entry
                        if (pointsRemaining <= 0)
                            _pending.Remove(key);
                        else
                            _pending[key] = (
                                v.TroopId,
                                v.Name,
                                v.Remaining,
                                v.SkillId,
                                pointsRemaining, // reduce staged
                                v.PointsPerHour,
                                v.Carry
                            );
                        delta += toCancel; // reduce actual removal
                        if (delta >= 0)
                            return; // all done
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
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static string Key(string troopId, string skillId) => $"{troopId}::{skillId}";

        private static string Key(WCharacter troop, SkillObject skill) =>
            Key(troop?.StringId, skill?.StringId);

        private bool TrainOptionCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Wait; // keep your icon
            if (Settlement.CurrentSettlement == null || _pending.Count == 0)
                return false;

            foreach (var kv in _pending)
            {
                var troopId = kv.Value.TroopId; // composite dict value has TroopId
                var w = new WCharacter(troopId);
                if (CanTrain(w)) // ← your restriction
                    return true; // show button if ANY pending entry is allowed here
            }
            return false; // hide button otherwise
        }

        private void ShowTrainPicker(CampaignGameStarter starter, MenuCallbackArgs args)
        {
            Log.Debug("TroopTrainingBehavior.ShowTrainPicker called.");

            var elements = new List<InquiryElement>();
            foreach (var kv in _pending)
            {
                var key = kv.Key;
                var (troopId, name, remaining, skillId, points, pph, carry) = kv.Value;

                // filter by fief rule
                var w = new WCharacter(troopId);
                if (!CanTrain(w))
                    continue;

                var skillName = !string.IsNullOrEmpty(skillId)
                    ? MBObjectManager.Instance.GetObject<SkillObject>(skillId)?.Name.ToString()
                    : null;

                elements.Add(
                    new InquiryElement(
                        identifier: key,
                        title: $"{name}\n+{points} {skillName}",
                        imageIdentifier: w.ImageIdentifier
                    )
                );
            }

            if (elements.Count == 0)
            {
                GameMenu.SwitchToMenu("town");
                return;
            }

            MBInformationManager.ShowMultiSelectionInquiry(
                new MultiSelectionInquiryData(
                    titleText: L.S("upgrade_train_select_troop", "Select a troop to train"),
                    descriptionText: L.S(
                        "upgrade_train_choose_troop",
                        "Choose one pending troop to start training now."
                    ),
                    inquiryElements: elements,
                    true,
                    1,
                    1,
                    affirmativeText: L.S("upgrade_train_begin", "Begin training"),
                    negativeText: L.S("cancel", "Cancel"),
                    // onDone
                    new Action<List<InquiryElement>>(selected =>
                    {
                        var key = (string)selected[0].Identifier;
                        var (troopId, name, remaining, skillId, points, pph, carry) = _pending[key];
                        var skill = !string.IsNullOrEmpty(skillId)
                            ? MBObjectManager.Instance.GetObject<SkillObject>(skillId)
                            : null;

                        TimedWaitMenu.Start(
                            starter,
                            idSuffix: $"train_{troopId}_{skillId}", // unique per run is already handled in TimedWaitMenu with a counter
                            title: L.T("upgrade_train_progress", "Training {NAME}...")
                                .SetTextVariable("NAME", name)
                                .ToString(),
                            durationHours: remaining,
                            onCompleted: () =>
                            {
                                if (_pending.TryGetValue(key, out var v3))
                                {
                                    while (v3.PointsRemaining > 0)
                                    {
                                        ApplyChange(troopId, skill, +1);
                                        v3.PointsRemaining -= 1;
                                    }
                                    _pending.Remove(key);
                                }
                                if (_pending.Count == 0)
                                {
                                    GameMenu.SwitchToMenu("town");
                                    // Display training complete popup
                                    Popup.Display(
                                        L.T("training_complete", "Training Complete"),
                                        L.T(
                                                "training_complete_text",
                                                "{TROOP} has completed their {SKILL} training."
                                            )
                                            .SetTextVariable("TROOP", new WCharacter(troopId).Name)
                                            .SetTextVariable("SKILL", skill?.Name)
                                    );
                                }
                            },
                            onAborted: () => { /* keep pending */
                            },
                            overlay: GameMenu.MenuOverlayType.SettlementWithBoth,
                            onWholeHour: _ =>
                            {
                                if (
                                    _pending.TryGetValue(key, out var v2)
                                    && v2.Remaining > 0
                                    && v2.PointsRemaining > 0
                                )
                                {
                                    v2.Remaining -= 1;
                                    v2.Carry += v2.PointsPerHour;

                                    int steps = Math.Min(
                                        (int)Math.Floor(v2.Carry),
                                        v2.PointsRemaining
                                    );
                                    if (steps > 0)
                                    {
                                        ApplyChange(troopId, skill, steps);
                                        v2.PointsRemaining -= steps;
                                        v2.Carry -= steps;
                                    }
                                    _pending[key] = v2;

                                    Log.Debug(
                                        $"Training hour applied: {name} +{steps} {skill?.Name}; left {v2.Remaining}h / {v2.PointsRemaining}pts (carry {v2.Carry:0.##})"
                                    );
                                }
                            }
                        );
                    }),
                    // onCancel
                    new Action<List<InquiryElement>>(_ =>
                    {
                        Log.Debug("Training picker cancelled by user.");
                    })
                )
            );
        }
    }
}
