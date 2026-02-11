using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Constant
{
    public static class MyJobType
    {
        public static int PlanStockCost = 10;

        /// <summary>
        /// Get job type name from type code
        /// </summary>
        /// <param name="typeCode">Type code</param>
        /// <returns>Job type name in Thai</returns>
        public static string GetTypeName(int typeCode)
        {
            return typeCode switch
            {
                10 => "แผนตีราคาสินค้า",
                _ => "ไม่ระบุประเภทงาน"
            };
        }

        /// <summary>
        /// Get job type name in English from type code
        /// </summary>
        /// <param name="typeCode">Type code</param>
        /// <returns>Job type name in English</returns>
        public static string GetTypeNameEn(int typeCode)
        {
            return typeCode switch
            {
                10 => "Plan Stock Cost",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get all job types as dictionary (for dropdown, etc.)
        /// </summary>
        /// <returns>Dictionary of type code and name</returns>
        public static Dictionary<int, string> GetAllTypes()
        {
            return new Dictionary<int, string>
            {
                { PlanStockCost, "แผนตีราคาสินค้า" }
            };
        }
    }

    public enum MyJobTypeEnum
    {
        PlanStockCost = 10
    }
}
