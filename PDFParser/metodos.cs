using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.IO;
using System.Text.RegularExpressions;

namespace PDFParser
{
    public class Metodos
    {
        #region Pershing structs
        public struct PershingCuenta
        {
            public string account;
            public float total;
        }

        public struct PershingInteres
        {
            public int ip;
            public List<PershingCuenta> cuentas;
            public float total;
        }

        public struct PershingPDFOrdenado
        {
            public List<PershingInteres> intereses;
            public float all;
        }

        #endregion

        #region Morgan structs
        public struct MorganTransaccion
        {
            public string account;
            public string cusip;
            public string settlementDate;
            public string tradeDate;
            public char buySell;
            public string quantity;
            public string grossRevenue;
        }

        public struct MorganPDF
        {
            public List<MorganTransaccion> transacciones;
        }
        #region Nuevos structs de Morgan para devolver el PDF ordenado por account, por el issue correspondiente de github.
        public struct MorganMovimiento
        {
            public string cusip;
            public string settlementDate;
            public string tradeDate;
            public char buySell;
            public string quantity;
            public string grossRevenue;
        }

        public struct MorganCuenta
        {
            public string account;
            public List<MorganMovimiento> movimientos;
        }

        public struct MorganPDFOrdenado
        {
            public List<MorganCuenta> cuentas;
        }
        #endregion

        #endregion


        #region Auxiliares

        private static string[] ArrayPerPdfPage(string inputPdfPath)
        {
            PdfReader reader = new PdfReader(inputPdfPath);

            int numberOfPages = reader.NumberOfPages;

            string[] paginas = new string[numberOfPages + 1]; //dejo el primer lugar vacio, para que la pagina 1 se guarde en paginas[1] 

            for (int page = 1; page <= numberOfPages; page++)
            {
                string textoDelPDF = PdfTextExtractor.GetTextFromPage(reader, page);
                paginas[page] = textoDelPDF;
            }
            reader.Close();
            return paginas;
        }

        #endregion

        #region Funciones para parsear cada transaccion/interes

        #region Pershing
        private static string ObtenerOFFDeEsaLinea(string linea)
        {
            linea = linea.Substring(6);
            IgnorarEspaciosIniciales(ref linea);
            char letra = linea[0];
            string s = "";
            int i = 0;
            while (letra != ' ')
            {
                s += letra;
                i++;
                letra = linea[i];
            }
            return s;
        }

        private static int ObtenerIPDeEsaLinea(string linea)
        {
            linea = linea.Substring(12); //me posiciono en el dato
            IgnorarEspaciosIniciales(ref linea);
            char letra = linea[0];
            string s = "";
            int i = 0;
            while (letra != ' ')
            {
                s += letra;
                i++;
                letra = linea[i];
            }
            int retorno = Convert.ToInt32(s);
            return retorno;
        }

        private static string ObtenerAccountDeEsaLineaPershing(string linea)
        {
            linea = linea.Substring(18); //me posiciono en el dato
            IgnorarEspaciosIniciales(ref linea);
            char letra = linea[0];
            string s = "";
            int i = 0;
            while (letra != ' ')
            {
                s += letra;
                i++;
                letra = linea[i];
            }
            return s;
        }


        private static float ObtenerNPLPartDeEsaLinea(string linea)
        {
            string s = "";
            if (linea.Length < 93) //puede que no tenga NPL Part, segun se ve en 
            {
                return 0;
            }
            else
            {
                linea = linea.Substring(93); //me salto las primeras columnas, voy directo a la ultima
                IgnorarEspaciosIniciales(ref linea);

                int i = 0;
                while (i < linea.Length && linea[i] != ' ')
                {
                    s += linea[i];
                    i++;
                }
                float retorno = Convert.ToSingle(s);
                return retorno;
            }
        }

        ///<summary>Se encarga de decidir si la linea en cuestion es un interes (o sea, son dos o mas lineas continuas con datos para procesar)
        ///o si es un total verficador de los del final del documento. </summary>
        private static bool EsInteres(string[] todasLasLineas, int lineaEspecifica)
        {
            string account = ObtenerAccountDeEsaLineaPershing(todasLasLineas[lineaEspecifica]);
            if (account == "TOTALS")
            {
                return false;
            }
            return true;
        }

