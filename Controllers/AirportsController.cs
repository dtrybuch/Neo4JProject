﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace FlightSearcher.Controllers
{
    public class AirportsController : Controller
    {
        private readonly NeoDriver driver = new NeoDriver("bolt://neo4j.fis.agh.edu.pl", "u7trybuch", "297926");
        public async Task<IActionResult> Index(string first, string second)
        {
            var rt = await driver.GetAllAirportsConnections(first, second);
            if(!rt.Any())
            {
                var firstAirport = await driver.GetAllAirportByCode(first);
                var secondAirport = await driver.GetAllAirportByCode(second);
                rt.Add(new Models.ConnectionViewModel()
                {
                    FirstAirport = firstAirport[0],
                    SecondAirport = secondAirport[0]
                });
            }
            return View(rt);
        }
    }
}