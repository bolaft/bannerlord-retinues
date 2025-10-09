using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Retinues.Doctrines.Model;
using Retinues.Utils;

namespace Retinues.Doctrines
{
    /// <summary>
    /// Static helper for discovering all Doctrine subclasses in the catalog namespace.
    /// </summary>
    public static class DoctrineDiscovery
    {
        // Discover all Doctrine subclasses in the target namespace/assembly.
        /// <summary>
        /// Finds all Doctrine subclasses in the given namespace, orders by grid position.
        /// </summary>
        public static IReadOnlyList<Doctrine> DiscoverDoctrines(
            string @namespaceStartsWith = "Retinues.Doctrines.Catalog"
        )
        {
            var list = new List<Doctrine>();

            try
            {
                var ass = Assembly.GetExecutingAssembly();

                foreach (var t in ass.GetTypes())
                {
                    if (!typeof(Doctrine).IsAssignableFrom(t) || t.IsAbstract)
                        continue;
                    if (!t.FullName.StartsWith(@namespaceStartsWith, StringComparison.Ordinal))
                        continue;

                    var ctor = t.GetConstructor(Type.EmptyTypes);
                    if (ctor == null)
                        continue;

                    var d = (Doctrine)Activator.CreateInstance(t);
                    list.Add(d);
                }

                // order by grid
                return [.. list.OrderBy(d => d.Column).ThenBy(d => d.Row)];
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }

            return list;
        }
    }
}
