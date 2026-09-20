# Instead Tax Annotation

This project is a tax-form annotation system built to map real taxpayer data onto official PDF tax forms without embedding values directly into the form layout.

## What this project is

The goal is to separate three concerns:

- taxpayer data: the actual values such as name, wages, deductions, and tax information
- form annotation metadata: where each value belongs on the PDF and how it should be placed
- rendering logic: the code that reads the data, resolves the correct field, formats it, and draws it onto the tax form

This lets the same annotation layout be reused for different tax returns or countries, while keeping the actual taxpayer details in a separate JSON file.

## What was added in the previous work

The project evolved from a simple layout prototype into a working PDF overlay system with these improvements:

- actual PDF rendering onto real tax-form backgrounds instead of just console output
- debug mode that draws red annotation rectangles over the original form for calibration
- country-based form routing for US and India examples
- separate taxpayer data JSON from annotation definition JSON
- nested value resolution such as income.w2[0].wages and india.salary.grossSalary
- formatting support for currency, numbers, dates, and checkbox values
- graceful handling for missing values and fallback values
- coordinate-based placement with PDF-point geometry and scaling
- tests covering runtime resolution and PDF generation
- project documentation and specification files for clarity

## Goals

- Define a reusable annotation schema for PDF-based tax forms
- Bind annotations to deeply nested taxpayer data paths
- Support precise placement using PDF coordinates
- Keep formatting separate from data values
- Demonstrate a working renderer in C#
- Provide documentation and a walkthrough suitable for submission

## Specification

The detailed project specification is documented in [docs/specification.md](docs/specification.md). It defines the annotation contract, supported field types, coordinate model, rendering behavior, and the features that are implemented versus planned for future work.

## Folder structure

- schema/annotation-schema.json — JSON schema for annotation documents
- examples/form-manifest.json — country-to-template and annotation routing
- examples/form-1040.json — US Form 1040 annotation definition
- examples/form-16.json — India Form 16 annotation definition
- examples/taxpayer-data.json — example tax return data
- renderer/ — C# PDF renderer and formatting engine
- templates/us-tax-form.pdf — original US tax form PDF used as the background
- templates/india-tax-form-blank.pdf — blank Indian Form 16 background
- output/ — generated filled PDFs
- tests/ — unit and PDF integration tests
- docs/ — specification and design notes
- walkthrough/README.md — presentation notes

## Core idea

An annotation does not hold the final tax value. Instead, it points at the value using a data path, and the renderer resolves that path against the taxpayer object at runtime.

Example:

- Annotation path: taxpayer.name.first
- Runtime value: John

- Annotation path: income.w2[0].wages
- Runtime value: 125000

The layout definition stays separate from the tax information, which makes the project reusable and easier to maintain.

## Quick start

From the project root, run:

```powershell
cd "c:\Users\SujayKummari\Desktop\Java\instead-tax-annotation\instead-tax-annotation"
dotnet run --project renderer
```

The renderer reads the country from examples/taxpayer-data.json, looks up the route in examples/form-manifest.json, opens the configured PDF template, and overlays the matching annotation definition.

The default US result is written to output/filled-form-1040.pdf.

To generate the India version, change this value in taxpayer-data.json:

```json
"country": "IN"
```

Then run the same command again. The app will switch to the India template and annotation file and generate output/filled-form-16.pdf.

To generate a debug copy with red rectangles and annotation IDs:

```powershell
dotnet run --project renderer -- --debug
```

This is useful for checking field placement against the original PDF.

To run the automated checks:

```powershell
dotnet test tests
```

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

- Coordinates use PDF points with the origin at the top-left of the page.
- x, y, width, and height describe the target box where the value should be drawn.
- The form metadata coordinate space is scaled to the actual PDF page size at render time.
- Missing values render blank unless a fallback is provided.
- Currency, number, and date formatting are kept separate from the source taxpayer data.
- Checkbox and radio annotations render only when the configured value matches.
- Debug mode highlights annotation rectangles so calibration can be adjusted visually.

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
  final filled PDF
```

The renderer never copies taxpayer values into the annotation definition. Instead, it resolves the path at runtime and draws the final value into the appropriate position.

## Supported use cases

The project was built to support:

- US tax form overlay generation with an official IRS form background
- India-form-style sample generation with a different route and annotation layout
- nested data extraction from complex JSON objects
- value formatting for print-friendly output
- calibration of form coordinates using debug overlay mode

## Future extension

The design can be extended to include:

- repeating sections
- conditional visibility
- richer validation
- multiline fields
- more country and tax-form templates
- OCR or scanned-form calibration support
- schema versioning and broader automation

## Summary

This project is a working prototype for tax-form PDF annotation. It demonstrates how to keep taxpayer records separate from form layout, resolve nested data paths, apply formatting, and render values onto real PDF forms. The main improvements added during development were actual PDF rendering, country routing, calibration overlays, and country-specific layout support for US and India examples.
