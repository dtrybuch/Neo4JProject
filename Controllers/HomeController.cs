using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FlightSearcher.Models;

namespace FlightSearcher.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly NeoDriver driver = new NeoDriver("bolt://neo4j.fis.agh.edu.pl", "u7trybuch", "297926");
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            await driver.DeleteAllAirports();
            await FillAirports();
            await CreateRelationshipBetweenAirports();
            var all = await driver.GetAllAirports();

            return View(all);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task FillAirports()
        {
            await driver.AddAirport("O'Hare International Airport", "Chicago", "United States of America", "ORD");
            await driver.AddAirport("Hartsfield Jackson", "Atlanta", "United States of America", "ATL");
            await driver.AddAirport("Heathrow", "London", "United Kingdom", "LHR");
            await driver.AddAirport("Charles De Gaulle", "Paris", "France", "CDG");
            await driver.AddAirport("Los Angeles International Airport", "Los Angeles", "United States of America", "LAX");
        }
        private async Task CreateRelationshipBetweenAirports()
        {
            await driver.CreateRelationshipBetweenAirports("ATL", "ORD", "10:30","16:30", "Boeing 737", "American Airlines", "John McClane");
            await driver.CreateRelationshipBetweenAirports("ORD", "ATL", "15:20", "21:20", "Boeing 787", "American Airlines", "John Travolta");

            await driver.CreateRelationshipBetweenAirports("LHR", "ORD", "10:30", "23:30", "Boeing 757", "American Airlines", "John Wayne");
            await driver.CreateRelationshipBetweenAirports("ORD", "LHR", "20:20", "09:20", "Boeing 787", "American Airlines", "John Travolta");

            await driver.CreateRelationshipBetweenAirports("CDG", "ORD", "10:30", "18:30", "Boeing 757", "American Airlines", "John Wayne");
            await driver.CreateRelationshipBetweenAirports("ORD", "CDG", "20:20", "02:20", "Boeing 787", "American Airlines", "John Travolta");

            await driver.CreateRelationshipBetweenAirports("LAX", "ORD", "10:30", "10:30", "Boeing 737", "American Airlines", "John McClane");
            await driver.CreateRelationshipBetweenAirports("ORD", "LAX", "15:20", "01:20", "Boeing 787", "American Airlines", "John Travolta");

            //await driver.CreateRelationshipBetweenAirports("ATL", "LHR", "10:30", "18:30", "Boeing 737", "American Airlines", "John McClane");
            //await driver.CreateRelationshipBetweenAirports("LHR", "ATL", "15:20", "23:20", "Boeing 787", "American Airlines", "John Travolta");

            await driver.CreateRelationshipBetweenAirports("CDG", "LHR", "10:30", "11:30", "Boeing 737", "Air France", "David Cameron");
            await driver.CreateRelationshipBetweenAirports("LHR", "CDG", "15:20", "16:20", "Boeing 787", "Air France", "Emmanuel Macron");

            //await driver.CreateRelationshipBetweenAirports("ATL", "LAX", "10:30", "16:30", "Boeing 737", "American Airlines", "John McClane");
            //await driver.CreateRelationshipBetweenAirports("LAX", "ATL", "15:20", "21:20", "Boeing 787", "American Airlines", "John Travolta");

            //await driver.CreateRelationshipBetweenAirports("ATL", "CDG", "10:30", "23:30", "Boeing 737", "American Airlines", "John McClane");
            //await driver.CreateRelationshipBetweenAirports("CDG", "ATL", "15:20", "04:20", "Boeing 787", "American Airlines", "John Travolta");


            //await driver.CreateRelationshipBetweenAirports("LAX", "LHR", "10:30", "16:30", "Boeing 737", "American Airlines", "John McClane");
            //await driver.CreateRelationshipBetweenAirports("LHR", "LAX", "15:20", "21:20", "Boeing 787", "American Airlines", "John Travolta");


        }
    }
}
