using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Retinues.Core.Features.Doctrines.Model;
using Retinues.Core.Utils;

namespace Retinues.Core.Features.Doctrines
{
    public static class DoctrineDiscovery
    {
        // Discover all Doctrine subclasses in the target namespace/assembly.
        public static IReadOnlyList<Doctrine> DiscoverDoctrines(
            string @namespaceStartsWith = "Retinues.Core.Features.Doctrines.Catalog"
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
