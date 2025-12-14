using System;
using System.Collections.Generic;
using System.Reflection;
using Retinues.Configuration;
using TaleWorlds.Localization;

namespace OldRetinues.Doctrines.Model
{
    /// <summary>
    /// Base class for doctrines. Provides metadata, costs, and auto-discovery of nested feats.
    /// </summary>
    public abstract class Doctrine
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Metadata                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public abstract TextObject Name { get; }
        public abstract TextObject Description { get; }
        public virtual int Column => 0; // 0..3
        public virtual int Row => 0; // 0..3
        public virtual bool IsDisabled => false;
        public virtual TextObject DisabledMessage => Description;
        public int GoldCost
        {
            get
            {
                int baseCost = Row switch
                {
                    0 => 1000,
                    1 => 5000,
                    2 => 25000,
                    3 => 100000,
                    _ => 0,
                };

                return (int)(baseCost * Config.DoctrineGoldCostMultiplier);
            }
        }

        public int InfluenceCost
        {
            get
            {
                int baseCost = Row switch
                {
                    0 => 50,
                    1 => 100,
                    2 => 200,
                    3 => 500,
                    _ => 0,
                };

                return (int)(baseCost * Config.DoctrineInfluenceCostMultiplier);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Type-based key used internally/persisted
        public string Key => GetType().FullName;

        /// <summary>
        /// Auto-discovers and instantiates nested Feat types for this doctrine.
        /// </summary>
        public virtual IEnumerable<Feat> InstantiateFeats()
        {
            var t = GetType();
            foreach (var ft in t.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!typeof(Feat).IsAssignableFrom(ft) || ft.IsAbstract)
                    continue;
                var ctor = ft.GetConstructor(Type.EmptyTypes);
                if (ctor == null)
                    continue;

                var feat = (Feat)Activator.CreateInstance(ft);
                yield return feat;
            }
        }
    }
}
