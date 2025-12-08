using System;
using TaleWorlds.Core;

namespace Retinues.Model.Equipments
{
    public class WItem : Wrapper<WItem, ItemObject>
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
        [Persistent]
        public int Stock
        {
            get => Get<int>();
            set => Set(Math.Max(0, value));
        }

        [Persistent]
        public bool Unlocked
        {
            get => Get<bool>();
            set => Set(value);
        }
    }
}