        private static float ProcesarTodosLosTotalesFinales(string[] todasLasLineas, ref int lineaEspecifica)
        {
            while (ObtenerOFFDeEsaLinea(todasLasLineas[lineaEspecifica]) != "ALL")
            //si no es el total marcado con "all", entonces salto esa linea, y salto tambien la siguiente porque esta en blanco, y repito.
            {
                lineaEspecifica += 2;
            }
            //ahora que sali del while, retorno su "NPL Part"
            float retorno = ObtenerNPLPartDeEsaLinea(todasLasLineas[lineaEspecifica]);
            return retorno;
        }


        private static PershingInteres ParsearUnInteresPershing(string[] todasLasLineas, ref int lineaEspecifica)
        {
            string lineaActual = todasLasLineas[lineaEspecifica];
            PershingInteres interes = new PershingInteres();
            interes.cuentas = new List<PershingCuenta>();
            PershingCuenta cuenta;

            interes.ip = ObtenerIPDeEsaLinea(lineaActual);
            cuenta.account = ObtenerOFFDeEsaLinea(lineaActual) + ObtenerAccountDeEsaLineaPershing(lineaActual);
            cuenta.total = ObtenerNPLPartDeEsaLinea(lineaActual);
            interes.cuentas.Add(cuenta);

            lineaEspecifica++;
            lineaActual = todasLasLineas[lineaEspecifica]; //bajo una linea, para chequear si es la ultima de ese interes o -
                                                           //- hay mas cuentas relacionadas a el.

            while (ObtenerAccountDeEsaLineaPershing(lineaActual) != "TOTALS")
            {
                cuenta.account = ObtenerOFFDeEsaLinea(lineaActual) + ObtenerAccountDeEsaLineaPershing(lineaActual);
                cuenta.total = ObtenerNPLPartDeEsaLinea(lineaActual);
                interes.cuentas.Add(cuenta);

                lineaEspecifica++;
                lineaActual = todasLasLineas[lineaEspecifica];
            }

            interes.total = ObtenerNPLPartDeEsaLinea(lineaActual);
            lineaEspecifica++; //luego de cada interes, tenemos una linea en blanco, que con esto 

            return interes;
        }

        #endregion

        #region Morgan-Stanley

        private static void IgnorarNumeritoInicial(ref string fila)
        {
            //funcion para ignorar el campo del numerito inicial del tipo "658 206"
            fila = fila.Substring(8);
        }

        private static string ObtenerAccountDeEsaLinea(ref string line)
        {
            IgnorarNumeritoInicial(ref line);
            char letra = line[0];
            string s = "";
            int i = 0;
            while (letra != ' ')
            {
                s += letra;
                i++;
                letra = line[i];
            }
            line = line.Substring(i); //esto me permite que en line ya no este el account, lo corto
            return s;
        }


        private static string ObtenerCusipDeEsaLinea(ref string line)
        {
            IgnorarNumeritoInicial(ref line);
            char letra = line[0];
            string s = "";
            int i = 0;
            while (letra != ' ')
            {
                s += letra;
                i++;
                letra = line[i];
            }
            line = line.Substring(i); //esto me permite que en line ya no este el account, lo corto
            return s;
        }


        private static string ObtenerSettlementDateDeEsaLinea(ref string line)
        {
            line = line.Substring(36);
            IgnorarEspaciosIniciales(ref line);
            string s = line.Substring(0, 10);
            line = line.Substring(10); //corto el dato
            return s;
        }

        private static string ObtenerTradeDateDeEsaLinea(ref string line)
        {
            line = line.Substring(36);
            IgnorarEspaciosIniciales(ref line);
            string s = line.Substring(0, 10);
            line = line.Substring(10); //corto el dato
            return s;
        }

        private static char ObtenerBuySellDeEsaLinea(ref string line)
        {
            IgnorarEspaciosIniciales(ref line);
            char retorno = line[0];
            line = line.Substring(1);
            return retorno;
        }

        private static string ObtenerQuantityDeEsaLinea(ref string line)
        {
            line = line.Substring(16);
            IgnorarEspaciosIniciales(ref line);
            char letra = line[0];
            int i = 0;
            string s = "";
            while (letra != ' ')
            {
                s = s + letra.ToString();
                i++;
                letra = line[i];
            }
            line = line.Substring(i); //corto el dato
            return s;
        }

        private static string ObtenerGrossRevenueDeEsaLinea(ref string line)
        {
            IgnorarEspaciosIniciales(ref line);
            char letra = line[0];
            int i = 0;
            string s = "";
            while (letra != ' ')
            {
                if (letra != '-') //para omitir los guioncitos molestos que aparecen al azar a veces en "Gross-Revenue"
                {
                    s = s + letra.ToString();
                }
                i++;
                letra = line[i];
            }
            line = line.Substring(i); //corto el dato

            return s;
        }


