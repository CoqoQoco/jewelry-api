namespace jewelry.Model.Production.Insight.Wip
{
    public class Request
    {
        public int StaleDays { get; set; } = 180;
        public int RiskWindowDays { get; set; } = 30;
    }
}
