﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Master
{
    public class CreateMasterModelRequest
    {
        public string Type { get; set; }
        public string NameEn { get; set; }
        public string NameTh { get; set; }
        public string Code { get; set; }
        public string? Description { get; set; }

        //zill
        public string? GoldCode { get; set; }
        public string? GoldSizeCode { get; set; }
    }
}
