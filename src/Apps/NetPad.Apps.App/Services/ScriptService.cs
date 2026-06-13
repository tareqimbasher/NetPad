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

    public async Task<bool> SaveScriptAsAsync(Guid scriptId)
    {
        var scriptEnvironment = session.Get(scriptId) ?? throw new ScriptNotFoundException(scriptId);
        var script = scriptEnvironment.Script;

        var path = await uiDialogService.AskUserForSaveLocation(script);
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        // A new script has no on-disk original to fork from; Save As is just Save.
        if (script.IsNew)
        {
            script.SetPath(path);
            await mediator.Send(new SaveScriptCommand(script));
            return true;
        }

        // If the user picked the script's current path, treat as a regular Save.
        if (script.IsPathEquivalent(path))
        {
            await mediator.Send(new SaveScriptCommand(script));
            return true;
        }

        // Real Save As: clone with a fresh Id, save the clone to the new path,
        // close the original tab (discarding its unsaved edits; they're preserved
        // in the clone), then open the clone as the new active tab.
        var clone = script.CloneWithNewId();
        clone.SetPath(path);

        await mediator.Send(new SaveScriptCommand(clone));
        await CloseScriptAsync(scriptId, discardUnsavedChanges: true);
        await mediator.Send(new OpenScriptCommand(clone));

        return true;
    }
}
