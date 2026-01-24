using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace Retinues.Compatibility.Legacy.Save
{
    /// <summary>
    /// Legacy skill save schema.
    /// Stores all skills as a single encoded string: "skillId:value;skillId:value".
    /// </summary>
    public sealed class TroopSkillData
    {
        [SaveableField(1)]
        public string Code;

        public TroopSkillData() { }

        public Dictionary<SkillObject, int> Deserialize()
        {
            var result = new Dictionary<SkillObject, int>();

            if (string.IsNullOrWhiteSpace(Code))
                return result;

            var mgr = MBObjectManager.Instance;
            if (mgr == null)
                return result;

            var parts = Code.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                var p = parts[i];
                if (string.IsNullOrWhiteSpace(p))
                    continue;

                var idx = p.IndexOf(':');
                if (idx <= 0 || idx >= p.Length - 1)
                    continue;

                var skillId = p.Substring(0, idx);
                var valueStr = p.Substring(idx + 1);
                if (string.IsNullOrWhiteSpace(skillId))
                    continue;

                if (!int.TryParse(valueStr, out var value))
                    continue;

                var skill = mgr.GetObject<SkillObject>(skillId);
                if (skill != null)
                    result[skill] = value;
            }

            return result;
        }
    }
}
