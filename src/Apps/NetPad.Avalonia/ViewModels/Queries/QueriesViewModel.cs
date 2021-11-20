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
using NetPad.Queries;
using NetPad.Runtimes;
using NetPad.Sessions;
using ReactiveUI;

namespace NetPad.ViewModels.Queries
{
    public class QueriesViewModel : ViewModelBase
    {
        private readonly IQueryRepository _queryRepository;
        public readonly ISession _session;
        private readonly Settings _settings;
        private readonly IClassicDesktopStyleApplicationLifetime _appLifetime;
        private ReadOnlyObservableCollection<QueryViewModel> _queries;
        private QueryViewModel? _selectedQuery;

        public QueriesViewModel()
        {
        }

        public QueriesViewModel(
            IQueryRepository queryRepository,
            ISession session,
            Settings settings,
            IClassicDesktopStyleApplicationLifetime appLifetime,
            IServiceProvider serviceProvider) : this()
        {
            _queryRepository = queryRepository;
            _session = session;
            _settings = settings;
            _appLifetime = appLifetime;


            session.OpenQueries
                .ToObservableChangeSet()
                .Transform(q => new QueryViewModel(q, serviceProvider.GetRequiredService<IQueryRuntime>()))
                .AsObservableList()
                .Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _queries)
                .Subscribe();

            _queryRepository.OpenAsync("/home/tips/X/tmp/NetPad/Queries/Query 1.netpad").Wait();
        }

        public ReadOnlyObservableCollection<QueryViewModel> Queries => _queries;
        public QueryViewModel? SelectedQuery { get; set; }


        public async Task CreateNewQueryAsync()
        {
            await _queryRepository.CreateAsync();
            Console.WriteLine("Queries: " + _session.OpenQueries.Count);
        }

        public async Task SaveQueryAsync()
        {
            if (SelectedQuery == null)
            {
                return;
            }

            if (!SelectedQuery.Query.IsDirty)
                return;

            if (SelectedQuery.Query.IsNew)
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Save Query",
                    InitialFileName = SelectedQuery.Query.Name + ".netpad",
                    Directory = _settings.QueriesDirectoryPath,
                    DefaultExtension = "netpad"
                };

                var selectedPath = await dialog.ShowAsync(_appLifetime.MainWindow);
                if (selectedPath == null)
                    return;

                SelectedQuery.Query.SetFilePath(selectedPath);
            }

            try
            {
                await _queryRepository.SaveAsync(SelectedQuery.Query);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }

        public async Task RunQueryAsync()
        {
            if (SelectedQuery == null)
            {
                return;
            }

            var vm = Queries.FirstOrDefault(q => q.Query == this.SelectedQuery.Query);
            if (vm != null)
            {
                await vm.RunQueryAsync();
            }
        }
    }
}
