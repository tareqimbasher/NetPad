import {bindable, BindingMode, ILogger} from "aurelia";
import {normalizeLogicalKey} from "@common";
import {ViewModelBase} from "@application";
import {ValueSelectOption} from "./value-select-option";

/**
 * A select dropdown where each option can carry a glyph and a secondary detail. Supports keyboard
 * navigation, and type-ahead.
 *
 * In `editable` mode the control is a text field that accepts values outside the offered set, and
 * the options act as suggestions filtered by what has been typed.
 */
export class NpValueSelect extends ViewModelBase {
    @bindable public options: ValueSelectOption[] = [];
    @bindable({mode: BindingMode.twoWay}) public value: unknown;
    @bindable public placeholder?: string;
    /** A leading glyph for the field itself, for when no option carries one. */
    @bindable public icon?: string;
    /** A trailing mono note about the field's state (ex: "3 more loaded"). */
    @bindable public hint?: string;
    /** A note under the options (ex: "Add to see more"). */
    @bindable public footer?: string;
    @bindable public disabled?: boolean;
    /** Swaps the leading glyph for a spinner. */
    @bindable public loading?: boolean;
    @bindable public loadingText = "loading…";
    @bindable public emptyText = "No options";
    @bindable public editable?: boolean;
    /** Opens the popup as soon as the control is attached, for a window whose first act is a choice. */
    @bindable public autoOpen?: boolean;
    /** Names the control for assistive tech. */
    @bindable public label?: string;

    public isOpen = false;
    public activeIndex = -1;
    /** Whether the value has been edited since the popup opened. */
    public typedSinceOpened = false;
    public readonly listboxId = `vs-list-${++NpValueSelect.instanceCount}`;

    private static instanceCount = 0;
    private static readonly typeAheadWindowMs = 700;

    private control: HTMLElement;
    private popup: HTMLElement;
    private typeAhead = "";
    private typeAheadResetHandle?: number;

    constructor(private readonly element: HTMLElement, @ILogger logger: ILogger) {
        super(logger);
    }

    public attached() {
        const documentMouseDownHandler = (ev: MouseEvent) => {
            if (!this.element.contains(ev.target as Node)) {
                this.close();
            }
        };
        document.addEventListener("mousedown", documentMouseDownHandler);
        this.addDisposable(() => document.removeEventListener("mousedown", documentMouseDownHandler));

        // Popup is fixed position so we have to reposition it with the control.
        const repositionHandler = (ev: Event) => {
            if (!this.isOpen || this.popup.contains(ev.target as Node)) {
                return;
            }
            this.positionPopup();
        };
        document.addEventListener("scroll", repositionHandler, true);
        window.addEventListener("resize", repositionHandler);
        this.addDisposable(() => document.removeEventListener("scroll", repositionHandler, true));
        this.addDisposable(() => window.removeEventListener("resize", repositionHandler));

        const windowBlurHandler = () => this.close();
        window.addEventListener("blur", windowBlurHandler);
        this.addDisposable(() => window.removeEventListener("blur", windowBlurHandler));

        if (this.autoOpen && !this.disabled) {
            this.control.focus();
            this.open();
        }
    }

    /** Id of the focusable control, so an external `<label for="...">` can reach it (read via `view-model.ref`). */
    public get controlElementId(): string {
        return `${this.listboxId}-control`;
    }

    public get selectedOption(): ValueSelectOption | undefined {
        return this.options?.find(o => o.value === this.value);
    }

    public get visibleOptions(): ValueSelectOption[] {
        const options = this.options ?? [];

        if (!this.editable || !this.typedSinceOpened) {
            return options;
        }

        const typed = typeof this.value === "string" ? this.value.trim().toLowerCase() : "";
        return typed ? options.filter(o => o.label.toLowerCase().includes(typed)) : options;
    }

    public get displayText(): string {
        return this.selectedOption?.label ?? (typeof this.value === "string" ? this.value : "");
    }

    public get leadingIcon(): string | undefined {
        return this.selectedOption?.icon ?? this.icon;
    }

    public get effectivePlaceholder(): string | undefined {
        return this.loading ? this.loadingText : this.placeholder;
    }

    public get emptyMessage(): string {
        if (this.loading) {
            return this.loadingText;
        }

        return this.options?.length ? "No matches" : this.emptyText;
    }

    public toggle() {
        if (this.isOpen) {
            this.close();
        } else {
            this.open();
        }
    }

    public open() {
        if (this.disabled || this.isOpen) {
            return;
        }

        this.isOpen = true;
        this.typedSinceOpened = false;
        this.activeIndex = this.visibleOptions.indexOf(this.selectedOption as ValueSelectOption);

        // Position after the popup has been laid out to capture its painted size.
        requestAnimationFrame(() => {
            if (this.isOpen) {
                this.positionPopup();
                this.scrollActiveIntoView();
            }
        });
    }

