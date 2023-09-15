using jewelry.Model;
using jewelry.Model.Helper.ResponseReadExcelProduct.cs;
using Microsoft.AspNetCore.Http;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Microsoft.AspNetCore.Http;

namespace Jewelry.Service.Helper
{
    public interface IReadExcelProduct
    {
        bool IsExcel(IFormFile file);
        ResponseReadExcelProduct Process(IFormFile file);
    }
    public class ReadExcelProduct : IReadExcelProduct
    {
        public ReadExcelProduct()
        {
            //CultureInfo thaiCulture = new CultureInfo("th-TH");
        }

        #region *** Public Method ***
        public bool IsExcel(IFormFile file)
        {
            bool valid = false;
            var extention = Path.GetExtension(file.FileName);

            if (extention != ".xls" && extention != ".xlsx")
            {
                return valid;
            }
            var contentType = file.ContentType;
            if (contentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" || contentType == "application/vnd.ms-excel")
            {
                valid = true;
            }
            return valid;
        }
        public ResponseReadExcelProduct Process(IFormFile file)
        {
            CultureInfo thaiCulture = new CultureInfo("th-TH");
            var response = new ResponseReadExcelProduct();
            ISheet sheet;
            using (var stream = file.OpenReadStream())
            {
                //get sheet[0]
                XSSFWorkbook xssWorkbook = new XSSFWorkbook(stream);
                sheet = xssWorkbook.GetSheetAt(0);
                IRow headerRow = sheet.GetRow(0);

                for (int countRow = 0; countRow <= sheet.LastRowNum; countRow++)
                {
                    IRow row = sheet.GetRow(countRow);

                    //Read >> [ReceivedDate]
                    if (countRow == 1)
                    {
                        if (!string.IsNullOrEmpty(row.GetCell(7).ToString()))
                        {
                            if (DateTime.TryParse(row.GetCell(7).ToString(), thaiCulture, DateTimeStyles.None, out DateTime value))
                            {
                                response.ReceivedDate = value;
                            }
                        }
                    }

                    //Read >> [WO]
                    if (countRow == 2)
                    {
                        if (!string.IsNullOrEmpty(row.GetCell(2).ToString()))
                        {
                            response.WO = row.GetCell(2).ToString();
                        }
                    }

                    //Read >> [WO_Number, RequiredDate]
                    if (countRow == 3)
                    {
                        if (!string.IsNullOrEmpty(row.GetCell(2).ToString()))
                        {
                            response.WO_Number = row.GetCell(2).ToString();
                        }

                        if (!string.IsNullOrEmpty(row.GetCell(7).ToString()))
                        {
                            var test = row.GetCell(7).ToString();
                            if (DateTime.TryParse(row.GetCell(7).ToString(), thaiCulture, DateTimeStyles.None, out DateTime value))
                            {
                                response.RequiredDate = value;
                            }
                        }
                    }

                    //Read >> [ProductCode]
                    if (countRow == 4)
                    {
                        if (!string.IsNullOrEmpty(row.GetCell(2).ToString()))
                        {
                            response.ProductCode = row.GetCell(2).ToString();
                        }
                    }


                    //Read >> [Mold, Mat_1, Customer]
                    if (countRow == 5)
                    {
                        if (!string.IsNullOrEmpty(row.GetCell(0).ToString()))
                        {
                            response.Mold = row.GetCell(0).ToString();
                        }

                        if (!string.IsNullOrEmpty(row.GetCell(1).ToString()))
                        {
                            response.Mat_1 = row.GetCell(1).ToString();
                        }

                        if (!string.IsNullOrEmpty(row.GetCell(7).ToString()))
                        {
                            response.Customer = row.GetCell(7).ToString();
                        }
                    }
                }
            }

            return response;
        }
        #endregion
    }
}
