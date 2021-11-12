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
using NetPad.Queries;
using NetPad.Runtimes;
using NetPad.Sessions;
using ReactiveUI;

namespace NetPad.ViewModels.Queries
{
    public class QueriesViewModel : ViewModelBase
    {
        private readonly IQueryManager _queryManager;
        public readonly ISession _session;
        private readonly IClassicDesktopStyleApplicationLifetime _appLifetime;
        private ReadOnlyObservableCollection<QueryViewModel> _queries;
        private QueryViewModel? _selectedQuery;

        public QueriesViewModel()
        {
        }

        public QueriesViewModel(
            IQueryManager queryManager,
            ISession session,
            IClassicDesktopStyleApplicationLifetime appLifetime) : this()
        {
            _queryManager = queryManager;
            _session = session;
            _appLifetime = appLifetime;


            session.OpenQueries
                .ToObservableChangeSet()
                .Transform(q => new QueryViewModel(q))
                .AsObservableList()
                .Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _queries)
                .Subscribe();

            _queryManager.OpenQueryAsync("/home/tips/X/tmp/NetPad/Queries/Query 1.netpad").Wait();
        }

        public ReadOnlyObservableCollection<QueryViewModel> Queries => _queries;
        public QueryViewModel? SelectedQuery { get; set; }


        public async Task CreateNewQueryAsync()
        {
            await _queryManager.CreateNewQueryAsync();
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
                    Directory = (await _queryManager.GetQueriesDirectoryAsync()).FullName,
                    DefaultExtension = "netpad"
                };

                var selectedPath = await dialog.ShowAsync(_appLifetime.MainWindow);
                if (selectedPath == null)
                    return;

                SelectedQuery.Query.SetFilePath(selectedPath);
            }

            try
            {
                await SelectedQuery.Query.SaveAsync();
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
