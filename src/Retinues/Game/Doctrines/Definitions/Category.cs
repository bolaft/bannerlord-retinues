using System.Collections.Generic;
using TaleWorlds.Localization;

namespace Retinues.Game.Doctrines.Definitions
{
    public sealed class Category(string id, TextObject name)
    {
        /// <summary>
        /// Gets a category by its ID.
        /// </summary>
        public static Category Get(string id) => DoctrinesRegistry.GetCategory(id);

        /* ━━━━━━━ Identity ━━━━━━━ */

        public string Id { get; } = id ?? string.Empty;
        public TextObject Name { get; } = name;

        /* ━━━━━━━ Doctrines ━━━━━━ */

        public List<Doctrine> Doctrines { get; } = [];

        /// <summary>
        /// Adds a doctrine to this category.
        /// </summary>
        public Doctrine Add(string id, TextObject name, TextObject description)
        {
            var doctrine = new Doctrine(
                id: id,
                category: this,
                name: name,
                description: description
            );

            Doctrines.Add(doctrine);

            return doctrine;
        }
    }
}
