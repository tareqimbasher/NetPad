using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using AvaloniaEdit;
using NetPad.OmniSharpWrapper;
using NetPad.UI.TextEditing;
using NetPad.ViewModels.Queries;
using OmniSharp.Models.v1.Completion;

namespace NetPad.Views.Queries
{
    public class QueryView : ReactiveUserControl<QueryViewModel>
    {
        private readonly IOmniSharpServer _omniSharpServer;
        private readonly TextEditorConfigurator _textEditorConfigurator;

        public QueryView()
        {
        }
        
        public QueryView(IOmniSharpServer omniSharpServer)
        {
            _omniSharpServer = omniSharpServer;
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
                    await _omniSharpServer.StartAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                
            });
            
            
            Task.Run(async () =>
            {
                await Task.Delay(7000);
                try
                {
                    var completionResponse = await _omniSharpServer.Send<CompletionRequest, CompletionResponse>(new CompletionRequest()
                    {
                        Line = 9,
                        Column = 22,
                        FileName = "/home/tips/Source/tmp/test-project/Program.cs"
                    });

                    var str = completionResponse.Items.Select(i => i.Label).ToArray();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        }
    }
}