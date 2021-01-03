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
        public string StartHour { get; set; }
        public string EndHour { get; set; }
        public string PilotName { get; set; }
        public string AirplaneName { get; set; }
        public string AirlineName { get; set; }
        public List<ConnectionViewModel> Changes { get; set; } = new List<ConnectionViewModel>();
    }
}
