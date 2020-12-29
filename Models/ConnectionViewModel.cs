using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightSearcher.Models
{
    public class ConnectionViewModel
    {
        public AirportViewModel FirstAirport { get; set; }
        public AirportViewModel SecondAirport { get; set; }
        public string Hour { get; set; }
    }
}
