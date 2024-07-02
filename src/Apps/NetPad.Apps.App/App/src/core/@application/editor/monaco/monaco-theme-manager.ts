import * as monaco from "monaco-editor";
import {Settings} from "@application";
import {MonacoThemeInfo} from "./monaco-theme-info";

export class MonacoThemeManager {
    private static initialized = false;
    private static themes = new Map<string, MonacoThemeInfo>();

    public static async initialize(settings?: Settings) {
        if (this.initialized) {
            return;
        }

        // Add built-in themes
        this.lazyDefineTheme(new MonacoThemeInfo(
            "netpad-light-theme",
            "NetPad Light",
            {
                base: "vs",
                inherit: true,
                rules: [],
                colors: {
                    // HACK: Added explicitly because when setting to a theme that sets this property,
                    // and then switching to a theme that does not have this property defined, the
                    // minimap background color remains on the color of the last theme that set this property.
                    // Setting it here forces the minimap to change color
                    "editor.background": "#fffffe"
                }
            },
            undefined
        ));

        this.lazyDefineTheme(new MonacoThemeInfo(
            "netpad-dark-theme",
            "NetPad Dark",
            {
                base: "vs-dark",
                inherit: true,
                rules: [],
                colors: {
                    "editor.background": "#1e1e1e"
                }
            },
            undefined
        ));

        // Add themes from the monaco-themes library
        const monacoThemes = await import("monaco-themes/themes/themelist.json");

        for (const themeId in monacoThemes) {
            const themeFileName = monacoThemes[themeId as keyof typeof monacoThemes] as string | undefined;

            if (typeof themeFileName !== "string") {
                continue;
            }

            this.lazyDefineTheme(new MonacoThemeInfo(themeId, themeFileName, undefined, themeFileName));
        }

        // Eagerly load the current user theme
        if (settings?.editor.monacoOptions?.theme) {
            await this.loadThemeData(settings.editor.monacoOptions.theme);
        }

        this.initialized = true;
    }

    public static getThemes() {
        return this.themes.values();
    }

    public static async getOrLoad(themeId: string) {
        const theme = this.themes.get(themeId);

        if (!theme) {
            throw new Error(`No theme registered with id: ${themeId}`);
        }

        if (theme.loaded) {
            return theme;
        }

        await this.loadThemeData(themeId);

        return theme;
    }

    public static setTheme(editor: monaco.editor.IStandaloneCodeEditor, themeId: string, customizations?: {
        colors?: object,
        rules?: monaco.editor.ITokenThemeRule[]
    }): Promise<void>;
    public static setTheme(editor: monaco.editor.IStandaloneCodeEditor, theme: MonacoThemeInfo, customizations?: {
        colors?: object,
        rules?: monaco.editor.ITokenThemeRule[]
    }): Promise<void>;
    public static async setTheme(editor: monaco.editor.IStandaloneCodeEditor, themeOrId: string | MonacoThemeInfo, customizations?: {
        colors?: object,
        rules?: monaco.editor.ITokenThemeRule[]
    }): Promise<void> {
        await this.initialize();

        let themeId: string;

        if (typeof themeOrId === "string") {
            if (!this.themes.has(themeOrId)) {
                throw new Error(`No theme registered with id: ${themeOrId}`);
            }

            themeId = themeOrId;
        } else {
            if (!this.themes.has(themeOrId.id)) {
                this.lazyDefineTheme(themeOrId);
            }

            themeId = themeOrId.id;
        }

        await this.loadThemeData(themeId);

        let theme = this.themes.get(themeId)!;

        if ((customizations?.colors && Object.keys(customizations.colors).length > 0) ||
            (customizations?.rules && customizations.rules.length > 0)) {
            // Copy theme to a new custom theme and apply customizations
            const customThemeData = JSON.parse(JSON.stringify(theme.data)) as monaco.editor.IStandaloneThemeData;

            if (customizations.colors) {
                for (const colorsKey in customizations.colors) {
                    const colorValue = customizations.colors[colorsKey as keyof typeof customizations.colors] as string;

                    if (colorValue && colorValue.startsWith("#")) {
                        customThemeData.colors[colorsKey] = colorValue;
                    }
                }
            }

            if (customizations.rules && customizations.rules.length > 0) {
                for (const rule of customizations.rules) {
                    const existingRule = customThemeData.rules.find(x => x.token == rule.token);
                    if (existingRule) {
                        Object.assign(existingRule, rule);
                    } else {
                        customThemeData.rules.push(rule);
                    }
                }
            }

            theme = new MonacoThemeInfo("custom", "Custom", customThemeData);
        }

        this.defineTheme(theme.id, theme.data!);

        const currentOptions = editor.getRawOptions() as { theme: string };
        currentOptions.theme = theme.id;
        editor.updateOptions(currentOptions);
    }

