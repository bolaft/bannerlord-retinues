using System;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor;
using Retinues.GUI.Helpers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace Retinues.GUI.Encyclopedia
{
    [ViewModelMixin(
        "TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages.EncyclopediaUnitPageVM"
    )]
    public sealed class UnitPageScreen : BaseViewModelMixin<EncyclopediaUnitPageVM>
    {
        public UnitPageScreen(EncyclopediaUnitPageVM vm)
            : base(vm)
        {
            try
            {
                SpriteLoader.LoadCategories("ui_clan");
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        [DataSourceProperty]
        public BasicTooltipViewModel EditorHint =>
            Tooltip.MakeTooltip(
                null,
                L.S("encyclopedia_retinues_link_hint", "Open in the global editor.")
            );

        [DataSourceMethod]
        public void ExecuteOpenEditor()
        {
            try
            {
                if (ViewModel.Obj is CharacterObject character)
                {
                    var troop = new WCharacter(character);

                    // Preselect this troop
                    State.PendingTroop = troop;

                    // Use the troop's own faction: culture for vanilla, mapped clan/kingdom for custom
                    State.PendingFaction = troop.Faction;

                    // Vanilla → Culture mode, Custom → Personal mode
                    var mode = troop.IsCustom ? EditorMode.Personal : EditorMode.Culture;
                    ClanScreen.LaunchEditor(mode);
                }
                else
                {
                    // Fallback: old behavior
                    ClanScreen.LaunchEditor(EditorMode.Culture);
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
