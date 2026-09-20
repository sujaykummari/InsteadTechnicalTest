# Tax Annotation Specification

## Overview

This specification defines a form-independent layer that maps tax return data into structured fields on a PDF form. The goal is to separate the annotation metadata from the taxpayer record so templates can be reused and updated without embedding tax data directly into the form definition.

## Coordinate system

All coordinates are expressed in PDF points unless otherwise stated. The origin is the top-left corner of the page. The `x` and `y` values identify the bounding-box origin, while `width` and `height` define the rectangular area reserved for the rendered value. The renderer scales this declared coordinate space to the actual target PDF page dimensions.

Example:

```json
"position": {
  "x": 450,
  "y": 300,
  "width": 90,
  "height": 18
}
```

This means a value is drawn within the rectangle beginning at $(450, 300)$ and extending to $(540, 318)$.

If the annotation document declares a `612 x 792` page and the loaded PDF page is `612 x 792`, the rectangle is used unchanged. If the loaded page is `595 x 842`, the renderer maps each coordinate by `actualPageWidth / 612` and `actualPageHeight / 792`. Relative coordinates continue to use normalized values from 0 to 1.

### US Form 1040 calibration

The US sample uses the original IRS Form 1040 first-page geometry (`612 x 792` points). Its core anchors are calibrated to the original form, not to a generated mock layout:

- filing status: approximately `(98, 201)`
- first name: approximately `(45, 84)`
- last name: approximately `(240, 84)`
- SSN: approximately `(470, 84)`
- wages line 1a: approximately `(505, 450)`

These rectangles are intentionally kept in the annotation document so layout changes remain separate from taxpayer data. Run the renderer with `--debug` to see each rectangle and its annotation ID over the source form before changing coordinates.

## Data lookup

Each annotation includes a `data.path` string. This path is resolved against the runtime tax object. The path supports:

- object properties: `taxpayer.name.first`
- array indexes: `income.w2[0].wages`
- nested structures: `deductions.standardDeduction`

A compliant resolver must traverse the object graph and return either the resolved value or a fallback value if the path is missing.

## Supported field types

- `text` — plain string values
- `number` — numeric values with optional formatting
- `currency` — money with separators and decimal control
- `date` — date conversion from ISO inputs
- `checkbox` — toggled based on equality against a configured value
- `radio` — grouping and selection logic
- `multiline` — text wrapping inside a field box
- `percent` — percentage formatting

## Formatting rules

Formatting is intentionally separated from the source data so the same value can be rendered differently in different use cases. Supported options include:

- decimal places
- thousands separators
- currency symbols
- alignment
- font family and font size
- output date pattern
- zero value placeholders
- negative value formatting

## Validation rules

Annotations may optionally declare validation requirements. Examples:

```json
"validation": {
  "pattern": "^\\d{3}-\\d{2}-\\d{4}$",
  "required": true
}
```

This supports structured validation before a form is submitted or printed.

## Rendering behavior

At render time, the engine should:

1. Read the annotation definition
2. Resolve the `data.path` against the taxpayer object
3. Apply the configured formatter
4. Determine whether the field is visible or checked
5. Draw the value into the bounding box
6. Handle overflow, nulls, and missing values gracefully

## Errors and missing values

The renderer should not crash if a field is empty or the path is absent. Recommended behavior:

- missing value + no fallback: render blank
- missing value + fallback: render fallback value
- invalid number: render blank or validation warning
- checkbox with missing value: leave unchecked

## Extension points

The annotation model is intentionally extensible. Future versions can add:

- repeating sections
- conditional visibility expressions
- per-field dependencies
- schema versioning
- accessibility metadata
- OCR calibration anchors
