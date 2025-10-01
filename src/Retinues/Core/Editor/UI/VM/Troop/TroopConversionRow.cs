using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI.VM.Troop
{
    [SafeClass]
    public sealed class TroopConversionRowVM : ViewModel
    {
        public TroopConversionRowVM(WCharacter from, WCharacter to, TroopEditorVM editor)
        {
            _from = from;
            _to = to;
            _editor = editor;
            Refresh();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly TroopEditorVM _editor;
        private readonly WCharacter _from;
        private readonly WCharacter _to;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string FromDisplay => $"{_from?.Name} ({FromAvailableVirtual})";

        [DataSourceProperty]
        public string ToDisplay => $"{_to?.Name} ({ToAvailableVirtual}/{_editor.RetinueCap})";

        [DataSourceProperty]
        public bool CanRecruit => _editor.GetMaxStageable(_from, _to) > 0;

        [DataSourceProperty]
        public bool CanRelease => _editor.GetVirtualCount(_to) > 0;

        [DataSourceProperty]
        public int ConversionCost => PendingAmount * TroopRules.ConversionCostPerUnit(_to);

        [DataSourceProperty]
        public int PendingAmount => _editor.GetPendingAmount(_from, _to);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteRecruit()
        {
            int amount = ReadBatchAmount();
            _editor.StageConversion(_from, _to, amount);
            // Row visuals update via Refresh()
            Refresh();
        }

        [DataSourceMethod]
        public void ExecuteRelease()
        {
            int amount = ReadBatchAmount();
            _editor.StageConversion(_to, _from, amount);
            Refresh();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int FromAvailable => _from != null ? Player.Party.MemberRoster.CountOf(_from) : 0;
        public int ToAvailable => _to != null ? Player.Party.MemberRoster.CountOf(_to) : 0;

        public void Refresh()
        {
            OnPropertyChanged(nameof(FromDisplay));
            OnPropertyChanged(nameof(ToDisplay));
            OnPropertyChanged(nameof(CanRecruit));
            OnPropertyChanged(nameof(CanRelease));
            OnPropertyChanged(nameof(ConversionCost));

            _editor.OnPropertyChanged(nameof(_editor.TroopCount));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private int FromAvailableVirtual => _editor.GetVirtualCount(_from);
        private int ToAvailableVirtual => _editor.GetVirtualCount(_to);

        private static int ReadBatchAmount()
        {
            if (Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl))
                return 500;
            if (Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift))
                return 5;
            return 1;
        }
    }
}