    private static async loadThemeData(themeId: string): Promise<monaco.editor.IStandaloneThemeData> {
        const theme = this.themes.get(themeId);

        if (!theme) {
            throw new Error(`No theme registered with the id: ${themeId}`);
        }

        if (!theme.data && !theme.url) {
            throw new Error(`No URL or data is defined for registered theme with the id: ${themeId}`);
        }

        if (!theme.loaded) {
            try {
                const themeData = await import(`monaco-themes/themes/${theme.url}.json`) as monaco.editor.IStandaloneThemeData;
                this.fillCompatibilityTokens(themeData);
                theme.data = themeData;
            } catch (e) {
                console.error("Could not find theme by url: ", theme, e);
            }
        }

        if (!theme.data) {
            throw new Error(`Could not load registered theme with the id: ${themeId}`);
        }

        return theme.data;
    }

    private static lazyDefineTheme(themeInfo: MonacoThemeInfo) {
        this.themes.set(themeInfo.id, themeInfo);
    }

    private static defineTheme(themeId: string, themeData: monaco.editor.IStandaloneThemeData) {
        if (!themeData.rules || themeData.rules.length === 0) {
            themeData.rules ??= [];
            themeData.rules.push(...themeData.base == "vs" ? this.lightThemeTokenThemeRules : this.darkThemeTokenThemeRules);
        }

        themeData.colors ??= {};

        monaco.editor.defineTheme(themeId, themeData);
    }

    /**
     * Fills tokens that are needed for proper C# syntax highlighting. Monaco themes don't typically contain C#
     * specific token names; it will be missing some important C# tokens like class, plainKeyword...etc. which end up
     * getting colored with the default foreground color.
     *
     * To fix this issue, this function will fill in these C#-specific tokens by using existing tokens from the Monaco
     * theme.
     * @param themeData The theme data to fill.
     */
    private static fillCompatibilityTokens(themeData: monaco.editor.IStandaloneThemeData) {
        const ruleMap = new Map<string, monaco.editor.ITokenThemeRule>();
        for (const rule of themeData.rules) {
            ruleMap.set(rule.token, rule);
        }

        for (const mapping of this.csharpMonacoTokenMapping) {
            const tokensToFill = mapping[1].filter(t => !ruleMap.has(t));

            if (tokensToFill.length === 0) {
                continue;
            }

            const fillFromTokens = mapping[0];
            let existing: monaco.editor.ITokenThemeRule | undefined;

            for (const token of fillFromTokens) {
                const value = ruleMap.get(token);
                if (value) {
                    existing = value;
                    break;
                }
            }

            if (!existing) {
                continue;
            }

            for (const tokenToFill of tokensToFill) {
                const newRule = Object.assign({}, existing);
                newRule.token = tokenToFill;
                ruleMap.set(tokenToFill, newRule);
            }
        }

        themeData.rules.splice(0);
        themeData.rules.push(...ruleMap.values());
    }

