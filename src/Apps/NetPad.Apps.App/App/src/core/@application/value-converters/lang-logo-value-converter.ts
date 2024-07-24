import {ScriptKind} from "@application";

export class LangLogoValueConverter {
    public toView(scriptKind: ScriptKind): string | null {
        if (scriptKind === "Program" || scriptKind == "Expression") {
            return "img/csharp-logo.png";
        } else if (scriptKind === "SQL") {
            return "img/sql-logo.svg";
        }
        else return null;
    }
}
