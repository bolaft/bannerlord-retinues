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
    public sealed class HeroPageScreen : BaseViewModelMixin<EncyclopediaHeroPageVM>
    {
        public HeroPageScreen(EncyclopediaHeroPageVM vm)
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
                if (ViewModel.Obj is Hero hero)
                {
                    // Preselect this hero as the current "troop"
                    State.PendingTroop = new WHero(hero);

                    // Use the hero's clan (or culture) as the active faction list
                    State.PendingFaction =
                        hero.Clan != null ? new WClan(hero.Clan)
                        : hero.Culture != null ? new WCulture(hero.Culture)
                        : null;
                }

                ClanScreen.LaunchEditor(EditorMode.Heroes);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
