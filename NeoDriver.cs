using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
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
        public async Task AddAirport(string name, string city, string country)
        {
            var allFromLabel = await GetAllFromAirportByName(name);
            if (allFromLabel.Any()) return;
            await ExecuteQuery("CREATE (n:AirportDT { name: $name, city: $city, country: $country }) RETURN n.name",
                         new { name, city, country });

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



        public async Task<List<string>> GetAllFromLabel(string label)
        {
            var records = new List<string>();
            IAsyncSession session = _driver.AsyncSession();
            try
            {
                var added = await session.WriteTransactionAsync(async tx =>
                {
                    var reader = await tx.RunAsync($"MATCH (n:{label}) RETURN n.name");
                    while (await reader.FetchAsync())
                    {
                        // Each current read in buffer can be reached via Current
                        if(reader != null && reader.Current.Keys.Count > 0)
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


        public async Task<List<string>> GetAllFromAirportByName(string name)
        {
            var records = new List<string>();
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
