# Tax Form Annotation Architecture

```text
+-----------------------------+
| schema/annotation-schema.json|
|                             |
| Defines the annotation      |
| structure / contract        |
+--------------+--------------+
               |
               | validates / describes
               v
+-----------------------------+
| examples/form-1040.json     |
|                             |
| Actual Form 1040            |
| annotations                 |
|                             |
| - id                        |
| - type                      |
| - data.path                 |
| - position                  |
| - format                    |
+--------------+--------------+
               |
               | read by
               v
+-----------------------------+
| renderer/example-renderer.cs|
|                             |
| TaxRenderer.Main()          |
|                             |
| 1. Load taxpayer data       |
| 2. Read country             |
| 3. Read form-manifest.json  |
| 4. Select template +        |
|    annotation file          |
| 5. Call PdfFormRenderer     |
+--------------+--------------+
               |
               | Render()
               v
+-----------------------------+
| renderer/PdfFormRenderer.cs |
|                             |
| Render()                    |
|     |                       |
|     +--> ToPdfRectangle()   |
|     |    position -> PDF    |
|     |    coordinates         |
|     |                       |
|     +--> DrawAnnotation()   |
|              |              |
|              +-->           |
|              AnnotationEngine
|                             |
+----------------+------------+
                 |
                 | resolve data.path
                 v
+-----------------------------+
| renderer/AnnotationEngine.cs|
|                             |
| ResolveValue()              |
|     |                       |
|     +--> ParsePath()        |
|                             |
| FormatValue()               |
|     |                       |
|     +--> FormatCurrency()   |
|     +--> FormatNumber()     |
|     +--> FormatDate()       |
|                             |
| IsChecked()                 |
+----------------+------------+
                 ^
                 |
                 | reads values from
                 |
+-----------------------------+
| examples/taxpayer-data.json |
|                             |
| Actual taxpayer data        |
|                             |
| taxpayer.name.first = John  |
| income.w2[0].wages = 125000|
| return.totalTax = 18500     |
+-----------------------------+
                 |
                 |
                 v
+-----------------------------+
| templates/us-tax-form.pdf   |
|                             |
| Original blank tax form     |
+-----------------------------+
                 |
                 v
+-----------------------------+
| output/filled-form-1040.pdf |
|                             |
| Final PDF with values       |
| rendered into form fields   |
+-----------------------------+
```

## File / Function Responsibilities

| File | Responsibility | Important functions |
|---|---|---|
| `schema/annotation-schema.json` | Defines the contract for annotations | JSON Schema definitions; no C# functions |
| `examples/form-1040.json` | Contains the actual Form 1040 field annotations | No C# functions; data consumed by renderer |
| `examples/taxpayer-data.json` | Contains the actual taxpayer values | No C# functions; consumed by `AnnotationEngine` |
| `examples/form-manifest.json` | Routes a country to form/template/annotation/output | No C# functions; read by `TaxRenderer.Main()` |
| `renderer/example-renderer.cs` | Application entry point / orchestration | `TaxRenderer.Main()`, `FindProjectRoot()` |
| `renderer/PdfFormRenderer.cs` | Opens PDF and draws annotations | `Render()`, `DrawAnnotation()`, `ToPdfRectangle()`, `DrawDebugRectangle()`, `GetDouble()` |
| `renderer/AnnotationEngine.cs` | Resolves nested data and formats values | `ResolveValue()`, `FormatValue()`, `IsChecked()`, `ParsePath()`, `FormatCurrency()`, `FormatNumber()`, `FormatDate()` |
| `templates/us-tax-form.pdf` | Blank/original PDF template used as background | N/A |
| `output/filled-form-1040.pdf` | Generated filled PDF | N/A |

## Example: First Name

### 1. Taxpayer data

`examples/taxpayer-data.json`

```json
{
  "taxpayer": {
    "name": {
      "first": "John"
    }
  }
}
```

### 2. Form 1040 annotation

`examples/form-1040.json`

```json
{
  "id": "taxpayer-first-name",
  "type": "text",
  "data": {
    "path": "taxpayer.name.first"
  },
  "position": {
    "x": 45,
    "y": 84,
    "width": 185,
    "height": 16
  },
  "format": {
    "fontFamily": "Helvetica",
    "fontSize": 9,
    "alignment": "left"
  }
}
```

### 3. What happens at runtime

```text
form-1040.json
      |
      | data.path = "taxpayer.name.first"
      v
AnnotationEngine.ResolveValue()
      |
      +--> ParsePath()
      |       taxpayer -> name -> first
      |
      v
    "John"
      |
      v
PdfFormRenderer.DrawAnnotation()
      |
      +--> reads position
      |       x=45, y=84, width=185, height=16
      |
      +--> reads format
      |       Helvetica, 9pt, left
      |
      v
Draw "John" onto the PDF
```

## Example: Deeply Nested Data

For wages, the annotation contains:

```json
{
  "type": "currency",
  "data": {
    "path": "income.w2[0].wages"
  }
}
```

`AnnotationEngine.ResolveValue()` parses the path as:

```text
income
  -> w2
     -> [0]
        -> wages
           -> 125000
```

Then `FormatValue()` converts it according to the annotation's format rules. For example:

```text
125000
   |
   | currency + thousandsSeparator
   v
125,000
```

The renderer then places `125,000` inside the annotation's position rectangle.

## Key Design Principle

```text
Taxpayer JSON
    = WHAT is the value?

Form annotation JSON
    = WHERE + HOW should the value appear?

Annotation schema
    = WHAT STRUCTURE must an annotation follow?

Renderer
    = EXECUTES the mapping and writes the value onto the PDF.
```
