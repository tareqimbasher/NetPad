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
                .SetAutoCheckUpdates(incoming.AutoCheckUpdates ?? true)
                .SetDotNetSdkDirectoryPath(incoming.DotNetSdkDirectoryPath)
                .SetScriptsDirectoryPath(incoming.ScriptsDirectoryPath)
                .SetPackageCacheDirectoryPath(incoming.PackageCacheDirectoryPath)
                .SetAppearanceOptions(incoming.Appearance)
                .SetEditorOptions(incoming.Editor)
                .SetResultsOptions(incoming.Results)
                .SetStyleOptions(incoming.Styles)
                .SetKeyboardShortcutOptions(incoming.KeyboardShortcuts)
                .SetOmniSharpOptions(incoming.OmniSharp);

            await _settingsRepository.SaveSettingsAsync(_settings);

            await _eventBus.PublishAsync(new SettingsUpdatedEvent(_settings));

            return Unit.Value;
        }
    }
}
