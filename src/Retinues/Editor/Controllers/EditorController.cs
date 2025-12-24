using System;
using System.Collections.Generic;
using Retinues.Utilities;
using TaleWorlds.Localization;

namespace Retinues.Editor.Controllers
{
    [SafeClass(IncludeDerived = true)]
    public abstract class EditorController
    {
        internal static State State => State.Instance;

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

        protected static EditorAction<TArg> Action<TArg>(string name) => new(name);
    }
}
