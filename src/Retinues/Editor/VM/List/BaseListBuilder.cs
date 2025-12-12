using System.Collections.Generic;
using System.Linq;

namespace Retinues.Editor.VM.List
{
    /// <summary>
    /// Base class for list builders.
    /// </summary>
    public abstract class BaseListBuilder
    {
        public void Build(ListVM list)
        {
            BuildSortButtons(list);
            BuildSections(list);

            list.RecomputeHeaderStates();
        }

        protected abstract void BuildSortButtons(ListVM list);
        protected abstract void BuildSections(ListVM list);
    }
}
