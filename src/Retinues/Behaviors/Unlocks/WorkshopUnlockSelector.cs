using System;
using System.Collections.Generic;
using Retinues.Domain.Equipments.Wrappers;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;

namespace Retinues.Game.Unlocks
{
    /// <summary>
    /// Selects appropriate unlock target items for workshops.
    /// </summary>
    public static class WorkshopUnlockSelector
    {
        [Flags]
        private enum Mat : byte
        {
            None = 0,
            Metal = 1 << 0,
            Leather = 1 << 1,
            Cloth = 1 << 2,
            Wood = 1 << 3,
        }

        private sealed class Candidate
        {
            public WItem Item;
            public int Score;
        }

        // Cache candidates per (culture|workshopTypeId). Candidates include score and are sorted.
        private static readonly Dictionary<string, List<Candidate>> _cache = new(
            StringComparer.Ordinal
        );

        /// <summary>
        /// Determines whether the given workshop can unlock equipment.
        /// </summary>
        public static bool CanUnlock(Workshop workshop)
        {
            if (workshop?.WorkshopType == null)
                return false;

            return CanUnlock(workshop.WorkshopType);
        }

        /// <summary>
        /// Determines whether the given workshop type can unlock equipment.
        /// </summary>
        public static bool CanUnlock(WorkshopType type)
        {
            return GetWorkshopMaterials(type) != Mat.None;
        }

        /// <summary>
        /// Picks a target item for the given workshop, or null if none available.
        /// </summary>
        public static WItem PickTargetItem(Workshop workshop, int seed)
        {
            var cultureId = workshop.Settlement.Culture?.StringId;
            var type = workshop.WorkshopType;

            var candidates = GetCandidates(type, cultureId);
            if (candidates == null || candidates.Count == 0)
                return null;

            // Find best score among currently locked items.
            var bestScore = int.MinValue;
            for (var i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (c.Item == null || !c.Item.IsValidEquipment || c.Item.IsUnlocked)
                    continue;

                bestScore = c.Score;
                break;
            }

            if (bestScore == int.MinValue)
                return null;

            // Collect all locked items with that best score.
            var best = new List<WItem>();
            for (var i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (c.Score != bestScore)
                    break;

                if (c.Item == null || !c.Item.IsValidEquipment || c.Item.IsUnlocked)
                    continue;

                best.Add(c.Item);
            }

            if (best.Count == 0)
                return null;

            best.Sort((a, b) => string.CompareOrdinal(a.StringId, b.StringId));

            var s = workshop.GetHashCode();
            s = unchecked(s * 397) ^ seed;

            var idx = s % best.Count;
            if (idx < 0)
                idx = -idx;

            return best[idx];
        }

        /// <summary>
        /// Gets candidate unlock items for the given workshop type and culture.
        /// </summary>
        private static List<Candidate> GetCandidates(WorkshopType type, string cultureId)
        {
            cultureId ??= "neutral";
            var typeId = type?.StringId ?? "unknown";

            var key = $"{cultureId}|{typeId}";
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            var mats = GetWorkshopMaterials(type);

            // If we cannot infer any relevant "equipment material" for this workshop (wine press, brewery,
            // olive press, etc), then it should not unlock equipment at all.
            if (mats == Mat.None)
            {
                var empty = new List<Candidate>(0);
                _cache[key] = empty;
                return empty;
            }

            var list = new List<Candidate>(256);

            foreach (var item in WItem.Equipments)
            {
                if (item == null || !item.IsValidEquipment)
                    continue;

                var cultureScore = 0;
                if (cultureId != "neutral")
                {
                    if (item.Culture == null)
                        cultureScore = 5;
                    else if (item.Culture.StringId == cultureId)
                        cultureScore = 15;
                }

                var score = ScoreItem(item, mats) + cultureScore;
                if (score <= 0)
                    continue;

                list.Add(new Candidate { Item = item, Score = score });
            }

            list.Sort(
                (a, b) =>
                {
                    var s = b.Score.CompareTo(a.Score);
                    if (s != 0)
                        return s;
                    return string.CompareOrdinal(a.Item?.StringId, b.Item?.StringId);
                }
            );

            _cache[key] = list;
            return list;
        }

