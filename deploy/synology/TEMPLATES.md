# Lease templates on NAS (T3.2)

Blank Brookside / Wiley Housing Authority lease forms live on the documents share
(not in git — `local-docs/` is gitignored).

## Canonical paths (production)

| File                          | Term              | Host path                                                                |
| ----------------------------- | ----------------- | ------------------------------------------------------------------------ |
| Year lease (source DOCX)      | 1 calendar year   | `/volume1/apartments/docs/templates/brookside-year-lease.docx`           |
| Month-to-month (source DOCX)  | 1 calendar month  | `/volume1/apartments/docs/templates/brookside-month-to-month-lease.docx` |
| Year lease (fillable PDF)     | preferred runtime | `/volume1/apartments/docs/templates/brookside-year-lease.pdf`            |
| Month-to-month (fillable PDF) | preferred runtime | `/volume1/apartments/docs/templates/brookside-month-to-month-lease.pdf`  |

Container path: `/docs/templates/brookside-*`

Source: Brookside WHA notices folder (blank templates only; filled tenant PDFs stay out of this folder).

## Preferred generation path

ClerkSuite prefers **fillable AcroForm PDF** templates:

1. On first use, if `brookside-*.pdf` is missing but the DOCX exists, `LeaseService` bootstraps a fillable PDF (DocIO → PDF + named form fields).
2. Generation fills AcroForm fields via `Syncfusion.Pdf.Net.Core` and writes `/docs/leases/*.pdf`.
3. Preview uses **SfPdfViewer2** (not Word DocumentEditor).
4. DOCX underscore fill remains a fallback/archive path.

## Local Mac

```bash
mkdir -p local-docs/templates
ssh mr-storage "cat /volume1/apartments/docs/templates/brookside-year-lease.docx" \
  > local-docs/templates/brookside-year-lease.docx
ssh mr-storage "cat /volume1/apartments/docs/templates/brookside-month-to-month-lease.docx" \
  > local-docs/templates/brookside-month-to-month-lease.docx
```

Point `ClerkSuite__DocumentRoot` at the repo `local-docs` directory for `dotnet run`
(`appsettings.Development.json` uses `../../local-docs`).

After first lease list/generate, fillable `.pdf` siblings appear next to the DOCX files — copy those
back to the NAS `templates/` share when ready.
