using MediatR;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.UiInterop;

namespace NetPad.CQs;

/// <summary>
/// Saves a script. Returns true if script was saved, false otherwise.
/// </summary>
public class SaveScriptCommand : Command<bool>
{
    public Script Script { get; }

    public SaveScriptCommand(Script script)
    {
        Script = script;
    }

    public class Handler : IRequestHandler<SaveScriptCommand, bool>
    {
        private readonly IUiDialogService _uiDialogService;
        private readonly IScriptRepository _scriptRepository;
        private readonly IAutoSaveScriptRepository _autoSaveScriptRepository;
        private readonly IEventBus _eventBus;

        public Handler(
            IUiDialogService uiDialogService,
            IScriptRepository scriptRepository,
            IAutoSaveScriptRepository autoSaveScriptRepository,
            IEventBus eventBus
        )
        {
            _uiDialogService = uiDialogService;
            _scriptRepository = scriptRepository;
            _autoSaveScriptRepository = autoSaveScriptRepository;
            _eventBus = eventBus;
        }

        public async Task<bool> Handle(SaveScriptCommand request, CancellationToken cancellationToken)
        {
            var script = request.Script;

            if (script.IsNew)
            {
                var path = await _uiDialogService.AskUserForSaveLocation(script);

                if (string.IsNullOrWhiteSpace(path))
                {
                    return false;
                }

                script.SetPath(path);
            }

            await _scriptRepository.SaveAsync(script);

            await _autoSaveScriptRepository.DeleteAsync(script);

            await _eventBus.PublishAsync(new ScriptSavedEvent(script));

            return true;
        }
    }
}
