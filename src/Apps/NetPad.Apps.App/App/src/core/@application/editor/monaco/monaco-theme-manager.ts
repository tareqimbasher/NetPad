import * as monaco from "monaco-editor";
import {Util} from "@common";
import {Settings} from "@application";
import {AppTheme, ThemeFamily, ThemeGround} from "@application/themes/app-theme";
import {MonacoThemeInfo} from "./monaco-theme-info";
import {buildAuroraTheme} from "./aurora-theme";
import {buildVisualStudioTheme} from "./visual-studio-theme";

interface AuroraTheme {
    id: string;
    name: string;
    cssClass: string;
    base: "vs" | "vs-dark";
    family: ThemeFamily;
}

/** A theme as the settings picker lists it. */
interface ThemeChoice {
    id: string;
    name: string;
}

export class MonacoThemeManager {
    /**
     * The id of the theme that uses editor colors similar to that found in Visual Studio and VS Code,
     * following the app's light/dark mode. It paints differently per mode, so it is not itself a registered theme.
     */
    public static readonly adaptiveVisualStudioThemeId = "visual-studio";

    /**
     * The app's own editor themes ("aurora"), one per app theme. Their colors are built from the
     * app theme's design tokens, so the editor always sits in the same palette as the chrome
     * around it.
     */
    private static readonly auroraThemes: readonly AuroraTheme[] = AppTheme.families.flatMap(family =>
        AppTheme.grounds.map(ground => ({
            id: MonacoThemeManager.auroraThemeId(family.id, ground),
            name: `Aurora: ${Util.toTitleCase(family.groundNames[ground])}`,
            cssClass: AppTheme.cssClass(family.id, ground),
            base: ground === "dark" ? "vs-dark" as const : "vs" as const,
            family,
        })));

    /** The two themes the {@link adaptiveVisualStudioThemeId} value resolves onto. */
    private static readonly visualStudioThemes = [
        {id: "visual-studio-dark", ground: "dark" as const, base: "vs-dark" as const},
        {id: "visual-studio-light", ground: "light" as const, base: "vs" as const},
    ];

    private static registration?: Promise<void>;
    private static themes = new Map<string, MonacoThemeInfo>();

    private static readonly netPadThemes: readonly ThemeChoice[] = [
        {id: MonacoThemeManager.adaptiveVisualStudioThemeId, name: "Visual Studio"},
        ...MonacoThemeManager.auroraThemes.map(theme => ({id: theme.id, name: theme.name})),
    ];
    private static libraryThemes: readonly ThemeChoice[] = [];

    /** The themes NetPad itself provides, in the order the settings picker lists them. */
    public static getNetPadThemes(): readonly ThemeChoice[] {
        return this.netPadThemes;
    }

    /**
     * The themes that come from the `monaco-themes` library, alphabetically.
     */
    public static getLibraryThemes(): readonly ThemeChoice[] {
        return this.libraryThemes;
    }

    /**
     * Registers every theme the editor can use, and, when `settings` are given, eagerly loads
     * the data for the theme selected in settings so the first editor paints with it right away.
     */
    public static async initialize(settings?: Settings) {
        this.registration ??= this.registerThemes().catch(error => {
            this.registration = undefined;
            throw error;
        });

        await this.registration;

        if (settings) {
            await this.loadTheme(this.resolveThemeId(settings));
        }
    }

    private static async registerThemes() {
        // Register aurora themes
        for (const theme of this.auroraThemes) {
            this.registerTheme(new MonacoThemeInfo(
                theme.id,
                theme.name,
                undefined,
                undefined,
                () => buildAuroraTheme(theme.cssClass, theme.base)
            ));
        }

        // Register visual studio themes
        for (const theme of this.visualStudioThemes) {
            this.registerTheme(new MonacoThemeInfo(
                theme.id,
                "Visual Studio",
                buildVisualStudioTheme(theme.base)
            ));
        }

        // Register themes that come from the monaco-themes library
        const libThemeList = await import("monaco-themes/themes/themelist.json");
        const libThemes: ThemeChoice[] = [];

        for (const themeId in libThemeList) {
            const themeFileName = libThemeList[themeId as keyof typeof libThemeList] as string | undefined;

            if (typeof themeFileName !== "string") {
                continue;
            }

            this.registerTheme(new MonacoThemeInfo(themeId, themeFileName, undefined, themeFileName));
            libThemes.push({id: themeId, name: themeFileName});
        }

        this.libraryThemes = libThemes.sort((a, b) => a.name.localeCompare(b.name));
    }

