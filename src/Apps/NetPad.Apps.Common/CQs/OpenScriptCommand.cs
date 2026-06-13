using MediatR;
using NetPad.Application;
using NetPad.Apps.Services;
using NetPad.Apps.UiInterop;
using NetPad.Events;
using NetPad.Exceptions;
using NetPad.Scripts;
using NetPad.Scripts.Events;
using NetPad.Sessions;

namespace NetPad.Apps.CQs;

public class OpenScriptCommand : Command<ScriptEnvironment>
{
    public OpenScriptCommand(Script script)
    {
        Script = script;
    }

    public OpenScriptCommand(Guid id)
    {
        Id = id;
    }

    public OpenScriptCommand(string path)
    {
        Path = path;
    }

    public Script? Script { get; }
    public Guid? Id { get; }
    public string? Path { get; }

    public class Handler(
        IScriptRepository scriptRepository,
        ISession session,
        IEventBus eventBus,
        IUiDialogService uiDialogService,
        IRecentScriptsService recentScriptsService,
        IAppStatusMessagePublisher appStatusMessagePublisher)
        : IRequestHandler<OpenScriptCommand, ScriptEnvironment>
    {
        public async Task<ScriptEnvironment> Handle(OpenScriptCommand request, CancellationToken cancellationToken)
        {
            Script script;
            bool openedFromPath = false;

            if (request.Script != null)
            {
                script = request.Script;
            }
            else if (request.Id != null)
            {
                script = await scriptRepository.GetAsync(request.Id.Value)
                         ?? throw new ScriptNotFoundException(request.Id.Value);
            }
            else if (!string.IsNullOrWhiteSpace(request.Path))
            {
                script = await scriptRepository.GetAsync(request.Path);
                openedFromPath = true;
            }
            else
            {
                throw new ArgumentException("Not enough information to open a script.");
            }

            if (openedFromPath)
            {
                // Opened by path. If a script with this Id is already open, reconcile before
                // opening: rebind an orphan that has no path, prompt on a genuine path clash,
                // or (same path + Id) fall through to dedup onto the existing environment.
                var existing = session.Get(script.Id);
                if (existing != null)
                {
                    if (existing.Script.Path == null)
                    {
                        // Existing environment has no path: it was orphan-recovered from auto-save
                        // (its original file was missing at recovery), or — far less likely — an
                        // unsaved new script whose random GUID collided with the file's. Rebind it
                        // to the path the user just opened so subsequent saves land on the right file.
                        existing.Script.SetPath(request.Path!);
                        await session.ActivateAsync(existing.Script.Id);
                        recentScriptsService.Add(request.Path!);

                        // Keep the recovered in-memory content rather than the file's — it's the
                        // user's work and usually newer. But if the two differ, the substitution
                        // would be invisible (they see recovered content, and an explicit Save
                        // overwrites the file), so surface a non-blocking warning. Closing the tab
                        // without saving discards the recovered copy, so reopening then loads the
                        // file's contents.
                        if (existing.Script.GetFingerprint() != script.GetFingerprint())
                        {
                            _ = appStatusMessagePublisher.PublishAsync(
                                existing.Script.Id,
                                $"Showing recovered unsaved changes for '{request.Path}', which differ from the file on disk. " +
                                "Saving will overwrite the file; close the tab without saving to load the file's contents instead.",
                                AppStatusMessagePriority.High);
                        }

                        return existing;
                    }

                    if (!existing.Script.IsPathEquivalent(script.Path!))
                    {
                        var openAsDuplicate = await uiDialogService.AskUserToOpenAsDuplicate(
                            script.Path!,
                            existing.Script.Path);

                        if (!openAsDuplicate)
                        {
                            await session.ActivateAsync(existing.Script.Id);
                            return existing;
                        }

                        var clone = script.CloneWithNewId();
                        clone.SetPath(request.Path!);
                        // The on-disk file at request.Path still holds the old Id, the clone's
                        // Id is fresh. Mark dirty so the tab shows the modified indicator and
                        // the close-prompt fires. Otherwise, the duplicate-Id resolution
                        // silently evaporates if the user closes without saving, and reopening
                        // the file would collide again.
                        clone.IsDirty = true;
                        script = clone;
                    }
                    // else same path, same id: fall through to session.OpenAsync which
                    // activates the existing environment.
                }
            }

            var environment = await session.OpenAsync(script, true);

            await eventBus.PublishAsync(new ScriptOpenedEvent(environment.Script));

            if (environment.Script.Path != null)
            {
                recentScriptsService.Add(environment.Script.Path);
            }

            return environment;
        }
    }
}
