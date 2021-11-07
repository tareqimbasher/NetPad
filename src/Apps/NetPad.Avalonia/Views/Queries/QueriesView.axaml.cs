using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using NetPad.ViewModels.Queries;
using ReactiveUI;

namespace NetPad.Views.Queries
{
    public class QueriesView : ReactiveUserControl<QueriesViewModel>
    {
        public QueriesView()
        {
            this.WhenActivated(disposables => { /* Handle interactions etc. */ });
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}