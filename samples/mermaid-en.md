# Mermaid — everything that is supported

## Flowchart (graph / flowchart)

```mermaid
graph TD
    A[Open a file] --> B{Parse}
    B -->|ok| C[Lay out]
    B -->|error| D[Show diagnostics]
    C --> E[Paint]
```

## Sequence diagram

```mermaid
sequenceDiagram
    autonumber
    participant U as User
    participant A as Application
    participant F as File
    U->>A: open notes.md
    A->>F: read all text
    F-->>A: contents
    A->>A: parse and lay out
    A-->>U: the finished window
    Note right of A: render 220 ms
    loop every 2 seconds
        F-->>A: file changed
        A-->>U: repaint
    end
```

## Pie chart

```mermaid
pie showData title Development time
    "Parser" : 34
    "Layout" : 28
    "Painting" : 18
    "Editor" : 12
    "Tests and docs" : 8
```

## Class diagram

```mermaid
classDiagram
    class Block {
        <<abstract>>
        +int LineNumber
        +Paint()
    }
    class ParagraphBlock {
        +Inline[] Inlines
    }
    class CodeBlock {
        +string Text
        +string Language
    }
    Block <|-- ParagraphBlock
    Block <|-- CodeBlock
    class LayoutEngine {
        +Layout(document, width)
        -LayoutBlocks()
    }
    LayoutEngine --> Block : measures
```

## State diagram

```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> Parsing : open a file
    Parsing --> Layout : success
    Parsing --> Error : exception
    Layout --> Idle : done
    Error --> Idle : close
    Error --> [*]
```

## Gantt chart

```mermaid
gantt
    title Release plan 1.0
    dateFormat YYYY-MM-DD
    section Core
    Markdown parser      :a1, 2026-10-20, 12d
    Layout and painting  :a2, after a1, 10d
    section Shell
    Window and modes     :b1, 2026-11-01, 8d
    Editor               :b2, after b1, 6d
    section Ship
    Tests                :c1, 2026-11-10, 5d
    Release              :milestone, m1, 2026-11-16, 0d
```

## User journey

```mermaid
journey
    title Opening a document
    section Start
      Double-click a .md file: 5: User
      The window appears: 4: User
    section Reading
      Scrolling: 5: User
      Searching the text: 4: User, Keyboard
    section Editing
      Switching to the editor: 3: User
      Saving: 5: User
```

## What is not supported

```mermaid
erDiagram
    CUSTOMER ||--o{ ORDER : places
```

Such types are shown as their source with an explanation — and never break the page.
