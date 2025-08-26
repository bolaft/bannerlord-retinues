using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI
{
    public sealed class TroopEditorVM : ViewModel
    {
        private readonly ClanManagementMixinVM _owner;

        public TroopEditorVM(ClanManagementMixinVM owner) => _owner = owner;

        [DataSourceMethod]
        public void ExecuteRenameSelected()
        {
            var sel = _owner?.Troop;
            if (sel == null || sel.Troop == null)
            {
                Log.Info("TroopEditorVM: no selected troop to rename.");
                return;
            }

            var current = sel.Troop.Name ?? sel.Troop.StringId ?? "";

            // BL 1.2.12 overload expects:
            // (title, text, showAff, showNeg, affText, negText,
            //  Action<string> onAffirmative, Action onNegative,
            //  bool isInputObfuscated,
            //  Func<string, Tuple<bool,string>> validator,
            //  string defaultInputText)
            var data = new TextInquiryData(
                new TextObject("Rename Troop").ToString(),
                new TextObject("Enter the new name:").ToString(),
                true,
                true,
                new TextObject("{=OK}OK").ToString(),
                new TextObject("{=Cancel}Cancel").ToString(),
                (string input) =>
                {
                    input = (input ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(input)) return;

                    // Apply to our OO model and push to engine
                    sel.Troop.SetName(input);

                    _owner.NotifyTroopRenamed();
                    Log.Info($"TroopEditorVM: renamed '{current}' → '{input}'");
                },
                null,               // onNegative
                false,              // isInputObfuscated (arg #9 MUST be bool)
                (string input) =>   // validator (arg #10)
                {
                    bool ok = !string.IsNullOrWhiteSpace(input?.Trim());
                    return new System.Tuple<bool, string>(ok, ok ? "" : "Name cannot be empty.");
                },
                current             // defaultInputText (arg #11)
            );

            InformationManager.ShowTextInquiry(data);
        }
    }
}
