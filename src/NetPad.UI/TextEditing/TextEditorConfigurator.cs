using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Indentation.CSharp;

namespace NetPad.UI.TextEditing
{
    public class TextEditorConfigurator
    {
        private CompletionWindow? _completionWindow;
        private OverloadInsightWindow? _insightWindow;

        public TextEditorConfigurator(TextEditor textEditor)
        {
            TextEditor = textEditor;
        }

        public TextEditor TextEditor { get; }
        
        public void Setup()
        {
            // TextEditor.Background = Brushes.Transparent;
            TextEditor.ShowLineNumbers = true;
            TextEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
            TextEditor.TextArea.IndentationStrategy = new CSharpIndentationStrategy();
            TextEditor.TextArea.TextEntered += TextEditor_TextArea_TextEntered;
            TextEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
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

        private void TextEditor_TextArea_TextEntered(object? sender, TextInputEventArgs e)
        {
            if (e.Text == ".")
            {
                _completionWindow = new CompletionWindow(TextEditor.TextArea);
                _completionWindow.Closed += (_, _) => _completionWindow = null;

                var data = _completionWindow.CompletionList.CompletionData;
                data.Add(new MyCompletionData("Item1"));
                data.Add(new MyCompletionData("Item2"));
                data.Add(new MyCompletionData("Item3"));
                data.Add(new MyCompletionData("Item4"));
                data.Add(new MyCompletionData("Item5"));
                data.Add(new MyCompletionData("Item6"));
                data.Add(new MyCompletionData("Item7"));
                data.Add(new MyCompletionData("Item8"));
                data.Add(new MyCompletionData("Item9"));
                data.Add(new MyCompletionData("Item10"));
                data.Add(new MyCompletionData("Item11"));
                data.Add(new MyCompletionData("Item12"));
                data.Add(new MyCompletionData("Item13"));


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

            public object Description => "Description for " + Text;

            public double Priority { get; } = 0;

            public void Complete(TextArea textArea, ISegment completionSegment,
                EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment, Text);
            }
        }
    }
}