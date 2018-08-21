using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace PDFParser
{
    public class Metodos
    {
        const string archivoDePrueba = "C:\\Users\\laptop1\\Downloads\\PDFS\\Linstal_APRIL_2018_Run.pdf";
        public static string ExtractTextFromPdf(string path)
        {
            PdfReader reader = new PdfReader(path);

            StringBuilder text = new StringBuilder();

            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                text.Append(PdfTextExtractor.GetTextFromPage(reader, i));
            }
            return text.ToString();
        }

        
      
    }
}
