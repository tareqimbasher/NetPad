using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using AvaloniaEdit;
using NetPad.ViewModels.Queries;

namespace NetPad.Views.Queries
{
    public class QueryView : ReactiveUserControl<QueryViewModel>
    {
        private readonly TextEditorManager _textEditorManager;
        
        public QueryView()
        {
            InitializeComponent();

            _textEditorManager = new TextEditorManager(this.FindControl<TextEditor>("Editor"));
            _textEditorManager.Setup();

            AddHandler(PointerWheelChangedEvent, (o, i) =>
            {
                var textEditor = _textEditorManager.TextEditor;
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