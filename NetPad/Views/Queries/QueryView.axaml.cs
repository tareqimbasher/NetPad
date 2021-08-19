using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using AvaloniaEdit;
using DynamicData.Binding;
using NetPad.TextEditing;
using NetPad.TextEditing.OmniSharp;
using NetPad.UI.TextEditing;
using NetPad.ViewModels.Queries;

namespace NetPad.Views.Queries
{
    public class QueryView : ReactiveUserControl<QueryViewModel>
    {
        private readonly ITextEditingEngine _textEditingEngine;
        private readonly TextEditorConfigurator _textEditorConfigurator;

        public QueryView()
        {
        }
        
        public QueryView(ITextEditingEngine textEditingEngine)
        {
            _textEditingEngine = textEditingEngine;
            InitializeComponent();
            
            _textEditorConfigurator = new TextEditorConfigurator(this.FindControl<TextEditor>("Editor"));
            _textEditorConfigurator.Setup();
            
            // this.WhenChanged(x => )

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
            _textEditorConfigurator.TextEditor.Text = ViewModel!.Query.Code;
            _textEditorConfigurator.TextEditor.TextChanged += (sender,  args) => 
                ViewModel!.Code = _textEditorConfigurator.TextEditor.Text;
            // _textEditorConfigurator.TextEditor.TextArea.TextEntered += (_, args) =>
            //     ViewModel!.Code = _textEditorConfigurator.TextEditor.Text ?? string.Empty;

            var vm = ViewModel!;

            Task.Run(async () =>
            {
                var query = vm.Query;

                try
                {
                    await _textEditingEngine.LoadAsync(query);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                
            });
            
            
            Task.Run(async () =>
            {
                await Task.Delay(5000);
                await _textEditingEngine.Autocomplete();
            });
        }
    }
}