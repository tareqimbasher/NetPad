using MediatR;
using NetPad.Configuration;
using NetPad.Configuration.Events;
using NetPad.Events;

namespace NetPad.Apps.CQs;

public class UpdateSettingsCommand(Settings settings) : Command
{
    public Settings Settings { get; } = settings;

    public class Handler(Settings settings, ISettingsRepository settingsRepository, IEventBus eventBus)
        : IRequestHandler<UpdateSettingsCommand>
    {
        public async Task<Unit> Handle(UpdateSettingsCommand request, CancellationToken cancellationToken)
        {
            var incoming = request.Settings;

            settings
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

            await settingsRepository.SaveSettingsAsync(settings);

            await eventBus.PublishAsync(new SettingsUpdatedEvent(settings));

            return Unit.Value;
        }
    }
}
