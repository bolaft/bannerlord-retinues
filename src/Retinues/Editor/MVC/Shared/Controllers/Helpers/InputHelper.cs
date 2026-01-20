using System;
using Retinues.Framework.Runtime;
using TaleWorlds.InputSystem;

namespace Retinues.Editor.MVC.Shared.Controllers.Helpers
{
    /// <summary>
    /// Helper methods for input handling in the editor GUI.
    /// </summary>
    [SafeClass]
    public static class InputHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Batch Input                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        const int DefaultBatchSize = 1;
        const int ShiftBatchSize = 5;
        const int ControlBatchSize = 1000;

        /// <summary>
        /// Determine batch input multiplier based on modifier keys.
        /// </summary>
        public static int BatchInput(int cap = int.MaxValue)
        {
            int batch;

            if (Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl))
                batch = ControlBatchSize;
            else if (Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift))
                batch = ShiftBatchSize;
            else
                batch = DefaultBatchSize;

            return Math.Min(batch, cap);
        }
    }
}
