# Styling

You can change the way NetPad looks by defining your own CSS styles in <kbd><kbd>Settings</kbd> > <kbd>
Styles</kbd></kbd>.

## Styling Output

Use the `.dump-container` class to define custom styles for the output container.

```css
.dump-container {
    font-size: 1.25rem;
}
```

You can also define your own CSS classes and use them when you `Dump()` a value:

```csharp
myObj.Dump(css: "important");
```

```css
.important {
    background-color: salmon;
}
```

## Theming NetPad

NetPad has 2 themes, Light and Dark. You can override the styles of these themes to make it feel yours. These are all
the
CSS variables and their values for both themes:

```css
.theme-netpad-dark {
    /* TEXT */
    --text-color: #dcdcdc;
    --text-strong-color: #ffffff;
    --text-hover-color: #ffffff;
    /* BACKGROUND */
    --background-color: #222222;
    --background-lighter-color: #333333;
    --list-background: #282828;
    --form-control-background-color: #333333;
    --form-control-disabled-background-color: #1c1c1c;
    --kbd-background-color: #59646e;
    /* BORDERS */
    --border-color: #3f3f3f;
    --border-contrast-color: #555555;
    /* ACTIONS */
    --hover-color: rgba(255 255 255 / 7.5%);
    --active-color: #0d6efd;
    --action-icon-color: silver;
    --action-icon-hover-color: white;
    /* SHADOWS */
    --box-shadow-bottom: 0 7px 8px -8px rgb(0 0 0 / 36%);
    --box-shadow-bottom-and-sides: 0 0 8px 2px rgb(0 0 0 / 36%);
    /* TITLEBAR */
    --titlebar-background: #1b1c1d;
    /* STATUSBAR */
    --statusbar-background: #1b1c1d;
    /* SCRIPT VIEW */
    --script-toolbar-background: #1e1e1e;
    --find-text-box-background: #1c1c1c;
    /* PANES */
    --pane-ribbon-background: #252525;
    --pane-background: #222222;
    --pane-toolbar-background: #222222;
    /* CONTEXT MENU */
    --context-menu-background: #323232;
    --context-menu-item-hover-background: #15539e;
    /* DIALOG */
    --dialog-background: #222222;
    /* TABS */
    --tab-background: #2d2d2d;
    --tab-active-background: #1e1e1e;
    --tab-active-text-color: #ffffff;
    --tab-inactive-text-color: rgb(255 255 255 / 65%);
    /* SCROLLBARS & SPLITTER */
    --scrollbar-track-color: #343434;
    --scrollbar-thumb-color: #777777;
    --splitter-color: #282828;
}

.theme-netpad-light {
    /* TEXT */
    --text-color: #34314b;
    --text-strong-color: #000000;
    --text-hover-color: #000000;
    /* BACKGROUND */
    --background-color: #f3f3f3;
    --background-lighter-color: #f6f6f6;
    --list-background: #f1f1f1;
    --form-control-background-color: #fafafa;
    --form-control-disabled-background-color: #ececec;
    --kbd-background-color: #212529;
    /* BORDERS */
    --border-color: #cccccc;
    --border-contrast-color: #bfbfbf;
    /* ACTIONS */
    --hover-color: rgb(0 0 0 / 0.75%);
    --active-color: #0d6efd;
    --action-icon-color: #595959;
    --action-icon-hover-color: black;
    /* SHADOWS */
    --box-shadow-bottom: 0 7px 8px -8px rgb(0 0 0 / 16%);
    --box-shadow-bottom-and-sides: 0 0 8px 2px rgb(0 0 0 / 16%);
    /* TITLEBAR */
    --titlebar-background: #dfdfdf;
    /* STATUSBAR */
    --statusbar-background: #e3e3e3;
    /* SCRIPT VIEW */
    --script-toolbar-background: #ffffff;
    --find-text-box-background: #ededed;
    /* PANES */
    --pane-ribbon-background: #efefef;
    --pane-background: #f3f3f3;
    /* CONTEXT MENU */
    --context-menu-background: #f3f3f3;
    --context-menu-item-hover-background: rgb(30 144 255 / 50%);
    /* DIALOG */
    --dialog-background: #f3f3f3;
    /* TABS */
    --tab-background: #ececec;
    --tab-active-background: #ffffff;
    --tab-active-text-color: #000000;
    --tab-inactive-text-color: rgb(0 0 0 / 65%);
    /* SCROLLBARS & SPLITTER */
    --scrollbar-track-color: #cecece;
    --scrollbar-thumb-color: #7e8182;
    --splitter-color: #dcdcdc;
}
```

