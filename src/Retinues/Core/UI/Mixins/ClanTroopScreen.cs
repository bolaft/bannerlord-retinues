using System;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using CustomClanTroops.UI.VM;
using CustomClanTroops.Logic;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI
{
    [ViewModelMixin("TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanManagementVM")]
    public sealed class ClanTroopScreen : BaseViewModelMixin<ClanManagementVM>, ITroopScreen
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private readonly EditorScreenVM _screen;

        // =========================================================================
        // Constructor
        // =========================================================================

        public ClanTroopScreen(ClanManagementVM vm) : base(vm)
        {
            try
            {
                _screen = new EditorScreenVM(Player.Clan, this);

                ViewModel.PropertyChangedWithBoolValue += OnVanillaTabChanged;
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty] public EditorScreenVM EditorScreen => _screen;

        // =========================================================================
        // Public API
        // =========================================================================

        public void UnselectVanillaTabs()
        {
            ViewModel.IsMembersSelected = false;
            ViewModel.IsFiefsSelected = false;
            ViewModel.IsPartiesSelected = false;
            ViewModel.IsIncomeSelected = false;

            ViewModel.ClanMembers.IsSelected = false;
            ViewModel.ClanParties.IsSelected = false;
            ViewModel.ClanFiefs.IsSelected = false;
            ViewModel.ClanIncome.IsSelected = false;
        }

        // =========================================================================
        // Internals
        // =========================================================================

        private void OnVanillaTabChanged(object sender, PropertyChangedWithBoolValueEventArgs e)
        {
            if (!e.Value) return;

            if (e.PropertyName == "IsMembersSelected" ||
                e.PropertyName == "IsFiefsSelected" ||
                e.PropertyName == "IsPartiesSelected" ||
                e.PropertyName == "IsIncomeSelected")
            {
                if (EditorScreen != null)
                {
                    EditorScreen.IsTroopsSelected = false;
                }
            }
        }
    }
}
