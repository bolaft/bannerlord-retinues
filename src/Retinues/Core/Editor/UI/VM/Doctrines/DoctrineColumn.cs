using System.Linq;
using System.Collections.Generic;
using Retinues.Core.Game.Features.Doctrines;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI.VM.Doctrines
{
    public sealed class DoctrineColumnVM : ViewModel
    {
        private string _name;

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

            Log.Debug($"Created doctrine column '{name}' with {Doctrines.Count} doctrines.");

            Refresh();
        }

        // =========================================================================
        // Static
        // =========================================================================

        public static MBBindingList<DoctrineColumnVM> CreateColumns()
        {
            var svc = Campaign.Current?.GetCampaignBehavior<DoctrineServiceBehavior>();
            var columns = new MBBindingList<DoctrineColumnVM>();
            
            List<string> columnNames = ["Unlocks", "Equipment", "Troops", "Retinues"];

            if (svc != null)
            {
                // group by column, then order rows
                var groups = svc.AllDoctrines()
                    .GroupBy(d => d.Column)
                    .OrderBy(g => g.Key);

                foreach (var g in groups)
                {
                    var colName = g.Key >= 0 && g.Key < columnNames.Count ? columnNames[g.Key] : $"Column {g.Key + 1}";
                    var vms = g.OrderBy(d => d.Row)
                            .Select(d => new DoctrineVM(d.Id))
                            .ToList();

                    columns.Add(new DoctrineColumnVM(colName, vms));
                }
            }

            return columns;
        }

        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        [DataSourceProperty]
        public MBBindingList<DoctrineVM> Doctrines { get; }

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
