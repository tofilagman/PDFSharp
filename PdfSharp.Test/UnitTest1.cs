using Microsoft.VisualStudio.TestTools.UnitTesting;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Annotations;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Security;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace PdfSharp.Test
{
    [TestClass]
    public class GraphicTest
    {

        [TestMethod]
        public void Graphics()
        {
            PdfDocument document = new PdfDocument();

            // Sample uses DIN A4, page height is 29.7 cm. We use margins of 2.5 cm.
            LayoutHelper helper = new LayoutHelper(document, XUnit.FromCentimeter(2.5), XUnit.FromCentimeter(29.7 - 2.5));
            XUnit left = XUnit.FromCentimeter(2.5);

            // Random generator with seed value, so created document will always be the same.
            Random rand = new Random(42);

            const int headerFontSize = 20;
            const int normalFontSize = 10;

            XFont fontHeader = new XFont("Verdana", headerFontSize, XFontStyle.BoldItalic);
            XFont fontNormal = new XFont("Verdana", normalFontSize, XFontStyle.Regular);

            const int totalLines = 666;
            bool washeader = false;
            for (int line = 0; line < totalLines; ++line)
            {
                bool isHeader = line == 0 || !washeader && line < totalLines - 1 && rand.Next(15) == 0;
                washeader = isHeader;
                // We do not want a single header at the bottom of the page, so if we have a header we require space for header and a normal text line.
                XUnit top = helper.GetLinePosition(isHeader ? headerFontSize + 5 : normalFontSize + 2, isHeader ? headerFontSize + 5 + normalFontSize : normalFontSize);

                helper.Gfx.DrawString(isHeader ? "Sed massa libero, semper a nisi nec" : "Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
                    isHeader ? fontHeader : fontNormal, XBrushes.Black, left, top, XStringFormats.TopLeft);
            }

            // Save the document... 
            const string filename = "MultiplePages.pdf";
            document.Save(filename);
            // ...and start a viewer.
            LaunchPdf(filename);
        }

        [TestMethod]
        public void ProtectDocument()
        {
            // Get a fresh copy of the sample PDF file
            const string filenameSource = "MultiplePages.pdf";
            const string filenameDest = "MultiplePages_tempfile.pdf";
            File.Copy(filenameSource, Path.Combine(Directory.GetCurrentDirectory(), filenameDest), true);

            // Open an existing document. Providing an unrequired password is ignored.
            PdfDocument document = PdfReader.Open(filenameDest, "some text");

            PdfSecuritySettings securitySettings = document.SecuritySettings;

            // Setting one of the passwords automatically sets the security level to 
            // PdfDocumentSecurityLevel.Encrypted128Bit.
            securitySettings.UserPassword = "user";
            securitySettings.OwnerPassword = "owner";

            // Don't use 40 bit encryption unless needed for compatibility
            //securitySettings.DocumentSecurityLevel = PdfDocumentSecurityLevel.Encrypted40Bit;

            // Restrict some rights.
            securitySettings.PermitAccessibilityExtractContent = false;
            securitySettings.PermitAnnotations = false;
            securitySettings.PermitAssembleDocument = false;
            securitySettings.PermitExtractContent = false;
            securitySettings.PermitFormsFill = true;
            securitySettings.PermitFullQualityPrint = false;
            securitySettings.PermitModifyDocument = true;
            securitySettings.PermitPrint = false;

            // Save the document...
            document.Save(filenameDest);
            // ...and start a viewer.
            LaunchPdf(filenameDest);
        }

        [TestMethod]
        public void HelloWorld()
        {
            PdfDocument document = new PdfDocument();
            document.Info.Title = "Created with PDFsharp";

            // Create an empty page
            PdfPage page = document.AddPage();

            // Get an XGraphics object for drawing
            XGraphics gfx = XGraphics.FromPdfPage(page);

            // Create a font
            XFont font = new XFont("Verdana", 20, XFontStyle.BoldItalic);

            // Draw the text
            gfx.DrawString("Hello, World!", font, XBrushes.Black,
              new XRect(0, 0, page.Width, page.Height),
              XStringFormats.Center);

            // Save the document...
            const string filename = "HelloWorld.pdf";
            document.Save(filename);
            // ...and start a viewer.
            //Process.Start(filename);
            LaunchPdf(filename);
        }

        [TestMethod]
        public void Booklet()
        {
            // Create the output document
            PdfDocument outputDocument = new PdfDocument();

            string filename = "MultiplePages.pdf";
            // Show single pages
            // (Note: one page contains two pages from the source document.
            //  If the number of pages of the source document can not be
            //  divided by 4, the first pages of the output document will
            //  each contain only one page from the source document.)
            outputDocument.PageLayout = PdfPageLayout.SinglePage;

            XGraphics gfx;

            // Open the external document as XPdfForm object
            XPdfForm form = XPdfForm.FromFile(filename);
            // Determine width and height
            double extWidth = form.PixelWidth;
            double extHeight = form.PixelHeight;

            int inputPages = form.PageCount;
            int sheets = inputPages / 4;
            if (sheets * 4 < inputPages)
                sheets += 1;
            int allpages = 4 * sheets;
            int vacats = allpages - inputPages;

            for (int idx = 1; idx <= sheets; idx += 1)
            {
                // Front page of a sheet:
                // Add a new page to the output document
                PdfPage page = outputDocument.AddPage();
                page.Orientation = PageOrientation.Landscape;
                page.Width = 2 * extWidth;
                page.Height = extHeight;
                double width = page.Width;
                double height = page.Height;

                gfx = XGraphics.FromPdfPage(page);

                // Skip if left side has to remain blank
                XRect box;
                if (vacats > 0)
                    vacats -= 1;
                else
                {
                    // Set page number (which is one-based) for left side
                    form.PageNumber = allpages + 2 * (1 - idx);
                    box = new XRect(0, 0, width / 2, height);
                    // Draw the page identified by the page number like an image
                    gfx.DrawImage(form, box);
                }

                // Set page number (which is one-based) for right side
                form.PageNumber = 2 * idx - 1;
                box = new XRect(width / 2, 0, width / 2, height);
                // Draw the page identified by the page number like an image
                gfx.DrawImage(form, box);

                // Back page of a sheet
                page = outputDocument.AddPage();
                page.Orientation = PageOrientation.Landscape;
                page.Width = 2 * extWidth;
                page.Height = extHeight;

                gfx = XGraphics.FromPdfPage(page);

                // Set page number (which is one-based) for left side
                form.PageNumber = 2 * idx;
                box = new XRect(0, 0, width / 2, height);
                // Draw the page identified by the page number like an image
                gfx.DrawImage(form, box);

                // Skip if right side has to remain blank
                if (vacats > 0)
                    vacats -= 1;
                else
                {
                    // Set page number (which is one-based) for right side
                    form.PageNumber = allpages + 1 - 2 * idx;
                    box = new XRect(width / 2, 0, width / 2, height);
                    // Draw the page identified by the page number like an image
                    gfx.DrawImage(form, box);
                }
            }

            // Save the document...
            filename = "Booklet.pdf";
            outputDocument.Save(filename);
            // ...and start a viewer.
            LaunchPdf(filename);
        }

        [TestMethod]
        public void Bookmarks()
        {
            // Create a new PDF document
            PdfDocument document = new PdfDocument();

            // Create a font
            XFont font = new XFont("Verdana", 16);

            // Create first page
            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);
            gfx.DrawString("Page 1", font, XBrushes.Black, 20, 50, XStringFormats.Default);

            // Create the root bookmark. You can set the style and the color.
            PdfOutline outline = document.Outlines.Add("Root", page, true, PdfOutlineStyle.Bold, XColors.Red);

            // Create some more pages
            for (int idx = 2; idx <= 5; idx++)
            {
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);

                string text = "Page " + idx;
                gfx.DrawString(text, font, XBrushes.Black, 20, 50, XStringFormats.Default);

                // Create a sub bookmark
                outline.Outlines.Add(text, page, true);
            }

            // Save the document...
            const string filename = "Bookmarks_tempfile.pdf";
            document.Save(filename);
            // ...and start a viewer.
            LaunchPdf(filename);
        }

        [TestMethod]
        public void ColorCMYK()
        {
            string filename = "MultiplePages.pdf";

            PdfDocument document = PdfReader.Open(filename);
            document.Options.ColorMode = PdfColorMode.Cmyk;

            // Set version to PDF 1.4 (Acrobat 5) because we use transparency.
            if (document.Version < 14)
                document.Version = 14;

            PdfPage page = document.Pages[0];

            // Get an XGraphics object for drawing beneath the existing content

            XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);

            gfx.DrawRectangle(new XSolidBrush(XColor.FromCmyk(1, 0.68, 0, 0.12)), new XRect(30, 60, 50, 50));
            gfx.DrawRectangle(new XSolidBrush(XColor.FromCmyk(0, 0.70, 1, 0)), new XRect(550, 60, 50, 50));

            gfx.DrawRectangle(new XSolidBrush(XColor.FromCmyk(0, 0, 0, 0)), new XRect(90, 100, 50, 50));
            gfx.DrawRectangle(new XSolidBrush(XColor.FromCmyk(0, 0, 0, 0)), new XRect(150, 100, 50, 50));

            gfx.DrawRectangle(new XSolidBrush(XColor.FromCmyk(0.7, 0, 0.70, 1, 0)), new XRect(90, 100, 50, 50));
            gfx.DrawRectangle(new XSolidBrush(XColor.FromCmyk(0.5, 0, 0.70, 1, 0)), new XRect(150, 100, 50, 50));

            gfx.DrawRectangle(new XSolidBrush(XColor.FromCmyk(0.35, 0.15, 0, 0.08)), new XRect(50, 360, 50, 50));
            gfx.DrawRectangle(new XSolidBrush(XColor.FromCmyk(0.25, 0.10, 0, 0.05)), new XRect(150, 360, 50, 50));
            gfx.DrawRectangle(new XSolidBrush(XColor.FromCmyk(0.15, 0.05, 0, 0)), new XRect(250, 360, 50, 50));


            filename = "CMYK.pdf";

            document.Save(filename);

            LaunchPdf(filename);
        }

        [TestMethod]
        public void MultiplePage()
        {
            PdfDocument document = new PdfDocument();

            // Sample uses DIN A4, page height is 29.7 cm. We use margins of 2.5 cm.
            LayoutHelper helper = new LayoutHelper(document, XUnit.FromCentimeter(2.5), XUnit.FromCentimeter(29.7 - 2.5));
            XUnit left = XUnit.FromCentimeter(2.5);

            // Random generator with seed value, so created document will always be the same.
            Random rand = new Random(42);

            const int headerFontSize = 20;
            const int normalFontSize = 10;

            XFont fontHeader = new XFont("Verdana", headerFontSize, XFontStyle.BoldItalic);
            XFont fontNormal = new XFont("Verdana", normalFontSize, XFontStyle.Regular);

            const int totalLines = 666;
            bool washeader = false;
            for (int line = 0; line < totalLines; ++line)
            {
                bool isHeader = line == 0 || !washeader && line < totalLines - 1 && rand.Next(15) == 0;
                washeader = isHeader;
                // We do not want a single header at the bottom of the page, so if we have a header we require space for header and a normal text line.
                XUnit top = helper.GetLinePosition(isHeader ? headerFontSize + 5 : normalFontSize + 2, isHeader ? headerFontSize + 5 + normalFontSize : normalFontSize);

                helper.Gfx.DrawString(isHeader ? "Sed massa libero, semper a nisi nec" : "Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
                    isHeader ? fontHeader : fontNormal, XBrushes.Black, left, top, XStringFormats.TopLeft);
            }

            // Save the document... 
            const string filename = "MultiplePages.pdf";
            document.Save(filename);
            // ...and start a viewer.
            LaunchPdf(filename);
        }

        [TestMethod]
        public void PageSizes()
        {
            // Create a new PDF document
            PdfDocument document = new PdfDocument();

            // Create a font
            XFont font = new XFont("Times", 25, XFontStyle.Bold);

            PageSize[] pageSizes = (PageSize[])Enum.GetValues(typeof(PageSize));
            foreach (PageSize pageSize in pageSizes)
            {
                if (pageSize == PageSize.Undefined)
                    continue;

                // One page in Portrait...
                PdfPage page = document.AddPage();
                page.Size = pageSize;
                XGraphics gfx = XGraphics.FromPdfPage(page);
                gfx.DrawString(pageSize.ToString(), font, XBrushes.DarkRed,
                  new XRect(0, 0, page.Width, page.Height),
                  XStringFormats.Center);

                // ... and one in Landscape orientation.
                page = document.AddPage();
                page.Size = pageSize;
                page.Orientation = PageOrientation.Landscape;
                gfx = XGraphics.FromPdfPage(page);
                gfx.DrawString(pageSize + " (landscape)", font,
                  XBrushes.DarkRed, new XRect(0, 0, page.Width, page.Height),
                  XStringFormats.Center);
            }

            // Save the document...
            const string filename = "PageSizes_tempfile.pdf";
            document.Save(filename);
            // ...and start a viewer.
            LaunchPdf(filename);
        }

        [TestMethod]
        public void Shapes()
        {
            string filename = "Shapes.pdf";

            PdfDocument document = new PdfDocument();
            //document.Options.ColorMode = PdfColorMode.Cmyk;

            // Set version to PDF 1.4 (Acrobat 5) because we use transparency.
            //if (document.Version < 14)
            //    document.Version = 14;

            //PdfPage page = document.Pages[0];

            //// Get an XGraphics object for drawing beneath the existing content

            //XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);

            LayoutHelper helper = new LayoutHelper(document, XUnit.FromCentimeter(2.5), XUnit.FromCentimeter(29.7 - 2.5));
            XUnit left = XUnit.FromCentimeter(2.5);

            helper.CreatePage();

            XGraphics gfx = helper.Gfx;

            XRect rect;
            XPen pen;
            double x = 50, y = 100;
            XFont fontH1 = new XFont("Times", 18, XFontStyle.Bold);
            XFont font = new XFont("Times", 12);
            XFont fontItalic = new XFont("Times", 12, XFontStyle.BoldItalic);
            double ls = font.GetHeight(); //gfx

            // Draw some text
            gfx.DrawString("Create PDF on the fly with PDFsharp",
                fontH1, XBrushes.Black, x, x);
            gfx.DrawString("With PDFsharp you can use the same code to draw graphic, " +
                "text and images on different targets.", font, XBrushes.Black, x, y);
            y += ls;
            gfx.DrawString("The object used for drawing is the XGraphics object.",
                font, XBrushes.Black, x, y);
            y += 2 * ls;

            // Draw an arc
            pen = new XPen(XColors.Red, 4);
            pen.DashStyle = XDashStyle.Dash;
            gfx.DrawArc(pen, x + 20, y, 100, 60, 150, 120);

            // Draw a star
            XGraphicsState gs = gfx.Save();
            gfx.TranslateTransform(x + 140, y + 30);
            for (int idx = 0; idx < 360; idx += 10)
            {
                gfx.RotateTransform(10);
                gfx.DrawLine(XPens.DarkGreen, 0, 0, 30, 0);
            }
            gfx.Restore(gs);

            // Draw a rounded rectangle
            rect = new XRect(x + 230, y, 100, 60);
            pen = new XPen(XColors.DarkBlue, 2.5);
            XColor color1 = XColor.FromKnownColor(KnownColor.DarkBlue);
            XColor color2 = XColors.Red;
            XLinearGradientBrush lbrush = new XLinearGradientBrush(rect, color1, color2,
              XLinearGradientMode.Vertical);
            gfx.DrawRoundedRectangle(pen, lbrush, rect, new XSize(10, 10));

            // Draw a pie
            pen = new XPen(XColors.DarkOrange, 1.5);
            pen.DashStyle = XDashStyle.Dot;
            gfx.DrawPie(pen, XBrushes.Blue, x + 360, y, 100, 60, -130, 135);

            // Draw some more text
            y += 60 + 2 * ls;
            gfx.DrawString("With XGraphics you can draw on a PDF page as well as " +
                "on any System.Drawing.Graphics object.", font, XBrushes.Black, x, y);
            y += ls * 1.1;
            gfx.DrawString("Use the same code to", font, XBrushes.Black, x, y);
            x += 10;
            y += ls * 1.1;
            gfx.DrawString("• draw on a newly created PDF page", font, XBrushes.Black, x, y);
            y += ls;
            gfx.DrawString("• draw above or beneath of the content of an existing PDF page",
                font, XBrushes.Black, x, y);
            y += ls;
            gfx.DrawString("• draw in a window", font, XBrushes.Black, x, y);
            y += ls;
            gfx.DrawString("• draw on a printer", font, XBrushes.Black, x, y);
            y += ls;
            gfx.DrawString("• draw in a bitmap image", font, XBrushes.Black, x, y);
            x -= 10;
            y += ls * 1.1;
            gfx.DrawString("You can also import an existing PDF page and use it like " +
                "an image, e.g. draw it on another PDF page.", font, XBrushes.Black, x, y);
            y += ls * 1.1 * 2;
            gfx.DrawString("Imported PDF pages are neither drawn nor printed; create a " +
                "PDF file to see or print them!", fontItalic, XBrushes.Firebrick, x, y);
            y += ls * 1.1;
            gfx.DrawString("Below this text is a PDF form that will be visible when " +
                "viewed or printed with a PDF viewer.", fontItalic, XBrushes.Firebrick, x, y);
            y += ls * 1.1;
            XGraphicsState state = gfx.Save();
            XRect rcImage = new XRect(100, y, 100, 100 * Math.Sqrt(2));
            gfx.DrawRectangle(XBrushes.Snow, rcImage);
            //gfx.DrawImage(XPdfForm.FromFile("../../../../../PDFs/SomeLayout.pdf"), rcImage);
            gfx.Restore(state);

            document.Save(filename);

            LaunchPdf(filename);
        }

        [TestMethod]
        public void Unicode()
        {
            // Create new document
            PdfDocument document = new PdfDocument();

            // Set font encoding to unicode
            XPdfFontOptions options = new XPdfFontOptions(PdfFontEncoding.Unicode);

            XFont font = new XFont("Times New Roman", 12, XFontStyle.Regular, options);

            // Draw text in different languages
            for (int idx = 0; idx < texts.Length; idx++)
            {
                PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XTextFormatter tf = new XTextFormatter(gfx);
                tf.Alignment = XParagraphAlignment.Left;

                tf.DrawString(texts[idx], font, XBrushes.Black,
                  new XRect(100, 100, page.Width - 200, 600), XStringFormats.TopLeft);
            }

            const string filename = "Unicode_tempfile.pdf";
            // Save the document...
            document.Save(filename);
            // ...and start a viewer.
            LaunchPdf(filename);
        }

        [TestMethod]
        public void UnProtectDocument()
        {
            // Get a fresh copy of the sample PDF file.
            // The passwords are 'user' and 'owner' in this sample.
            const string filenameSource = "HelloWorld (protected).pdf";
            const string filenameDest = "HelloWorld_tempfile.pdf";
            File.Copy(Path.Combine("../../../../../PDFs/", filenameSource),
              Path.Combine(Directory.GetCurrentDirectory(), filenameDest), true);

            PdfDocument document;

            // Opening a document will fail with an invalid password.
            try
            {
                document = PdfReader.Open(filenameDest, "invalid password");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            // You can specifiy a delegate, which is called if the document needs a
            // password. If you want to modify the document, you must provide the
            // owner password.
            document = PdfReader.Open(filenameDest, PdfDocumentOpenMode.Modify, PasswordProvider);

            // Open the document with the user password.
            document = PdfReader.Open(filenameDest, "user", PdfDocumentOpenMode.ReadOnly);

            // Use the property HasOwnerPermissions to decide whether the used password
            // was the user or the owner password. In both cases PDFsharp provides full
            // access to the PDF document. It is up to the programmer who uses PDFsharp
            // to honor the access rights. PDFsharp doesn't try to protect the document
            // because this make little sence for an open source library.
            bool hasOwnerAccess = document.SecuritySettings.HasOwnerPermissions;

            // Open the document with the owner password.
            document = PdfReader.Open(filenameDest, "owner");
            hasOwnerAccess = document.SecuritySettings.HasOwnerPermissions;

            // A document opened with the owner password is completely unprotected
            // and can be modified.
            XGraphics gfx = XGraphics.FromPdfPage(document.Pages[0]);
            gfx.DrawString("Some text...",
              new XFont("Times New Roman", 12), XBrushes.Firebrick, 50, 100);

            // The modified document is saved without any protection applied.
            PdfDocumentSecurityLevel level = document.SecuritySettings.DocumentSecurityLevel;

            // If you want to save it protected, you must set the DocumentSecurityLevel
            // or apply new passwords.
            // In the current implementation the old passwords are not automatically
            // reused. See 'ProtectDocument' sample for further information.

            // Save the document...
            document.Save(filenameDest);
            // ...and start a viewer.
            Process.Start(filenameDest);
        }

        [TestMethod]
        public void Watermarks()
        {
            /*
            // Variation 1: Draw watermark as text string

            // Get an XGraphics object for drawing beneath the existing content
            XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Prepend);

            // Get the size (in point) of the text
            XSize size = gfx.MeasureString(watermark, font);

            // Define a rotation transformation at the center of the page
            gfx.TranslateTransform(page.Width / 2, page.Height / 2);
            gfx.RotateTransform(-Math.Atan(page.Height / page.Width) * 180 / Math.PI);
            gfx.TranslateTransform(-page.Width / 2, -page.Height / 2);

            // Create a string format
            XStringFormat format = new XStringFormat();
            format.Alignment = XStringAlignment.Near;
            format.LineAlignment = XLineAlignment.Near;

            // Create a dimmed red brush
            XBrush brush = new XSolidBrush(XColor.FromArgb(128, 255, 0, 0));

            // Draw the string
            gfx.DrawString(watermark, font, brush,
              new XPoint((page.Width - size.Width) / 2, (page.Height - size.Height) / 2),
              format);
            */

            /*
             // Variation 2: Draw watermark as outlined graphical path
 
                // Get an XGraphics object for drawing beneath the existing content
                XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Prepend);
 
                // Get the size (in point) of the text
                XSize size = gfx.MeasureString(watermark, font);
 
                // Define a rotation transformation at the center of the page
                gfx.TranslateTransform(page.Width / 2, page.Height / 2);
                gfx.RotateTransform(-Math.Atan(page.Height / page.Width) * 180 / Math.PI);
                gfx.TranslateTransform(-page.Width / 2, -page.Height / 2);
 
                // Create a graphical path
                XGraphicsPath path = new XGraphicsPath();
 
                // Add the text to the path
                path.AddString(watermark, font.FontFamily, XFontStyle.BoldItalic, 150,
                  new XPoint((page.Width - size.Width) / 2, (page.Height - size.Height) / 2),
                  XStringFormats.TopLeft);
 
                // Create a dimmed red pen
                XPen pen = new XPen(XColor.FromArgb(128, 255, 0, 0), 2);
 
                // Stroke the outline of the path
                gfx.DrawPath(pen, path);
             */

            /*
             // Variation 3: Draw watermark as transparent graphical path above text
 
                // Get an XGraphics object for drawing above the existing content
                XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
 
                // Get the size (in point) of the text
                XSize size = gfx.MeasureString(watermark, font);
 
                // Define a rotation transformation at the center of the page
                gfx.TranslateTransform(page.Width / 2, page.Height / 2);
                gfx.RotateTransform(-Math.Atan(page.Height / page.Width) * 180 / Math.PI);
                gfx.TranslateTransform(-page.Width / 2, -page.Height / 2);
 
                // Create a graphical path
                XGraphicsPath path = new XGraphicsPath();
 
                // Add the text to the path
                path.AddString(watermark, font.FontFamily, XFontStyle.BoldItalic, 150,
                  new XPoint((page.Width - size.Width) / 2, (page.Height - size.Height) / 2),
                  XStringFormats.TopLeft);
 
                // Create a dimmed red pen and brush
                XPen pen = new XPen(XColor.FromArgb(50, 75, 0, 130), 3);
                XBrush brush = new XSolidBrush(XColor.FromArgb(50, 106, 90, 205));
 
                // Stroke the outline of the path
                gfx.DrawPath(pen, brush, path);
             */
        }

        protected void LaunchPdf(string Pdf)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo(Pdf)
            {
                UseShellExecute = true
            };
            p.Start();
        }

        static readonly string[] texts = new string[]
            {
  // International version of the text in English
                "English\n" +
  "PDFsharp is a .NET library for creating and processing PDF documents 'on the fly'. " +
  "The library is completely written in C# and based exclusively on safe, managed code. " +
  "PDFsharp offers two powerful abstraction levels to create and process PDF documents.\n" +
  "For drawing text, graphics, and images there is a set of classes which are modeled similar to the classes " +
  "of the name space System.Drawing of the .NET framework. With these classes it is not only possible to create " +
  "the content of PDF pages in an easy way, but they can also be used to draw in a window or on a printer.\n" +
  "Additionally PDFsharp completely models the structure elements PDF is based on. With them existing PDF documents " +
  "can be modified, merged, or split with ease.\n" +
  "The source code of PDFsharp is Open Source under the MIT license (http://en.wikipedia.org/wiki/MIT_License). " +
  "Therefore it is possible to use PDFsharp without limitations in non open source or commercial projects/products.",
 
  // PDFsharp is 'Made in Germany'
  "German\n" +
  "PDFsharp ist eine .NET-Bibliothek zum Erzeugen und Verarbeiten von PDF-Dokumenten 'On the Fly'. " +
  "Die Bibliothek ist vollständig in C# geschrieben und basiert ausschließlich auf sicherem, verwaltetem Code. " +
  "PDFsharp bietet zwei leistungsstarke Abstraktionsebenen zur Erstellung und Verarbeitung von PDF-Dokumenten.\n" +
  "Zum Zeichnen von Text, Grafik und Bildern gibt es einen Satz von Klassen, die sehr stark an die Klassen " +
  "des Namensraums System.Drawing des .NET Frameworks angelehnt sind. Mit diesen Klassen ist es nicht " +
  "nur auf einfache Weise möglich, den Inhalt von PDF-Seiten zu gestalten, sondern sie können auch zum " +
  "Zeichnen in einem Fenster oder auf einem Drucker verwendet werden.\n" +
  "Zusätzlich modelliert PDFsharp vollständig die Stukturelemente, auf denen PDF basiert. Dadurch können existierende " +
  "PDF-Dokumente mit Leichtigkeit zerlegt, ergänzt oder umgebaut werden.\n" +
  "Der Quellcode von PDFsharp ist Open-Source unter der MIT-Lizenz (http://de.wikipedia.org/wiki/MIT-Lizenz). " +
  "Damit kann PDFsharp auch uneingeschränkt in Nicht-Open-Source- oder kommerziellen Projekten/Produkten eingesetzt werden.",
 
  // Greek version
  // The text was translated by Babel Fish. We here in Germany have no idea what it means.
  // If you are a native speaker please correct it and mail it to mailto: ((see Impressum))
  "Greek (Translated with Babel Fish)\n" +
  "Το PDFsharp είναι βιβλιοθήκη δικτύου α. για τη δημιουργία και την επεξεργασία των εγγράφων PDF 'σχετικά με τη μύγα'. "+
  "Η βιβλιοθήκη γράφεται εντελώς γ # και βασίζεται αποκλειστικά εκτός από, διοικούμενος κώδικας. "+
  "Το PDFsharp προσφέρει δύο ισχυρά επίπεδα αφαίρεσης για να δημιουργήσει και να επεξεργαστεί τα έγγραφα PDF. "+
  "Για το κείμενο, τη γραφική παράσταση, και τις εικόνες σχεδίων υπάρχει ένα σύνολο κατηγοριών που διαμορφώνονται "+
  "παρόμοιος με τις κατηγορίες του διαστημικού σχεδίου συστημάτων ονόματος του. πλαισίου δικτύου. "+
  "Με αυτές τις κατηγορίες που είναι όχι μόνο δυνατό να δημιουργηθεί το περιεχόμενο των σελίδων PDF με έναν εύκολο "+
  "τρόπο, αλλά αυτοί μπορεί επίσης να χρησιμοποιηθεί για να επισύρει την προσοχή σε ένα παράθυρο ή σε έναν εκτυπωτή. "+
  "Επιπλέον PDFsharp διαμορφώνει εντελώς τα στοιχεία PDF δομών είναι βασισμένο. Με τους τα υπάρχοντα έγγραφα PDF "+
  "μπορούν να τροποποιηθούν, συγχωνευμένος, ή να χωρίσουν με την ευκολία. Ο κώδικας πηγής PDFsharp είναι ανοικτή πηγή "+
  "με άδεια MIT (http://en.wikipedia.org/wiki/MIT_License). Επομένως είναι δυνατό να χρησιμοποιηθεί PDFsharp χωρίς "+
  "προβλήματα στη μη ανοικτή πηγή ή τα εμπορικά προγράμματα/τα προϊόντα.",
 
  // Russian version (by courtesy of Alexey Kuznetsov)
  "Russian\n" +
  "PDFsharp это .NET библиотека для создания и обработки PDF документов 'налету'. " +
  "Библиотека полностью написана на языке C# и базируется исключительно на безопасном, управляемом коде. " +
  "PDFsharp использует два мощных абстрактных уровня для создания и обработки PDF документов.\n" +
  "Для рисования текста, графики, и изображений в ней используется набор классов, которые разработаны аналогично с" +
  "пакетом System.Drawing, библиотеки .NET framework. С помощью этих классов возможно не только создавать" +
  "содержимое PDF страниц очень легко, но они так же позволяют рисовать напрямую в окне приложения или на принтере.\n" +
  "Дополнительно PDFsharp имеет полноценные модели структурированных базовых элементов PDF. Они позволяют работать с существующим PDF документами " +
  "для изменения их содержимого, склеивания документов, или разделения на части.\n" +
  "Исходный код PDFsharp библиотеки это Open Source распространяемый под лицензией MIT (http://ru.wikipedia.org/wiki/MIT_License). " +
  "Теоретически она позволяет использовать PDFsharp без ограничений в не open source проектах или коммерческих проектах/продуктах.",
 
  // Your language may come here
  "Invitation\n" +
  "If you use PDFsharp and haven't found your native language in this document, we will be pleased to get your translation of the text above and include it here.\n" +
  "Mail to ((see Impressum))"
};

        /// <summary>
        /// The 'get the password' call back function.
        /// </summary>
        static void PasswordProvider(PdfPasswordProviderArgs args)
        {
            // Show a dialog here in a real application
            args.Password = "owner";
        }
    }

    public class LayoutHelper
    {
        private readonly PdfDocument _document;
        private readonly XUnit _topPosition;
        private readonly XUnit _bottomMargin;
        private XUnit _currentPosition;

        public LayoutHelper(PdfDocument document, XUnit topPosition, XUnit bottomMargin)
        {
            _document = document;
            _topPosition = topPosition;
            _bottomMargin = bottomMargin;
            // Set a value outside the page - a new page will be created on the first request.
            _currentPosition = bottomMargin + 10000;
        }

        public XUnit GetLinePosition(XUnit requestedHeight)
        {
            return GetLinePosition(requestedHeight, -1f);
        }

        public XUnit GetLinePosition(XUnit requestedHeight, XUnit requiredHeight)
        {
            XUnit required = requiredHeight == -1f ? requestedHeight : requiredHeight;
            if (_currentPosition + required > _bottomMargin)
                CreatePage();
            XUnit result = _currentPosition;
            _currentPosition += requestedHeight;
            return result;
        }

        public XGraphics Gfx { get; private set; }
        public PdfPage Page { get; private set; }

        public void CreatePage()
        {
            Page = _document.AddPage();
            Page.Size = PageSize.A4;
            Gfx = XGraphics.FromPdfPage(Page);
            _currentPosition = _topPosition;
        }
    }
}
