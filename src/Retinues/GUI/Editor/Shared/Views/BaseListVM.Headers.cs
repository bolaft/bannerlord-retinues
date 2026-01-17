using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.Shared.Views
{
    /// <summary>
    /// Partial class for base list ViewModel handling headers.
    /// </summary>
    public abstract partial class BaseListVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Headers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private MBBindingList<ListHeaderVM> _headers = [];

        [DataSourceProperty]
        public MBBindingList<ListHeaderVM> Headers
        {
            get => _headers;
            protected set
            {
                if (ReferenceEquals(value, _headers))
                    return;

                _headers = value ?? [];
                OnPropertyChanged(nameof(Headers));
            }
        }

        /// <summary>
        /// Adds a header to the list.
        /// </summary>
        public void AddHeader(ListHeaderVM header)
        {
            if (header == null)
                return;

            // Insert at index 0 because the list is displayed in reverse.
            _headers.Insert(0, header);
        }

        /// <summary>
        /// Sets the headers of the list, replacing any existing ones.
        /// </summary>
        public void SetHeaders(IEnumerable<ListHeaderVM> headers)
        {
            var newHeaders = new MBBindingList<ListHeaderVM>();

            if (headers != null)
            {
                // List is displayed in reverse, so add in reverse order once.
                var tmp = headers as IList<ListHeaderVM> ?? [.. headers];
                for (int i = tmp.Count - 1; i >= 0; i--)
                {
                    var h = tmp[i];
                    if (h == null)
                        continue;

                    newHeaders.Add(h);
                }
            }

            Headers = newHeaders;
        }

        /// <summary>
        /// Recomputes the visibility and enabled states of all headers.
        /// </summary>
        public void RecomputeHeaderStates()
        {
            for (int i = 0; i < _headers.Count; i++)
                _headers[i]?.UpdateState();
        }

        /// <summary>
        /// Captures the current expansion state of all headers.
        /// </summary>
        protected Dictionary<string, bool> CaptureExpansion()
        {
            var map = new Dictionary<string, bool>(StringComparer.Ordinal);

            for (int i = 0; i < _headers.Count; i++)
            {
                var h = _headers[i];
                if (h != null)
                    map[h.Id] = h.IsExpanded;
            }

            return map;
        }

        /// <summary>
        /// Restores header expansion states from a captured map.
        /// </summary>
        protected void RestoreExpansion(Dictionary<string, bool> map)
        {
            if (map == null)
                return;

            for (int i = 0; i < _headers.Count; i++)
            {
                var h = _headers[i];
                if (h != null && map.TryGetValue(h.Id, out var expanded))
                    h.IsExpanded = expanded;
            }
        }
    }
}
