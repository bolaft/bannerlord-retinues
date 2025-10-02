using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Localization;

namespace Retinues.Core.Features.Doctrines.Model
{
    public abstract class Doctrine
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Metadata                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public abstract TextObject Name { get; }
        public abstract TextObject Description { get; }
        public virtual int Column => 0; // 0..3
        public virtual int Row => 0; // 0..3
        public int GoldCost
        {
            get
            {
                return Row switch
                {
                    0 => 1000,
                    1 => 5000,
                    2 => 25000,
                    3 => 100000,
                    _ => 0,
                };
            }
        }

        public int InfluenceCost
        {
            get
            {
                return Row switch
                {
                    0 => 50,
                    1 => 100,
                    2 => 200,
                    3 => 500,
                    _ => 0,
                };
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Type-based key used internally/persisted
        public string Key => GetType().FullName;

        // Auto-discover nested feats (public sealed classes w/ parameterless ctor)
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
