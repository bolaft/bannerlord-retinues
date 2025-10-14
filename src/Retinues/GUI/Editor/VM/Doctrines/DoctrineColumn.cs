using System.Collections.Generic;
using System.Linq;
using Retinues.Doctrines;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Doctrines
{
    /// <summary>
    /// ViewModel for a doctrine column. Handles grouping, display, and refreshing doctrine VMs.
    /// </summary>
    [SafeClass]
    public sealed class DoctrineColumnVM : BaseComponent
    {
        public DoctrineColumnVM(string name, IEnumerable<DoctrineVM> doctrines)
        {
            _name = name;
            Doctrines = [];
            if (doctrines != null)
            {
                foreach (var d in doctrines)
                {
                    d.Column = this;
                    Doctrines.Add(d);
                }
            }

            Refresh();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Static                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Creates all doctrine columns from the service, grouping by column and ordering by row.
        /// </summary>
        [SafeMethod]
        public static MBBindingList<DoctrineColumnVM> CreateColumns()
        {
            var svc = Campaign.Current?.GetCampaignBehavior<DoctrineServiceBehavior>();
            var columns = new MBBindingList<DoctrineColumnVM>();

            List<string> columnNames =
            [
                L.S("doctrines_col_0", "Unlocks"),
                L.S("doctrines_col_1", "Equipment"),
                L.S("doctrines_col_2", "Troops"),
                L.S("doctrines_col_3", "Retinues"),
            ];

            if (svc != null)
            {
                // group by column, then order rows
                var groups = svc.AllDoctrines().GroupBy(d => d.Column).OrderBy(g => g.Key);

                foreach (var g in groups)
                {
                    var colName =
                        g.Key >= 0 && g.Key < columnNames.Count ? columnNames[g.Key] : string.Empty;
                    var vms = g.OrderBy(d => d.Row).Select(d => new DoctrineVM(d.Key)).ToList();

                    columns.Add(new DoctrineColumnVM(colName, vms));
                }
            }

            return columns;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private string _name;

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                    return;
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        [DataSourceProperty]
        public MBBindingList<DoctrineVM> Doctrines { get; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Refreshes all doctrine VMs in the column and updates bindings.
        /// </summary>
        public void Refresh()
        {
            // If you update items, call each item.Refresh() first.
            foreach (var d in Doctrines)
                d.Refresh();

            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Doctrines));
        }
    }
}
