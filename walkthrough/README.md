# 5-Minute Walkthrough Script

## 0:00–0:30 — Problem statement

"The goal is to define a form-independent annotation layer that tells a renderer what data should be printed, where it should be printed, and how it should look."

This solves the central problem in many tax systems: the form layout should not be tightly coupled to the taxpayer record or the business logic that resolves values.

## 0:30–1:30 — Schema overview

Show the annotation document and explain the key sections:

- `form` metadata
- `annotations` array
- coordinate system
- field IDs
- position and sizing
- data path bindings

Explain that the annotation does not contain the actual tax value itself.

## 1:30–2:30 — Deep data lookup

Show examples like:

```json
"data": { "path": "taxpayer.name.first" }
```

and

```json
"data": { "path": "income.w2[0].wages" }
```

Then explain that the same schema is resolving a path into a concrete value at runtime.

## 2:30–3:45 — Rendering

Show how a renderer takes:

- the annotation definition
- the taxpayer data object
- formatting metadata

and produces a rendered field. Emphasize that the renderer is responsible for the final display of the value inside the correct PDF coordinates.

## 3:45–4:30 — Edge cases

Discuss:

- checkboxes
- missing values
- negative numbers
- multiline text
- different form versions
- coordinate systems

These are all critical in real tax forms.

## 4:30–5:00 — Future improvements

End with a short statement about future expansions:

- repeating sections
- conditional visibility
- schema versioning
- accessibility
- OCR or template calibration
- automation and PDF validation

This makes the solution feel real and production-minded rather than just a static JSON example.
