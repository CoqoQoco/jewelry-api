namespace jewelry.Model.Report.Executive.Flow
{
    public class FlowLabelPcs
    {
        public string Label { get; set; } = null!;
        public int Pcs { get; set; }
    }

    public class FlowShareItem
    {
        public string Label { get; set; } = null!;
        public int Pcs { get; set; }
        public decimal? Percent { get; set; }
    }

    public class FlowDimItem
    {
        public string Label { get; set; } = null!;
        public int Plans { get; set; }
        public int Pcs { get; set; }
    }

    public class FlowTrustItem
    {
        // fail | warn | info
        public string Level { get; set; } = null!;
        public string Zone { get; set; } = null!;
        public string Key { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Evidence { get; set; } = null!;
        public string Owner { get; set; } = null!;
    }
}
