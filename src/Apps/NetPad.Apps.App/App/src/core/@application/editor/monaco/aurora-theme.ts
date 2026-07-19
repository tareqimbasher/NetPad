import * as monaco from "monaco-editor";
import {ThemeTokens} from "@application/themes/theme-tokens";

/**
 * "Aurora": the editor half of the app's design system. Built from the app theme's design tokens.
 */
export function buildAuroraTheme(themeCssClass: string, base: "vs" | "vs-dark"): monaco.editor.IStandaloneThemeData {
    const dark = base === "vs-dark";
    const p = new Palette(themeCssClass, dark);

    const syntax = {
        keyword: p.rule("syntax-keyword"),
        type: p.rule("syntax-type"),
        string: p.rule("syntax-string"),
        number: p.rule("syntax-number"),
        method: p.rule("syntax-method"),
        comment: p.rule("syntax-comment"),
        value: p.rule("syntax-value"),
        punctuation: p.rule("syntax-punctuation"),
        regex: p.rule("syntax-regex"),
    };

    const rules: monaco.editor.ITokenThemeRule[] = [
        {token: "", foreground: syntax.value},
    ];

    const color = (foreground: string, tokens: string[]) =>
        rules.push(...tokens.map(token => ({token, foreground})));

    color(syntax.comment, [
        "comment",
        "excludedCode",
        "xmlDocCommentAttributeName",
        "xmlDocCommentAttributeQuotes",
        "xmlDocCommentAttributeValue",
        "xmlDocCommentCDataSection",
        "xmlDocCommentComment",
        "xmlDocCommentDelimiter",
        "xmlDocCommentEntityReference",
        "xmlDocCommentName",
        "xmlDocCommentProcessingInstruction",
        "xmlDocCommentText",
    ]);

    color(syntax.keyword, [
        "keyword",
        "plainKeyword",
        "controlKeyword",
        "preprocessorKeyword",
        "tag",
        "metatag",
        "annotation",
    ]);

    color(syntax.type, [
        "type",
        "class",
        "struct",
        "interface",
        "enum",
        "delegate",
        "namespace",
        "typeParameter",
        "module",
    ]);

    color(syntax.method, [
        "function",
        "member",
        "extensionMethod",
        "operatorOverloaded",
        "predefined",
        "attribute.name",
        "attribute",
    ]);

    color(syntax.string, [
        "string",
        "stringVerbatim",
        "preprocessorText",
        "attribute.value",
    ]);

    color(syntax.number, [
        "number",
        "enumMember",
        "constant",
    ]);

    color(syntax.regex, [
        "regexp",
        "stringEscapeCharacter",
        "regexComment",
        "regexCharacterClass",
        "regexAnchor",
        "regexQuantifier",
        "regexGrouping",
        "regexAlternation",
        "regexSelfEscapedCharacter",
        "regexOtherEscape",
    ]);

    color(syntax.punctuation, [
        "operator",
        "delimiter",
        "punctuation",
    ]);

    // Identifiers stay in the editor's default foreground. We still have to set them rather than
    // omit: `inherit: true` would otherwise let the base theme color these.
    color(syntax.value, [
        "variable",
        "parameter",
        "field",
        "local",
        "event",
        "identifier",
        "variable.predefined",
        "variable.parameter",
    ]);

    color(p.rule("err"), ["invalid"]);

    // The base themes we inherit from carry language-specific rules (`string.sql`, `keyword.json`, ...)
    // that outrank the generic ones above, so the palette has to answer them directly. These
    // cover the languages the editor registers: C#, SQL, JSON and CSS.
    color(syntax.string, ["string.sql", "string.value.json"]);
    color(syntax.method, ["predefined.sql", "string.key.json", "key"]);
    color(syntax.punctuation, ["operator.scss"]);
    // SQL's tokenizer files AND / OR / LIKE / JOIN under "operator" together with `=` and `>`.
    // They read as keywords, which is what the majority of them are.
    color(syntax.keyword, ["keyword.flow", "operator.sql"]);
    color(syntax.number, [
        "keyword.json",
        "number.hex",
        "attribute.value.number.css",
        "attribute.value.unit.css",
        "attribute.value.hex.css",
    ]);

    return {
        base,
        inherit: true,
        rules,
        colors: {
            ...editorColors(p),
            ...widgetColors(p, dark),
            ...symbolIconColors(p),
        }
    };
}

