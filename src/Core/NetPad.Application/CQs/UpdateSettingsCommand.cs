using MediatR;
using NetPad.Configuration;
using NetPad.Events;

namespace NetPad.CQs;

public class UpdateSettingsCommand : Command
{
    public UpdateSettingsCommand(Settings settings)
    {
        Settings = settings;
    }

    public Settings Settings { get; }

    public class Handler : IRequestHandler<UpdateSettingsCommand>
    {
        private readonly Settings _settings;
        private readonly ISettingsRepository _settingsRepository;
        private readonly IEventBus _eventBus;

        public Handler(Settings settings, ISettingsRepository settingsRepository, IEventBus eventBus)
        {
            _settings = settings;
            _settingsRepository = settingsRepository;
            _eventBus = eventBus;
        }

        public async Task<Unit> Handle(UpdateSettingsCommand request, CancellationToken cancellationToken)
        {
            var incoming = request.Settings;

            _settings
                .SetTheme(incoming.Theme)
                .SetScriptsDirectoryPath(incoming.ScriptsDirectoryPath)
                .SetPackageCacheDirectoryPath(incoming.PackageCacheDirectoryPath)
                .SetEditorBackgroundColor(incoming.EditorBackgroundColor)
                .SetEditorOptions(incoming.EditorOptions)
                .SetResultsOptions(incoming.ResultsOptions)
                ;

            await _settingsRepository.SaveSettingsAsync(_settings);

            await _eventBus.PublishAsync(new SettingsUpdated(_settings));

            return Unit.Value;;
        }
    }
}
