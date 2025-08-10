using MediatR;
using NetPad.Apps.CQs;
using NetPad.Apps.UiInterop;
using NetPad.Exceptions;
using NetPad.Sessions;

namespace NetPad.Services;

/// <summary>
/// High-level service that handles some common script-related operations that involves prompting user for action.
/// </summary>
public class ScriptService(ISession session, IUiDialogService uiDialogService, IMediator mediator)
{
    public async Task CloseScriptAsync(Guid scriptId, bool discardUnsavedChanges)
    {
        var scriptEnvironment = session.Get(scriptId) ?? throw new ScriptNotFoundException(scriptId);
        var script = scriptEnvironment.Script;

        bool shouldAskUserToSave;
        if (discardUnsavedChanges)
        {
            shouldAskUserToSave = false;
        }
        else if (script.IsNew && string.IsNullOrEmpty(script.Code))
        {
            shouldAskUserToSave = false;
        }
        else
        {
            shouldAskUserToSave = script.IsDirty;
        }

        if (shouldAskUserToSave)
        {
            var response = await uiDialogService.AskUserIfTheyWantToSave(script);
            if (response == YesNoCancel.Cancel)
            {
                return;
            }

            if (response == YesNoCancel.Yes)
            {
                bool saved = await SaveScriptAsync(scriptId);
                if (!saved)
                {
                    return;
                }
            }
        }

        await mediator.Send(new CloseScriptCommand(scriptId));
    }

    public async Task<bool> SaveScriptAsync(Guid scriptId)
    {
        var scriptEnvironment = session.Get(scriptId) ?? throw new ScriptNotFoundException(scriptId);
        var script = scriptEnvironment.Script;

        if (script.IsNew)
        {
            var path = await uiDialogService.AskUserForSaveLocation(script);

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            script.SetPath(path);
        }

        await mediator.Send(new SaveScriptCommand(script));

        return true;
    }
}
