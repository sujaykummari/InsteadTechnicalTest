# Design Decisions

## 1. Separate data from the form template

The annotation definition references a data path rather than storing a concrete value. This allows the same template to be used with different taxpayer data without duplication or mutation.

## 2. Use PDF points for geometry

PDF layouts are typically measured in points, and most PDF rendering engines operate in that coordinate system. Stating this up front makes the data easier to reason about and easier to validate in tests.

## 3. Support nested lookups

Tax forms require access to highly nested records like `income.w2[0].wages`. This deeply nested access pattern is the norm in tax software and cannot be handled with flat field names alone.

## 4. Keep formatting independent from source values

Formatting should not be mixed with the tax dataset. For example, a value might be stored as `125000` but rendered as `$125,000` or `125000` depending on the form and print context.

## 5. Provide a schema, not just a sample object

A schema ensures that future form templates are valid and can be checked programmatically. It also makes maintenance easier when new forms and field types are introduced.

## 6. Support reusable templates

The renderer receives template and annotation paths from a manifest. It does not embed a country-specific template choice in the rendering engine, so adding another document means adding data and configuration rather than rewriting the renderer.

## 7. Support both absolute and relative coordinates

The design includes coordinate-system metadata, and can support `relative` coordinates in the future. This helps when a form is rendered at different physical sizes or when a layout engine normalizes all rectangles to a percentage-based coordinate system.

## 8. Preserve extensibility

The annotation object includes `additionalProperties: true` in the schema. This makes it easier to add things like dependencies, field groups, or metadata in later iterations without breaking older pipeline consumers.

## 9. Treat validation as first-class metadata

Validation enables better UX and better forms. It is especially valuable for SSNs, dates, and tax IDs.

## 10. Fallbacks and missing data are explicit

Blank and missing values are common in tax forms. The renderer should have a consistent rule set for how to display them so a form does not render unpredictably.

## 11. Design for automation and quality assurance

The project is structured so it can later be integrated into automated validation flows: preview generation, schema checks, template calibration, and regression tests for rendered PDFs.
