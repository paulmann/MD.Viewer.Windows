# Command line reference

```
mdview [file.md] [options]
```

Everything is optional. With no arguments the last session is restored (up to eight files);
`--no-session` disables that.

## Opening and navigation

| Option | Alias | Meaning |
| --- | --- | --- |
| `file.md` | — | Open this file; extra paths open as additional tabs |
| `--mode <render\|source\|editor>` | `-m`, `--view` | Start in a view mode (`raw`, `text`, `preview` accepted) |
| `--line <n>` | `-l` | Jump to line `n` (source/editor) or the section that contains it |
| `--anchor <id>` | `-a` | Scroll to a heading id |
| `--toc` / `--no-toc` | `--outline`, `--sidebar` | Show or hide the outline panel |
| `--maximized` | `--max` | Start maximized |
| `--fullscreen` | — | Borderless maximized window |

## Appearance

| Option | Meaning |
| --- | --- |
| `--theme <light\|dark\|auto>` | Colour theme; `auto` follows the Windows app mode |
| `--zoom <1.25>` | Absolute factor |
| `--zoom <125%>` | Percentage |
| `--zoom <+10%>` | Relative to the current value |
| `--width <px>` | Reading column width, clamped to 380…2400 |
| `--serif` / `--sans` | Reading font family |
| `--numbering` / `--no-numbering` | Number headings |
| `--line-numbers` / `--no-line-numbers` | Line numbers in source and editor |
| `--wrap` / `--no-wrap` | Wrap long code lines |

## Output

| Option | Meaning |
| --- | --- |
| `--export <html\|text\|png\|md>` | Export and exit, without opening a window |
| `--out <file>` | Export target; defaults to the source name with a new extension |
| `--print` | Open the print dialog with the document rendered |
| `--screenshot <file>` | Show the window, capture the client area, exit (development) |
| `--screenshot-delay <ms>` | Wait before capturing (default 700) |
| `--ui-dump` | Print the live control geometry and exit (development) |

## Other

| Option | Meaning |
| --- | --- |
| `--watch` / `--no-watch` | Reload when the file changes on disk |
| `--lang <ru\|en>` | Interface language; default follows the OS |
| `--no-session` | Ignore the saved session |
| `--reset-settings` | Restore defaults and exit |
| `--diagnostics` | Print version, runtime, fonts, DPI, theme and settings path |
| `--version`, `-v` | Print the version |
| `--help`, `-h`, `-?` | Print this reference |

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Success (including GUI runs and exports) |
| `2` | Bad argument, unknown option, or missing input file |

## Notes

* The executable is a GUI-subsystem app, but it attaches to the parent console so `--version`,
  `--export` and `--diagnostics` print into PowerShell or `cmd`.
* A GUI process does not block PowerShell's `&`, so use `Start-Process -Wait` in scripts when the
  next step depends on the window being closed.
* Unknown options and impossible values are reported and the help text is printed; the app never
  starts with a silently wrong configuration.