    public close() {
        if (!this.isOpen) {
            return;
        }

        this.isOpen = false;
        this.activeIndex = -1;
    }

    public select(option: ValueSelectOption) {
        this.value = option.value;
        this.close();
        this.control.focus();
    }

    public handleKeyDown(event: KeyboardEvent) {
        if (this.disabled) {
            return;
        }

        const consume = () => {
            event.preventDefault();
            event.stopPropagation();
        };

        switch (normalizeLogicalKey(event.key)) {
            case "ArrowDown":
                if (!this.isOpen) this.open();
                else this.moveActive(1);
                consume();
                return;
            case "ArrowUp":
                if (!this.isOpen) this.open();
                else this.moveActive(-1);
                consume();
                return;
            case "Home":
                if (this.isOpen && !this.editable) {
                    this.setActive(0);
                    consume();
                }
                return;
            case "End":
                if (this.isOpen && !this.editable) {
                    this.setActive(this.visibleOptions.length - 1);
                    consume();
                }
                return;
            case "Enter": {
                if (this.isOpen) {
                    const active = this.visibleOptions[this.activeIndex];
                    if (active) this.select(active);
                    else this.close();
                    consume();
                } else if (!this.editable) {
                    this.open();
                    consume();
                }
                return;
            }
            case "Space":
                if (!this.editable && !this.isOpen) {
                    this.open();
                    consume();
                }
                return;
            case "Escape":
                if (this.isOpen) {
                    this.close();
                    consume();
                }
                return;
            case "Tab":
                this.close();
                return;
            default:
                if (!this.editable && this.handleTypeAhead(event)) {
                    consume();
                }
        }
    }

    public handleInput() {
        if (!this.isOpen) {
            this.open();
        } else {
            this.activeIndex = -1;
        }

        this.typedSinceOpened = true;
    }

    private handleTypeAhead(event: KeyboardEvent): boolean {
        if (event.key.length !== 1 || event.ctrlKey || event.altKey || event.metaKey) {
            return false;
        }

        this.typeAhead += event.key.toLowerCase();

        if (this.typeAheadResetHandle) {
            window.clearTimeout(this.typeAheadResetHandle);
        }
        this.typeAheadResetHandle = window.setTimeout(() => this.typeAhead = "", NpValueSelect.typeAheadWindowMs);

        // Jump to the option that matches the typed text
        const match = this.options?.findIndex(o => o.label.toLowerCase().startsWith(this.typeAhead)) ?? -1;
        if (match >= 0) {
            if (this.isOpen) {
                this.setActive(match);
            } else {
                this.select(this.options[match]);
            }
        }

        return true;
    }

    private moveActive(delta: number) {
        const count = this.visibleOptions.length;
        if (!count) {
            return;
        }

        const next = this.activeIndex < 0
            ? (delta > 0 ? 0 : count - 1)
            : (this.activeIndex + delta + count) % count;

        this.setActive(next);
    }

    private setActive(index: number) {
        this.activeIndex = index;
        this.scrollActiveIntoView();
    }

    private scrollActiveIntoView() {
        if (this.activeIndex < 0) {
            return;
        }

        const rows = this.popup?.querySelectorAll(".vs-row");
        rows?.item(this.activeIndex)?.scrollIntoView({block: "nearest"});
    }

    private positionPopup() {
        const gap = 6;
        const margin = 8;

        // App zoom in the browser and Tauri shells is CSS `zoom` on the body, which scales the
        // coordinate space a fixed child is positioned in. Measured rects are in screen pixels, so
        // they have to be divided back into that space.
        const zoom = parseFloat(getComputedStyle(document.body).zoom) || 1;
        const rect = this.control.getBoundingClientRect();
        const control = {
            top: rect.top / zoom,
            bottom: rect.bottom / zoom,
            left: rect.left / zoom,
            width: rect.width / zoom,
        };
        const viewportHeight = window.innerHeight / zoom;
        const viewportWidth = window.innerWidth / zoom;

        this.popup.style.minWidth = `${control.width}px`;

        const popupHeight = this.popup.offsetHeight;
        const spaceBelow = viewportHeight - control.bottom - gap - margin;
        const openUpward = popupHeight > spaceBelow && control.top - gap - margin > spaceBelow;

        this.popup.style.top = openUpward
            ? `${Math.max(margin, control.top - gap - popupHeight)}px`
            : `${control.bottom + gap}px`;

        const maxHeight = openUpward ? control.top - gap - margin : spaceBelow;
        this.popup.style.maxHeight = `${Math.max(120, maxHeight)}px`;

        const overflowRight = control.left + this.popup.offsetWidth - (viewportWidth - margin);
        this.popup.style.left = `${Math.max(margin, control.left - Math.max(0, overflowRight))}px`;
    }
}
