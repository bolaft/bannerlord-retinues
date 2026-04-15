using System;
using System.Collections.Generic;
using TaleWorlds.Localization;

namespace Retinues.Behaviors.Doctrines.Definitions
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
        public Doctrine Add(
            string id,
            TextObject name,
            TextObject description,
            string sprite,
            Func<bool> overridden,
            TextObject overriddenHint,
            Func<string> previewCharacterId = null,
            bool previewCivilian = false
        )
        {
            var doctrine = new Doctrine(
                id: id,
                category: this,
                name: name,
                description: description,
                sprite: sprite,
                overridden: overridden,
                overriddenHint: overriddenHint,
                previewCharacterId: previewCharacterId,
                previewCivilian: previewCivilian
            );

            Doctrines.Add(doctrine);

            return doctrine;
        }
    }
}
