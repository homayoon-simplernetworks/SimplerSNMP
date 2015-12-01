using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using Excel;

namespace SimplerSNMP
{   
    public class XcConnectionLine
    {
        public string command { get; set; }
        public string fromCard { get; set; }   
        public string FromPort { get; set; }   
        public string toCard { get; set; }   
        public string toPort { get; set; }   
        public string service { get; set; }
        public string fromAlias { get; set; }
        public string toAlias { get; set; }
        public string circuitId { get; set; }

    }   
    public class ExcelDataService
    {   
       
        public ObservableCollection<XcConnectionLine> loadExcel( string filePath)
        {
            ObservableCollection<XcConnectionLine> xcConnectionLines = new ObservableCollection<XcConnectionLine>();
            FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            IExcelDataReader excelReader;

            if (filePath.EndsWith(".xls")){
                //1. Reading from a binary Excel file ('97-2003 format; *.xls)
                 excelReader = ExcelReaderFactory.CreateBinaryReader(stream);

            }
            else
            {
                //2. Reading from a OpenXml Excel file (2007 format; *.xlsx)
                 excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
            }


            
            //3. DataSet - The result of each spreadsheet will be created in the result.Tables
            //DataSet result = excelReader.AsDataSet();


            
            while (excelReader.Read())
            {
                //excelReader.GetInt32(0);
                if (excelReader[0] != null && ! excelReader[0].ToString().StartsWith("#"))
                {
                    
                    XcConnectionLine xcline = new XcConnectionLine();
                    xcline.command =  excelReader[0].ToString().Trim();
                    if (excelReader[0] != null && excelReader[1].ToString().Trim() != "")
                    {
                        xcline.fromCard = excelReader[1].ToString().Trim();
                    }
                    else
                    {
                        xcline.fromCard = "0";
                    }

                    if (excelReader[0] != null && excelReader[2].ToString().Trim() != "")
                    {
                        xcline.FromPort = excelReader[2].ToString().Trim();
                    }
                    else
                    {
                        xcline.FromPort = "0";
                    }

                    try
                    {
                        xcline.fromAlias = excelReader[3].ToString().Trim();
                    }
                    catch
                    {
                        xcline.fromAlias = "";
                    }
                    

                    try
                    {
                        xcline.toCard = excelReader[4].ToString().Trim();
                        if (xcline.toCard == "") xcline.toCard = "0";
                    }
                    catch
                    {
                        xcline.toCard = "0";
                    }

                    try
                    {
                        xcline.toPort = excelReader[5].ToString().Trim();
                        if (xcline.toPort == "") xcline.toPort = "0";
                    }
                    catch
                    {
                        xcline.toPort = "0";
                    }

                    try
                    {
                        xcline.toAlias = excelReader[6].ToString().Trim();
                    }
                    catch
                    {
                        xcline.toAlias = "";
                    }

                    try
                    {
                        xcline.service = excelReader[7].ToString().Trim();
                        if (xcline.service == "") xcline.service = "2";
                    }
                    catch
                    {
                        xcline.service = "2";
                    }
                    try
                    {
                        xcline.circuitId = excelReader[8].ToString().Trim();
                    }
                    catch
                    {
                        xcline.circuitId = "";
                    }
                    
                    xcConnectionLines.Add(xcline);
                }

            }

            //6. Free resources (IExcelDataReader is IDisposable)
            excelReader.Close();
            return xcConnectionLines;
        }
   
       
    }   
}  
