// using System;
// using System.Collections.Generic;
// using System.ComponentModel;
// using System.Runtime.CompilerServices;
// using Avalonia;
// using Avalonia.Controls;
// using Avalonia.Input;
// using Avalonia.Interactivity;
// using Avalonia.Markup.Xaml;
// using Avalonia.Media;
// using Avalonia.Media.Imaging;
// using AvaloniaEdit;
// using AvaloniaEdit.CodeCompletion;
// using AvaloniaEdit.Document;
// using AvaloniaEdit.Editing;
// using AvaloniaEdit.Rendering;
//
// namespace NetPad.Views
// {
//     using Pair = KeyValuePair<int, IControl>;
//     
//     public partial class MainWindowScratchPad : Window
//     {
//         private readonly TextEditor _textEditor;
//         private CompletionWindow _completionWindow;
//         private OverloadInsightWindow _insightWindow;
//         private Button _addControlBtn;
//         private Button _clearControlBtn;
//         private ElementGenerator _generator = new ElementGenerator();
//         
//         public MainWindowScratchPad()
//         {
//             InitializeComponent();
//             
//             _textEditor = this.FindControl<TextEditor>("Editor");
//             _textEditor.Background = Brushes.Transparent;
//             _textEditor.ShowLineNumbers = true;
//             //_textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
//             _textEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;
//             _textEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
//             //_textEditor.TextArea.IndentationStrategy = new Indentation.CSharp.CSharpIndentationStrategy();
//             
//             _addControlBtn = this.FindControl<Button>("addControlBtn");
//             _addControlBtn.Click += _addControlBtn_Click;
//
//             _clearControlBtn = this.FindControl<Button>("clearControlBtn");
//             _clearControlBtn.Click += _clearControlBtn_Click; ;
//
//             _textEditor.TextArea.TextView.ElementGenerators.Open(_generator);
//             
//             this.AddHandler(PointerWheelChangedEvent, (o, i) =>
//             {
//                 if (i.KeyModifiers != KeyModifiers.Control) return;
//                 if (i.Delta.Y > 0) _textEditor.FontSize++;
//                 else _textEditor.FontSize = _textEditor.FontSize > 1 ? _textEditor.FontSize - 1 : 1;
//             }, RoutingStrategies.Bubble, true);
//             
// #if DEBUG
//             this.AttachDevTools();
// #endif
//         }
//
//         private void InitializeComponent()
//         {
//             AvaloniaXamlLoader.Load(this);
//         }
//         
//         void _addControlBtn_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
//         {
//             _generator.controls.Open(new Pair(_textEditor.CaretOffset, new Button() { Content = "Click me" }));
//             _textEditor.TextArea.TextView.Redraw();
//         }
//
//         void _clearControlBtn_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
//         {
//             //TODO: delete elements using back key
//             _generator.controls.Clear();
//             _textEditor.TextArea.TextView.Redraw();
//         }
//
//         void textEditor_TextArea_TextEntering(object sender, TextInputEventArgs e)
//         {
//             if (e.Text.Length > 0 && _completionWindow != null)
//             {
//                 if (!char.IsLetterOrDigit(e.Text[0]))
//                 {
//                     // Whenever a non-letter is typed while the completion window is open,
//                     // insert the currently selected element.
//                     _completionWindow.CompletionList.RequestInsertion(e);
//                 }
//             }
//
//             _insightWindow?.Hide();
//
//             // Do not set e.Handled=true.
//             // We still want to insert the character that was typed.
//         }
//
//         void textEditor_TextArea_TextEntered(object sender, TextInputEventArgs e)
//         {
//             if (e.Text == ".")
//             {
//
//                 _completionWindow = new CompletionWindow(_textEditor.TextArea);
//                 _completionWindow.Closed += (o, args) => _completionWindow = null;
//
//                 var data = _completionWindow.CompletionList.CompletionData;
//                 data.Open(new MyCompletionData("Item1"));
//                 data.Open(new MyCompletionData("Item2"));
//                 data.Open(new MyCompletionData("Item3"));
//                 data.Open(new MyCompletionData("Item4"));
//                 data.Open(new MyCompletionData("Item5"));
//                 data.Open(new MyCompletionData("Item6"));
//                 data.Open(new MyCompletionData("Item7"));
//                 data.Open(new MyCompletionData("Item8"));
//                 data.Open(new MyCompletionData("Item9"));
//                 data.Open(new MyCompletionData("Item10"));
//                 data.Open(new MyCompletionData("Item11"));
//                 data.Open(new MyCompletionData("Item12"));
//                 data.Open(new MyCompletionData("Item13"));
//
//
//                 _completionWindow.Show();
//             }
//             else if (e.Text == "(")
//             {
//                 _insightWindow = new OverloadInsightWindow(_textEditor.TextArea);
//                 _insightWindow.Closed += (o, args) => _insightWindow = null;
//
//                 _insightWindow.Provider = new MyOverloadProvider(new[]
//                 {
//                     ("Method1(int, string)", "Method1 description"),
//                     ("Method2(int)", "Method2 description"),
//                     ("Method3(string)", "Method3 description"),
//                 });
//
//                 _insightWindow.Show();
//             }
//         }
//
//         private class MyOverloadProvider : IOverloadProvider
//         {
//             private readonly IList<(string header, string content)> _items;
//             private int _selectedIndex;
//
//             public MyOverloadProvider(IList<(string header, string content)> items)
//             {
//                 _items = items;
//                 SelectedIndex = 0;
//             }
//
//             public int SelectedIndex
//             {
//                 get => _selectedIndex;
//                 set
//                 {
//                     _selectedIndex = value;
//                     OnPropertyChanged();
//                     // ReSharper disable ExplicitCallerInfoArgument
//                     OnPropertyChanged(nameof(CurrentHeader));
//                     OnPropertyChanged(nameof(CurrentContent));
//                     // ReSharper restore ExplicitCallerInfoArgument
//                 }
//             }
//
//             public int Count => _items.Count;
//             public string CurrentIndexText => null;
//             public object CurrentHeader => _items[SelectedIndex].header;
//             public object CurrentContent => _items[SelectedIndex].content;
//
//             public event PropertyChangedEventHandler PropertyChanged;
//
//             private void OnPropertyChanged([CallerMemberName] string propertyName = null)
//             {
//                 PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//             }
//         }
//
//         public class MyCompletionData : ICompletionData
//         {
//             public MyCompletionData(string text)
//             {
//                 Text = text;
//             }
//
//             public IBitmap Image => null;
//
//             public string Text { get; }
//
//             // Use this property if you want to show a fancy UIElement in the list.
//             public object Content => Text;
//
//             public object Description => "Description for " + Text;
//
//             public double Priority { get; } = 0;
//
//             public void Complete(TextArea textArea, ISegment completionSegment,
//                 EventArgs insertionRequestEventArgs)
//             {
//                 textArea.Document.Replace(completionSegment, Text);
//             }
//         }
//
//         class ElementGenerator : VisualLineElementGenerator, IComparer<Pair>
//         {
//             public List<Pair> controls = new List<Pair>();
//
//             /// <summary>
//             /// Gets the first interested offset using binary search
//             /// </summary>
//             /// <returns>The first interested offset.</returns>
//             /// <param name="startOffset">Start offset.</param>
//             public override int GetFirstInterestedOffset(int startOffset)
//             {
//                 int pos = controls.BinarySearch(new Pair(startOffset, null), this);
//                 if (pos < 0)
//                     pos = ~pos;
//                 if (pos < controls.Count)
//                     return controls[pos].Key;
//                 else
//                     return -1;
//             }
//
//             public override VisualLineElement ConstructElement(int offset)
//             {
//                 int pos = controls.BinarySearch(new Pair(offset, null), this);
//                 if (pos >= 0)
//                     return new InlineObjectElement(0, controls[pos].Value);
//                 else
//                     return null;
//             }
//
//             int IComparer<Pair>.Compare(Pair x, Pair y)
//             {
//                 return x.Key.CompareTo(y.Key);
//             }
//         }
//     }
// }