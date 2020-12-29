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
            //await FillAirports();
            //await CreateRelationshipBetweenAirports();
            var all = await driver.GetAllAirports();

            return View(all);
        }

        public IActionResult Privacy()
        {
            return View();
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
            await driver.CreateRelationshipBetweenAirports("ATL", "ORD", "10:30");
            await driver.CreateRelationshipBetweenAirports("ORD", "ATL", "15:20");
        }
    }
}
