using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using NetPad.ViewModels;
using ReactiveUI;
using Splat;

namespace NetPad
{
    public class ViewLocator : IDataTemplate, IViewLocator
    {
        public bool SupportsRecycling => false;

        public IControl Build(object data)
        {
            return (IControl) (ResolveViewFromDiContainer(data) ?? new TextBlock {Text = "Not Found: " + data.GetType().FullName});
        }

        public bool Match(object data)
        {
            return data is ViewModelBase;
        }

        public IViewFor? ResolveView<T>(T viewModel, string? contract = null)
        {
            return ResolveViewFromDiContainer(viewModel) as IViewFor;
        }

        private object? ResolveViewFromDiContainer(object viewModel)
        {
            var name = viewModel.GetType().FullName!.Replace("ViewModel", "View");
            var type = Type.GetType(name);

            return type != null ? Locator.Current.GetRequiredService(type) : null;
        }
    }
}