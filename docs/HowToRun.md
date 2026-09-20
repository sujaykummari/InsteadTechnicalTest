
## Typical run command

Open PowerShell and run:

```powershell
cd "c:\Users\SujayKummari\Desktop\Java\instead-tax-annotation\instead-tax-annotation"
dotnet run --project renderer
```

This creates the filled output PDF in the output folder.

## Debug mode

To show the red placement boxes and verify alignment on the original PDF, run:

```powershell
cd "c:\Users\SujayKummari\Desktop\Java\instead-tax-annotation\instead-tax-annotation"
dotnet run --project renderer -- --debug
```

This writes a debug output PDF so the rectangles can be checked against the original form.

## Run tests

```powershell
cd "c:\Users\SujayKummari\Desktop\Java\instead-tax-annotation\instead-tax-annotation"
dotnet test tests
```

## Requirements

Before running the project, ensure that the machine has:

- .NET 8 SDK installed
- PowerShell available
- access to the project folder above

## Key design principle

The project follows this separation:

```text
Taxpayer JSON = WHAT value is being filled
Form annotation JSON = WHERE and HOW it should appear on the form
Renderer = executes the mapping and writes it into the PDF
```

## Notes

- The project uses actual PDF templates instead of a generated mock layout.
- The renderer supports country-based route selection through the manifest.json file.
- Debug mode is intended for calibration and visual verification of field placement.

## Summary

This project is a compact PDF annotation prototype for tax forms. It is designed to be easy to understand, easy to extend, and suitable for demonstrating how taxpayer data can be mapped to official tax form fields without embedding values directly into the annotation layout.
