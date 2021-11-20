using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using NetPad.ViewModels.Scripts;
using ReactiveUI;

namespace NetPad.Views.Scripts
{
    public class ScriptsView : ReactiveUserControl<ScriptsViewModel>
    {
        public ScriptsView()
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
