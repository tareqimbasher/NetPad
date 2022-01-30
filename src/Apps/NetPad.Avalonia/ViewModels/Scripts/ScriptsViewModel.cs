using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DynamicData;
using DynamicData.Binding;
using DynamicData.PLinq;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Configuration;
using NetPad.Scripts;
using NetPad.Runtimes;
using NetPad.Sessions;
using ReactiveUI;

namespace NetPad.ViewModels.Scripts
{
    public class ScriptsViewModel : ViewModelBase
    {
        private readonly IScriptRepository _scriptRepository;
        public readonly ISession _session;
        private readonly Settings _settings;
        private readonly IClassicDesktopStyleApplicationLifetime _appLifetime;
        private ReadOnlyObservableCollection<ScriptViewModel> _scripts;
        private ScriptViewModel? _selectedScript;

        public ScriptsViewModel()
        {
        }

        public ScriptsViewModel(
            IScriptRepository scriptRepository,
            ISession session,
            Settings settings,
            IClassicDesktopStyleApplicationLifetime appLifetime,
            IServiceProvider serviceProvider) : this()
        {
            _scriptRepository = scriptRepository;
            _session = session;
            _settings = settings;
            _appLifetime = appLifetime;


            session.Environments
                .ToObservableChangeSet()
                .Transform(q => new ScriptViewModel(q, serviceProvider.GetRequiredService<IScriptRuntime>()))
                .AsObservableList()
                .Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _scripts)
                .Subscribe();
        }

        public ReadOnlyObservableCollection<ScriptViewModel> Scripts => _scripts;
        public ScriptViewModel? SelectedScript { get; set; }


        public async Task CreateNewScriptAsync()
        {
            var name = await _session.GetNewScriptNameAsync();
            await _scriptRepository.CreateAsync(name);
            Console.WriteLine("Scripts: " + _session.Environments.Count);
        }

        public async Task SaveScriptAsync()
        {
            if (SelectedScript == null)
            {
                return;
            }

            var script = SelectedScript.ScriptEnvironment.Script;

            if (!script.IsDirty)
                return;

            if (script.IsNew)
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Save Script",
                    InitialFileName = script.Name + Script.STANARD_EXTENSION,
                    Directory = _settings.ScriptsDirectoryPath,
                    DefaultExtension = Script.STANARD_EXTENSION_WO_DOT
                };

                var selectedPath = await dialog.ShowAsync(_appLifetime.MainWindow);
                if (selectedPath == null)
                    return;

                script.SetPath(selectedPath.Replace(_settings.ScriptsDirectoryPath, string.Empty));
            }

            try
            {
                await _scriptRepository.SaveAsync(script);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }

        public async Task RunScriptAsync()
        {
            if (SelectedScript == null)
            {
                return;
            }

            var vm = Scripts.FirstOrDefault(q => q.ScriptEnvironment == this.SelectedScript.ScriptEnvironment);
            if (vm != null)
            {
                await vm.RunScriptAsync();
            }
        }
    }
}
