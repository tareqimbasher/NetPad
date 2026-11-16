import {DI, ILogger, Registration} from "aurelia";
import {IDialogDom} from "@aurelia/dialog";
import {
    AppDependencyCheckResult,
    DotNetFrameworkVersion,
    DotNetPathReport,
    DotNetSdkVersion,
    IAppService,
    SemanticVersion,
} from "@application";
import {AppDependenciesCheckDialog} from "@application/app/app-dependencies-check-dialog/app-dependencies-check-dialog";

// The barrel loads components, which need their templates.
jest.mock("@application", () => ({
    ...jest.requireActual("@application/api"),
    ...jest.requireActual("@application/app/iapp-service"),
}));
jest.mock(
    "@application/app/app-dependencies-check-dialog/app-dependencies-check-dialog.html",
    () => ({name: "app-dependencies-check-dialog", template: ""}));

const sdk = (version: string) => new DotNetSdkVersion({
    version: new SemanticVersion({string: version, major: parseInt(version)} as SemanticVersion),
    dotNetRootDirectory: "/usr/share/dotnet",
});

const result = (
    sdkVersions: string[],
    missingAspNetRuntime: DotNetFrameworkVersion[],
    canRunScripts: boolean,
) => {
    const sdks = sdkVersions.map(sdk);
    return new AppDependencyCheckResult({
        dotNetRuntimeVersion: "10.0.1",
        dotNetSdkVersions: sdks,
        supportedDotNetSdkVersionsInstalled: sdks,
        frameworkVersionsMissingAspNetRuntime: missingAspNetRuntime,
        canRunScripts,
    });
};

const settle = () => new Promise(resolve => setTimeout(resolve));

async function openDialog(dependencyCheckResult: AppDependencyCheckResult) {
    const logger = {scopeTo: () => logger, debug: () => undefined};
    const dialogDom = {contentHost: document.createElement("div"), overlay: document.createElement("div")};
    const appService = {
        checkDependencies: () => Promise.resolve(dependencyCheckResult),
        getDotNetPathReport: () => Promise.resolve(new DotNetPathReport()),
    };

    const container = DI.createContainer().register(
        Registration.instance(ILogger, logger),
        Registration.instance(IDialogDom, dialogDom),
        Registration.instance(IAppService, appService),
    );

    const dialog = container.invoke(AppDependenciesCheckDialog);
    dialog.bound();
    await settle();
    return dialog;
}

describe("AppDependenciesCheckDialog", () => {
    test("reports the ASP.NET Core runtime installed when no supported SDK version is missing it", async () => {
        const dialog = await openDialog(result(["8.0.416", "9.0.304"], [], true));

        expect(dialog.aspNetRuntimeStatus).toBe("installed");
        expect(dialog.canRunScripts).toBe(true);
    });

    test("warns about the .NET versions missing the ASP.NET Core runtime while others can run scripts", async () => {
        const dialog = await openDialog(result(["8.0.416", "9.0.304", "10.0.402"], ["DotNet8", "DotNet9"], true));

        expect(dialog.aspNetRuntimeStatus).toBe("partial");
        expect(dialog.aspNetRuntimeMissingVersions).toBe(".NET 8, .NET 9");
        expect(dialog.canRunScripts).toBe(true);
    });

    test("reports the ASP.NET Core runtime missing when no installed SDK version can run scripts", async () => {
        const dialog = await openDialog(result(["9.0.304"], ["DotNet9"], false));

        expect(dialog.aspNetRuntimeStatus).toBe("missing");
        expect(dialog.aspNetRuntimeMissingVersions).toBe(".NET 9");
        expect(dialog.canRunScripts).toBe(false);
    });

    test("hides the ASP.NET Core runtime status and asks for the SDK when no supported SDK is installed", async () => {
        const dialog = await openDialog(result([], [], false));

        expect(dialog.dotnetSdkMissing).toBe(true);
        expect(dialog.aspNetRuntimeStatus).toBeUndefined();
        expect(dialog.canRunScripts).toBe(false);
    });
});
