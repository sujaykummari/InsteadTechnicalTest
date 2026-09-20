# Instead Tax Annotation

This project is a compact engineering assessment for a tax-form annotation system. It separates the taxpayer data from the physical layout and formatting rules for a PDF form, so the same annotation definition can be reused across different tax returns and form versions.

## Goals

- Define a reusable annotation schema for PDF-based tax forms
- Bind annotations to deeply nested taxpayer data paths
- Support precise placement using PDF coordinates
- Keep formatting separate from data values
- Demonstrate a working renderer in C#
- Provide documentation and a walkthrough suitable for submission

## Folder structure

- `schema/annotation-schema.json` — JSON schema for annotation documents
- `examples/form-manifest.json` — country-to-template and annotation routing
- `examples/form-1040.json` — multi-page US Form 1040 annotation definition
- `examples/form-16.json` — India Form 16 Part A/B-style annotation definition
- `examples/taxpayer-data.json` — example tax return data
- `renderer/` — C# PDF renderer and formatting engine
- `templates/us-tax-form.pdf` — original US tax form PDF used as the background
- `templates/india-tax-form-blank.pdf` — blank Indian Form 16 format used as the background
- `output/filled-form-1040.pdf` — generated annotated PDF after running the demo
- `tests/` — unit and PDF integration tests
- `docs/` — technical specification and design decisions
- `walkthrough/README.md` — 5-minute presentation script

## Core idea

An annotation does not hold the final tax value. Instead, it points at the value using a data path, and the renderer resolves that path against the taxpayer object at runtime.

Example:

- Annotation path: `taxpayer.name.first`
- Runtime value: `John`

- Annotation path: `income.w2[0].wages`
- Runtime value: `125000`

This keeps the layout definition independent from the actual tax information while still letting the same template be reused.

## Quick start

From the project folder, run:

```powershell
dotnet run --project renderer
```

The renderer reads `country` from `examples/taxpayer-data.json`, looks up that country in `examples/form-manifest.json`, opens the configured PDF background, and overlays the matching annotation definition. The default `US` result is written to `output/filled-form-1040.pdf`.

To generate the India document, change only this value:

```json
"country": "IN"
```

Then run the same command. The renderer selects `examples/form-16.json` and `templates/india-tax-form-blank.pdf`, writing `output/filled-form-16.pdf` by default. Restore `"country": "US"` to return to the Form 1040 example.

To choose another output path:

```powershell
dotnet run --project renderer -- C:\temp\filled-1040.pdf
```

To generate a coordinate-debug copy with red dashed rectangles and annotation IDs:

```powershell
dotnet run --project renderer -- --debug
```

This writes `output/filled-form-1040-debug.pdf` by default. A custom output path can be combined with debug mode: `dotnet run --project renderer -- C:\temp\debug.pdf --debug`.

The debug output is especially useful for calibrating the original IRS background. The current US anchors target the filing-status checkbox, taxpayer name/SSN row, and Form W-2 wages line 1a; they are no longer based on the earlier generated demonstration page.

Run the automated checks with:

```powershell
dotnet test tests
```

The US background is the original IRS Form 1040 PDF downloaded from `irs.gov`. The India background is a public blank Form 16 format with empty value areas, selected so example taxpayer values do not overlap prefilled sample data. Form 16 is not an Indian Form 1040 equivalent: the Income Tax Department describes it as an employer-issued salary TDS certificate with Part A and Part B. A production Form 16 should be generated and supplied by the employer/TRACES workflow. Replace either template PDF with another form and keep the annotation coordinates aligned to that form's page geometry.

Generated PDFs are intentionally excluded from source control. The renderer creates them under `output/` when you run the demo.

## Adding a template

1. Put the background PDF in `templates/`.
2. Create an annotation JSON document with `form` metadata and `annotations`; each annotation only stores a data path such as `taxpayer.name.first` or `income.w2[0].wages`.
3. Add a route in `examples/form-manifest.json` with the template and annotation paths.
4. Keep runtime values in `examples/taxpayer-data.json` or another input file; do not copy values into the annotation document.
5. Run with `--debug` and adjust the rectangles until they sit over the intended boxes.

For small vertical alignment corrections, add `"verticalOffset": 3` inside a field's `format` object. The value is measured in PDF points from the top of the annotation rectangle. The renderer defaults to `3` points, so this is the exact setting to change when a value needs to move slightly up or down without moving the debug box.

## Example annotation shape

```json
{
  "id": "wages",
  "type": "currency",
  "data": {
    "path": "income.w2[0].wages"
  },
  "position": {
    "x": 450,
    "y": 300,
    "width": 90,
    "height": 18
  },
  "format": {
    "type": "currency",
    "decimalPlaces": 0,
    "thousandsSeparator": true,
    "currencySymbol": ""
  }
}
```

## Design assumptions

- Coordinates use PDF points with the origin at the top-left of the page. `x`, `y`, `width`, and `height` are defined in the form metadata coordinate space and are scaled to the actual target PDF page width and height at render time.
- `x` and `y` represent the bounding box origin; `width` and `height` define the area available for drawing the value.
- A position may use `unit: "relative"` for normalized 0-1 coordinates; relative values are scaled by the form width and height.
- Missing values render blank unless `data.fallback` is present.
- Currency supports decimal places, thousands separators, and a configurable symbol.
- Checkbox and radio annotations render their configured mark only when the annotation's equality rule matches.
- Text is centered vertically inside its bounding box, with left, center, and right alignment supported.
- An annotation may override the form-level page with `page`, allowing one annotation file to cover multiple PDF pages.
- Debug mode draws each resolved rectangle in red and labels it with the annotation ID, making coordinate calibration visible without modifying taxpayer data.

## Rendering flow

```text
annotation JSON + taxpayer JSON
    |
  nested path resolver
    |
  field formatter
    |
  PDF template overlay
    |
  filled-form-1040.pdf
```

The renderer never copies taxpayer values into the annotation definition. For example, `income.w2[0].wages` is resolved at render time and formatted as `$125,000` in the US PDF, while `india.salary.grossSalary` becomes `INR 2,400,000` in the India document.

The sources used for the background documents are:

- US Form 1040: `https://www.irs.gov/pub/irs-pdf/f1040.pdf`
- Form 16 reference: `https://www.incometaxindia.gov.in/w/form-16-and-form-16a`
- Blank Form 16 format: `http://www.forms.in/wp-content/uploads/forms/Form-No.16.pdf`

## Future extension

The design can grow to support:

- repeating sections
- conditional visibility
- validation
- checkboxes and radio buttons
- localization
- OCR/calibration for scanned forms
- schema versioning
