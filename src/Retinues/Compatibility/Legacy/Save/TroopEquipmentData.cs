using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using TaleWorlds.SaveSystem;

namespace Retinues.Compatibility.Legacy.Save
{
    /// <summary>
    /// Legacy equipment save schema.
    /// Stores equipment sets as equipment codes plus a per-index civilian flag list.
    /// </summary>
    public sealed class TroopEquipmentData
    {
        [SaveableField(1)]
        public List<string> Codes;

        // Per-index civilian flags (null for older legacy saves).
        [SaveableField(2)]
        public List<bool> Civilians;

        public TroopEquipmentData() { }

        public List<MEquipment> Deserialize(WCharacter owner)
        {
            var result = new List<MEquipment>(Codes?.Count ?? 0);

            if (owner == null)
                return result;

            // Back-compat default: if Civilians is missing or size mismatch,
            // use "index 1 is civilian, all others battle".
            bool hasFlags = Civilians != null && Civilians.Count == Codes?.Count;

            for (int idx = 0; idx < (Codes?.Count ?? 0); idx++)
            {
                var code = Codes[idx];

                bool isCivilian;
                if (hasFlags)
                    isCivilian = Civilians[idx];
                else
                    isCivilian = idx == 1;

                MEquipment me;
                if (string.IsNullOrWhiteSpace(code))
                    me = MEquipment.Create(owner, civilian: isCivilian, source: null);
                else
                {
                    me = MEquipment.FromCode(owner, code);
                    if (me == null)
                        me = MEquipment.Create(owner, civilian: isCivilian, source: null);

                    me.IsCivilian = isCivilian;
                }

                result.Add(me);
            }

            return result;
        }
    }
}
