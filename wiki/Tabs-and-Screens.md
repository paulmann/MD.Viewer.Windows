## Tabs and screens

### Tabs

Every document lives in a tab. **Ctrl+Tab** switches, **Ctrl+W** closes, a **middle click** closes
the pill under the pointer. Many tabs shrink instead of running off the window.

New files open in tabs by default. **View → Settings…** (`Ctrl+,`) switches to *open in a new
window*; that preference only affects files opened from the interface — start-up, session restore and
the command line always stay in the window that is already running.

### Dragging a tab

| You drag a tab… | What happens |
| --- | --- |
| to another monitor / the desktop | the document opens in a new window **exactly where you dropped it**, and leaves the tab strip |
| onto another MD Viewer window | it becomes a **tab of that window** and disappears from the source window |
| back onto its own window | nothing — no duplicate, no lost document |
| with unsaved changes | the file is saved first; if saving fails, the tab stays where it was |

An untitled document cannot be moved (there is no file to hand over) — it stays in its window.

How it works, for the curious: the drag carries the path plus the window handle in a custom
clipboard format (with a prefixed text fallback). The receiving window answers `Move` (“I took it”) or
`Link` (“keep it where it is”); nothing accepting the drop means the document gets its own window at
the drop point via `--position`.

### Width of the reading column

The button next to the magnifier cycles **100 % → 75 % → 50 %** of the free pane. 100 % is measured
*after* the outline sidebar, so a 2560-pixel screen is used instead of a narrow column. The choice is
remembered; `--width <px>` still pins an explicit column for one session.

### Monitors

Menus open on the monitor where the window lives. The File menu is anchored in screen coordinates
and, after opening, verified: if a dropdown ended up on another monitor it is moved back and kept
inside that monitor's working area.

The window itself is restored inside the virtual desktop, so unplugging a monitor cannot leave the
window off-screen.
