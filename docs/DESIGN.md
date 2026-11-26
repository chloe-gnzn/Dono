# Design notes

The look follows the approved inspiration image: a clean medical style with a
strong red, white surfaces, a pale aqua page, charcoal text, pill buttons and a
solid red band for highlights.

## Palette

| Token        | Hex       | Used for                                      |
|--------------|-----------|-----------------------------------------------|
| `brand-500`  | `#F44336` | Bright highlight (shield in the inspiration)  |
| `brand-600`  | `#D32F2F` | Primary: buttons, logo, red bands             |
| `brand-700`  | `#C62828` | Hover, error text                             |
| `brand-50..200` | tints  | Alert and badge backgrounds                   |
| `aqua-100`   | `#D3EFF1` | Page background                               |
| `aqua-200`   | `#B5DADD` | Flat offset shadow of framed cards            |
| `ink`        | `#2D2D2D` | Body and heading text                         |
| `ink-soft`   | `#5C5C5C` | Secondary text                                |
| `accent`     | `#26A5DD` | Info states (blue icons in the inspiration)   |

## Typography

- Headings: Montserrat (600-800), loaded from Google Fonts, falls back to system sans.
- Body: system UI font stack.

## Shape and components

- Buttons are fully rounded pills, uppercase, small and bold (`btn`, `btn-primary`, `btn-outline`).
- Cards are white, `rounded-2xl`. `card-offset` adds the flat aqua offset shadow.
- Red band (`band`) is used for the footer and highlight strips.

## Tailwind gotcha

Tailwind only emits classes it finds as literal text in source files. In Razor
code always write full class names (`"badge-success"`), never build them with
string concatenation (`"badge-" + name`), or they will be missing from the CSS.
