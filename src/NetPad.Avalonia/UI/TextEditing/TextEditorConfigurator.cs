using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Indentation.CSharp;
using OmniSharp;

namespace NetPad.UI.TextEditing
{
    public class TextEditorConfigurator
    {
        private readonly IOmniSharpServer _omniSharpServer;
        private CompletionWindow? _completionWindow;
        private OverloadInsightWindow? _insightWindow;

        public TextEditorConfigurator(TextEditor textEditor, IOmniSharpServer omniSharpServer)
        {
            _omniSharpServer = omniSharpServer;
            TextEditor = textEditor;
        }

        public TextEditor TextEditor { get; }
        
        public void Setup()
        {
            // TextEditor.Background = Brushes.Transparent;
            TextEditor.Foreground = Brushes.Black;
            TextEditor.ShowLineNumbers = true;
            TextEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
            TextEditor.TextArea.IndentationStrategy = new CSharpIndentationStrategy();
            TextEditor.TextArea.TextEntered += TextEditor_TextArea_TextEntered;
            TextEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
            
            // Task.Run(async () =>
            // {
            //     try
            //     {
            //         await _omniSharpServer.StartAsync();
            //     }
            //     catch (Exception e)
            //     {
            //         Console.WriteLine(e);
            //     }
            //     
            // });
        }

        void textEditor_TextArea_TextEntering(object? sender, TextInputEventArgs e)
        {
            if (e.Text?.Length > 0 && _completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    _completionWindow.CompletionList.RequestInsertion(e);
                }
            }

            _insightWindow?.Hide();

            // Do not set e.Handled=true.
            // We still want to insert the character that was typed.
        }

        private async void TextEditor_TextArea_TextEntered(object? sender, TextInputEventArgs e)
        {
            if (e.Text == ".")
            {
                _completionWindow = new CompletionWindow(TextEditor.TextArea);
                _completionWindow.Closed += (_, _) => _completionWindow = null;
                var data = _completionWindow.CompletionList.CompletionData;

                // try
                // {
                //     var completionResponse = await _omniSharpServer.Send<CompletionResponse>(new CompletionRequest()
                //     {
                //         Line = 9,
                //         Column = 22,
                //         FileName = "/home/tips/Source/tmp/test-project/Program.cs"
                //     });
                //
                //     foreach (var item in completionResponse.Items)
                //     {
                //         data.Add(new MyCompletionData(item.Label)
                //         {
                //             Description = item.Documentation ?? "Documentation for: " + item.Label,
                //             Priority = 0
                //         });
                //     }
                // }
                // catch (Exception ex)
                // {
                //     Console.WriteLine(ex);
                // }

                _completionWindow.Show();
            }
            else if (e.Text == "(")
            {
                _insightWindow = new OverloadInsightWindow(TextEditor.TextArea);
                _insightWindow.Closed += (_, _) => _insightWindow = null;

                _insightWindow.Provider = new MyOverloadProvider(new[]
                {
                    ("Method1(int, string)", "Method1 description"),
                    ("Method2(int)", "Method2 description"),
                    ("Method3(string)", "Method3 description"),
                });

                _insightWindow.Show();
            }
        }


        private class MyOverloadProvider : IOverloadProvider
        {
            private readonly IList<(string header, string content)> _items;
            private int _selectedIndex;

            public MyOverloadProvider(IList<(string header, string content)> items)
            {
                _items = items;
                SelectedIndex = 0;
            }

            public int SelectedIndex
            {
                get => _selectedIndex;
                set
                {
                    _selectedIndex = value;
                    OnPropertyChanged();
                    // ReSharper disable ExplicitCallerInfoArgument
                    OnPropertyChanged(nameof(CurrentHeader));
                    OnPropertyChanged(nameof(CurrentContent));
                    // ReSharper restore ExplicitCallerInfoArgument
                }
            }

            public int Count => _items.Count;
            public string? CurrentIndexText => null;
            public object CurrentHeader => _items[SelectedIndex].header;
            public object CurrentContent => _items[SelectedIndex].content;

            public event PropertyChangedEventHandler? PropertyChanged;

            private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public class MyCompletionData : ICompletionData
        {
            public MyCompletionData(string text)
            {
                Text = text;
            }

            public IBitmap? Image => null;

            public string Text { get; }

            // Use this property if you want to show a fancy UIElement in the list.
            public object Content => Text;

            public object Description { get; set; }

            public double Priority { get; set; }

            public void Complete(TextArea textArea, ISegment completionSegment,
                EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment, Text);
            }
        }
    }
}