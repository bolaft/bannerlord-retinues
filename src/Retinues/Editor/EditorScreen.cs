using Retinues.Editor.Integration.Barber;
using Retinues.Framework.Runtime;
using Retinues.Interface.Services;
using Retinues.Settings;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
# if BL12
using TaleWorlds.GauntletUI.Data;
# endif

namespace Retinues.Editor
{
    /// <summary>
    /// Gauntlet screen for the editor.
    /// </summary>
    [GameStateScreen(typeof(EditorGameState))]
    public sealed class EditorScreen(GameState state) : ScreenBase, IGameStateListener
    {
        private readonly GameState _state = state;

        private GauntletLayer _gauntletLayer;
        private EditorVM _dataSource;

#if BL13 || BL14
        private GauntletMovieIdentifier _movie;
#else
        private IGauntletMovie _movie;
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Open / Close                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// True while an editor game state exists.
        /// </summary>
        public static bool IsOpen { get; internal set; }

        [StaticClearAction]
        public static void Clear() => IsOpen = false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sprites                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static readonly string[] SpriteSheetsToLoad =
        [
            "ui_charactercreation",
            "ui_characterdeveloper",
            "ui_clan",
            "ui_kingdom",
            "ui_inventory",
            // May not be needed, but load just in case.
            "ui_armymanagement",
            "ui_town_management",
            "ui_partyscreen",
            "ui_mapbar",
            "ui_gameover",
            "ui_crafting",
            "ui_quest",
            "ui_saveload",
            "ui_boardgame",
        ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Called each frame while the editor screen is active.
        /// </summary>
        protected override void OnFrameTick(float dt)
        {
            if (_gauntletLayer == null)
                return;

            if (
                Input.IsKeyReleased(InputKey.Escape)
                || _gauntletLayer.Input.IsHotKeyReleased("Exit")
                || _gauntletLayer.Input.IsGameKeyPressed(41)
            )
            {
                Close();
            }

            // Tick for the settings manager to buffer events.
            ConfigurationManager.Tick();
        }

        /// <summary>
        /// Called when the editor game state is activated.
        /// </summary>
        void IGameStateListener.OnActivate()
        {
            OnActivate();
            IsOpen = true;

            // If we are coming back from the barber, keep the existing movie/layer.
            if (_gauntletLayer != null)
            {
                _gauntletLayer.IsFocusLayer = true;
                ScreenManager.TrySetFocus(_gauntletLayer);
                return;
            }

            Sprites.Load(SpriteSheetsToLoad);

#if BL13 || BL14
            _gauntletLayer = new GauntletLayer("GauntletLayer", 1, shouldClear: true);
#else
            _gauntletLayer = new GauntletLayer(1, "GauntletLayer", shouldClear: true);
#endif

            _gauntletLayer.InputRestrictions.SetInputRestrictions();
            _gauntletLayer.Input.RegisterHotKeyCategory(
                HotKeyManager.GetCategory("GenericPanelGameKeyCategory")
            );
            _gauntletLayer.Input.RegisterHotKeyCategory(
                HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory")
            );
            _gauntletLayer.IsFocusLayer = true;
            ScreenManager.TrySetFocus(_gauntletLayer);

            AddLayer(_gauntletLayer);

            var args = (_state as EditorGameState)?.LaunchArgs;

            _dataSource = new EditorVM(Close, args) { IsVisible = true };
            _movie = _gauntletLayer.LoadMovie("EditorScreen", _dataSource);
        }

        /// <summary>
        /// Called when the editor game state is finalized.
        /// </summary>
        void IGameStateListener.OnFinalize()
        {
            _dataSource?.OnFinalize();
            _dataSource = null;

            IsOpen = false;

            // Ensure we clean up even if we skipped teardown during barber.
            if (_gauntletLayer != null && _movie != null)
            {
                _gauntletLayer.ReleaseMovie(_movie);
                _movie = null;
            }

            if (_gauntletLayer != null)
            {
                RemoveLayer(_gauntletLayer);
                _gauntletLayer = null;
            }
        }

        /// <summary>
        /// Called when the editor game state is deactivated.
        /// </summary>
        void IGameStateListener.OnDeactivate()
        {
            OnDeactivate();

            if (_gauntletLayer == null)
                return;

            // If the barber is open, do NOT destroy our UI.
            if (BarberHelper.HasActiveSession)
            {
                _gauntletLayer.IsFocusLayer = false;
                ScreenManager.TryLoseFocus(_gauntletLayer);
                return;
            }

            if (_movie != null)
            {
                _gauntletLayer.ReleaseMovie(_movie);
                _movie = null;
            }

            _gauntletLayer.IsFocusLayer = false;
            ScreenManager.TryLoseFocus(_gauntletLayer);

            RemoveLayer(_gauntletLayer);
            _gauntletLayer = null;
        }

        /// <summary>
        /// Called when the editor game state is initialized.
        /// </summary>
        void IGameStateListener.OnInitialize() => OnInitialize();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void Close() => Game.Current?.GameStateManager?.PopState();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                       Game State                       //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Game state for the editor screen.
    /// </summary>
    public sealed class EditorGameState : GameState
    {
        public override bool IsMenuState => true;
        public EditorLaunchArgs LaunchArgs { get; set; }

        // Convenience helpers for navigation / UI layers.
        public EditorMode Mode => LaunchArgs?.Mode ?? EditorMode.Universal;
        public bool IsMapBarIntegrated => Mode == EditorMode.Player;
    }
}
