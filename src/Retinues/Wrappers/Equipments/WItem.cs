using System;
using TaleWorlds.Core;

namespace Retinues.Wrappers.Equipments
{
    /// <summary>
    /// Wrapper for ItemObject, provides helpers and persistent state.
    /// </summary>
    public class WItem : WrappedObject<WItem, ItemObject>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// The name of this item.
        /// </summary>
        public string Name => Base.Name.ToString();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Persistent State                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Current stock count for this item.
        /// </summary>
        [WrapData]
        public int Stock
        {
            get => Get<int>();
            set => Set(Math.Max(0, value));
        }

        [WrapData]
        public bool Unlocked
        {
            get => Get<bool>();
            set => Set(value);
        }
    }
}
