using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Queries;
using NetPad.Sessions;
using NetPad.ViewModels;
using NetPad.Views;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace NetPad
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var services = new ServiceCollection();
            services.AddSingleton<Settings>();
            services.AddSingleton<ISession, Session>();
            services.AddSingleton<IQueryManager, QueryManager>();

            RegisterViewsAndViewModels(services);
            
            // services.AddSingleton<IViewLocator>(new ViewLocator());
            // Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetExecutingAssembly());
            // Locator.CurrentMutable.RegisterLazySingleton(() => new ViewLocator(), typeof(IViewLocator));
            
            services.AddSingleton(x => (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime);
            
            services.UseMicrosoftDependencyResolver();
            Locator.CurrentMutable.InitializeSplat();
            Locator.CurrentMutable.InitializeReactiveUI();
            

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = Locator.Current.GetRequiredService<MainWindowViewModel>()
                };
            }

            
            
            base.OnFrameworkInitializationCompleted();
        }

        private void RegisterViewsAndViewModels(IServiceCollection services)
        {
            var assemblies = new[] { Assembly.GetExecutingAssembly() };

            foreach (var assembly in assemblies)
            {
                var types = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
                    .ToArray();
                
                var views = types.Where(t => typeof(IViewFor).IsAssignableFrom(t));
                var viewModels = types.Where(t => typeof(ReactiveObject).IsAssignableFrom(t));

                foreach (var view in views)
                {
                    services.AddTransient(view);
                }

                foreach (var viewModel in viewModels)
                {
                    services.AddTransient(viewModel);
                }
            }
        }
    }
}