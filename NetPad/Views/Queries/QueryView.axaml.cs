using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using AvaloniaEdit;
using NetPad.UI.TextEditing;
using NetPad.ViewModels.Queries;

namespace NetPad.Views.Queries
{
    public class QueryView : ReactiveUserControl<QueryViewModel>
    {
        private readonly TextEditorConfigurator _textEditorConfigurator;
        
        public QueryView()
        {
            InitializeComponent();
            
            _textEditorConfigurator = new TextEditorConfigurator(this.FindControl<TextEditor>("Editor"));
            _textEditorConfigurator.Setup();
            _textEditorConfigurator.TextEditor.TextChanged += (sender,  args) => ViewModel!.Query.Code = _textEditorConfigurator.TextEditor.Text;

            AddHandler(PointerWheelChangedEvent, (o, i) =>
            {
                var textEditor = _textEditorConfigurator.TextEditor;
                if (i.KeyModifiers != KeyModifiers.Control) return;
                if (i.Delta.Y > 0) textEditor.FontSize++;
                else textEditor.FontSize = textEditor.FontSize > 1 ? textEditor.FontSize - 1 : 1;
            }, RoutingStrategies.Bubble, true);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}