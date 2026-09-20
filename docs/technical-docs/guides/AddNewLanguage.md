# Adding a new language

NetPad's UI is localized through a lightweight translation layer built into the frontend. English (`en`) is the
default and fallback language; Simplified Chinese (`zh-CN`) and Japanese (`ja`) are currently shipped as examples.
This guide explains how to add another language, e.g. French (`fr`) or German (`de`).

No backend changes are required. The work is limited to the frontend and takes roughly an hour for the initial pass.

### How localization works

A quick mental model before you start — four pieces cooperate:

1. **Resource files** — one flat JSON file per language in
   `src/Apps/NetPad.Apps.App/App/src/core/@application/i18n/` (`en.json`, `zh-CN.json`, `ja.json`). Every file maps
   the exact same set of dot-separated keys to translated strings.
2. **`TranslationService`** — holds the current language, resolves `t(key)` lookups, and registers languages.
3. **`TValueConverter`** — the `| t` value converter used in templates (e.g. `${'menu.file.new' | t}`). It
   re-evaluates automatically when the language changes.
4. **Settings** — the selected language is stored in app settings and broadcast to every window via the
   `SettingsUpdatedEvent`, so all windows switch together.

Lookups fall back in a fixed chain: **current language → `en` → the key itself**. This means a new language does not
have to be 100% complete on day one: any key you haven't translated yet silently shows the English text. The shipped
Japanese translation is intentionally partial and relies on this.

### Steps

1. Create the resource file. Copy `en.json` to `<locale>.json` in the same folder (e.g. `fr.json`), then translate
   the **values**. Keep the following strict:
    - Do not rename, add, or delete keys — the key set must mirror `en.json` exactly.
    - Keep `{placeholder}` tokens (e.g. `{scriptPath}`, `{message}`) intact and position them where the target
      language's grammar requires.
    - Keep the file flat: a single JSON object mapping `key` → `string`. No nested objects.
    - Save as UTF-8.
2. Register the language in `src/Apps/NetPad.Apps.App/App/src/core/@application/i18n/translation-service.ts`:
    - Import the new file and add it to the `RESOURCES` map.
    - Add an entry to `availableLanguages`. The `displayName` must be the language's **native name** (e.g.
      `"Français"`, `"Deutsch"`), not its English name — the selector shows it to speakers of that language.

   ```typescript
   import fr from "./fr.json";
   // ...
   const RESOURCES: Record<string, Record<string, string>> = {
       "en": en,
       "zh-CN": zhCN,
       "ja": ja,
       "fr": fr,
   };
   // ...
   public readonly availableLanguages: LanguageInfo[] = [
       {code: "en", displayName: "English"},
       {code: "zh-CN", displayName: "中文"},
       {code: "ja", displayName: "日本語"},
       {code: "fr", displayName: "Français"},
   ];
   ```

3. That's it — no other registration points exist. The language selector on the General settings page is built from
   `availableLanguages`, persistence goes through the existing `language` setting, and cross-window sync rides the
   existing `SettingsUpdatedEvent` broadcast.

### Translation conventions

| Convention | Detail |
|------------|--------|
| Fallback chain | Current language → `en` → the key itself. Missing keys are safe but should be filled in over time. |
| Interpolation | `{name}` placeholders are replaced via `t(key, {name: value})`. Never translate or reword the placeholder itself. |
| Key naming | Dot-separated by area: `menu.*`, `settings.*`, `ui.*`. Follow the existing prefix of the key you are translating. |
| Tone | Keep it short — most strings are menu items, button labels, and tooltips. Prefer the imperative for actions. |

A quick way to find untranslated keys in a partial locale:

```bash
node -e "const en=require('./src/Apps/NetPad.Apps.App/App/src/core/@application/i18n/en.json'),xx=require('./src/Apps/NetPad.Apps.App/App/src/core/@application/i18n/<locale>.json');console.log(Object.keys(en).filter(k=>!(k in xx)))"
```

### Test it

After the code changes are in place, test it:

1. Build check: `tsc --noEmit` must pass (a malformed JSON or missing registration fails here).
2. Run NetPad and open **Settings → General**. The new language should appear in the selector with its native name.
3. Select it and click **Apply**. All visible text in the current window should switch immediately; other windows
   should follow without interaction.
4. Verify untranslated keys display English text (expected fallback), not raw keys. Raw keys visible in the UI mean
   the key is missing from `en.json` too — fix that separately.
5. Restart NetPad. The language selection must persist.
6. Spot-check dynamic strings that interpolate values (e.g. the "Could not open script" alert) to confirm
   placeholders render correctly.

> If your language is not fully translated yet, that's fine — ship the partial translation and iterate.
