namespace JasperBus.Marten
{
    public class MartenSubscriptionSettings
    {
        public string ConnectionString { get; set; }

        public double  PollingIntervalSeconds { get; set; } = 60;

        public string PostgresNotifyChannelName { get; set; } = "JasperSubscriptions";
    }
}
