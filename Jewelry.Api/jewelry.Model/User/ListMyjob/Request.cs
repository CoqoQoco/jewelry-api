using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.User.ListMyJob
{
    public class Request : DataSourceRequest
    {
        public Search Search { get; set; }
    }

    /// <summary>
    /// Search filter for ListMyJob query
    /// All array fields support partial matching (Contains)
    /// </summary>
    public class Search
    {
        /// <summary>
        /// Filter by Job IDs (exact match with array)
        /// </summary>
        public List<int>? Id { get; set; }

        /// <summary>
        /// Filter by CreateBy username (partial match with array)
        /// Example: ["admin", "user"] will match any job where CreateBy contains "admin" OR "user"
        /// </summary>
        public List<string>? CreateBy { get; set; }

        /// <summary>
        /// Filter by IsActive status
        /// true = active jobs, false = inactive jobs, null = all jobs
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Filter by UpdateBy username (partial match with array)
        /// Example: ["admin"] will match any job where UpdateBy contains "admin"
        /// </summary>
        public List<string>? UpdateBy { get; set; }

        /// <summary>
        /// Filter by Status IDs (exact match with array)
        /// Example: [10, 20] will match jobs with StatusId = 10 OR 20
        /// </summary>
        public List<int>? StatusId { get; set; }

        /// <summary>
        /// Filter by Status Name (partial match with array)
        /// Example: ["Pending", "Progress"] will match StatusName containing "Pending" OR "Progress"
        /// </summary>
        public List<string>? StatusName { get; set; }

        /// <summary>
        /// Filter by DataJob JSON content (partial match with array)
        /// Example: ["CP-"] will match any job where DataJob contains "CP-"
        /// </summary>
        public List<string>? DataJob { get; set; }

        /// <summary>
        /// Filter by Job Running Number (partial match with array)
        /// Example: ["CP-2024"] will match jobs where JobRunning contains "CP-2024"
        /// </summary>
        public List<string>? JobRunning { get; set; }

        /// <summary>
        /// Filter by Job Type Name (partial match with array)
        /// Example: ["Plan Stock Cost"] will match jobs where JobTypeName contains "Plan Stock Cost"
        /// </summary>
        public List<string>? JobTypeName { get; set; }

        /// <summary>
        /// Filter by Job Type IDs (exact match with array)
        /// Example: [10] will match jobs with JobTypeId = 10 (PlanStockCost)
        /// </summary>
        public List<int>? JobTypeId { get; set; }

        /// <summary>
        /// Filter by CreateDate - Start date (inclusive)
        /// Jobs created on or after this date
        /// </summary>
        public DateTime? CreateDateFrom { get; set; }

        /// <summary>
        /// Filter by CreateDate - End date (inclusive)
        /// Jobs created on or before this date
        /// </summary>
        public DateTime? CreateDateTo { get; set; }

        /// <summary>
        /// Filter by UpdateDate - Start date (inclusive)
        /// Jobs updated on or after this date
        /// </summary>
        public DateTime? UpdateDateFrom { get; set; }

        /// <summary>
        /// Filter by UpdateDate - End date (inclusive)
        /// Jobs updated on or before this date
        /// </summary>
        public DateTime? UpdateDateTo { get; set; }
    }
}
