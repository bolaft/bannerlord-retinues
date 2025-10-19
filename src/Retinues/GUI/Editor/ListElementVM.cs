namespace Retinues.GUI.Editor
{
    public abstract class ListElementVM : ButtonVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void ApplyFilter(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                IsVisible = true;
            else
                IsVisible = FilterMatch(filter);
        }

        public abstract bool FilterMatch(string filter);
    }
}
