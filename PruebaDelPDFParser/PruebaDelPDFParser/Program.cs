using PdfParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PdfParser.Metodos;

namespace PruebaDelPDFParser
{
    class Program
    {
        static void Main(string[] args)
        {
            string pdfPathPershing = @"C:\Users\Ezequiel\Desktop\pdfParser\SourcePDFs\Intereses_NDG_(reporte_de_NetX360_FUN070DH).pdf";
            string pdfPathMorgan = @"C:\Users\Ezequiel\Desktop\pdfParser\SourcePDFs\Linstal_APRIL_2018_Run.pdf";
            string finalOutputTxtFilePathPershing = @"C:\PDFParser\Results\Pershing.txt";
            string finalOutputTxtFilePathMorgan = @"C:\PDFParser\Results\Morgan.txt";

            SdtPershingPDF pershingPDFProcesado = ProcesarPDFPershing(pdfPathPershing);
            SdtMorganPDF morganPDFProcesado = ProcesarPDFMorgan(pdfPathMorgan);
            Console.WriteLine("PDFs Procesados con exito!");
            Console.ReadKey();
            Console.Clear();

            //PDFDeveloperFileMorgan(morganPDFProcesado, finalOutputTxtFilePathMorgan);
            //PDFDeveloperFilePershing(pershingPDFProcesado, finalOutputTxtFilePathPershing);
            //Console.WriteLine("Archivos Creados!");
            //Console.ReadKey();
            //Console.Clear();

        }
    }
}
