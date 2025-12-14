using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Screen;
using Retinues.Model.Factions;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;

namespace Retinues.GUI.Prefabs.Encyclopedia
{
    [ViewModelMixin]
    public sealed class FactionPageScreen(EncyclopediaFactionPageVM vm)
        : BaseEncyclopediaPageScreen<EncyclopediaFactionPageVM>(vm)
    {
        [DataSourceProperty]
        public override int MarginTop => 8;

        [DataSourceMethod]
        public override void ExecuteOpenEditor()
        {
            try
            {
                // Kingdoms show up on the faction page; ignore other faction types.
                if (ViewModel.Obj is not Kingdom kingdom)
                    return;

                // Use kingdom culture.
                var culture = WKingdom.Get(kingdom)?.Culture;
                if (culture == null)
                    return;

                // Launch the editor with the kingdom culture.
                EditorLauncher.Launch(culture);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
