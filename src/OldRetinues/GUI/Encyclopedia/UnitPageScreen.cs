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

namespace OldRetinues.GUI.Encyclopedia
{
    [ViewModelMixin]
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
                    State.PendingTroop = troop;
                    State.PendingFaction = troop.Faction;

                    var mode = troop.IsCustom ? EditorMode.Personal : EditorMode.Culture;
                    ClanScreen.LaunchEditor(mode);
                }
                else
                {
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
