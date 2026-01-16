using System;
using System.Collections.Generic;
using Retinues.Framework.Runtime;
using TaleWorlds.Localization;

namespace Retinues.GUI.Editor.Shared.Controllers
{
    [SafeClass(IncludeDerived = true)]
    public abstract class BaseController
    {
        internal static EditorState State => EditorState.Instance;

        /// <summary>
        /// Checks a series of conditions and returns the first failing reason, if any.
        /// </summary>
        public static bool Check(
            IEnumerable<(Func<bool> Test, TextObject Reason)> conditions,
            out TextObject reason
        )
        {
            foreach (var (Test, Reason) in conditions)
            {
                if (!Test())
                {
                    reason = Reason;
                    return false;
                }
            }

            reason = null;
            return true;
        }

        protected static ControllerAction<TArg> Action<TArg>(string name) => new(name);
    }
}
