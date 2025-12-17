using Retinues.Editor.VM;
using Retinues.Helpers;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace Retinues.Editor.Screen
{
    /// <summary>
    /// Game state for the editor screen.
    /// </summary>
    public sealed class EditorState : GameState
    {
        public override bool IsMenuState => true;
        public EditorLaunchArgs LaunchArgs { get; set; }
    }

    /// <summary>
    /// Gauntlet screen for the editor.
    /// </summary>
    [GameStateScreen(typeof(EditorState))]
    public sealed class GauntletEditorScreen(GameState state) : ScreenBase, IGameStateListener
    {
#if BL13
        private GauntletMovieIdentifier _movie;
#else
        private IGauntletMovie _movie;
#endif

        private readonly GameState _state = state;

        private GauntletLayer _gauntletLayer;
        private EditorVM _dataSource;

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

        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);

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
        }

        void IGameStateListener.OnActivate()
        {
            base.OnActivate();

            // If we are coming back from the barber, keep the existing movie/layer.
            if (_gauntletLayer != null)
            {
                _gauntletLayer.IsFocusLayer = true;
                ScreenManager.TrySetFocus(_gauntletLayer);
                return;
            }

            Sprites.Load(SpriteSheetsToLoad);

#if BL13
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

            var args = (_state as EditorState)?.LaunchArgs;

            _dataSource = new EditorVM(Close, args) { IsVisible = true };
            _movie = _gauntletLayer.LoadMovie("EditorScreen", _dataSource);
        }

        void IGameStateListener.OnFinalize()
        {
            _dataSource?.OnFinalize();
            _dataSource = null;

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

        void IGameStateListener.OnDeactivate()
        {
            base.OnDeactivate();

            if (_gauntletLayer == null)
                return;

            // If the barber is open, do NOT destroy our UI.
            if (Retinues.Editor.Barber.HasActiveSession)
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

        void IGameStateListener.OnInitialize()
        {
            OnInitialize();
        }

        private void Close()
        {
            Game.Current?.GameStateManager?.PopState();
        }
    }
}
