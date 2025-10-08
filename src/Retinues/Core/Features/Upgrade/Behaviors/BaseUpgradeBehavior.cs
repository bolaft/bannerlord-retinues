using System;
using System.Collections.Generic;
using Retinues.Core.Editor;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.SaveSystem;

namespace Retinues.Core.Features.Upgrade.Behaviors
{
    [Serializable]
    public class BasePendingData
    {
        [SaveableField(1)]
        public string TroopId;

        [SaveableField(2)]
        public int Remaining;

        [SaveableField(3)]
        public float Carry;
    }

    [SafeClass]
    public abstract class BaseUpgradeBehavior<T> : CampaignBehaviorBase
        where T : BasePendingData
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static BaseUpgradeBehavior<T> Instance { get; private set; }

        public BaseUpgradeBehavior()
        {
            Instance = this;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            Log.Debug("BaseUpgradeBehavior.RegisterEvents called.");
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected abstract string MenuId { get; }
        protected abstract string OptionId { get; }
        protected abstract string OptionText { get; }

        protected virtual GameMenuOption.LeaveType OptionIcon => GameMenuOption.LeaveType.Wait;

        public void OnSessionLaunched(CampaignGameStarter starter)
        {
            Log.Debug("BaseUpgradeBehavior.OnSessionLaunched called.");

            // Town example; add to others if needed ("castle", "village", etc.)
            starter.AddGameMenuOption(
                MenuId,
                OptionId,
                OptionText,
                OptionCondition,
                args => ShowPicker(starter, args),
                isLeave: false,
                index: 0
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public Dictionary<string, Dictionary<string, T>> _pending = [];
        public Dictionary<string, Dictionary<string, T>> Pending => _pending;

        protected abstract string SaveFieldName { get; set; }

        public override void SyncData(IDataStore data)
        {
            if (data.IsLoading)
                _pending.Clear(); // to avoid cross-save contamination

            data.SyncData(SaveFieldName, ref _pending);

            _pending ??= []; // in case it was null in the save
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Abstract                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected abstract string InquiryTitle { get; }
        protected abstract string InquiryDescription { get; }
        protected abstract string InquiryAffirmative { get; }
        protected abstract string InquiryNegative { get; }
        protected abstract string ActionString { get; }
        protected abstract GameMenuOption.LeaveType LeaveType { get; }

        protected abstract void StartWait(
            CampaignGameStarter starter,
            string troopId,
            string objId,
            T data
        );

        protected abstract string BuildElementTitle(WCharacter troop, T data);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Virtual                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected virtual void ShowPicker(CampaignGameStarter starter, MenuCallbackArgs args)
        {
            var elements = GetInquiryElements();
            if (elements.Count == 0)
            {
                GameMenu.SwitchToMenu(MenuId);
                return;
            }

            MBInformationManager.ShowMultiSelectionInquiry(
                new MultiSelectionInquiryData(
                    titleText: InquiryTitle,
                    descriptionText: InquiryDescription,
                    inquiryElements: elements,
                    isExitShown: true,
                    minSelectableOptionCount: 1,
                    maxSelectableOptionCount: 1,
                    affirmativeText: InquiryAffirmative,
                    negativeText: InquiryNegative,
                    new Action<List<InquiryElement>>(selected =>
                    {
                        var (troopId, objId) = ParseIdentifier((string)selected[0].Identifier);
                        var data = GetPending(troopId, objId);
                        if (data == null)
                        {
                            Log.Error($"Invalid selection: {troopId}::{objId}");
                            return;
                        }
                        StartWait(starter, troopId, objId, data);
                    }),
                    new Action<List<InquiryElement>>(_ =>
                    { /* cancelled */
                    })
                )
            );
        }

        protected virtual List<InquiryElement> GetInquiryElements()
        {
            var elements = new List<InquiryElement>();
            foreach (var kvp in _pending)
            {
                var troopId = kvp.Key;
                var dict = kvp.Value;
                foreach (var kvp2 in dict)
                {
                    var objId = kvp2.Key;
                    var data = kvp2.Value;
                    var troop = new WCharacter(data.TroopId);
                    if (!IsEntryEligible(troop, data))
                        continue;

                    elements.Add(
                        new InquiryElement(
                            identifier: ComposeIdentifier(troopId, objId),
                            title: BuildElementTitle(troop, data),
                            imageIdentifier: BuildElementImage(troop, objId, data)
                        )
                    );
                }
            }
            return elements;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public T GetPending(string troopId, string objId)
        {
            if (
                _pending.TryGetValue(troopId, out var dict)
                && dict.TryGetValue(objId, out var value)
            )
                return value;
            return null;
        }

        public void SetPending(string troopId, string objId, T data)
        {
            if (!_pending.ContainsKey(troopId))
                _pending[troopId] = [];
            _pending[troopId][objId] = data;
        }

        protected void RemovePending(string troopId, string objId)
        {
            if (Pending.TryGetValue(troopId, out var d))
            {
                d.Remove(objId);
                if (d.Count == 0)
                    Pending.Remove(troopId);
            }
        }

        protected bool IsEntryEligible(WCharacter troop, T data) =>
            troop.IsValid && CanEdit(troop) && data.Remaining > 0;

        protected ImageIdentifier BuildElementImage(WCharacter troop, string objId, T data) =>
            troop.ImageIdentifier;

        protected string ComposeIdentifier(string troopId, string objId) => $"{troopId}::{objId}";

        protected (string troopId, string objId) ParseIdentifier(string id)
        {
            var parts = id?.Split(new[] { "::" }, StringSplitOptions.None);
            return (parts != null && parts.Length > 1) ? (parts[0], parts[1]) : (parts?[0], null);
        }

        protected bool OptionCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = LeaveType;
            if (Settlement.CurrentSettlement == null || _pending.Count == 0)
                return false;

            foreach (var kv in _pending)
            {
                foreach (var obj in kv.Value.Values)
                {
                    var troop = new WCharacter(obj.TroopId);
                    if (troop.IsValid && CanEdit(troop))
                        return true; // show button if at least one troop can be edited
                }
            }
            return false; // hide button otherwise
        }

        protected static bool CanEdit(WCharacter troop)
        {
            return Config.GetOption<bool>("RestrictEditingToFiefs") == false
                || TroopRules.IsAllowedInContext(
                    troop,
                    troop.Faction,
                    Instance.ActionString,
                    showPopup: false
                );
        }
    }
}
