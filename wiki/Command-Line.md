## Command line

```text
mdview [file.md] [more.md …] [options]
```

Several files open as tabs. Exit codes: `0` success, `2` bad arguments or a missing input.

### Appearance

| Switch | Meaning |
| --- | --- |
| `--mode <render\|source\|editor>` | start in a specific mode |
| `--theme <light\|dark\|auto>` | colour theme |
| `--zoom <1.25\|125%\|+10%>` | zoom factor |
| `--width <px>` | reading column width (380…2400); cannot exceed the pane |
| `--serif` / `--sans` | reading font family |
| `--numbering` | number the headings |
| `--toc` / `--no-toc` | outline sidebar |
| `--line-numbers`, `--wrap` / `--no-wrap` | code presentation |
| `--maximized`, `--fullscreen` | window state |
| `--position <x,y>` | window origin in screen coordinates (used by a dragged tab) |

### Navigation

| Switch | Meaning |
| --- | --- |
| `--line <n>` | jump to a line |
| `--anchor <id>` | jump to a heading anchor |

### Output

| Switch | Meaning |
| --- | --- |
| `--export <html\|text\|png\|md>` | export and exit, without a window |
| `--out <file>` | export target |
| `--print` | open the print dialog |

### Other

| Switch | Meaning |
| --- | --- |
| `--watch` / `--no-watch` | reload when the file changes on disk |
| `--lang <ru\|en>` | interface language |
| `--no-session` | ignore the saved session |
| `--reset-settings` | restore defaults and exit |
| `--diagnostics` | print environment information |
| `--version`, `--help` | version, help |

### Development switches

These exist for the screenshot and layout suites; they are documented because they are useful when
reporting a bug.

| Switch | Meaning |
| --- | --- |
| `--screenshot <file>`, `--screenshot-delay <ms>` | capture the client area and exit |
| `--ui-dump` | print control geometry, tabs and layout statistics |
| `--menu-dump` | open the File menu, print the button and menu rectangles in screen coordinates |
| `--theme-clicks <n>`, `--width-clicks <n>` | press the theme / width button n times |
| `--scroll <px>` | scroll the rendered view |
| `--prefs-shot <file>`, `--about-shot <file>` | capture a dialog |
| `--save-icon <file>` | write the generated application icon |