    /**
     * Map of new tokens that should be added to a theme (if they don't already exist), and where they should be filled
     * from. Each entry is an array with 2 elements.
     *
     * The first element is a collection of "source" tokens to use to fill the destination. The first token from this
     * collection that is already found in the theme will be used as the value of the destination token we will be setting.
     *
     * The second element is a collection of "destination" tokens that we will set from the value of the "source". There
     * are 2 conditions that have to be true to fill a destination token:
     * 1. The source value exists (ie. we found one source token to use as the value)
     * 2. The destination token does not already exist in the theme
     */
    private static readonly csharpMonacoTokenMapping = [
        [["keyword"], ["plainKeyword"]],
        [[
            "entity.name",
            "entity.name.class",
            "entity.name.type.class",
            "entity.name.type.class-type",
            "entity.name.type",
            "entity",
        ], [
            "class",
            "struct",
            "type",
            "typeParameter",
            "namespace",
            "delegate",
        ]],
        [[
            "entity.other.inherited-class",
            "entity.name.tag"
        ], [
            "interface"
        ]],
        [[
            "entity.name.function",
            "meta.function-call",
            "support.function",
        ], [
            "function",
            "extensionMethod",
            "member",
            "operatorOverloaded",
        ]],
        [[
            "variable.language",
            "variable.other",
        ], [
            "variable",
            "event",
            "field",
            "local",
        ]],
        [["variable.parameter"], ["parameter"]],
        [[
            "comment",
            "comment.block",
        ], [
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
        ]],
    ];

    private static lightThemeTokenThemeRules: monaco.editor.ITokenThemeRule[] = [
        {token: "comment", foreground: "008000"},
        {token: "string", foreground: "a31515"},
        {token: "keyword", foreground: "0000ff"},
        {token: "number", foreground: "098658"},
        {token: "regexp", foreground: "EE0000"},
        {token: "operator", foreground: "000000"},
        {token: "namespace", foreground: "267f99"},
        {token: "type", foreground: "267f99"},
        {token: "struct", foreground: "6C9F6C"},
        {token: "class", foreground: "267f99"},
        {token: "interface", foreground: "89b35b"},
        {token: "enum", foreground: "267f99"},
        {token: "typeParameter", foreground: "267f99"},
        {token: "function", foreground: "795E26"},
        {token: "member", foreground: "795E26"},
        // {token: "macro", foreground: "000000"},
        {token: "variable", foreground: "001080"},
        {token: "parameter", foreground: "001080"},
        // {token: "property", foreground: "001080"},
        {token: "enumMember", foreground: "0070C1"},
        {token: "event", foreground: "001080"},
        // {token: "label", foreground: "000000"},
        {token: "plainKeyword", foreground: "0000ff"},
        {token: "controlKeyword", foreground: "AF00DB"},
        {token: "operatorOverloaded", foreground: "795e26"},
        {token: "preprocessorKeyword", foreground: "0000ff"},
        {token: "preprocessorText", foreground: "a31515"},
        {token: "excludedCode", foreground: "BEBEBE"},
        // {token: "punctuation", foreground: "AF00DB"},
        {token: "stringVerbatim", foreground: "a31515"},
        {token: "stringEscapeCharacter", foreground: "EE0000"},
        {token: "delegate", foreground: "267f99"},
        // {token: "module", foreground: "000000"},
        {token: "extensionMethod", foreground: "795E26"},
        {token: "field", foreground: "001080"},
        {token: "local", foreground: "001080"},
        {token: "xmlDocCommentAttributeName", foreground: "008000"},
        {token: "xmlDocCommentAttributeQuotes", foreground: "008000"},
        {token: "xmlDocCommentAttributeValue", foreground: "008000"},
        {token: "xmlDocCommentCDataSection", foreground: "008000"},
        {token: "xmlDocCommentComment", foreground: "008000"},
        {token: "xmlDocCommentDelimiter", foreground: "008000"},
        {token: "xmlDocCommentEntityReference", foreground: "008000"},
        {token: "xmlDocCommentName", foreground: "008000"},
        {token: "xmlDocCommentProcessingInstruction", foreground: "008000"},
        {token: "xmlDocCommentText", foreground: "008000"},
        {token: "regexComment", foreground: "EE0000"},
        {token: "regexCharacterClass", foreground: "EE0000"},
        {token: "regexAnchor", foreground: "EE0000"},
        {token: "regexQuantifier", foreground: "EE0000"},
        {token: "regexGrouping", foreground: "EE0000"},
        {token: "regexAlternation", foreground: "EE0000"},
        {token: "regexSelfEscapedCharacter", foreground: "EE0000"},
        {token: "regexOtherEscape", foreground: "EE0000"},
    ]

