using System.ComponentModel.DataAnnotations;

namespace jewelry.Model.PublicProduct.Get
{
    public class Request
    {
        [Required]
        public string Token { get; set; }
    }
}
