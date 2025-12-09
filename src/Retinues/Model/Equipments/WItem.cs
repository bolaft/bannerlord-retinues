using System;
using TaleWorlds.Core;

namespace Retinues.Model.Equipments
{
    public class WItem(ItemObject @base) : WBase<WItem, ItemObject>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Name => Base.Name.ToString();
    }
}
