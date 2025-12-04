using System.Collections.Generic;
using Retinues.Safety.Sanitizer;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace Retinues.Mods.BanditMilitias
{
    /// <summary>
    /// Console cheats related to Bandit Militias compatibility.
    /// Allows purging Retinues custom troops from all Bandit Militia parties.
    /// </summary>
    public static class BanditMilitiasCheats
    {
        /// <summary>
        /// Purges all Retinues custom troops from Bandit Militias parties
        /// by sanitizing their rosters with replaceAllCustom=true.
        /// Usage: retinues.purge_bandit_militias
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("purge_bandit_militias", "retinues")]
        public static string PurgeBanditMilitias(List<string> args)
        {
            if (!ModCompatibility.HasBanditMilitias)
                return "BanditMilitias mod not detected; cannot purge Bandit Militia parties.";

            int partiesFound = 0;

            foreach (var mp in MobileParty.All)
            {
                if (mp?.PartyComponent == null)
                    continue;

                // Avoid compile-time dependency on BanditMilitias.dll:
                // identify their parties by PartyComponent type name.
                var typeName = mp.PartyComponent.GetType().FullName;
                if (typeName != "BanditMilitias.ModBanditMilitiaPartyComponent")
                    continue;

                partiesFound++;

                // Sanitize both member and prison rosters, replacing all custom troops.
                PartySanitizer.SanitizeParty(mp, replaceAllCustom: true);
            }

            if (partiesFound == 0)
                return "No Bandit Militia parties found (is BanditMilitias active in this save?).";

            return $"Sanitized {partiesFound} Bandit Militia parties.";
        }
    }
}
