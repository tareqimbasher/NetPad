import {Constructable} from "@aurelia/kernel/src/interfaces";
import {KeyCode} from "@common";
import {ShortcutActionExecutionContext} from "./shortcut-action-execution-context";
import {KeyCombo} from "./key-combo";

/**
 * A shortcut that executes an action.
 */
export class Shortcut {
    public id: string;
    public name: string;
    public action?: (context: ShortcutActionExecutionContext) => void;
    public event?: Constructable | (() => (unknown | Promise<unknown>));
    public isConfigurable = false;
    public isEnabled = false;
    public keyCombo: KeyCombo;
    public defaultKeyCombo: KeyCombo;

    constructor(id: string, name: string) {
        this.id = id;
        this.name = name;
        this.keyCombo = new KeyCombo();
        this.defaultKeyCombo = new KeyCombo();
    }

    public withCtrlKey(required = true): Shortcut {
        this.keyCombo.withCtrlKey(required);
        return this;
    }

    public withAltKey(required = true): Shortcut {
        this.keyCombo.withAltKey(required);
        return this;
    }

    public withShiftKey(required = true): Shortcut {
        this.keyCombo.withShiftKey(required);
        return this;
    }

    public withMetaKey(required = true): Shortcut {
        this.keyCombo.withMetaKey(required);
        return this;
    }

    public withKey(key: KeyCode): Shortcut {
        this.keyCombo.withKey(key);
        return this;
    }

    public captureDefaultKeyCombo(): Shortcut {
        this.defaultKeyCombo = this.keyCombo.clone();
        return this;
    }

    public get isDefaultKeyCombo(): boolean {
        return this.defaultKeyCombo.matches(this.keyCombo);
    }

    public resetKeyCombo(): Shortcut {
        this.keyCombo.updateFrom(this.defaultKeyCombo);
        return this;
    }

    public hasAction(action: (context: ShortcutActionExecutionContext) => void): Shortcut {
        this.action = action;
        return this;
    }

    public firesEvent(eventGetter: () => (unknown | Promise<unknown>)): Shortcut;
    public firesEvent<TEventType extends Constructable>(eventType: TEventType): Shortcut;

    public firesEvent<TEventType extends Constructable>(eventTypeOrGetter: TEventType | (() => (unknown | Promise<unknown>))): Shortcut {
        this.event = eventTypeOrGetter;
        return this;
    }

    public configurable(isConfigurable = true): Shortcut {
        this.isConfigurable = isConfigurable;
        return this;
    }

    public enabled(isEnabled = true): Shortcut {
        this.isEnabled = isEnabled;
        return this;
    }

    public toString(): string {
        return `${this.name} (${this.keyCombo.toString()})`;
    }
}
