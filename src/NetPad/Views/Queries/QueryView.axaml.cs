using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using AvaloniaEdit;
using NetPad.UI.TextEditing;
using NetPad.ViewModels.Queries;
using OmniSharp;
using OmniSharp.Models.v1.Completion;

namespace NetPad.Views.Queries
{
    public class QueryView : ReactiveUserControl<QueryViewModel>
    {
        private readonly TextEditorConfigurator _textEditorConfigurator;

        public QueryView()
        {
        }
        
        public QueryView(IOmniSharpServer omniSharpServer)
        {
            InitializeComponent();
            
            _textEditorConfigurator = new TextEditorConfigurator(this.FindControl<TextEditor>("Editor"), omniSharpServer);
            _textEditorConfigurator.Setup();
            
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

        protected override void OnInitialized()
        {
            if (_textEditorConfigurator != null)
            {
                _textEditorConfigurator.TextEditor.Text = ViewModel!.Query.Code;
                _textEditorConfigurator.TextEditor.TextChanged += (sender,  args) => 
                    ViewModel!.Code = _textEditorConfigurator.TextEditor.Text;
                // _textEditorConfigurator.TextEditor.TextArea.TextEntered += (_, args) =>
                //     ViewModel!.Code = _textEditorConfigurator.TextEditor.Text ?? string.Empty;
            }
        }
    }
}