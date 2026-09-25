using System.Collections.Generic;

namespace jewelry.Model.Stock.Product.Image.CreateBulk
{
    public class Response
    {
        public List<string> Created { get; set; } = new List<string>();
        public List<string> Overwritten { get; set; } = new List<string>();
        public List<string> Skipped { get; set; } = new List<string>();
        public List<string> NotFound { get; set; } = new List<string>();
    }
}