function editorColors(p: Palette): Record<string, string> {
    return {
        "editor.background": p.hex("editor"),
        "editor.foreground": p.hex("syntax-value"),
        "editorCursor.foreground": p.hex("accent"),
        "editorGutter.background": p.hex("editor"),
        "editorLineNumber.foreground": p.hex("text-3"),
        "editorLineNumber.activeForeground": p.hex("text-2"),

        // The 2px accent edge the design puts on the current line is drawn in CSS (Monaco can only
        // outline the line on all four sides), so the border is cleared here.
        "editor.lineHighlightBackground": p.alpha("accent", 0.06),
        "editor.lineHighlightBorder": "#00000000",

        "editor.selectionBackground": p.hex("accent-dim"),
        "editor.inactiveSelectionBackground": p.alpha("accent", 0.16),
        "editor.selectionHighlightBackground": p.alpha("accent", 0.12),
        "editor.wordHighlightBackground": p.alpha("accent", 0.12),
        "editor.wordHighlightStrongBackground": p.alpha("accent", 0.2),
        "editor.rangeHighlightBackground": p.alpha("accent", 0.1),

        "editor.findMatchBackground": p.alpha("warn", 0.38),
        "editor.findMatchHighlightBackground": p.alpha("warn", 0.2),
        "editor.findRangeHighlightBackground": p.alpha("accent", 0.1),

        "editorWhitespace.foreground": p.hex("line"),
        "editorIndentGuide.background": p.hex("line-soft"),
        "editorIndentGuide.activeBackground": p.hex("line"),
        "editorIndentGuide.background1": p.hex("line-soft"),
        "editorIndentGuide.activeBackground1": p.hex("line"),
        "editorRuler.foreground": p.hex("line-soft"),

        "editorBracketMatch.background": p.alpha("accent", 0.18),
        "editorBracketMatch.border": p.alpha("accent", 0.45),
        // Rainbow brackets, in the theme's own hues. The outermost pair stays
        // punctuation-grey, so color only appears where there is nesting.
        "editorBracketHighlight.foreground1": p.hex("syntax-punctuation"),
        "editorBracketHighlight.foreground2": p.hex("syntax-number"),
        "editorBracketHighlight.foreground3": p.hex("syntax-string"),
        "editorBracketHighlight.foreground4": p.hex("syntax-type"),
        "editorBracketHighlight.foreground5": p.hex("syntax-method"),
        "editorBracketHighlight.foreground6": p.hex("syntax-keyword"),
        "editorBracketHighlight.unexpectedBracket.foreground": p.hex("err"),

        "editorError.foreground": p.hex("err"),
        "editorWarning.foreground": p.hex("warn"),
        "editorInfo.foreground": p.hex("accent"),
        "editorHint.foreground": p.hex("text-3"),
        "editorLink.activeForeground": p.hex("accent"),
        "editorCodeLens.foreground": p.hex("text-3"),

        "editorInlayHint.background": p.hex("bg2"),
        "editorInlayHint.foreground": p.hex("text-3"),
        "editorInlayHint.typeBackground": p.hex("bg2"),
        "editorInlayHint.typeForeground": p.hex("text-3"),
        "editorInlayHint.parameterBackground": p.hex("bg2"),
        "editorInlayHint.parameterForeground": p.hex("text-3"),

        "editorOverviewRuler.border": "#00000000",
        "editorOverviewRuler.errorForeground": p.hex("err"),
        "editorOverviewRuler.warningForeground": p.hex("warn"),
        "editorOverviewRuler.infoForeground": p.hex("accent"),
        "editorOverviewRuler.findMatchForeground": p.alpha("warn", 0.6),
        "editorOverviewRuler.selectionHighlightForeground": p.alpha("accent", 0.5),
        "editorOverviewRuler.bracketMatchForeground": p.hex("text-3"),

        "editorStickyScroll.background": p.hex("bg1"),
        "editorStickyScrollHover.background": p.hex("bg2"),
    };
}

