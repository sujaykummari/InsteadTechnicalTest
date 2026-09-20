
## Requirements

Before running the project, ensure that the machine has:

- .NET 8 SDK installed
- PowerShell available
- access to the project folder above

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


