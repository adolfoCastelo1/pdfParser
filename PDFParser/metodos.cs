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

        private static string PershingOMorgan(string filePath)
        {
            string text = "";
            PdfReader reader = new PdfReader(filePath);
            text += PdfTextExtractor.GetTextFromPage(reader, 1);
            reader.Close();
            string[] texto = text.Split('\n');
            string primeraLinea = texto[0].ToString();
            IgnorarEspaciosIniciales(ref primeraLinea);
            string s = "";
            int i = 0;
            while (primeraLinea[i] != ' ')
            {
                s += primeraLinea[i];
                i++;
            }
            if (s == "PERSHING")
            {
                return "P";
            }
            else
            {
                return "M";
            }
        }
        #endregion

        #region Funciones para parsear cada transaccion

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


        private static string ObtenerIPDeEsaLinea(string linea)
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
            return s;
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


        private static string ObtenerNPLPartDeEsaLinea(string linea)
        {
            string s = "";
            if (linea.Length < 93) //puede que no tenga NPL Part, segun se ve en 
            {
                return "0";
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
                return s;
            }
        }


        private static string ParsearUnInteresPershing(string[] todasLasLineas, ref int lineaEspecifica)
        {
            string lineaActual = todasLasLineas[lineaEspecifica];
            string retorno = "";

            retorno = "{\"IP\": " + ObtenerIPDeEsaLinea(lineaActual) + ", ";
            retorno += "\"Cuentas\": [{\"Account\" : " + ObtenerOFFDeEsaLinea(lineaActual) + ObtenerAccountDeEsaLineaPershing(lineaActual) + ", ";
            retorno += "\"Total\": " + ObtenerNPLPartDeEsaLinea(lineaActual);
            lineaEspecifica++;
            lineaActual = todasLasLineas[lineaEspecifica]; //bajo una linea, para chequear si es la ultima de ese interes o -
                                                           //- hay mas cuentas relacionadas a el.

            while (ObtenerAccountDeEsaLineaPershing(lineaActual) != "TOTALS")
            {
                retorno += "},  \r\n {\"Account\" : " + ObtenerOFFDeEsaLinea(lineaActual) + ObtenerAccountDeEsaLineaPershing(lineaActual) + ", ";
                retorno += "\"Total\": " + ObtenerNPLPartDeEsaLinea(lineaActual);
                lineaEspecifica++;
                lineaActual = todasLasLineas[lineaEspecifica];
            }

            retorno += "}], \"Total\" : " + ObtenerNPLPartDeEsaLinea(lineaActual) + "}";
            lineaEspecifica++; //luego de cada interes, tenemos una linea en blanco, que con esto 

            return retorno;
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

        private static string ObtenerBuySellDeEsaLinea(ref string line)
        {
            IgnorarEspaciosIniciales(ref line);
            string retorno = line[0].ToString();
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


        static string ParsearUnaTransaccionMorgan(string[] todasLasLineasDeLaPagina, int initialLine)
        {
            //cada dos filas, tenemos una transaccion. Este metodo parsea eso.
            string lineaInicial = todasLasLineasDeLaPagina[initialLine];
            string segundaLinea = todasLasLineasDeLaPagina[initialLine + 1];
            string retorno = "{Account : " + ObtenerAccountDeEsaLinea(ref lineaInicial) + ", ";
            retorno += "Cusip : " + ObtenerCusipDeEsaLinea(ref segundaLinea) + ", ";
            retorno += "Settlement Date : " + ObtenerSettlementDateDeEsaLinea(ref lineaInicial) + ", ";
            retorno += "Trade Date: " + ObtenerTradeDateDeEsaLinea(ref segundaLinea) + ", ";
            retorno += "BuySell: " + ObtenerBuySellDeEsaLinea(ref segundaLinea) + ", ";
            retorno += "Quantity : " + ObtenerQuantityDeEsaLinea(ref lineaInicial) + ", ";
            retorno += "Gross Revenue : " + ObtenerGrossRevenueDeEsaLinea(ref lineaInicial) + "}";
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
        
        private static string ParsearUnaPaginaTxtPershing(string[] paginas, int pageNumber)
        {
            string[] todasLasLineas = paginas[pageNumber].Split('\n');
            int lineaEspecifica = 6; //Las primeras 6 filas son encabezado, no datos (son el nombre del banco y de las columnas)
            string transaccionParseada;
            string retorno = "";
            #region Turbio
            //while (lineaEspecifica < todasLasLineas.Length) 
            //El -8 es para evitar las ultimas 8 lineas especiales de totales verificadores
            while (lineaEspecifica < todasLasLineas.Length - 8)
            #endregion
            {
                if (!EsLineaEnBlanco(todasLasLineas[lineaEspecifica]))
                {
                    transaccionParseada = ParsearUnInteresPershing(todasLasLineas, ref lineaEspecifica);
                    retorno += transaccionParseada + ",\r\n";
                    //lineaEspecifica += 2; lo cambio, esto es variable, por tanto lo maneja/incrementa el 
                    //parser de intereses, que tiene acceso directo a los datos 
                }
                else
                {
                    lineaEspecifica += 1;
                }
            }
            //para que el ultimo interes no tenga la comita "," en offside al final. 
            retorno = retorno.Remove(retorno.Length - 3);
            retorno += "\r\n";
            return retorno;
        }

        private static string ProcesarTxtPagesPershing(string[] paginas)
        {
            string retorno = "{ \r\n\"PDF\":\r\n{\r\n\"Intereses\":[\r\n";
            for (int page = 1; page <= paginas.Length - 1; page++)
            {
                retorno += ParsearUnaPaginaTxtPershing(paginas, page);
            }
            retorno += "]\r\n}\r\n}";
            return retorno;
        }


        private static string ParsearUnaPaginaTxtMorganStanley(string[] paginas, int pageNumber)
        {
            string[] todasLasLineas = paginas[pageNumber].Split('\n');
            //Las primeras 9 filas son encabezado, no datos (son el nombre del banco y de las columnas, las ignoro
            int lineaEspecifica = 9;
            string transaccionParseada = ParsearUnaTransaccionMorgan(todasLasLineas, lineaEspecifica);
            string retorno = "";
            while (lineaEspecifica < todasLasLineas.Length)
            {
                if (!EsLineaEnBlanco(todasLasLineas[lineaEspecifica]))
                {
                    transaccionParseada = ParsearUnaTransaccionMorgan(todasLasLineas, lineaEspecifica);
                    retorno += transaccionParseada + ",\r\n";
                }
                lineaEspecifica += 2;
            }
            //para que la ultima transaccion no tenga la comita "," en offside al final. 
            retorno = retorno.Remove(retorno.Length-3);
            retorno += "\r\n";
            return retorno;
        }

        private static string ProcesarTxtPagesMorganStanley(string[] paginas)
        {
            string retorno = "{ \r\n\"PDF\":\r\n{\r\n\"Transacciones\":[\r\n";
            for (int page = 1; page <= paginas.Length - 1; page++)
            {
                retorno += ParsearUnaPaginaTxtMorganStanley(paginas, page);
            }
            retorno += "]\r\n    }\r\n}";
            return retorno;
        }
        #endregion

        /// <summary> 
        /// Dado un path hacia el archivo PDF, devuelve un string con estilo JSON
        /// </summary>
        /// <returns>string PDF</returns>
        public static string ProcesarPDF(string filePath)
        {
            string[] paginas = ArrayPerPdfPage(filePath);
            if (PershingOMorgan(filePath) == "P")
            {
                return ProcesarTxtPagesPershing(paginas);
            }
            else if (PershingOMorgan(filePath) == "M")
            {
                return ProcesarTxtPagesMorganStanley(paginas);
            }
            else
            {
                return "ERROR 404: UN GATITO MORIRA POR ESTO";
            }
        }

        /// <summary> 
        /// Dado un path hacia el archivo PDF, crea un txt con estilo JSON en la direccion especificada
        /// </summary>
        //public static void ProcesarPDFDevuelveArchivo(string filePath, string outputFilePath)
        //{
        //    string[] paginas = ArrayPerPdfPage(filePath);
        //    if (PershingOMorgan(filePath) == "P")
        //    {
        //        using (StreamWriter sw = File.CreateText(outputFilePath))
        //        {
        //            sw.Write(ProcesarTxtPagesPershing(paginas));
        //        }
        //    }
        //    else if (PershingOMorgan(filePath) == "M")
        //    {
        //        using (StreamWriter sw = File.CreateText(outputFilePath))
        //        {
        //            sw.Write(ProcesarTxtPagesMorganStanley(paginas));
        //        }
        //    }
        //}
    }
}
