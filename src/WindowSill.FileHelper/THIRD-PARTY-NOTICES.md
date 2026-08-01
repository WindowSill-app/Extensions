# Third-Party Notices

WindowSill.FileHelper uses the third-party components listed below. Their licenses and attribution requirements differ — in particular, the DOCX-to-PDF conversion is powered by Syncfusion, which is **commercial** software (used here under a free Community License), not a permissive open-source library.

## Syncfusion&reg; Essential Studio&reg; (DocIO, DocIORenderer, PDF)

Used to convert Word documents (`.docx`) to PDF fully in-process (`WordDocument` → `DocIORenderer.ConvertToPDF` → `PdfDocument`). Syncfusion's document engine performs true Word-style page layout, so cover pages, headers/footers, tables of contents, tables, lists, shapes and inline images are preserved in the output.

- License: **Proprietary / commercial** — Syncfusion Software License Agreement. This is **not** an open-source or royalty-free license.
  - License agreement: https://www.syncfusion.com/nuget/license
  - This project uses Syncfusion under the free **Community License** (for qualifying individuals and organizations): https://www.syncfusion.com/products/communitylicense
- A valid Syncfusion license key is registered at runtime; without one the generated PDFs carry a Syncfusion trial watermark.
- Project: https://www.syncfusion.com/document-sdk
- Packages: `Syncfusion.DocIO.Net.Core`, `Syncfusion.DocIORenderer.Net.Core`, `Syncfusion.Pdf.Net.Core`, `Syncfusion.Pdf.Imaging.Net.Core`, `Syncfusion.OfficeChart.Net.Core`, `Syncfusion.Compression.Net.Core`, `Syncfusion.MetafileRenderer.Net.Core`, `Syncfusion.SkiaSharpHelper.Net.Core`, `Syncfusion.Licensing`, `Syncfusion.Markdown`, `Syncfusion.Telemetry`.

> Syncfusion's usage telemetry is explicitly disabled at startup (`Syncfusion.Telemetry.Telemetry.Disable()`) so the extension does not send any data to Syncfusion.

## SkiaSharp / HarfBuzzSharp

Pulled in transitively by Syncfusion's document renderer (`Syncfusion.DocIORenderer` depends on `SkiaSharp.HarfBuzz`) and redistributed alongside it as the native text-shaping and rasterization layer. These are the same centrally-versioned SkiaSharp/HarfBuzzSharp dependencies already used elsewhere in this repository, kept version-aligned so a single native library is shared across the WindowSill process.

- License: MIT
- Projects: https://github.com/mono/SkiaSharp , https://github.com/HarfBuzz/harfbuzzsharp