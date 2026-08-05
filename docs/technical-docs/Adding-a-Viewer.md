# Adding a New Viewer Type

This page explains how to add a new view type (e.g. a plain text file viewer, a REPL/notebook viewer, a settings editor) to the main window's work area — the tabbed editor region that currently hosts script tabs.

## Concepts

The work area is built from four layers:

| Layer | Responsibility | File |
|---|---|---|
| `ViewableObject` | The *model* of what's open in a tab — holds state, exposes capability methods, owns event subscriptions | `work-area/viewers/viewable-object.ts` |
| `Viewer` | The *Aurelia component* that renders a viewable — has its own HTML template + TypeScript view-model | `work-area/viewers/viewer.ts` |
| `ViewerHost` | Owns a collection of viewables and one viewer instance per viewer class, shown in a single split pane | `work-area/viewers/viewer-host.ts` |
| `IViewerRegistry` | Maps a viewable to the viewer class that handles it (predicate-based) | `work-area/viewers/viewer-registry.ts` |

The flow when a user opens a tab:

1. Something (e.g. `WorkAreaService.createScriptViewable`) constructs a `ViewableObject` subclass instance.
2. The viewable is passed to `WorkAreaService.open(viewable)`, which delegates to `ViewerHost.addViewables()` / `activate()`.
3. `ViewerHost.getViewer(viewable)` asks `IViewerRegistry.resolve(viewable)` for the viewer class, instantiates it via DI if not already cached, and stores one instance per class per host.
4. The tab bar displays the viewable using its observable display properties (`kindBadge`, `subtitle`, `statusIndicator`, etc.).
5. The viewer is mounted via `<au-compose component.bind="host.activeViewer">` in `work-area.html`.

## Step 1 — Subclass `ViewableObject`

A viewable is a long-lived model for a single tab. Subclass either `ViewableObject` directly (for non-text content) or `ViewableTextDocument` (for anything backed by a Monaco text model).

```typescript
// src/windows/main/work-area/viewers/text-file-viewer/viewable-text-file.ts

import {ViewableTextDocument} from "../viewable-text-document";
import {ViewerHost} from "../viewer-host";

export class ViewableTextFile extends ViewableTextDocument {
    constructor(
        private readonly filePath: string,
        initialText: string,
    ) {
        super(
            filePath,              // id — use something stable and unique
            filePath.split(/[\\/]/).pop() ?? filePath,  // name
            "plaintext",           // TextLanguage
            initialText,
        );

        this.path = filePath;
        this.tooltip = filePath;
        this.kindBadge = "TXT";      // short mono label for the kind of thing this is
    }

    // Universal navigation — every viewable must implement these.
    public override open(host: ViewerHost): Promise<void> {
        host.addViewables(this);
        return Promise.resolve();
    }

    public override async close(host: ViewerHost): Promise<void> {
        host.removeViewables(this);
    }

    public override activate(host: ViewerHost): Promise<void> {
        host.activate(this);
        return Promise.resolve();
    }

    // Override only the capabilities this viewable supports. Defaults are "not supported".
    public override canSave(): boolean { return true; }
    public override async save(): Promise<boolean> {
        // write the text back to disk via whatever service you use
        return true;
    }

    public override canOpenContainingFolder(): boolean { return !!this.path; }
    public override async openContainingFolder(): Promise<void> {
        // delegate to IAppService.openFolderContainingScript or similar
    }
}
```

### Display properties (optional but usually wanted)

`ViewableObject` exposes the following fields consumed by the generic tab bar:

| Field | Purpose |
|---|---|
| `kindBadge` | Short uppercase label shown before the tab name, naming what kind of document this is (`C#`, `SQL`) |
| `subtitle` | Short text shown after the tab name (e.g. connection name) |
| `subtitleIcon` | Glyph rendered before the subtitle — also an `IconName` |
| `hasProductionWarning` | Applies the production-warning highlight to the subtitle, the amber edge down the left of the editor, and the `PROD` pill beside the environment strip's Connection value |
| `tooltip` | Full hover tooltip |
| `statusIndicator` | `"running"` \| `"stopping"` \| `"success"` \| `"error"` — drives status icons |
| `path` | File path, used by `canOpenContainingFolder` logic |

`subtitleIcon` is typed `IconName`, so only a glyph that exists in the registry compiles. Tabs
render it through `<np-icon>`; pick an existing key or add one — see
[Adding an Icon](/technical-docs/Adding-an-Icon.md).

Kind is text rather than a glyph: two or three mono capitals stay readable at tab size, where a
drawing that has to distinguish one language from another does not. Keep the badge to about four
characters.

These must be **assigned to**, not computed in getters — Aurelia's binding observer system does not track getter call graphs. For values that change over time (e.g. execution status), subscribe to the relevant event in the viewable's constructor and assign to the field inside the handler. See `ViewableScriptDocument` for a reference.

