# Tax Annotation Specification

## 1. Purpose

This project defines a form-independent annotation layer for tax forms. The purpose is to keep three concerns separate:

1. the taxpayer data itself,
2. the layout definition for a specific PDF form, and
3. the display formatting rules used when values are rendered.

This separation allows one annotation definition to be reused across tax years, countries, and form versions without copying taxpayer values into the layout file.

## 2. Scope

The current implementation supports overlaying resolved values onto a source PDF form using a JSON annotation model. The project is intentionally narrow in scope and is designed for tax-form printing and annotation, not for general-purpose UI rendering.

The supported workflow is:

- read taxpayer data from a JSON document,
- look up the selected country and template,
- load the annotation document for that form,
- resolve each `data.path` against the taxpayer object,
- apply the configured formatter,
- draw the final value onto the page,
- optionally output a debug version with red rectangles around each target box.

## 3. Core data model

Every annotation document has the following top-level shape:

```json
{
  "form": {
    "id": "us-1040",
    "version": "2025",
    "page": 1,
    "coordinateSystem": "pdf-points",
    "width": 612,
    "height": 792
  },
  "annotations": [
    {
      "id": "wages",
      "type": "currency",
      "data": {
        "path": "income.w2[0].wages"
      },
      "position": {
        "x": 505,
        "y": 452,
        "width": 75,
        "height": 16
      },
      "format": {
        "type": "currency",
        "decimalPlaces": 0,
        "thousandsSeparator": true,
        "currencySymbol": ""
      }
    }
  ]
}
```

### Required fields

- `form.id`: form identifier
- `form.version`: form version label
- `form.page`: default page number for the annotation set
- `form.coordinateSystem`: either `pdf-points` or `relative`
- `form.width`: form coordinate width
- `form.height`: form coordinate height
- `annotations[]`: array of annotation definitions
- each annotation requires `id`, `type`, `data`, and `position`

## 4. Coordinate system

Coordinates are defined in PDF points with the origin at the top-left of the page, unless `unit: "relative"` is specified. The position object is:

```json
"position": {
  "x": 450,
  "y": 300,
  "width": 90,
  "height": 18
}
```

This creates a target rectangle whose top-left corner is at $(x, y)$ and whose size is `width x height`.

Rules:

- `x`, `y`, `width`, and `height` are interpreted in the form coordinate space declared by `form.width` and `form.height`.
- If the template PDF has different page dimensions, the coordinates are scaled to match the actual page size.
- `coordinateSystem: "relative"` means values are normalized between 0 and 1.
- `unit: "relative"` on a position overrides the form-level coordinate system for that annotation.

This is the core calibration mechanism used when aligning to the original IRS PDF and other form templates.

## 5. Data lookup and nested paths

Each annotation points to a value using a `data.path`. The resolver traverses the taxpayer JSON object and supports:

- property access: `taxpayer.name.first`
- array index access: `income.w2[0].wages`
- nested object chains: `return.adjustedGrossIncome`

If a lookup fails, the resolver returns `data.fallback` when present. Otherwise it renders blank.

Example:

```json
"data": {
  "path": "income.w2[0].wages",
  "fallback": 0
}
```

## 6. Supported field types

The renderer currently supports the following annotation types:

- `text` — plain string output
- `number` — numeric formatting
- `currency` — thousands-separator and currency-string formatting
- `date` — ISO date parsing with a configured output pattern
- `percent` — numeric value with `%` suffix
- `checkbox` — rendered when a value matches the configured equality condition
- `radio` — same matching logic as checkbox, intended for choice-style fields

The following are documented as planned future work and are not treated as implemented behavior today:

- `multiline` text wrapping in a bounded box
- advanced validation-driven rendering behavior
- complex conditional visibility logic
- repeating sections and dynamic form regions

## 7. Formatting rules

Formatting remains separate from taxpayer data so a single value can be rendered differently by form context.

Supported formatting members include:

- `type`
- `decimalPlaces`
- `thousandsSeparator`
- `currencySymbol`
- `alignment` (`left`, `center`, `right`)
- `verticalAlignment` (`top`, `center`, `bottom`)
- `verticalOffset` (PDF points)
- `fontFamily`
- `fontSize`
- `output` (for date formatting)
- `zeroFormat`
- `negativeFormat`

Example:

```json
"format": {
  "type": "currency",
  "decimalPlaces": 0,
  "thousandsSeparator": true,
  "currencySymbol": "",
  "alignment": "right",
  "verticalAlignment": "center",
  "verticalOffset": 3
}
```

## 8. Checkbox and value matching

Checkbox and radio annotations use their `value.equals` rule to decide whether to render the configured mark.

Example:

```json
{
  "id": "filing-status-single",
  "type": "checkbox",
  "data": {
    "path": "taxpayer.filingStatus"
  },
  "value": {
    "equals": "single"
  },
  "appearance": {
    "mark": "X"
  }
}
```

If the resolved value equals the configured value, the renderer draws the mark. Otherwise it leaves the field blank.

## 9. Debug overlay behavior

The debug mode is a required calibration tool for this project. When running with `--debug`, the renderer:

- draws the annotation rectangle in red,
- draws the annotation `id` near the box,
- renders the final PDF while preserving the underlying form underneath.

This allows the annotation author to verify exact field placement on the original PDF without changing taxpayer data.

## 10. Rendering flow

The renderer executes this path:

1. Load the source PDF template.
2. Read the annotation document.
3. Resolve each annotation path against the taxpayer object.
4. Determine the annotation type and apply the format rules.
5. Draw the value into the bounding rectangle.
6. Save the modified PDF.
7. If debug mode is enabled, draw red calibration rectangles.

## 11. Error handling and missing values

The current renderer is designed to fail gracefully rather than crash on incomplete data.

Behavior:

- missing value with no fallback: render blank
- missing value with fallback: render fallback value
- invalid number: render blank
- invalid date: render blank
- checkbox with no match: leave unchecked
- invalid annotation page number: throw an error because the page is outside the document range

## 12. Country routing and templates

The project routes country selections through `examples/form-manifest.json`.

Example:

```json
{
  "countries": {
    "US": {
      "template": "templates/us-tax-form.pdf",
      "annotations": "examples/form-1040.json",
      "output": "output/filled-form-1040.pdf"
    },
    "IN": {
      "template": "templates/india-tax-form-blank.pdf",
      "annotations": "examples/form-16.json",
      "output": "output/filled-form-16.pdf"
    }
  }
}
```

The runtime country key is read from `examples/taxpayer-data.json` and matched against the manifest.

## 13. Run instructions

From the project root:

```powershell
cd "c:\Users\SujayKummari\Desktop\Java\instead-tax-annotation\instead-tax-annotation"
dotnet test tests
dotnet run --project renderer
dotnet run --project renderer -- --debug
```

The output PDF is written under `output/`, and the debug run creates a debug copy with the label overlays.

## 14. Non-goals and future extension

This project does not currently aim to provide full tax filing logic, OCR, or general-purpose form authoring. It is a focused prototype for annotation-driven PDF overlay generation.

Future improvements may include:

- richer validation rules,
- multiline field rendering,
- conditional visibility,
- dynamic repeating sections,
- improved calibration tooling,
- schema versioning and compatibility checks,
- broader country and form coverage.

## 15. Summary

This specification defines the current contract of the project: a JSON annotation system that resolves nested taxpayer data, applies formatting, and overlays values onto official-style tax PDFs with controlled debug calibration. The implementation is intentionally compact but stable, and the documented future-work list makes the project boundaries explicit.