    private static registerTheme(themeInfo: MonacoThemeInfo) {
        this.themes.set(themeInfo.id, themeInfo);
    }

    /**
     * Resolves the editor theme stored in settings to the id of a registered theme. The stored
     * value is one of: nothing (Auto, the current app theme's aurora theme), `"visual-studio"`
     * (VS editor colors, following light/dark mode), or a concrete theme id, returned as-is
     * unless nothing is registered for that id, in which case Auto is used.
     *
     * Only call this once {@link initialize} has run so themes have been registered and can be resolved.
     */
    public static resolveThemeId(settings: Settings): string {
        const picked = settings.editor.monacoOptions?.theme;
        const ground = AppTheme.resolveGround(settings.appearance.mode);
        const auto = this.auroraThemeId(AppTheme.resolveFamily(settings.appearance.themeFamily).id, ground);

        if (!picked) {
            return auto;
        }

        if (picked === this.adaptiveVisualStudioThemeId) {
            return this.visualStudioThemes.find(theme => theme.ground === ground)!.id;
        }

        return this.themes.has(picked) ? picked : auto;
    }

    public static async setTheme(editor: monaco.editor.IStandaloneCodeEditor, themeId: string, customizations?: {
        colors?: object,
        rules?: monaco.editor.ITokenThemeRule[]
    }): Promise<void> {
        await this.initialize();

        if (!this.themes.has(themeId)) {
            throw new Error(`No theme registered with id: ${themeId}`);
        }

        let theme = await this.loadTheme(themeId);

        // Apply user customizations if any
        if ((customizations?.colors && Object.keys(customizations.colors).length > 0) ||
            (customizations?.rules && customizations.rules.length > 0)) {
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

            // Define it as a new custom theme
            theme = new MonacoThemeInfo("custom", "Custom", customThemeData);
        }

        // Add or update the theme with monaco itself
        monaco.editor.defineTheme(theme.id, theme.data!);

        const currentOptions = editor.getRawOptions() as { theme: string };
        currentOptions.theme = theme.id;
        editor.updateOptions(currentOptions);
    }

    private static auroraThemeId(family: string, ground: ThemeGround): string {
        return `aurora-${family}-${ground}`;
    }

    private static async loadTheme(themeId: string): Promise<MonacoThemeInfo> {
        const theme = this.themes.get(themeId);

        if (!theme) {
            throw new Error(`No theme registered with the id: ${themeId}`);
        }

        if (theme.build) {
            theme.data = theme.build();
        } else {
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
        }

        this.normalizeThemeData(theme.data);

        return theme;
    }

    /**
     * A few library themes carry no token rules at all. Here we fill them with the default family's
     * aurora rules so that code is still syntax-colored instead of falling back to one flat foreground.
     */
    private static normalizeThemeData(themeData: monaco.editor.IStandaloneThemeData) {
        if (!themeData.rules || themeData.rules.length === 0) {
            const isDark = themeData.base === "vs-dark" || themeData.base === "hc-black";
            const aurora = this.auroraThemes.find(theme =>
                theme.family.id === AppTheme.defaultFamily && (theme.base === "vs-dark") === isDark)!;
            themeData.rules = buildAuroraTheme(aurora.cssClass, aurora.base).rules;
        }

        themeData.colors ??= {};
    }

    /**
     * Monaco themes come from the TextMate world and name none of the classifications the C#
     * language service emits, so tokens like `class` and `plainKeyword` would fall back to the
     * plain foreground. This fills them in from the nearest token the theme does define.
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
     * What {@link fillCompatibilityTokens} fills, and from where. Each entry is
     * `[sources, destinations]`: the first source the theme already defines supplies the color, and
     * only destinations the theme does not already define are added.
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