function widgetColors(p: Palette, dark: boolean): Record<string, string> {
    return {
        "foreground": p.hex("text"),
        "descriptionForeground": p.hex("text-2"),
        "errorForeground": p.hex("err"),
        "focusBorder": p.hex("accent"),
        "widget.shadow": dark ? "#00000066" : "#3d3d4a1f",
        "sash.hoverBorder": p.hex("accent"),
        "progressBar.background": p.hex("accent"),
        "badge.background": p.hex("accent-dim"),
        "badge.foreground": p.hex("text"),
        "toolbar.hoverBackground": p.hex("bg3"),
        "textLink.foreground": p.hex("accent"),
        "textLink.activeForeground": p.hex("accent-bright"),
        "textCodeBlock.background": p.hex("bg2"),

        "editorWidget.background": p.hex("bg1"),
        "editorWidget.foreground": p.hex("text"),
        "editorWidget.border": p.hex("line"),
        "editorWidget.resizeBorder": p.hex("accent"),

        "editorSuggestWidget.background": p.hex("bg1"),
        "editorSuggestWidget.border": p.hex("line"),
        "editorSuggestWidget.foreground": p.hex("text"),
        "editorSuggestWidget.selectedBackground": p.hex("accent-dim"),
        "editorSuggestWidget.selectedForeground": p.hex("text"),
        "editorSuggestWidget.highlightForeground": p.hex("accent-bright"),
        "editorSuggestWidget.focusHighlightForeground": p.hex("accent-bright"),
        "editorSuggestWidgetStatus.foreground": p.hex("text-3"),

        "editorHoverWidget.background": p.hex("bg1"),
        "editorHoverWidget.foreground": p.hex("text"),
        "editorHoverWidget.border": p.hex("line"),
        "editorHoverWidget.statusBarBackground": p.hex("bg2"),

        "quickInput.background": p.hex("bg1"),
        "quickInput.foreground": p.hex("text"),
        "quickInputTitle.background": p.hex("bg2"),
        "quickInputList.focusBackground": p.hex("accent-dim"),
        "quickInputList.focusForeground": p.hex("text"),
        "quickInputList.focusIconForeground": p.hex("accent"),
        "pickerGroup.foreground": p.hex("text-3"),
        "pickerGroup.border": p.hex("line-soft"),

        "list.hoverBackground": p.hex("bg2"),
        "list.hoverForeground": p.hex("text"),
        "list.focusBackground": p.hex("accent-dim"),
        "list.focusForeground": p.hex("text"),
        "list.activeSelectionBackground": p.hex("accent-dim"),
        "list.activeSelectionForeground": p.hex("text"),
        "list.inactiveSelectionBackground": p.hex("bg2"),
        "list.inactiveSelectionForeground": p.hex("text"),
        "list.highlightForeground": p.hex("accent-bright"),

        "input.background": p.hex("bg2"),
        "input.foreground": p.hex("text"),
        "input.border": p.hex("line"),
        "input.placeholderForeground": p.hex("text-3"),
        "inputOption.activeBackground": p.hex("accent-dim"),
        "inputOption.activeForeground": p.hex("text"),
        "inputOption.activeBorder": p.hex("accent"),
        "inputValidation.errorBackground": p.hex("bg1"),
        "inputValidation.errorBorder": p.hex("err"),
        "inputValidation.warningBackground": p.hex("bg1"),
        "inputValidation.warningBorder": p.hex("warn"),
        "inputValidation.infoBackground": p.hex("bg1"),
        "inputValidation.infoBorder": p.hex("accent"),

        "dropdown.background": p.hex("bg2"),
        "dropdown.foreground": p.hex("text"),
        "dropdown.border": p.hex("line"),

        "menu.background": p.hex("bg1"),
        "menu.foreground": p.hex("text"),
        "menu.border": p.hex("line"),
        "menu.separatorBackground": p.hex("line-soft"),
        "menu.selectionBackground": p.hex("accent-dim"),
        "menu.selectionForeground": p.hex("text"),

        "keybindingLabel.background": p.hex("bg1"),
        "keybindingLabel.foreground": p.hex("text-2"),
        "keybindingLabel.border": p.hex("line"),
        "keybindingLabel.bottomBorder": p.hex("line"),

        "scrollbar.shadow": "#00000000",
        "scrollbarSlider.background": p.alpha("text-3", 0.25),
        "scrollbarSlider.hoverBackground": p.alpha("text-3", 0.4),
        "scrollbarSlider.activeBackground": p.alpha("text-3", 0.55),

        "peekView.border": p.hex("accent"),
        "peekViewEditor.background": p.hex("editor"),
        "peekViewEditor.matchHighlightBackground": p.alpha("warn", 0.3),
        "peekViewResult.background": p.hex("bg1"),
        "peekViewResult.fileForeground": p.hex("text"),
        "peekViewResult.lineForeground": p.hex("text-2"),
        "peekViewResult.selectionBackground": p.hex("accent-dim"),
        "peekViewResult.selectionForeground": p.hex("text"),
        "peekViewResult.matchHighlightBackground": p.alpha("warn", 0.3),
        "peekViewTitle.background": p.hex("bg2"),
        "peekViewTitleLabel.foreground": p.hex("text"),
        "peekViewTitleDescription.foreground": p.hex("text-2"),
    };
}