## Step 2 — Subclass `Viewer`

A viewer is an Aurelia component with a TypeScript view-model, an HTML template, and (optionally) a scoped SCSS file. One instance exists per viewer class per `ViewerHost`.

```typescript
// src/windows/main/work-area/viewers/text-file-viewer/text-file-viewer.ts

import {ILogger} from "aurelia";
import {Viewer} from "../viewer";
import {ViewableObject} from "../viewable-object";
import {ITextEditor} from "@application/editor/text-editor";
import {ViewableTextFile} from "./viewable-text-file";

export class TextFileViewer extends Viewer {
    public editor: ITextEditor;

    constructor(@ILogger logger: ILogger) {
        super(logger);
    }

    public attached() {
        if (this.viewable && (!this.editor.active || this.editor.active.id !== this.viewable.id)) {
            this.open(this.viewable as ViewableTextFile);
        }
    }

    public override canOpen(viewable: ViewableObject): boolean {
        return viewable instanceof ViewableTextFile;
    }

    public override open(viewable: ViewableTextFile): void {
        this.viewable = viewable;
        this.editor?.open(viewable.textDocument);
    }

    public override close(viewable: ViewableTextFile): void {
        this.editor?.close(viewable.textDocument.id);
    }
}
```

```html
<!-- text-file-viewer.html -->
<template>
    <import from="core/@application/editor/text-editor"></import>
    <text-editor component.ref="editor"></text-editor>
</template>
```

**Important**: Do **not** pass the `ViewerHost` as a constructor parameter. `Viewer.host` is assigned via `setHost()` by the `ViewerHost` after DI construction. This lets the container resolve viewers cleanly via `container.getFactory(viewerClass)`.

## Step 3 — Register the viewer

Viewers are registered with the singleton `IViewerRegistry` via a predicate. Registration usually happens in one of two places:

### For built-in viewers
In `main-window-bootstrapper.ts`, inside `registerBuiltInViewers()`:

```typescript
private registerBuiltInViewers(container: IContainer) {
    const registry = container.get(IViewerRegistry);

    registry.register({
        id: "script",
        viewerClass: ScriptViewer,
        canHandle: v => v instanceof ViewableScriptDocument
    });

    // New viewer
    registry.register({
        id: "text-file",
        viewerClass: TextFileViewer,
        canHandle: v => v instanceof ViewableTextFile
    });
}
```

### For plugin-contributed viewers
In your plugin's `configure(container)` hook:

```typescript
// src/core/@plugins/my-plugin/plugin.ts
import {IContainer} from "aurelia";
import {IViewerRegistry} from "@application";   // or wherever you alias it

export function configure(container: IContainer) {
    const registry = container.get(IViewerRegistry);
    registry.register({
        id: "my-plugin-view",
        viewerClass: MyCustomViewer,
        canHandle: v => v instanceof MyCustomViewable
    });
}
```

Plugin registrations run **before** built-in registrations, and `IViewerRegistry.resolve` returns the first match. So when a plugin and a built-in both claim the same viewable type, the plugin wins — this lets a plugin replace a default viewer. Duplicate IDs throw.

## Opening an instance

Once registered, create and open a viewable from anywhere that has `IWorkAreaService`:

```typescript
const viewable = new ViewableTextFile("/path/to/file.txt", fileContents);
await workAreaService.open(viewable);
workAreaService.activate(viewable.id);
```

For script viewables specifically, use `workAreaService.createScriptViewable(env)` which injects the services the `ViewableScriptDocument` needs. When other viewable types need similar factories, extract an `IViewableFactory` rather than piling methods onto `WorkAreaService`.

## Reference implementation

The canonical example is the built-in script viewer:

- `viewers/script-viewer/viewable-script-document.ts` — full `ViewableObject` subclass with observable display properties, event subscriptions, capability overrides, and drag-drop handling.
- `viewers/script-viewer/script-viewer.ts` — the Aurelia viewer component.
- `viewers/script-viewer/script-viewer.html` — the viewer template.
- `viewers/script-viewer/script-toolbar.ts` — a viewer-owned toolbar (optional; tab bar chrome is generic, but viewers can compose their own toolbar inside their template).

## Checklist

- [ ] Subclass `ViewableObject` (or `ViewableTextDocument`)
- [ ] Implement abstract `open(host)`, `close(host)`, `activate(host)`
- [ ] Override the capability methods your viewable supports (`canSave`/`save`, `canRun`/`run`, etc.)
- [ ] Populate display properties (`kindBadge`, `subtitle`, `tooltip`, `statusIndicator`, `path`)
- [ ] Subscribe to any events that should update display properties reactively
- [ ] Subclass `Viewer` — no `host` constructor param, use `@ILogger` for logging
- [ ] Create the viewer's HTML template
- [ ] Register with `IViewerRegistry` (bootstrapper or plugin)
- [ ] Call `workAreaService.open(viewable)` to show a tab
