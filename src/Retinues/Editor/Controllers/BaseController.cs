using Retinues.Utilities;

namespace Retinues.Editor.Controllers
{
    [SafeClass(IncludeDerived = true)]
    public abstract class BaseController
    {
        internal static State State => State.Instance;
    }
}
