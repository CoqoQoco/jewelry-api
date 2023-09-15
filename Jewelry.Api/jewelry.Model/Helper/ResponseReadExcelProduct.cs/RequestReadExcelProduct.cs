using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Helper.ResponseReadExcelProduct.cs
{
    public class RequestReadExcelProduct
    {
        [Required]
        public IFormFile ExcelFile { get; set; }
    }
}
