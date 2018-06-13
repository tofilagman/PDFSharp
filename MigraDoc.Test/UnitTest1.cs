using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using System.Diagnostics;

namespace MigraDoc.Test
{
    [TestClass]
    public class MigraDocTest
    {
        [TestMethod]
        public void Borders()
        {
            string outputFile = "Borders.pdf";

            Document document = new Document();
            Section sec = document.Sections.AddSection();
            sec.AddParagraph("A paragraph before.");
            Table table = sec.AddTable();
            table.Borders.Visible = true;
            table.AddColumn();
            table.AddColumn();
            table.Rows.HeightRule = RowHeightRule.Exactly;
            table.Rows.Height = 14;
            Row row = table.AddRow();
            Cell cell = row.Cells[0];
            cell.Borders.Visible = true;
            cell.Borders.Left.Width = 8;
            cell.Borders.Right.Width = 2;
            cell.AddParagraph("First Cell");

            row = table.AddRow();
            cell = row.Cells[1];
            cell.AddParagraph("Last Cell within this table");
            cell.Borders.Bottom.Width = 15;
            cell.Shading.Color = Colors.LightBlue;
            sec.AddParagraph("A Paragraph afterwards");
            PdfDocumentRenderer renderer = new PdfDocumentRenderer();
            renderer.Document = document;
            renderer.RenderDocument();
            renderer.PdfDocument.Save(outputFile);

            LaunchPdf(outputFile);
        }

        [TestMethod]
        public   void CellMerge( )
        {
            string outputFile = "CellMerge.pdf";

            Document document = new Document();
            Section sec = document.Sections.AddSection();
            sec.AddParagraph("A paragraph before.");
            Table table = sec.AddTable();
            table.Borders.Visible = true;
            table.AddColumn();
            table.AddColumn();
            Row row = table.AddRow();
            Cell cell = row.Cells[0];
            cell.MergeRight = 1;
            cell.Borders.Visible = true;
            cell.Borders.Left.Width = 8;
            cell.Borders.Right.Width = 2;
            cell.AddParagraph("First Cell");

            row = table.AddRow();
            cell = row.Cells[1];
            cell.AddParagraph("Last Cell within this row");
            cell.MergeDown = 1;
            cell.Borders.Bottom.Width = 15;
            cell.Borders.Right.Width = 30;
            cell.Shading.Color = Colors.LightBlue;
            row = table.AddRow();
            sec.AddParagraph("A Paragraph afterwards");
            PdfDocumentRenderer renderer = new PdfDocumentRenderer();
            renderer.Document = document;
            renderer.RenderDocument();
            renderer.PdfDocument.Save(outputFile);

            LaunchPdf(outputFile);
        }

        [TestMethod]
        public void VerticalAlign( )
        {
            string outputFile = "VerticalAlign.pdf";
            Document document = new Document();
            Section sec = document.Sections.AddSection();
            sec.AddParagraph("A paragraph before.");
            Table table = sec.AddTable();
            table.Borders.Visible = true;
            table.AddColumn();
            table.AddColumn();
            Row row = table.AddRow();
            row.HeightRule = RowHeightRule.Exactly;
            row.Height = 70;
            row.VerticalAlignment = VerticalAlignment.Center;
            row[0].AddParagraph("First Cell");
            row[1].AddParagraph("Second Cell");
            sec.AddParagraph("A Paragraph afterwards.");


            PdfDocumentRenderer renderer = new PdfDocumentRenderer();
            renderer.Document = document;
            renderer.RenderDocument();
            renderer.PdfDocument.Save(outputFile);

            LaunchPdf(outputFile);
        }

        [TestMethod]
        public void TwoParagraphs()
        {
            string outputFile = "TwoParagraphs.pdf";
            Document doc = new Document();
            Section sec = doc.Sections.AddSection();

            sec.PageSetup.TopMargin = 0;
            sec.PageSetup.BottomMargin = 0;

            Paragraph par1 = sec.AddParagraph();
            TestParagraphRenderer.FillFormattedParagraph(par1);
            TestParagraphRenderer.GiveBorders(par1);
            par1.Format.SpaceAfter = "2cm";
            par1.Format.SpaceBefore = "3cm";
            Paragraph par2 = sec.AddParagraph();
            TestParagraphRenderer.FillFormattedParagraph(par2);
            TestParagraphRenderer.GiveBorders(par2);
            par2.Format.SpaceBefore = "3cm";

            PdfDocumentRenderer renderer = new PdfDocumentRenderer();
            renderer.Document = doc;
            renderer.RenderDocument();
            renderer.PdfDocument.Save(outputFile);

            LaunchPdf(outputFile);
        }

        [TestMethod]
        public void A1000Paragraphs()
        {
            string outputFile = "TwoParagraphs.pdf";
            Document doc = new Document();
            Section sec = doc.Sections.AddSection();

            sec.PageSetup.TopMargin = 0;
            sec.PageSetup.BottomMargin = 0;

            for (int idx = 1; idx <= 1000; ++idx)
            {
                Paragraph par = sec.AddParagraph();
                par.AddText("Paragraph " + idx + ": ");
                TestParagraphRenderer.FillFormattedParagraph(par);
                TestParagraphRenderer.GiveBorders(par);
            }
            PdfDocumentRenderer renderer = new PdfDocumentRenderer();
            renderer.Document = doc;
            renderer.RenderDocument();
            renderer.PdfDocument.Save(outputFile);
            LaunchPdf(outputFile);
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
    }
}
