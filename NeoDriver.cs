using FlightSearcher.Models;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightSearcher
{
    public class NeoDriver : IDisposable
    {
        private readonly IDriver _driver;
        public NeoDriver(string uri, string user, string password)
        {
            _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
        }
        public async Task AddAirport(string name, string city, string country, string code)
        {
            var allFromLabel = await GetAllFromAirportByCode(code);
            if (allFromLabel.Any()) return;
            await ExecuteQuery("CREATE (n:AirportDT { name: $name, city: $city, country: $country, code: $code }) RETURN n.name",
                         new { name, city, country, code });

        }
        public async Task AddPilot(string firstName, string lastName, int countOfHours)
        {
            var allFromLabel = await GetAllFromPilotByName(firstName, lastName);
            if (allFromLabel.Any()) return;
            await ExecuteQuery("CREATE (n:PilotDT { firstName: $firstName, lastName: $lastName, countOfHours: $countOfHours }) RETURN n.firstName",
                new { firstName, lastName, countOfHours });
        }

        private async Task ExecuteQuery(string query, object parameters)
        {
            IAsyncSession session = _driver.AsyncSession();
            try
            {
                await session.WriteTransactionAsync(async tx =>
                {
                    var records = new List<string>();
                    var reader = await tx.RunAsync(query, parameters);
                });
            }
            finally
            {
                // asynchronously close session
                await session.CloseAsync();
            }
        }
        public async Task CreateRelationshipBetweenAirports(string firstCode, string secondCode, string hour)

        {
            IAsyncSession session = _driver.AsyncSession();
            try
            {
                await session.WriteTransactionAsync(async tx =>
                { 
                    var reader = await tx.RunAsync("MATCH (a:AirportDT),(b:AirportDT) WHERE a.code = $firstCode AND b.code = $secondCode" +
                        " CREATE (a) -[r:LEADS_TO { hour: $hour }] -> (b)" +
                        " RETURN type(r), r.hour", new { firstCode, secondCode, hour});
                });
            }
            finally
            {
                // asynchronously close session
                await session.CloseAsync();
            }
        }

        public async Task<List<AirportViewModel>> GetAllAirports()
        {
            var records = new List<AirportViewModel>();
            IAsyncSession session = _driver.AsyncSession();
            try
            {
                var added = await session.WriteTransactionAsync(async tx =>
                {
                    var reader = await tx.RunAsync($"MATCH (n:AirportDT) RETURN n");
                    while (await reader.FetchAsync())
                    {
                        // Each current read in buffer can be reached via Current
                        if(reader != null && reader.Current.Keys.Count > 0)
                        {
                            var nodeProps = JsonConvert.SerializeObject(reader.Current[0].As<INode>().Properties);
                            records.Add(JsonConvert.DeserializeObject<AirportViewModel>(nodeProps));
                        }
                    }
                    return records;
                });
            }
            finally
            {
                // asynchronously close session
                await session.CloseAsync();
            }
            return records;
        }

        public async Task<List<ConnectionViewModel>> GetAllAirportsConnections(string firstCode, string secondCode)
        {
            var records = new List<ConnectionViewModel>();
            IAsyncSession session = _driver.AsyncSession();
            try
            {
                var added = await session.WriteTransactionAsync(async tx =>
                {
                    var reader = await tx.RunAsync("MATCH (a:AirportDT {code: $firstCode })<-[conn:LEADS_TO]-(b:AirportDT {code: $secondCode}) " +
                        "RETURN a, b, conn.hour", new { firstCode, secondCode});
                    while (await reader.FetchAsync())
                    {
                        // Each current read in buffer can be reached via Current
                        if (reader != null && reader.Current.Keys.Count > 2)
                        {
                            var firstNode = JsonConvert.SerializeObject(reader.Current.Values["a"].As<INode>().Properties);
                            var firstAirport = JsonConvert.DeserializeObject<AirportViewModel>(firstNode);
                            var secondNode = JsonConvert.SerializeObject(reader.Current.Values["b"].As<INode>().Properties);
                            var secondAirport = JsonConvert.DeserializeObject<AirportViewModel>(secondNode);
                            var hour = reader.Current.Values["conn.hour"].ToString();
                            records.Add(new ConnectionViewModel() 
                            { 
                                FirstAirport = firstAirport,
                                SecondAirport = secondAirport,
                                Hour = hour
                            });
                        }
                    }
                    return records;
                });
            }
            finally
            {
                // asynchronously close session
                await session.CloseAsync();
            }
            return records;
        }

        public async Task<List<AirportViewModel>> GetAllFromAirportByName(string name)
        {
            var records = new List<AirportViewModel>();
            IAsyncSession session = _driver.AsyncSession();
            try
            {
                var added = await session.WriteTransactionAsync(async tx =>
                {
                    var reader = await tx.RunAsync($"MATCH (n:AirportDT) WHERE n.name = $name RETURN n", new { name});
                    while (await reader.FetchAsync())
                    {
                        // Each current read in buffer can be reached via Current
                        if (reader != null && reader.Current.Keys.Count > 0)
                        {
                            var nodeProps = JsonConvert.SerializeObject(reader.Current[0].As<INode>().Properties);
                            records.Add(JsonConvert.DeserializeObject<AirportViewModel>(nodeProps));
                        }
                    }
                    return records;
                });
            }
            finally
            {
                // asynchronously close session
                await session.CloseAsync();
            }
            return records;
        }

        public async Task<List<AirportViewModel>> GetAllFromAirportByCode(string code)
        {
            var records = new List<AirportViewModel>();
            IAsyncSession session = _driver.AsyncSession();
            try
            {
                var added = await session.WriteTransactionAsync(async tx =>
                {
                    var reader = await tx.RunAsync($"MATCH (n:AirportDT) WHERE n.code = $code RETURN n", new { code });
                    while (await reader.FetchAsync())
                    {
                        // Each current read in buffer can be reached via Current
                        if (reader != null && reader.Current.Keys.Count > 0)
                        {
                            var nodeProps = JsonConvert.SerializeObject(reader.Current[0].As<INode>().Properties);
                            records.Add(JsonConvert.DeserializeObject<AirportViewModel>(nodeProps));
                        }
                    }
                    return records;
                });
            }
            finally
            {
                // asynchronously close session
                await session.CloseAsync();
            }
            return records;
        }

        public async Task<List<string>> GetAllFromPilotByName(string firstName, string lastName)
        {
            var records = new List<string>();
            IAsyncSession session = _driver.AsyncSession();
            try
            {
                var added = await session.WriteTransactionAsync(async tx =>
                {
                    var reader = await tx.RunAsync($"MATCH (n:PilotDT) WHERE n.firstName = $firstName AND m.lastName = $lastName RETURN n", new { firstName, lastName});
                    while (await reader.FetchAsync())
                    {
                        // Each current read in buffer can be reached via Current
                        if (reader != null && reader.Current.Keys.Count > 0)
                            records.Add(reader.Current[0]?.ToString());
                    }
                    return records;
                });
            }
            finally
            {
                // asynchronously close session
                await session.CloseAsync();
            }
            return records;
        }


        public async Task DeleteAllAirports()
        {
            IAsyncSession session = _driver.AsyncSession();
            try
            {
                await session.WriteTransactionAsync(async tx =>
                {
                    var reader = await tx.RunAsync("MATCH (n:AirportDT) DELETE n");
                });
            }
            finally
            {
                // asynchronously close session
                await session.CloseAsync();
            }
        }

        public void Dispose()
        {
            _driver?.Dispose();
        }
    }
}
