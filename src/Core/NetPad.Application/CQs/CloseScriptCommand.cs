using MediatR;
using NetPad.Events;
using NetPad.Exceptions;
using NetPad.Scripts;
using NetPad.Sessions;
using NetPad.UiInterop;

namespace NetPad.CQs;

public class CloseScriptCommand : Command
{
    public CloseScriptCommand(Guid scriptId)
    {
        ScriptId = scriptId;
    }

    public Guid ScriptId { get; }

    public class Handler : IRequestHandler<CloseScriptCommand>
    {
        private readonly ISession _session;
        private readonly IAutoSaveScriptRepository _autoSaveScriptRepository;
        private readonly IUiDialogService _uiDialogService;
        private readonly IMediator _mediator;
        private readonly IEventBus _eventBus;

        public Handler(
            ISession session,
            IAutoSaveScriptRepository autoSaveScriptRepository,
            IUiDialogService uiDialogService,
            IMediator mediator,
            IEventBus eventBus)
        {
            _session = session;
            _autoSaveScriptRepository = autoSaveScriptRepository;
            _uiDialogService = uiDialogService;
            _mediator = mediator;
            _eventBus = eventBus;
        }

        public async Task<Unit> Handle(CloseScriptCommand request, CancellationToken cancellationToken)
        {
            var scriptId = request.ScriptId;

            var scriptEnvironment = _session.Get(scriptId) ?? throw new ScriptNotFoundException(scriptId);
            var script = scriptEnvironment.Script;

            bool shouldAskUserToSave = script.IsDirty;
            if (script.IsNew && string.IsNullOrEmpty(script.Code)) shouldAskUserToSave = false;

            if (shouldAskUserToSave)
            {
                var response = await _uiDialogService.AskUserIfTheyWantToSave(script);
                if (response == YesNoCancel.Cancel)
                {
                    return Unit.Value;
                }

                if (response == YesNoCancel.Yes)
                {
                    bool saved = await _mediator.Send(new SaveScriptCommand(script));
                    if (!saved)
                    {
                        return Unit.Value;
                    }
                }
            }

            await _session.CloseAsync(scriptId);

            await _autoSaveScriptRepository.DeleteAsync(script);

            await _eventBus.PublishAsync(new ScriptClosedEvent(script));

            return Unit.Value;
        }
    }
}
