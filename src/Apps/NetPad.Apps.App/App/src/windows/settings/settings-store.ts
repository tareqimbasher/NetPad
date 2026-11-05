import {RoutedPages} from "@application/windowing/routed-pages";
import {RailNavPage} from "@application/ui/rail-nav/np-rail-nav";

export class SettingsStore extends RoutedPages<RailNavPage> {
    public readonly identityParams: string[] = [];

    constructor() {
        super([
            {route: "general", text: "General", icon: "settings"},
            {route: "editor", text: "Editor", icon: "code"},
            {route: "results", text: "Results", icon: "results"},
            {route: "style", text: "Custom CSS", icon: "custom-css"},
            {route: "keyboard-shortcuts", text: "Shortcuts", icon: "keyboard"},
            {route: "omnisharp", text: "OmniSharp", icon: "code-intelligence"},
            {route: "about", text: "About", icon: "info"},
        ]);
    }
}
