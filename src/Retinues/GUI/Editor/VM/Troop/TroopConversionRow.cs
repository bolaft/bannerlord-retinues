using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop
{
    /// <summary>
    /// ViewModel for a troop conversion row. Handles recruiting, releasing, cost calculation, and UI refresh.
    /// </summary>
    [SafeClass]
    public sealed class TroopConversionRowVM(WCharacter from, WCharacter to, TroopPanelVM editor)
        : ViewModel
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly TroopPanelVM _editor = editor;
        private readonly WCharacter _from = from;
        private readonly WCharacter _to = to;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string FromDisplay => $"{Format.Crop(_from?.Name, 40)} ({FromAvailableVirtual})";

        [DataSourceProperty]
        public string ToDisplay =>
            $"{Format.Crop(_to?.Name, 40)} ({ToAvailableVirtual}/{_editor.RetinueCap})";

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
            if (_editor.Screen?.ConversionIsAllowed == false)
                return; // Conversion not allowed in current context
            int amount = ReadBatchAmount();
            _editor.StageConversion(_from, _to, amount);
        }

        [DataSourceMethod]
        public void ExecuteRelease()
        {
            if (_editor.Screen?.ConversionIsAllowed == false)
                return; // Conversion not allowed in current context
            int amount = ReadBatchAmount();
            _editor.StageConversion(_to, _from, amount);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int FromAvailable => _from != null ? Player.Party.MemberRoster.CountOf(_from) : 0;
        public int ToAvailable => _to != null ? Player.Party.MemberRoster.CountOf(_to) : 0;

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
