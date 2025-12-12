using System.Collections.Generic;
using System.Linq;

namespace Retinues.Editor.VM.List
{
    /// <summary>
    /// Base class for list builders.
    /// </summary>
    public abstract class BaseListBuilder
    {
        private static readonly List<BaseListBuilder> _instances = [];

        public static BaseListBuilder GetInstance<T>()
            where T : BaseListBuilder, new()
        {
            var existing = _instances.OfType<T>().FirstOrDefault();
            if (existing != null)
                return existing;
            var instance = new T();
            _instances.Add(instance);
            return instance;
        }

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
