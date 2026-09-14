using System.Collections.Generic;

namespace jewelry.Model.Certificate.Create
{
    public class Response
    {
        public string Batch { get; set; } = null!;
        public List<CertificateResult> Certificates { get; set; } = new();
    }

    public class CertificateResult
    {
        public string Running { get; set; } = null!;
        public string CertificateNo { get; set; } = null!;
        public int IssueNo { get; set; }
    }
}