        /// <summary>
        /// Scores an item for suitability for unlocking by a workshop producing the given materials.
        /// </summary>
        private static int ScoreItem(WItem item, Mat workshopMats)
        {
            // No "broad fallback": if the workshop doesn't map to equipment materials, it shouldn't unlock.
            if (workshopMats == Mat.None)
                return 0;

            var itemMats = GetItemMaterials(item);
            if (itemMats == Mat.None)
                return 0;

            var overlap = itemMats & workshopMats;
            if (overlap == Mat.None)
                return 0;

            var overlapBits = BitCount((byte)overlap);
            var score = 50 + overlapBits * 40;

            if ((workshopMats & Mat.Metal) != 0)
            {
                if (item.IsWeapon)
                    score += 15;
                if (item.IsArmor)
                    score += 10;
                if (item.IsShield)
                    score += 8;
            }

            if ((workshopMats & Mat.Leather) != 0)
            {
                if (item.Type == ItemObject.ItemTypeEnum.HorseHarness)
                    score += 15;
                if (item.IsArmor)
                    score += 8;

                // Prefer leather-first projects: penalize mixed leather+metal items for tanneries.
                if ((itemMats & Mat.Metal) != 0)
                    score -= 25;
            }

            if ((workshopMats & Mat.Cloth) != 0)
            {
                if (item.Type == ItemObject.ItemTypeEnum.Cape)
                    score += 15;
                if (item.IsArmor)
                    score += 8;

                // Prefer cloth-first projects: penalize mixed cloth+metal items for weaveries.
                if ((itemMats & Mat.Metal) != 0)
                    score -= 25;
            }

            if ((workshopMats & Mat.Wood) != 0)
            {
                if (item.IsShield)
                    score += 12;
                if (
                    item.Type == ItemObject.ItemTypeEnum.Bow
                    || item.Type == ItemObject.ItemTypeEnum.Crossbow
                )
                    score += 12;
                if (item.IsAmmo)
                    score += 8;
            }

            // Metal shops should strongly prefer metal-first items (and avoid cloth/leather-first).
            if ((workshopMats & Mat.Metal) != 0)
            {
                var ac = item.Base?.ArmorComponent;

                if (ac != null)
                {
                    // ArmorComponent gives a strong signal for armor/harnesses.
                    switch (ac.MaterialType)
                    {
                        case ArmorComponent.ArmorMaterialTypes.Leather:
                            score -= 45; // stronger penalty than before
                            break;

                        case ArmorComponent.ArmorMaterialTypes.Cloth:
                            score -= 55; // even less desirable for smithies
                            break;

                        case ArmorComponent.ArmorMaterialTypes.Chainmail:
                        case ArmorComponent.ArmorMaterialTypes.Plate:
                            score += 20; // metal-first bonus
                            break;
                    }
                }
                else
                {
                    // Fallback: if it's not clearly "metal-only", penalize a bit.
                    // This keeps smithies from selecting wood/leather/cloth-heavy items when any metal-heavy item exists.
                    var hasMetal = (itemMats & Mat.Metal) != 0;
                    var hasOther = (itemMats & (Mat.Leather | Mat.Cloth | Mat.Wood)) != 0;

                    if (hasMetal && !hasOther)
                        score += 15; // "full metal" per our heuristic
                    else if (!hasMetal)
                        score -= 40; // no metal at all => very unlikely for smithy
                    else
                        score -= 10; // mixed => mild penalty (still possible for crossbows/shields etc)
                }
            }

            return Math.Max(score, 1);
        }

        /// <summary>
        /// Gets candidate unlock items for the given workshop type and culture.
        /// </summary>
        private static Mat GetWorkshopMaterials(WorkshopType type)
        {
            if (type?.Productions == null || type.Productions.Count == 0)
                return Mat.None;

            Mat mats = Mat.None;

            for (var p = 0; p < type.Productions.Count; p++)
            {
                var prod = type.Productions[p];
                mats |= CategorizeCategories(prod.Inputs);
                mats |= CategorizeCategories(prod.Outputs);
            }

            if (mats == Mat.None)
                mats = FallbackWorkshopIdGuess(type.StringId);

            return mats;
        }

        /// <summary>
        /// Categorizes a list of item categories into material flags.
        /// </summary>
        private static Mat CategorizeCategories(IReadOnlyList<(ItemCategory, int)> cats)
        {
            Mat mats = Mat.None;

            if (cats == null)
                return mats;

            for (var i = 0; i < cats.Count; i++)
            {
                var c = cats[i].Item1;
                if (c == null)
                    continue;

                mats |= CategorizeString(c.StringId);
            }

            return mats;
        }

