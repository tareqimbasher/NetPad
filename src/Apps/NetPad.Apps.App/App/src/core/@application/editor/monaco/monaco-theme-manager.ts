import * as monaco from "monaco-editor";
import {Settings} from "@application";
import {MonacoThemeInfo} from "./monaco-theme-info";
import {buildAuroraTheme} from "./aurora-theme";

export class MonacoThemeManager {
    private static initialized = false;
    private static themes = new Map<string, MonacoThemeInfo>();

    /**
     * The app's own editor themes ("aurora"), one per app theme. Their colors are built from the
     * app theme's design tokens, so the editor always sits in the same palette as the chrome
     * around it.
     */
    public static readonly auroraThemes = [
        {id: "netpad-dark-theme", name: "Aurora Dark", cssClass: "theme-netpad-dark", base: "vs-dark"},
        {id: "netpad-light-theme", name: "Aurora Light", cssClass: "theme-netpad-light", base: "vs"},
    ] as const;

    public static async initialize(settings?: Settings) {
        if (this.initialized) {
            return;
        }

        for (const theme of this.auroraThemes) {
            this.lazyDefineTheme(new MonacoThemeInfo(
                theme.id,
                theme.name,
                undefined,
                undefined,
                () => buildAuroraTheme(theme.cssClass, theme.base)
            ));
        }

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

        if (theme.build) {
            theme.data = theme.build();
            return theme.data;
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
        // A few library themes carry no token rules at all. They borrow aurora's so that code is
        // still syntax-colored instead of falling back to one flat foreground.
        if (!themeData.rules || themeData.rules.length === 0) {
            const isDark = themeData.base === "vs-dark" || themeData.base === "hc-black";
            const aurora = this.auroraThemes.find(t => (t.base === "vs-dark") === isDark)!;
            themeData.rules = buildAuroraTheme(aurora.cssClass, aurora.base).rules;
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
}
