using NC_Setup_Assist.ViewModels;
using System.Windows.Controls;

namespace NC_Setup_Assist.Views
{
    public partial class AnalysisView : UserControl
    {
        public AnalysisView()
        {
            InitializeComponent();
            DataContextChanged += (s, e) =>
            {
                if (e.OldValue is AnalysisViewModel oldVm)
                {
                    oldVm.RequestScrollAndSelect -= OnRequestScrollAndSelect;
                }
                if (e.NewValue is AnalysisViewModel newVm)
                {
                    newVm.RequestScrollAndSelect += OnRequestScrollAndSelect;
                }
            };
        }

        private void OnRequestScrollAndSelect(int startIndex, int length)
        {
            if (startIndex >= 0 && startIndex + length <= NcCodeTextBox.Text.Length)
            {
                NcCodeTextBox.Focus();
                NcCodeTextBox.Select(startIndex, length);
                var rect = NcCodeTextBox.GetRectFromCharacterIndex(startIndex);
                if (!rect.IsEmpty)
                {
                    NcCodeTextBox.ScrollToVerticalOffset(NcCodeTextBox.VerticalOffset + rect.Top);
                }
            }
        }
    }
}