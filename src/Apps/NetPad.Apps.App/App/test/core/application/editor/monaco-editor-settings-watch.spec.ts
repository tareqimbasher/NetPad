import {PLATFORM} from "aurelia";
import {setPlatform, TestContext} from "@aurelia/testing";
import {IDisposable} from "@common";
import {Settings} from "@application/api";
import {EditorThemeSettings, MonacoEditorUtil} from "@application/editor/monaco/monaco-editor-util";
import {MonacoThemeManager} from "@application/editor/monaco/monaco-theme-manager";
import {editorThemeSettings} from "../../../../src/windows/settings/editor-theme-settings";

setPlatform(PLATFORM);

let systemGroundListeners: (() => void)[];

const flush = () => new Promise<void>(resolve => queueMicrotask(resolve));
const switchSystemGround = () => systemGroundListeners.forEach(listener => listener());

const createSettings = () => {
    const settings = new Settings();
    settings.appearance.themeFamily = "inkwell";
    settings.appearance.mode = "Dark";
    settings.editor.monacoOptions = {};
    return settings;
};

beforeEach(() => {
    systemGroundListeners = [];
    window.matchMedia = jest.fn().mockReturnValue({
        addEventListener: (_: string, listener: () => void) => systemGroundListeners.push(listener),
        removeEventListener: (_: string, listener: () => void) =>
            systemGroundListeners.splice(systemGroundListeners.indexOf(listener), 1),
    });
});

describe("MonacoEditorUtil.onEditorSettingsChanged", () => {
    let settings: Settings;
    let calls: number;
    let watch: IDisposable;

    beforeEach(() => {
        settings = createSettings();

        calls = 0;
        watch = MonacoEditorUtil.onEditorSettingsChanged(
            TestContext.create().observerLocator,
            settings,
            () => calls++);
    });

    afterEach(() => watch.dispose());

    test.each([
        ["theme family", () => settings.appearance.themeFamily = "gunmetal"],
        ["mode", () => settings.appearance.mode = "Light"],
        ["background", () => settings.appearance.background = "Neutral"],
        ["Monaco options", () => settings.editor.monacoOptions = {fontSize: 16}],
        ["custom CSS", () => settings.styles.customCss = ".x {}"],
        ["custom CSS switch", () => settings.styles.enabled = !settings.styles.enabled],
    ])("calls back on a change to the %s", async (_, change) => {
        change();
        await flush();

        expect(calls).toBe(1);
    });

    test("calls back once for a settings update that replaces several values", async () => {
        settings.init({
            ...settings.toJSON(),
            appearance: {...settings.appearance.toJSON(), themeFamily: "gunmetal", mode: "Light"},
            editor: {...settings.editor.toJSON(), monacoOptions: {fontSize: 16}},
        });
        await flush();

        expect(calls).toBe(1);
    });

    test("calls back when the machine switches between light and dark in System mode", async () => {
        settings.appearance.mode = "System";
        await flush();
        calls = 0;

        switchSystemGround();
        await flush();

        expect(calls).toBe(1);
    });

    test("ignores the machine switching between light and dark outside System mode", async () => {
        switchSystemGround();
        await flush();

        expect(calls).toBe(0);
    });

    test("does not call back once disposed, even for a change made before", async () => {
        settings.appearance.mode = "Light";
        watch.dispose();
        await flush();

        settings.appearance.mode = "Dark";
        switchSystemGround();
        await flush();

        expect(calls).toBe(0);
        expect(systemGroundListeners).toHaveLength(0);
    });
});

describe("the settings window's editor theme settings", () => {
    let saved: Settings;
    let editable: Settings;
    let themeSettings: EditorThemeSettings;
    let calls: number;
    let watch: IDisposable;

    beforeEach(() => {
        saved = createSettings();
        editable = saved.clone();
        themeSettings = editorThemeSettings(saved, editable);

        calls = 0;
        watch = MonacoEditorUtil.onEditorSettingsChanged(
            TestContext.create().observerLocator,
            themeSettings,
            () => calls++);
    });

    afterEach(() => watch.dispose());

    test.each([
        ["saved theme family", 1, () => saved.appearance.themeFamily = "gunmetal"],
        ["edited Monaco options", 1, () => editable.editor.monacoOptions = {fontSize: 16}],
        ["edited theme family", 0, () => editable.appearance.themeFamily = "gunmetal"],
        ["saved Monaco options", 0, () => saved.editor.monacoOptions = {fontSize: 16}],
    ])("a change to the %s makes %i call(s)", async (_, expected, change) => {
        change();
        await flush();

        expect(calls).toBe(expected);
    });

    test("follows a save that replaces the saved appearance", async () => {
        saved.init({
            ...saved.toJSON(),
            appearance: {...saved.appearance.toJSON(), mode: "Light"},
        });
        await flush();

        expect(calls).toBe(1);

        saved.appearance.themeFamily = "gunmetal";
        await flush();

        expect(calls).toBe(2);
    });

    test("resolves the automatic theme from the saved appearance", () => {
        saved.appearance.themeFamily = "gunmetal";
        saved.appearance.mode = "Light";

        expect(MonacoThemeManager.resolveThemeId(themeSettings)).toBe("aurora-gunmetal-light");
    });

    test("resolves a theme picked in the edited options against the saved appearance", () => {
        saved.appearance.mode = "Light";
        editable.editor.monacoOptions = {theme: MonacoThemeManager.adaptiveVisualStudioThemeId};

        expect(MonacoThemeManager.resolveThemeId(themeSettings)).toBe("visual-studio-light");
    });
});