        static MorganTransaccion ParsearUnaTransaccionMorgan(string[] todasLasLineasDeLaPagina, int initialLine)
        {
            //cada dos filas, tenemos una transaccion. Este metodo parsea eso.
            string lineaInicial = todasLasLineasDeLaPagina[initialLine];
            string segundaLinea = todasLasLineasDeLaPagina[initialLine + 1];
            MorganTransaccion retorno = new MorganTransaccion();
            retorno.account = ObtenerAccountDeEsaLinea(ref lineaInicial);
            retorno.cusip = ObtenerCusipDeEsaLinea(ref segundaLinea);
            retorno.settlementDate = ObtenerSettlementDateDeEsaLinea(ref lineaInicial);
            retorno.tradeDate = ObtenerTradeDateDeEsaLinea(ref segundaLinea);
            retorno.buySell = ObtenerBuySellDeEsaLinea(ref segundaLinea);
            retorno.quantity = ObtenerQuantityDeEsaLinea(ref lineaInicial);
            retorno.grossRevenue = ObtenerGrossRevenueDeEsaLinea(ref lineaInicial);
            return retorno;
        }
        #endregion

        #region Auxiliares
        private static void IgnorarEspaciosIniciales(ref string line)
        {
            char letra = line[0];
            int i = 0;
            while (i < line.Length && line[i] == ' ')
            {
                i++;
            }
            line = line.Substring(i);
        }

        private static bool EsLineaEnBlanco(string linea)
        {
            bool retorno = true;
            foreach (char c in linea)
            {
                if (c != ' ')
                {
                    retorno = false;
                    break;
                }
            }

            return retorno;
        }

        #endregion

        #endregion

        #region Metodos de Parseo pagina por pagina

        private static PershingPDFOrdenado ParsearUnaPaginaTxtPershing(string[] paginas, int pageNumber)
        {
            string[] todasLasLineas = paginas[pageNumber].Split('\n');
            int lineaEspecifica = 6; //Las primeras 6 filas son encabezado, no datos (son el nombre del banco y de las columnas)
            PershingPDFOrdenado retorno = new PershingPDFOrdenado();
            List<PershingInteres> intereses = new List<PershingInteres>();
            PershingInteres interesParseado;
            bool termine = false;
            while (lineaEspecifica < todasLasLineas.Length - 1 && !termine)
            {
                if (!EsLineaEnBlanco(todasLasLineas[lineaEspecifica]))
                {
                    if (EsInteres(todasLasLineas, lineaEspecifica))
                    {
                        interesParseado = ParsearUnInteresPershing(todasLasLineas, ref lineaEspecifica);
                        intereses.Add(interesParseado);
                        //lineaEspecifica += 2; lo cambio, esto es variable, por tanto lo maneja/incrementa el 
                        //parser de intereses, que tiene acceso directo a los datos 
                    }
                    else // si no es un interes, entonces es un "total final verificador"
                    {
                        retorno.all = ProcesarTodosLosTotalesFinales(todasLasLineas, ref lineaEspecifica);
                        termine = true;
                    }
                }
                else
                {
                    lineaEspecifica += 1;
                }
            }
            retorno.intereses = intereses;
            return retorno;
        }

        private static PershingPDFOrdenado ProcesarTxtPagesPershing(string[] paginas)
        {
            PershingPDFOrdenado retorno = new PershingPDFOrdenado();
            for (int page = 1; page <= paginas.Length - 1; page++)
            {
                retorno = ParsearUnaPaginaTxtPershing(paginas, page);
            }
            return retorno;
        }


        private static List<MorganTransaccion> ParsearUnaPaginaTxtMorganStanley(string[] paginas, int pageNumber)
        {
            List<MorganTransaccion> listaRetorno = new List<MorganTransaccion>();
            string[] todasLasLineas = paginas[pageNumber].Split('\n');
            //Las primeras 9 filas son encabezado, no datos (son el nombre del banco y de las columnas, las ignoro
            int lineaEspecifica = 9;
            MorganTransaccion transaccionParseada = ParsearUnaTransaccionMorgan(todasLasLineas, lineaEspecifica);
            listaRetorno.Add(transaccionParseada);
            lineaEspecifica += 2;
            while (lineaEspecifica < todasLasLineas.Length)
            {
                if (!EsLineaEnBlanco(todasLasLineas[lineaEspecifica]))
                {
                    transaccionParseada = ParsearUnaTransaccionMorgan(todasLasLineas, lineaEspecifica);
                    listaRetorno.Add(transaccionParseada);
                }
                lineaEspecifica += 2;
            }
            return listaRetorno;
        }

