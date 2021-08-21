using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using NetPad.ViewModels.Queries;

namespace NetPad.Views.Queries
{
    public class QueriesView : ReactiveUserControl<QueriesViewModel>
    {
        public QueriesView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}