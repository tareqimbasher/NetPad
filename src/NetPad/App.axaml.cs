using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Queries;
using NetPad.Sessions;
using NetPad.TextEditing;
using NetPad.TextEditing.OmniSharp;
using NetPad.ViewModels;
using NetPad.Views;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace NetPad
{
    public class App : Application
    {
        private Startup _startup;
        
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            _startup = new Startup(ApplicationLifetime);
            _startup.Configure();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = Locator.Current.GetRequiredService<MainWindowViewModel>()
                };
            }
            
            base.OnFrameworkInitializationCompleted();
        }
    }
}