import * as monaco from "monaco-editor";
import {buildAuroraTheme} from "./aurora-theme";
import {buildVisualStudioTheme} from "./visual-studio-theme";

/**
 * Visual Studio's syntax colors with the current aurora theme's background.
 */
export function buildVisualStudioPaletteTheme(
    themeCssClasses: string,
    base: "vs" | "vs-dark"): monaco.editor.IStandaloneThemeData {
    return {
        ...buildAuroraTheme(themeCssClasses, base),
        rules: buildVisualStudioTheme(base).rules,
    };
}
