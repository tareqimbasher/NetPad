using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DynamicData;
using DynamicData.Binding;
using NetPad.Queries;
using NetPad.Sessions;
using ReactiveUI;

namespace NetPad.ViewModels.Queries
{
    public class QueriesViewModel : ViewModelBase
    {
        private readonly IQueryManager _queryManager;
        private readonly IClassicDesktopStyleApplicationLifetime _appLifetime;

        public QueriesViewModel()
        {
        }

        public QueriesViewModel(IQueryManager queryManager, ISession session, IClassicDesktopStyleApplicationLifetime appLifetime)
        {
            _queryManager = queryManager;
            _appLifetime = appLifetime;

            _queries = session.OpenQueries
                .ToObservableChangeSet().ToCollection()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(queries => queries.Select(query => new QueryViewModel(query)))
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.Queries, scheduler: RxApp.MainThreadScheduler);
            
            _queries.ThrownExceptions.Subscribe(ex =>
            {
                Trace.TraceError($"TIPS-TRACE ERROR: {ex}");
            });
        }

        private readonly ObservableAsPropertyHelper<IEnumerable<QueryViewModel>> _queries;
        public List<QueryViewModel> Queries => _queries.Value.ToList();
        
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
    }
}