    private static darkThemeTokenThemeRules: monaco.editor.ITokenThemeRule[] = [
        {token: "comment", foreground: "6A9955"},
        {token: "string", foreground: "ce9178"},
        {token: "keyword", foreground: "569cd6"},
        {token: "number", foreground: "b5cea8"},
        {token: "regexp", foreground: "D7BA7D"},
        {token: "operator", foreground: "d4d4d4"},
        {token: "namespace", foreground: "4EC9B0"},
        {token: "type", foreground: "4EC9B0"},
        {token: "struct", foreground: "86C691"},
        {token: "class", foreground: "4EC9B0"},
        {token: "interface", foreground: "B8D7A3"},
        {token: "enum", foreground: "B8D7A3"},
        {token: "typeParameter", foreground: "4EC9B0"},
        {token: "function", foreground: "DCDCAA"},
        {token: "member", foreground: "DCDCAA"},
        // {token: "macro", foreground: "FFFFFF"},
        {token: "variable", foreground: "9CDCFE"},
        {token: "parameter", foreground: "9CDCFE"},
        // {token: "property", foreground: "FFFFFF"},
        {token: "enumMember", foreground: "4FC1FF"},
        {token: "event", foreground: "9CDCFE"},
        // {token: "label", foreground: "FFFFFF"},
        {token: "plainKeyword", foreground: "569cd6"},
        {token: "controlKeyword", foreground: "C586C0"},
        {token: "operatorOverloaded", foreground: "dcdcaa"},
        {token: "preprocessorKeyword", foreground: "569cd6"},
        {token: "preprocessorText", foreground: "ce9178"},
        {token: "excludedCode", foreground: "EEEEEE"},
        // {token: "punctuation", foreground: "ffd700"},
        {token: "stringVerbatim", foreground: "ce9178"},
        {token: "stringEscapeCharacter", foreground: "D7BA7D"},
        {token: "delegate", foreground: "4EC9B0"},
        // {token: "module", foreground: "FFFFFF"},
        {token: "extensionMethod", foreground: "DCDCAA"},
        {token: "field", foreground: "9CDCFE"},
        {token: "local", foreground: "9CDCFE"},
        {token: "xmlDocCommentAttributeName", foreground: "6A9955"},
        {token: "xmlDocCommentAttributeQuotes", foreground: "6A9955"},
        {token: "xmlDocCommentAttributeValue", foreground: "6A9955"},
        {token: "xmlDocCommentCDataSection", foreground: "6A9955"},
        {token: "xmlDocCommentComment", foreground: "6A9955"},
        {token: "xmlDocCommentDelimiter", foreground: "6A9955"},
        {token: "xmlDocCommentEntityReference", foreground: "6A9955"},
        {token: "xmlDocCommentName", foreground: "6A9955"},
        {token: "xmlDocCommentProcessingInstruction", foreground: "6A9955"},
        {token: "xmlDocCommentText", foreground: "6A9955"},
        {token: "regexComment", foreground: "D7BA7D"},
        {token: "regexCharacterClass", foreground: "D7BA7D"},
        {token: "regexAnchor", foreground: "D7BA7D"},
        {token: "regexQuantifier", foreground: "D7BA7D"},
        {token: "regexGrouping", foreground: "D7BA7D"},
        {token: "regexAlternation", foreground: "D7BA7D"},
        {token: "regexSelfEscapedCharacter", foreground: "D7BA7D"},
        {token: "regexOtherEscape", foreground: "D7BA7D"},
    ]
}
