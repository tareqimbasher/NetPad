using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
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

            // _queries = session.OpenQueries
            //     .ToObservableChangeSet().ToCollection()
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Select(queries => queries.Select(query =>
            //     {
            //         // var qq = this.Queries.Any(q => q.Query == query);
            //         // if (qq != null)
            //         //     return 
            //         //     
            //         return new QueryViewModel(query);
            //     }))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .ToProperty(this, x => x.Queries, scheduler: RxApp.MainThreadScheduler);
            
            // _queries.ThrownExceptions.Subscribe(ex =>
            // {
            //     Trace.TraceError($"TIPS-TRACE ERROR: {ex}");
            // });

            // var sourceCache = new SourceCache<Query, string>(q => q.Name);
            // var disposable = sourceCache
            //     .Connect()
            //     .Transform(q => new QueryViewModel(q))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Bind(out _queries)
            //     .Subscribe();

            session.OpenQueries
                .ToObservableChangeSet(q => q.Name)
                .Transform(q => new QueryViewModel(q))
                .AsObservableCache()
                .Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _queries)
                .Subscribe();
                
            
            // Queries = session.OpenQueries
            //     .ToObservableChangeSet()
            //     .Select(q =>
            //     {
            //     })
            //     .AsObservableList();
            //
            // this.WhenAnyValue(x => x._session.OpenQueries.Count)
            //     .Subscribe(x =>
            //     {
            //         foreach (var query in session.OpenQueries)
            //         {
            //             if (Queries.Any(qq => qq.Query == query)) continue;
            //             Queries.Add(new QueryViewModel(query));   
            //         }
            //     });
            
            // session.OpenQueries.ToObservable()
            //     .Subscribe(Observer.Create<Query>(q => Queries.Add(new QueryViewModel(q))));
            
            _queryManager.OpenQueryAsync("/home/tips/X/tmp/NetPad/Queries/Query 1.netpad").Wait();
            _queryManager.OpenQueryAsync("/home/tips/X/tmp/NetPad/Queries/Query 2.netpad").Wait();
            // Queries = new ObservableCollection<QueryViewModel>(
            //     session.OpenQueries.Select(q => new QueryViewModel(q)));
        }

        // private readonly ObservableAsPropertyHelper<IEnumerable<QueryViewModel>> _queries;
        // public List<QueryViewModel> Queries => _queries.Value.ToList();

        public ReadOnlyObservableCollection<QueryViewModel> Queries => _queries;
        public QueryViewModel? SelectedQuery { get; set; }
        
        public async Task CreateNewQueryAsync()
        {
            await _queryManager.CreateNewQueryAsync();
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
                    InitialFileName = SelectedQuery.Query.Name,
                    Directory = (await _queryManager.GetQueriesDirectoryAsync()).FullName,
                    DefaultExtension = "netpad"
                };
            
                var selectedPath = await dialog.ShowAsync(_appLifetime.MainWindow);
                if (selectedPath == null)
                    return;

                SelectedQuery.Query.SetFilePath(selectedPath);;
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