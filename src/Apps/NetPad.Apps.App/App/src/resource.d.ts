declare module '*.html' {
    import {IContainer, PartialBindableDefinition} from 'aurelia';
    export const name: string;
    export const template: string;
    export default template;
    export const dependencies: string[];
    export const containerless: boolean | undefined;
    export const bindables: Record<string, PartialBindableDefinition>;
    export const shadowOptions: { mode: 'open' | 'closed' } | undefined;

    export function register(container: IContainer): void;
}

declare module '*.css';
declare module '*.scss';

// All JSON is handled natively by webpack (at runtime); TS only provides types.
// We don't use resolveJsonModule: it makes the TS language service parse the JSON
// file itself, which triggers a TS 5.9 path-separator Debug Failure under Windows
// with non-ASCII (Chinese) paths (the build errors out right after you edit a JSON file).
// Currently only i18n translation dictionaries use this, and they're all flat string maps;
// when nested-structure JSON is introduced later, we'll declare types for it separately.
declare module '*.json' {
    const value: Record<string, string>;
    export default value;
}
