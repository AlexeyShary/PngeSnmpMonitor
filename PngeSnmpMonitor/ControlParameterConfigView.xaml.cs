using System.Windows;
using System.Windows.Controls;

namespace PngeSnmpMonitor
{
    public partial class ControlParameterConfigView
    {
        public ControlParameterConfigView()
        {
            InitializeComponent();
        }

        private void DataGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            var grid = (DataGrid)sender;
            grid.CommitEdit(DataGridEditingUnit.Row, true);
        }
    }
}