#if BL12
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using Retinues.Domain;
using TaleWorlds.Library;
using Retinues.Interface.Services;
using Retinues.Settings;
using Retinues.Utilities;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.ScreenSystem;

namespace Retinues.Editor.Integration.MapBar
{
    /// <summary>
    /// Adds the Troops navigation button to the map bar (BL12).
    /// </summary>
    [ViewModelMixin]
    internal sealed class MapNavigationVMMixin : BaseViewModelMixin<MapNavigationVM>
    {
        private readonly MapNavigationHandler _handler;
        private readonly BasicTooltipViewModel _troopsHint;

        private bool _isTroopsEnabled;
        private bool _isTroopsActive;

        /// <summary>Shared instance updated by the OnTick Harmony patch each frame.</summary>
        internal static MapNavigationVMMixin Current { get; private set; }

        public MapNavigationVMMixin(MapNavigationVM vm)
            : base(vm)
        {
            // GetPrivate<T> only searches public members — use Reflection.GetFieldValue
            // which searches all members including private fields.
            _handler =
                Reflection.GetFieldValue<object>(vm, "_navigationHandler") as MapNavigationHandler;
            _troopsHint = new BasicTooltipViewModel(GetTooltipString);
            Current = this;
        }

        private string GetTooltipString() =>
            Configuration.EditorHotkey
                ? L.S("troops_navigation_tooltip_hotkey", "Troops [R]")
                : L.S("troops_navigation_tooltip", "Troops");

        [DataSourceProperty]
        public bool IsTroopsEnabled
        {
            get => _isTroopsEnabled;
            set => SetField(ref _isTroopsEnabled, value, nameof(IsTroopsEnabled));
        }

        [DataSourceProperty]
        public bool IsTroopsActive
        {
            get => _isTroopsActive;
            set => SetField(ref _isTroopsActive, value, nameof(IsTroopsActive));
        }

        [DataSourceProperty]
        public BasicTooltipViewModel TroopsHint => _troopsHint;

        [DataSourceMethod]
        public void ExecuteOpenTroops()
        {
            if (!_isTroopsEnabled)
                return;

            EditorLauncher.Launch(EditorMode.Player);
        }

        public override void OnRefresh() => Refresh();

        /// <summary>Called by the OnTick patch every frame to keep button state current.</summary>
        internal void Refresh()
        {
            // Update active first: ComputeIsTroopsEnabled uses it.
            IsTroopsActive = ComputeIsTroopsActive();
            IsTroopsEnabled = ComputeIsTroopsEnabled();
        }

        private bool ComputeIsTroopsActive() =>
            ScreenManager.TopScreen is EditorScreen
            && Game.Current?.GameStateManager?.ActiveState is EditorGameState egs
            && egs.IsMapBarIntegrated;

        private bool ComputeIsTroopsEnabled()
        {
            if (_handler == null)
                return false;
            if (_handler.IsNavigationLocked)
                return false;
            if (!IsNavigationBarEnabled())
                return false;
            // Cannot re-open what is already active.
            if (_isTroopsActive)
                return false;
            if (!HasEditableTroops())
                return false;
            return true;
        }

        /// <summary>
        /// Mirrors MapNavigationHandler.IsNavigationBarEnabled() (private).
        /// </summary>
        private bool IsNavigationBarEnabled()
        {
            try
            {
                return (bool)(
                    Reflection.InvokeMethod(_handler, "IsNavigationBarEnabled", null) ?? false
                );
            }
            catch
            {
                return false;
            }
        }

        internal static bool HasEditableTroops() =>
            Player.Clan?.Troops.Any(t => t != null && !t.IsHero && t.IsFactionTroop) == true
            || Player.Kingdom?.Troops.Any(t => t != null && !t.IsHero && t.IsFactionTroop) == true;
    }
}
#endif
