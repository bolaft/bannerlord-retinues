using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

[SafeClass]
public static class RetinueSources
{
    private const int WEIGHT_MOUNTED = 1000000;
    private const int WEIGHT_RANGED = 100000;
    private const int WEIGHT_FEMALE = 10000;
    private const double WEIGHT_WEAP = 1.0; // weapon Jaccard gets multiplied by this * 1000
    private const double WEIGHT_SKILL = 1.0; // skill cosine gets multiplied by this * 1000

    public static List<WCharacter> GetRetinueSourceTroops(WCharacter retinue)
    {
        Log.Debug($"RetinueSources: get sources for {retinue?.Name}.");

        var sources = new List<WCharacter>(2);

        if (retinue is null || !retinue.IsRetinue)
        {
            Log.Error($"RetinueSources: {retinue?.StringId} is not a retinue.");
            return sources;
        }

        // Identify which root to look under for culture and faction
        WCharacter cultureRoot = null, factionRoot = null;

        if (retinue.IsElite)
        {
            cultureRoot = retinue.Culture?.RootElite;
            factionRoot = retinue.Faction?.RootElite;
        }
        else
        {
            cultureRoot = retinue.Culture?.RootBasic;
            factionRoot = retinue.Faction?.RootBasic;
        }

        // Culture pick
        var culturePick = PickBestFromTree(cultureRoot, retinue);
        if (IsValid(culturePick))
            sources.Add(culturePick);

        // Faction pick (avoid duplicate)
        var factionPick = PickBestFromTree(factionRoot, retinue, exclude: culturePick);
        if (IsValid(factionPick))
            sources.Add(factionPick);

        return sources;
    }

    private static WCharacter PickBestFromTree(WCharacter root, WCharacter retinue, WCharacter exclude = null)
    {
        Log.Info($"RetinueSources: picking best from '{root?.Name}' tree for {retinue?.Name}.");
        if (!IsValid(root)) return null;
        Log.Info("RetinueSources: root is valid.");
        // Hard filter by Tier & IsElite first
        var candidates = root.Tree?
            .Where(t => IsValid(t)
                        && t.Tier == retinue.Tier
                        && (exclude == null || t.StringId != exclude.StringId))
            .ToList() ?? [];

        if (candidates.Count == 0)
            return null;

        // Rank by score; stable tie-break by StringId
        var best = candidates
            .Select(t => new { Troop = t, Score = EligibilityScore(t, retinue) })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Troop.StringId, StringComparer.Ordinal)
            .First().Troop;

        Log.Debug($"RetinueSources: best from '{root?.Name}' tree => {best?.Name}");
        return best;
    }

    private static bool IsValid(WCharacter t) => t != null && t.IsActive && !string.IsNullOrEmpty(t.StringId);

    private static int EligibilityScore(WCharacter troop, WCharacter retinue)
    {
        int score = 0;

        // 1) Mounted match (strongest)
        if (troop.IsMounted == retinue.IsMounted)
            score += WEIGHT_MOUNTED;

        // 2) Ranged match
        if (troop.IsRanged == retinue.IsRanged)
            score += WEIGHT_RANGED;

        // 3) Female match
        if (troop.IsFemale == retinue.IsFemale)
            score += WEIGHT_FEMALE;

        // 4) Weapon classes similarity (Jaccard)
        var w1 = SafeWeaponClasses(troop);
        var w2 = SafeWeaponClasses(retinue);
        double jacc = Similarity.Jaccard(w1, w2);
        score += (int)Math.Round(jacc * 1000.0 * WEIGHT_WEAP);

        // 5) Skillset similarity (cosine on shared keys)
        var s1 = SafeSkills(troop);
        var s2 = SafeSkills(retinue);
        double cos = Similarity.Cosine(s1, s2);
        score += (int)Math.Round(cos * 1000.0 * WEIGHT_SKILL);

        return score;
    }

    private static HashSet<string> SafeWeaponClasses(WCharacter c)
    {
        try
        {
            return new HashSet<string>(EquippedWeaponClasses(c).Where(s => !string.IsNullOrWhiteSpace(s)),
                                       StringComparer.OrdinalIgnoreCase);
        }
        catch { return new HashSet<string>(StringComparer.OrdinalIgnoreCase); }
    }

    private static List<string> EquippedWeaponClasses(WCharacter c)
    {
        var classes = new List<string>();
        foreach (var slot in WEquipment.Slots)
        {
            var item = c.Equipment.GetItem(slot);
            if (item != null && item.IsWeapon)
                classes.Add(item.Class);
        }
        return classes;
    }

    private static Dictionary<string, int> SafeSkills(WCharacter c)
    {
        try
        {
            // Convert SkillObject keys to stable string IDs
            var src = c?.Skills ?? [];
            var dict = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var kv in src)
            {
                var skillObj = kv.Key;
                var id = skillObj?.StringId;
                if (string.IsNullOrEmpty(id)) continue;
                dict[id] = kv.Value;
            }
            return dict;
        }
        catch { return new Dictionary<string, int>(StringComparer.Ordinal); }
    }
}
