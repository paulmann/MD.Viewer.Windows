# Mermaid — всё, что поддерживается

## Блок-схема (graph / flowchart)

```mermaid
graph TD
    A[Открыть файл] --> B{Разобрать}
    B -->|ok| C[Сверстать]
    B -->|ошибка| D[Показать диагностику]
    C --> E[Нарисовать]
```

## Диаграмма последовательности

```mermaid
sequenceDiagram
    autonumber
    participant U as Пользователь
    participant A as Приложение
    participant F as Файл
    U->>A: открыть notes.md
    A->>F: read all text
    F-->>A: содержимое
    A->>A: разобрать и сверстать
    A-->>U: готовое окно
    Note right of A: рендер 220 мс
    loop каждые 2 секунды
        F-->>A: изменение файла
        A-->>U: перерисовать
    end
```

## Круговая диаграмма

```mermaid
pie showData title Время разработки
    "Парсер" : 34
    "Вёрстка" : 28
    "Отрисовка" : 18
    "Редактор" : 12
    "Тесты и документация" : 8
```

## Диаграмма классов

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

## Диаграмма состояний

```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> Parsing : открыть файл
    Parsing --> Layout : успех
    Parsing --> Error : исключение
    Layout --> Idle : готово
    Error --> Idle : закрыть
    Error --> [*]
```

## Диаграмма Ганта

```mermaid
gantt
    title План релиза 1.0
    dateFormat YYYY-MM-DD
    section Ядро
    Парсер Markdown      :a1, 2026-10-20, 12d
    Вёрстка и отрисовка  :a2, after a1, 10d
    section Оболочка
    Окно и режимы        :b1, 2026-11-01, 8d
    Редактор             :b2, after b1, 6d
    section Выпуск
    Тесты                :c1, 2026-11-10, 5d
    Релиз                :milestone, m1, 2026-11-16, 0d
```

## Путь пользователя

```mermaid
journey
    title Открытие документа
    section Запуск
      Двойной клик по .md: 5: Пользователь
      Окно появляется: 4: Пользователь
    section Чтение
      Прокрутка: 5: Пользователь
      Поиск по тексту: 4: Пользователь, Клавиатура
    section Правка
      Переключение в редактор: 3: Пользователь
      Сохранение: 5: Пользователь
```

## Что не поддерживается

```mermaid
erDiagram
    CUSTOMER ||--o{ ORDER : places
```

Такие типы показываются исходником с пояснением — и никогда не ломают страницу.