/** Tints the glyphs the suggest widget puts next to each completion, by symbol kind. */
function symbolIconColors(p: Palette): Record<string, string> {
    const byKind: Record<string, string> = {
        class: "syntax-type",
        interface: "syntax-type",
        struct: "syntax-type",
        enumerator: "syntax-type",
        namespace: "syntax-type",
        module: "syntax-type",
        package: "syntax-type",
        typeParameter: "syntax-type",
        object: "syntax-type",
        method: "syntax-method",
        function: "syntax-method",
        constructor: "syntax-method",
        event: "syntax-method",
        operator: "syntax-punctuation",
        keyword: "syntax-keyword",
        snippet: "syntax-keyword",
        variable: "syntax-value",
        field: "syntax-value",
        property: "syntax-value",
        key: "syntax-value",
        reference: "syntax-value",
        constant: "syntax-number",
        enumeratorMember: "syntax-number",
        number: "syntax-number",
        boolean: "syntax-number",
        unit: "syntax-number",
        array: "syntax-punctuation",
        null: "syntax-punctuation",
        string: "syntax-string",
        text: "text-2",
        file: "text-2",
        folder: "text-2",
        color: "text-2",
    };

    return Object.fromEntries(
        Object.entries(byKind).map(([kind, token]) => [`symbolIcon.${kind}Foreground`, p.hex(token)])
    );
}

/**
 * The theme's tokens, in the shapes Monaco wants: `#rrggbb` for editor colors, bare `rrggbb` for
 * token rules, `#rrggbbaa` where a color has to be laid over the editor surface.
 */
class Palette {
    private static readonly hexPattern = /^#[0-9a-f]{6}$/i;

    private readonly tokens: ReadonlyMap<string, string>;
    private readonly fallback: string;

    constructor(themeCssClass: string, dark: boolean) {
        this.tokens = ThemeTokens.read(themeCssClass, Palette.tokenNames);
        this.fallback = dark ? "#d4d4d4" : "#333333";
    }

    public hex(name: string): string {
        const value = this.tokens.get(name);
        // A token can be missing or in a format we can't parse only when user custom CSS has
        // redefined it: the editor stays usable in the base theme's color rather than breaking.
        return value && Palette.hexPattern.test(value) ? value.toLowerCase() : this.fallback;
    }

    public rule(name: string): string {
        return this.hex(name).substring(1);
    }

    public alpha(name: string, alpha: number): string {
        const channel = Math.round(Math.min(Math.max(alpha, 0), 1) * 255).toString(16).padStart(2, "0");
        return this.hex(name) + channel;
    }

    private static readonly tokenNames = [
        "bg1", "bg2", "bg3", "editor",
        "line", "line-soft",
        "text", "text-2", "text-3",
        "accent", "accent-bright", "accent-dim",
        "warn", "err",
        "syntax-keyword", "syntax-type", "syntax-string", "syntax-number", "syntax-method",
        "syntax-comment", "syntax-value", "syntax-punctuation", "syntax-regex",
    ];
}
