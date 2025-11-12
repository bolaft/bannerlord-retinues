using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Game.Wrappers;
using Retinues.Managers;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop.Panel
{
    /// <summary>
    /// ViewModel for a single conversion row managing recruitment/release between source and retinue.
    /// </summary>
    [SafeClass]
    public sealed class TroopConversionRowVM(WCharacter source) : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly WCharacter Source = source;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override Dictionary<UIEvent, string[]> EventMap =>
            new()
            {
                [UIEvent.Conversion] =
                [
                    nameof(PendingAmount),
                    nameof(GoldConversionCost),
                    nameof(InfluenceConversionCost),
                    nameof(SourceDisplay),
                    nameof(TargetDisplay),
                    nameof(CanRecruit),
                    nameof(CanRelease),
                    nameof(HasPendingConversions),
                ],
                [UIEvent.Party] =
                [
                    nameof(SourceDisplay),
                    nameof(TargetDisplay),
                    nameof(CanRecruit),
                    nameof(CanRelease),
                ],
            };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool OtherRowsHavePendingConversions =>
            State.ConversionData.Any(kvp => kvp.Key != Source && kvp.Value != 0);
        private int Amount => State.ConversionData.TryGetValue(Source, out var amount) ? amount : 0;
        private int TotalAmount => State.ConversionData.Values.Sum();
        private int TargetCount =>
            State.PartyData.TryGetValue(State.Troop, out var count) ? count : 0;
        private int SourceCount => State.PartyData.TryGetValue(Source, out var count) ? count : 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public int PendingAmount => Amount;

        [DataSourceProperty]
        public int GoldConversionCost =>
            RetinueManager.ConversionGoldCostPerUnit(State.Troop) * Math.Max(0, Amount);

        [DataSourceProperty]
        public int InfluenceConversionCost =>
            RetinueManager.ConversionInfluenceCostPerUnit(State.Troop) * Math.Max(0, Amount);

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string ButtonApplyConversionsText =>
            L.S("ret_apply_conversions_button_text", "Convert");

        [DataSourceProperty]
        public string ButtonClearConversionsText =>
            L.S("ret_clear_conversions_button_text", "Clear");

        [DataSourceProperty]
        public string SourceDisplay => $"{Format.Crop(Source.Name, 40)} ({SourceCount - Amount})";

        [DataSourceProperty]
        public string TargetDisplay =>
            $"{Format.Crop(State.Troop.Name, 40)} ({TargetCount + TotalAmount}/{RetinueManager.RetinueCapFor(State.Troop)})";

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool CanRecruit =>
            !OtherRowsHavePendingConversions
            && TargetCount + TotalAmount < RetinueManager.RetinueCapFor(State.Troop)
            && SourceCount - Amount > 0;

        [DataSourceProperty]
        public bool CanRelease => !OtherRowsHavePendingConversions && TargetCount + TotalAmount > 0;

        [DataSourceProperty]
        public bool HasPendingConversions => Amount != 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Increase pending conversion amount from source into the retinue.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteRecruit()
        {
            if (
                ContextManager.IsAllowedInContextWithPopup(
                    State.Troop,
                    L.S("action_convert", "convert")
                ) == false
            )
                return; // Conversion not allowed in current context

            for (int i = 0; i < BatchInput(); i++)
            {
                if (CanRecruit == false)
                    break;

                State.ConversionData[Source] += 1;
            }

            State.UpdateConversionData(State.ConversionData);
        }

        /// <summary>
        /// Decrease pending conversion amount (release from retinue back to source).
        /// </summary>
        [DataSourceMethod]
        public void ExecuteRelease()
        {
            if (
                ContextManager.IsAllowedInContextWithPopup(
                    State.Troop,
                    L.S("action_convert", "convert")
                ) == false
            )
                return; // Conversion not allowed in current context

            for (int i = 0; i < BatchInput(); i++)
            {
                if (CanRelease == false)
                    break;

                State.ConversionData[Source] -= 1;
            }

            State.UpdateConversionData(State.ConversionData);
        }
    }
}
