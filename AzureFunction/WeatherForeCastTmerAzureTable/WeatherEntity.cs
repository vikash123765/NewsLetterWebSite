using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;

namespace WeatherForeCastTmerAzureTable
{
    internal class WeatherEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "WeatherData";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; } = ETag.All;

        // Weather properties
        public string City { get; set; }
        public int TemperatureC { get; set; }
        public int TemperatureF { get; set; }
        public int Humidity { get; set; }
        public int WindSpeed { get; set; }
        public string Summary { get; set; }
        public DateTime Date { get; set; }
    }

}