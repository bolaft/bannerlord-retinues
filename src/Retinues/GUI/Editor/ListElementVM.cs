namespace Retinues.GUI.Editor
{
    /// <summary>
    /// ViewModel for a single filterable list element.
    /// </summary>
    public abstract class ListElementVM : ButtonVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Apply the given filter to update element visibility.
        /// </summary>
        public void ApplyFilter(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                IsVisible = true;
            else
                IsVisible = FilterMatch(filter);
        }

        /// <summary>
        /// Determine whether this element matches the provided filter.
        /// </summary>
        public abstract bool FilterMatch(string filter);
    }
}