        private static List<MorganTransaccion> ProcesarTxtPagesMorganStanley(string[] paginas)
        {
            List<MorganTransaccion> retorno = new List<MorganTransaccion>();
            for (int page = 1; page <= paginas.Length - 1; page++)
            {
                retorno = retorno.Concat(ParsearUnaPaginaTxtMorganStanley(paginas, page)).ToList();
            }
            return retorno;
        }
        #endregion


        public static MorganPDF ParsearPDFMorgan(string filePath)
        {
            string[] paginas = ArrayPerPdfPage(filePath);
            List<MorganTransaccion> listaTransacciones = ProcesarTxtPagesMorganStanley(paginas);
            MorganPDF retorno = new MorganPDF();
            retorno.transacciones = listaTransacciones;
            return retorno;
        }

        public static MorganPDFOrdenado ProcesarPDFMorgan(string filePath)
        {
            MorganPDF pdfParseado = ParsearPDFMorgan(filePath);
            MorganPDFOrdenado pdfRetornoOrdenado = new MorganPDFOrdenado();
            pdfRetornoOrdenado.cuentas = new List<MorganCuenta>();

            //explico estos bucles horribles...
            //1-recorro una a una las transacciones parseadas
            //2-agarro su account, y busco cada otra transaccion que tenga el mismo account (segundo bucle)
            //3-cuando la encuentro, copio los datos en un objeto "movimiento", lo agrego a los movimientos de esa cuenta particular
            


            for(int i=0; i < pdfParseado.transacciones.Count; i++)
            {
                MorganCuenta cuenta = new MorganCuenta();
                cuenta.account = pdfParseado.transacciones[i].account;
                cuenta.movimientos = new List<MorganMovimiento>();

                foreach (MorganTransaccion morganTransaccion in pdfParseado.transacciones.Where(morganTransaccion => morganTransaccion.account == pdfParseado.transacciones[i].account))
                {
                    MorganMovimiento movimiento = new MorganMovimiento();
                    movimiento.cusip = morganTransaccion.cusip;
                    movimiento.tradeDate = morganTransaccion.tradeDate;
                    movimiento.settlementDate = morganTransaccion.settlementDate;
                    movimiento.buySell = morganTransaccion.buySell;
                    movimiento.quantity = morganTransaccion.quantity;
                    movimiento.grossRevenue = morganTransaccion.grossRevenue;

                    cuenta.movimientos.Add(movimiento);
                }

                pdfParseado.transacciones.RemoveAll(transaccion => transaccion.account == pdfParseado.transacciones[i].account);
                pdfRetornoOrdenado.cuentas.Add(cuenta);
            }

            return pdfRetornoOrdenado;
        }

        public static PershingPDFOrdenado ProcesarPDFPershing(string filePath)
        {
            string[] paginas = ArrayPerPdfPage(filePath);
            return ProcesarTxtPagesPershing(paginas);
        }


        #region Developer tools
        public static void PDFDeveloperFileMorgan(MorganPDFOrdenado morgan, string outputTxtFilePath)
        {
            string retorno = "";
            foreach (MorganCuenta mc in morgan.cuentas)
            {
                retorno += "{ Account: " + mc.account;

                foreach (MorganMovimiento mm in mc.movimientos)
                {
                    retorno += "\r\nMovimiento: [" + "Cusip: " + mm.cusip + " Settlement Date: " + mm.settlementDate + " Trade Date: " + mm.tradeDate + " BuySell: " + mm.buySell + " quantity: " + mm.quantity + " grossRevenue: " + mm.grossRevenue + "]\r\n";
                }
                retorno += "}\r\n";

            }
            using (StreamWriter sw = File.CreateText(outputTxtFilePath))
            {
                sw.Write(retorno);
            }
        }

        public static void PDFDeveloperFilePershing(PershingPDFOrdenado pershing, string outputTxtFilePath)
        {
            string retorno = "";
            foreach (PershingInteres pi in pershing.intereses)
            {
                retorno += "{ IP: " + pi.ip + " Cuentas: [ \r\n";
                foreach (PershingCuenta pc in pi.cuentas)
                {
                    retorno += " cuenta: " + pc.account;
                    retorno += " total: " + pc.total;
                }
                retorno += "]\r\nTotal: " + pi.total + " }\r\n";

            }
            retorno += "All: " + pershing.all + "\r\n}";
            using (StreamWriter sw = File.CreateText(outputTxtFilePath))
            {
                sw.Write(retorno);
            }
        }
        #endregion
    }
}