        /// <summary>
        /// Gets the material types used by the given item.
        /// </summary>
        private static Mat GetItemMaterials(WItem item)
        {
            if (item?.Base == null)
                return Mat.None;

            Mat mats = Mat.None;

            var ac = item.Base.ArmorComponent;
            if (ac != null)
            {
                switch (ac.MaterialType)
                {
                    case ArmorComponent.ArmorMaterialTypes.Cloth:
                        mats |= Mat.Cloth;
                        break;
                    case ArmorComponent.ArmorMaterialTypes.Leather:
                        mats |= Mat.Leather;
                        break;
                    case ArmorComponent.ArmorMaterialTypes.Chainmail:
                    case ArmorComponent.ArmorMaterialTypes.Plate:
                        mats |= Mat.Metal;
                        break;
                }
            }

            switch (item.Type)
            {
                case ItemObject.ItemTypeEnum.Cape:
                    mats |= Mat.Cloth;
                    break;

                case ItemObject.ItemTypeEnum.HorseHarness:
                    mats |= Mat.Leather | Mat.Metal;
                    break;

                case ItemObject.ItemTypeEnum.Bow:
                    mats |= Mat.Wood;
                    break;

                case ItemObject.ItemTypeEnum.Crossbow:
                    mats |= Mat.Wood | Mat.Metal;
                    break;

                case ItemObject.ItemTypeEnum.Arrows:
                case ItemObject.ItemTypeEnum.Bolts:
#if BL13
                case ItemObject.ItemTypeEnum.SlingStones:
#endif
                    mats |= Mat.Wood | Mat.Metal;
                    break;

                case ItemObject.ItemTypeEnum.Shield:
                    mats |= Mat.Wood | Mat.Leather | Mat.Metal;
                    break;

                case ItemObject.ItemTypeEnum.OneHandedWeapon:
                case ItemObject.ItemTypeEnum.TwoHandedWeapon:
                case ItemObject.ItemTypeEnum.Polearm:
                case ItemObject.ItemTypeEnum.Thrown:
                case ItemObject.ItemTypeEnum.Pistol:
                case ItemObject.ItemTypeEnum.Musket:
                case ItemObject.ItemTypeEnum.Bullets:
                    mats |= Mat.Metal | Mat.Wood;
                    break;
            }

            mats |= CategorizeString(item.StringId);
            mats |= CategorizeString(item.Category?.StringId);

            return mats;
        }

        /// <summary>
        /// Categorizes a string identifier into material flags.
        /// </summary>
        private static Mat CategorizeString(string s)
        {
            if (string.IsNullOrEmpty(s))
                return Mat.None;

            var id = s.ToLowerInvariant();
            Mat mats = Mat.None;

            if (
                id.Contains("iron")
                || id.Contains("steel")
                || id.Contains("ore")
                || id.Contains("tool")
                || id.Contains("weapon")
                || id.Contains("armour")
                || id.Contains("armor")
                || id.Contains("mail")
                || id.Contains("plate")
                || id.Contains("chain")
                || id.Contains("jewel")
                || id.Contains("silver")
                || id.Contains("gold")
            )
                mats |= Mat.Metal;

            if (
                id.Contains("leather")
                || id.Contains("hide")
                || id.Contains("fur")
                || id.Contains("pelt")
            )
                mats |= Mat.Leather;

            if (
                id.Contains("cloth")
                || id.Contains("linen")
                || id.Contains("wool")
                || id.Contains("velvet")
                || id.Contains("cotton")
                || id.Contains("silk")
            )
                mats |= Mat.Cloth;

            if (id.Contains("wood") || id.Contains("hardwood") || id.Contains("lumber"))
                mats |= Mat.Wood;

            return mats;
        }

        /// <summary>
        /// Gets candidate unlock items for the given workshop type and culture.
        /// </summary>
        private static Mat FallbackWorkshopIdGuess(string workshopTypeId)
        {
            if (string.IsNullOrEmpty(workshopTypeId))
                return Mat.None;

            var id = workshopTypeId.ToLowerInvariant();

            if (id.Contains("smith") || id.Contains("blacksmith") || id.Contains("silversmith"))
                return Mat.Metal;

            if (id.Contains("tann") || id.Contains("leather"))
                return Mat.Leather;

            if (
                id.Contains("weav")
                || id.Contains("linen")
                || id.Contains("velvet")
                || id.Contains("wool")
            )
                return Mat.Cloth;

            if (id.Contains("wood") || id.Contains("lumber"))
                return Mat.Wood;

            return Mat.None;
        }

        /// <summary>
        /// Counts the number of set bits in a byte.
        /// </summary>
        private static int BitCount(byte v)
        {
            var c = 0;
            while (v != 0)
            {
                v &= (byte)(v - 1);
                c++;
            }
            return c;
        }
    }
}
