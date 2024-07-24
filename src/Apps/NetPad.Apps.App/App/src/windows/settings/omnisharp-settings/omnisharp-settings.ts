import {Settings} from "@application";
import {bindable} from "aurelia";

export class OmniSharpSettings {
    @bindable public settings: Settings;
    public options: OmniSharpOption[];

    public binding() {
        this.options = [
            new OmniSharpOption(
                "Enable Analyzers Support",
                "Enables support for roslyn analyzers",
                this.settings.omniSharp,
                nameof(this.settings.omniSharp, "enableAnalyzersSupport")
            ),
            new OmniSharpOption(
                "Enable Import Completion",
                "Enables support for showing unimported types and unimported extension methods in completion lists. When committed, the appropriate using directive will be added to namespaces. This option can have a negative impact on initial completion responsiveness, particularly for the first few completion sessions after opening a script.",
                this.settings.omniSharp,
                nameof(this.settings.omniSharp, "enableImportCompletion")
            ),
            new OmniSharpOption(
                "Enable Semantic Highlighting",
                "Enables Semantic Highlighting. Restart application for changes to take effect.",
                this.settings.omniSharp,
                nameof(this.settings.omniSharp, "enableSemanticHighlighting")
            ),
            new OmniSharpOption(
                "Enable CodeLens for References",
                "Enables CodeLens to show references. Restart application for changes to take effect.",
                this.settings.omniSharp,
                nameof(this.settings.omniSharp, "enableCodeLensReferences")
            ),
            new OmniSharpOption(
                "Diagnostics > Enabled",
                "Display diagnostics in editor. Restart application for changes to take effect.",
                this.settings.omniSharp.diagnostics,
                nameof(this.settings.omniSharp.diagnostics, "enabled")
            ),
            new OmniSharpOption(
                "Diagnostics > Information: Enabled",
                "Display information diagnostics in editor. Restart application or make an edit for changes to take effect.",
                this.settings.omniSharp.diagnostics,
                nameof(this.settings.omniSharp.diagnostics, "enableInfo")
            ),
            new OmniSharpOption(
                "Diagnostics > Warning: Enabled",
                "Display warning diagnostics in editor. Restart application or make an edit for changes to take effect.",
                this.settings.omniSharp.diagnostics,
                nameof(this.settings.omniSharp.diagnostics, "enableWarnings")
            ),
            new OmniSharpOption(
                "Diagnostics > Hints: Enabled",
                "Display hint diagnostics (such as 'unreachable code') in editor. Restart application or make an edit for changes to take effect.",
                this.settings.omniSharp.diagnostics,
                nameof(this.settings.omniSharp.diagnostics, "enableHints")
            ),
            new OmniSharpOption(
                "Inlay Hints > Parameters: Enabled",
                "Display inline parameter name hints",
                this.settings.omniSharp.inlayHints,
                nameof(this.settings.omniSharp.inlayHints, "enableParameters")
            ),
            new OmniSharpOption(
                "Inlay Hints > Parameters: For Indexer Parameters",
                "Show hints for indexers",
                this.settings.omniSharp.inlayHints,
                nameof(this.settings.omniSharp.inlayHints, "enableIndexerParameters")
            ),
            new OmniSharpOption(
                "Inlay Hints > Parameters: For Literal Parameters",
                "Show hints for literals",
                this.settings.omniSharp.inlayHints,
                nameof(this.settings.omniSharp.inlayHints, "enableLiteralParameters")
            ),
            new OmniSharpOption(
                "Inlay Hints > Parameters: For Object Creation Parameters",
                "Show hints for 'new' expressions",
                this.settings.omniSharp.inlayHints,
                nameof(this.settings.omniSharp.inlayHints, "enableObjectCreationParameters")
            ),
            new OmniSharpOption(
                "Inlay Hints > Parameters: For Other Parameters",
                "Show hints for everything else",
                this.settings.omniSharp.inlayHints,
                nameof(this.settings.omniSharp.inlayHints, "enableOtherParameters")
            ),
            new OmniSharpOption(
                "Inlay Hints > Parameters: Suppress For Parameters That Differ Only By Suffix",
                "Suppress hints when parameter names differ only by suffix",
                this.settings.omniSharp.inlayHints,
                nameof(this.settings.omniSharp.inlayHints, "suppressForParametersThatDifferOnlyBySuffix")
            ),
            new OmniSharpOption(
                "Inlay Hints > Parameters: Suppress For Parameters That Match Argument Name",
                "Suppress hints when argument matches parameter name",
                this.settings.omniSharp.inlayHints,
                nameof(this.settings.omniSharp.inlayHints, "suppressForParametersThatMatchArgumentName")
            ),
            new OmniSharpOption(
                "Inlay Hints > Parameters: Suppress For Parameters That Match Method Intent",
                "Suppress hints when parameter name matches the method's intent",
                this.settings.omniSharp.inlayHints,
                nameof(this.settings.omniSharp.inlayHints, "suppressForParametersThatMatchMethodIntent")
            ),
            new OmniSharpOption(
                "Inlay Hints > Types: Enabled",
                "Display inline type hints",
                this.settings.omniSharp.inlayHints,
                nameof(this.settings.omniSharp.inlayHints, "enableTypes")
            ),
            new OmniSharpOption(
                "Inlay Hints > Types: For Implicit Object Creation",
                "Show hints for implicit object creation",
                this.settings.omniSharp.inlayHints,
                nameof(this.settings.omniSharp.inlayHints, "enableImplicitObjectCreation")
            ),
            new OmniSharpOption(
                "Inlay Hints > Types: For Variable Types",
                "Show hints for variables with inferred types",
                this.settings.omniSharp.inlayHints,
                nameof(this.settings.omniSharp.inlayHints, "enableImplicitVariableTypes")
            ),
            new OmniSharpOption(
                "Inlay Hints > Types: For Lambda Parameter Types",
                "Show hints for lambda parameter types",
                this.settings.omniSharp.inlayHints,
                nameof(this.settings.omniSharp.inlayHints, "enableLambdaParameterTypes")
            ),
        ]
    }
}

class OmniSharpOption {
    constructor(
        public label: string,
        public description: string | null,
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        public obj: any,
        public prop: string) {
    }

    public get value(): boolean {
        return this.obj[this.prop] as boolean;
    }

    public set value(value) {
        this.obj[this.prop] = value;
    }
}