To override a variable, define it in your custom styles.

```css
.theme-netpad-dark {
    --active-color: red;
}
```

Here's an example of customizing the Dark theme using the Dracula color palette, which goes great with the Dracula
editor theme in <kbd><kbd>Settings</kbd> > <kbd>Editor</kbd></kbd>:

```css
.theme-netpad-dark {
    --drac-light: #343746;
    --drac-dark: #22222c;

    --text-color: #F8F8F2;
    --background-color: var(--drac-dark);
    --list-background: var(--drac-light);
    --form-control-background-color: #343746;
    --active-color: #dbb5fa;
    --script-toolbar-background: #242632;
    --pane-ribbon-background: var(--drac-light);
    --pane-background: var(--drac-dark);
    --pane-toolbar-background: var(--drac-dark);
    --dialog-background: var(--drac-dark);
    --tab-background: #333541;
    --tab-active-background: #242632;
    --tab-active-text-color: #fffff;
    --scrollbar-thumb-color: #777777;
    --splitter-color: #2b2d39;
}
```

### Theming Anything

You can style pretty much anything in NetPad. If you find something you'd like to style that isn't covered by
the pre-defined CSS variables open the Developer Console (`CTRL + SHIFT + I`), locate the element(s) you'd like to
customize and add them to your custom styles!

Example:

```css
.save-icon {
    color: orange;
}
```

!> **Note** that breaking changes to theme CSS variables and to DOM structure can occur in NetPad updates. If that
happens, it will be announced.

## Styling the Editor

### Predefined Themes

NetPad uses the Monaco editor. You can customize the look and feel of the editor by going to <kbd><kbd>
Settings</kbd> > <kbd>Editor</kbd></kbd>. There you'll find a number of themes to select from (powered by
the [monaco-themes](https://github.com/brijeshb42/monaco-themes) project).

### Custom Styles

You can customize a selected theme using the `themeCustomizations` property.

```jsonc
{
    "cursorBlinking": "smooth",
    "lineNumbers": "on",
    "wordWrap": "off",
    "mouseWheelZoom": true,
    "minimap": {
        "enabled": false
    },
    "themeCustomizations": {
        // General editor colors
        "colors": {
          "editor.background": "#282a36"
        },
    
        // Semantic highlighting token styles
        "rules": [
          {
            "token": "interface",
            "foreground": "50fa7b",
            "fontStyle": "underline"
          }
        ]
    }
}
```

> :bulb: See [IColors](https://microsoft.github.io/monaco-editor/typedoc/types/editor.IColors.html) and
> [ITokenThemeRule](https://microsoft.github.io/monaco-editor/typedoc/interfaces/editor.ITokenThemeRule.html) for the
> definition of the `colors` and `rules` properties respectively.

The value keys that can be added to the `colors` property are not all clearly defined by the Monaco project but here are
some:

```json
{
    "colors": {
        "editor.foreground": "#f6f8fa",
        "editor.background": "#24292e",
        "editor.selectionBackground": "#4c2889",
        "editor.inactiveSelectionBackground": "#444d56",
        "editor.lineHighlightBackground": "#444d56",
        "editorCursor.foreground": "#ffffff",
        "editorWhitespace.foreground": "#6a737d",
        "editorIndentGuide.background": "#6a737d",
        "editorIndentGuide.activeBackground": "#f6f8fa",
        "editor.selectionHighlightBorder": "#444d56"
    }
}
```

Token names for use in the `rules` property are also not clearly defined. However, you can find the ones NetPad uses
[here](https://github.com/tareqimbasher/NetPad/blob/5d0e78714383f41254253fd47190964510475b76/src/Apps/NetPad.Apps.App/App/src/core/%40application/editor/monaco/monaco-theme-manager.ts#L319).
