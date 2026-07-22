import {bindable} from "aurelia";
import {Settings} from "@application";
import {INativeDialogService} from "@application/dialogs/inative-dialog-service";

export class OmniSharpSettings {
    @bindable public settings: Settings;
    public currentSettings: Readonly<Settings>;
    public groups: OmniSharpOptionGroup[];

    constructor(
        currentSettings: Settings,
        @INativeDialogService private readonly nativeDialogService: INativeDialogService) {
        this.currentSettings = currentSettings;
    }

    public binding() {
        const omniSharp = this.settings.omniSharp;
        const current = this.currentSettings.omniSharp;

        this.groups = [
            new OmniSharpOptionGroup("Code intelligence", [
                new OmniSharpOption(
                    "Roslyn analyzers",
                    "Run analyzer diagnostics in the editor.",
                    omniSharp, current, nameof(omniSharp, "enableAnalyzersSupport")),
                new OmniSharpOption(
                    "Import completion",
                    "Suggest unimported types and extension methods, adding the using directive when one is committed. The first completions after opening a script may be slower.",
                    omniSharp, current, nameof(omniSharp, "enableImportCompletion")),
                new OmniSharpOption(
                    "Semantic highlighting",
                    "Color identifiers by what they mean, not just by syntax.",
                    omniSharp, current, nameof(omniSharp, "enableSemanticHighlighting"), {needsAppRestart: true}),
                new OmniSharpOption(
                    "CodeLens references",
                    "Show reference counts above declarations.",
                    omniSharp, current, nameof(omniSharp, "enableCodeLensReferences"), {needsAppRestart: true}),
            ]),

            new OmniSharpOptionGroup("Diagnostics", [
                new OmniSharpOption(
                    "Show diagnostics",
                    "Squiggles and problem markers in the editor.",
                    omniSharp.diagnostics, current.diagnostics,
                    nameof(omniSharp.diagnostics, "enabled"), {needsAppRestart: true}),
                new OmniSharpOption(
                    "Information", null,
                    omniSharp.diagnostics, current.diagnostics,
                    nameof(omniSharp.diagnostics, "enableInfo"), {sub: true}),
                new OmniSharpOption(
                    "Warnings", null,
                    omniSharp.diagnostics, current.diagnostics,
                    nameof(omniSharp.diagnostics, "enableWarnings"), {sub: true}),
                new OmniSharpOption(
                    "Hints", "Suggestions like \"unreachable code\".",
                    omniSharp.diagnostics, current.diagnostics,
                    nameof(omniSharp.diagnostics, "enableHints"), {sub: true}),
            ]),

            new OmniSharpOptionGroup("Inlay hints", [
                new OmniSharpOption(
                    "Parameter names",
                    "Inline hints for argument names.",
                    omniSharp.inlayHints, current.inlayHints,
                    nameof(omniSharp.inlayHints, "enableParameters")),
                new OmniSharpOption(
                    "For indexer arguments", null,
                    omniSharp.inlayHints, current.inlayHints,
                    nameof(omniSharp.inlayHints, "enableIndexerParameters"), {sub: true}),
                new OmniSharpOption(
                    "For literal arguments", null,
                    omniSharp.inlayHints, current.inlayHints,
                    nameof(omniSharp.inlayHints, "enableLiteralParameters"), {sub: true}),
                new OmniSharpOption(
                    "For object creation", null,
                    omniSharp.inlayHints, current.inlayHints,
                    nameof(omniSharp.inlayHints, "enableObjectCreationParameters"), {sub: true}),
                new OmniSharpOption(
                    "For everything else", null,
                    omniSharp.inlayHints, current.inlayHints,
                    nameof(omniSharp.inlayHints, "enableOtherParameters"), {sub: true}),
                new OmniSharpOption(
                    "Hide when names differ only by suffix", null,
                    omniSharp.inlayHints, current.inlayHints,
                    nameof(omniSharp.inlayHints, "suppressForParametersThatDifferOnlyBySuffix"), {sub: true}),
                new OmniSharpOption(
                    "Hide when the argument matches the parameter name", null,
                    omniSharp.inlayHints, current.inlayHints,
                    nameof(omniSharp.inlayHints, "suppressForParametersThatMatchArgumentName"), {sub: true}),
                new OmniSharpOption(
                    "Hide when the name matches the method's intent", null,
                    omniSharp.inlayHints, current.inlayHints,
                    nameof(omniSharp.inlayHints, "suppressForParametersThatMatchMethodIntent"), {sub: true}),
                new OmniSharpOption(
                    "Type names",
                    "Inline hints for inferred types.",
                    omniSharp.inlayHints, current.inlayHints,
                    nameof(omniSharp.inlayHints, "enableTypes")),
                new OmniSharpOption(
                    "For variable types", null,
                    omniSharp.inlayHints, current.inlayHints,
                    nameof(omniSharp.inlayHints, "enableImplicitVariableTypes"), {sub: true}),
                new OmniSharpOption(
                    "For implicit object creation", null,
                    omniSharp.inlayHints, current.inlayHints,
                    nameof(omniSharp.inlayHints, "enableImplicitObjectCreation"), {sub: true}),
                new OmniSharpOption(
                    "For lambda parameter types", null,
                    omniSharp.inlayHints, current.inlayHints,
                    nameof(omniSharp.inlayHints, "enableLambdaParameterTypes"), {sub: true}),
            ]),
        ];
    }

    public async browseForExecutable() {
        const paths = await this.nativeDialogService.showFileSelectorDialog({
            title: "Select OmniSharp Executable",
            defaultPath: this.settings.omniSharp.executablePath || undefined,
        });

        if (paths?.length) this.settings.omniSharp.executablePath = paths[0];
    }
}

class OmniSharpOptionGroup {
    constructor(public label: string, public options: OmniSharpOption[]) {
    }
}

class OmniSharpOption {
    constructor(
        public title: string,
        public description: string | null,
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        private readonly obj: any,
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        private readonly currentObj: any,
        private readonly prop: string,
        private readonly traits?: {sub?: boolean; needsAppRestart?: boolean}) {
    }

    public get value(): boolean {
        return this.obj[this.prop] as boolean;
    }

    public set value(value) {
        this.obj[this.prop] = value;
    }

    /** Dependent settings sit indented under the toggle that governs them. */
    public get isSubOption(): boolean {
        return this.traits?.sub === true;
    }

    public get needsRestart(): boolean {
        return this.traits?.needsAppRestart === true && this.value !== this.currentObj[this.prop];
    }
}
