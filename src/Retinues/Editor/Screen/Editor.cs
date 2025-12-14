using Retinues.Editor.VM;
using Retinues.Engine;
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
    }

    /// <summary>
    /// Gauntlet screen for the editor.
    /// </summary>
    [GameStateScreen(typeof(EditorState))]
    public sealed class GauntletEditorScreen(GameState state) : ScreenBase, IGameStateListener
    {
        private readonly GameState _state = state;
        private GauntletMovieIdentifier _movie;
        private GauntletLayer _gauntletLayer;

        private EditorVM _dataSource;

        static readonly string[] SpriteSheetsToLoad =
        [
            "ui_charactercreation",
            "ui_characterdeveloper",
            "ui_clan",
            "ui_kingdom",
            "ui_inventory",
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

            Sprites.Load(SpriteSheetsToLoad);

            _gauntletLayer = new GauntletLayer("GauntletLayer", 1, shouldClear: true);
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

            // Create EditorVM with a close callback
            _dataSource = new EditorVM(Close) { IsVisible = true };

            _movie = _gauntletLayer.LoadMovie("EditorScreen", _dataSource);
        }

        void IGameStateListener.OnFinalize()
        {
            _dataSource?.OnFinalize();
            _dataSource = null;
            _movie = null;
            _gauntletLayer = null;
        }

        private void Close()
        {
            Game.Current?.GameStateManager?.PopState();
        }

        void IGameStateListener.OnDeactivate()
        {
            base.OnDeactivate();

            if (_gauntletLayer == null)
                return;

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
    }
}
