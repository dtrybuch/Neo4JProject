using FlightSearcher.Models;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
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
            var allFromLabel = await GetAllAirportByCode(code);
            if (allFromLabel.Any()) return;
            await ExecuteQuery("CREATE (n:AirportDT { name: $name, city: $city, country: $country, code: $code }) RETURN n.name",
                         new { name, city, country, code });

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
        public async Task CreateRelationshipBetweenAirports(string firstCode, string secondCode, string startHour, string endHour, string airplaneName,
            string airlineName, string pilotName )

        {
            var relations = await GetRelationBetweenAirports(firstCode, secondCode);
            if (relations.Any()) return;
            IAsyncSession session = _driver.AsyncSession();
            try
            {
                await session.WriteTransactionAsync(async tx =>
                { 
                    var reader = await tx.RunAsync("MATCH (a:AirportDT),(b:AirportDT) WHERE a.code = $firstCode AND b.code = $secondCode" +
                        " CREATE (a) -[r:LEADS_TO { startHour: $startHour, endHour: $endHour, airplaneName: $airplaneName, airlineName: $airlineName," +
                        " pilotName: $pilotName }] -> (b)" +
                        " RETURN type(r), r.startHour", new { firstCode, secondCode, startHour, endHour, airplaneName, airlineName, pilotName});
                });
            }
            finally
            {
                // asynchronously close session
                await session.CloseAsync();
            }
        }
        private async Task<List<AirportViewModel>> GetRelationBetweenAirports(string firstCode, string secondCode)
        {
            IAsyncSession session = _driver.AsyncSession();
            var result = new List<AirportViewModel>();
            try
            {
                await session.WriteTransactionAsync(async tx =>
                {
                    var reader = await tx.RunAsync("MATCH (n:AirportDT { code: $firstCode }) --> (:AirportDT { code: $secondCode}) RETURN n", new { firstCode, secondCode});
                    while (await reader.FetchAsync())
                    {
                        // Each current read in buffer can be reached via Current
                        if (reader != null && reader.Current.Keys.Count > 0)
                        {
                            var nodeProps = JsonConvert.SerializeObject(reader.Current[0].As<INode>().Properties);
                            result.Add(JsonConvert.DeserializeObject<AirportViewModel>(nodeProps));
                        }
                    }
                    return result;
                });
            }
            finally
            {
                // asynchronously close session
                await session.CloseAsync();
            }
            return result;
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
                await session.WriteTransactionAsync(async tx =>
                {
                    var reader = await tx.RunAsync("MATCH (a:AirportDT {code: $firstCode }) -[conn:LEADS_TO] -> (b:AirportDT {code: $secondCode}) " +
                        "RETURN a, b, conn.startHour, conn.endHour, conn.airlineName, conn.airplaneName, conn.pilotName", new { firstCode, secondCode});
                    while (await reader.FetchAsync())
                    {
                        // Each current read in buffer can be reached via Current
                        if (reader != null && reader.Current.Keys.Count > 2)
                        {
                            records.Add(
                                GetConnecionViewModel(
                                    JsonConvert.SerializeObject(reader.Current.Values["a"].As<INode>().Properties),
                                    JsonConvert.SerializeObject(reader.Current.Values["b"].As<INode>().Properties),
                                    reader.Current.Values["conn.startHour"].ToString(),
                                    reader.Current.Values["conn.endHour"].ToString(),
                                    reader.Current.Values["conn.airlineName"].ToString(),
                                    reader.Current.Values["conn.airplaneName"].ToString(),
                                    reader.Current.Values["conn.pilotName"].ToString()
                                    )
                                );
                        }
                    }
                });
                if(!records.Any())
                {
                    await session.WriteTransactionAsync(async tx =>
                    {
                        var reader = await tx.RunAsync("MATCH p=shortestPath((a:AirportDT {code: $firstCode }) -[conn:LEADS_TO*1..3] -> (b:AirportDT {code: $secondCode})) " +
                            "RETURN p", new { firstCode, secondCode });
                        while (await reader.FetchAsync())
                        {
                            // Each current read in buffer can be reached via Current
                            if (reader != null && reader.Current.Keys.Any())
                            {
                                var firstNode = JsonConvert.SerializeObject(reader.Current.Values["p"].As<IPath>().Start.Properties);
                                var firstAirport = JsonConvert.DeserializeObject<AirportViewModel>(firstNode);
                                var secondNode = JsonConvert.SerializeObject(reader.Current.Values["p"].As<IPath>().End.Properties);
                                var secondAirport = JsonConvert.DeserializeObject<AirportViewModel>(secondNode);
                                var startHour = reader.Current.Values["p"].As<IPath>().Relationships[0].Properties["startHour"].ToString();
                                var endHour = reader.Current.Values["p"].As<IPath>().Relationships[^1].Properties["endHour"].ToString();
                                var connectionModel = new ConnectionViewModel()
                                {
                                    FirstAirport = firstAirport,
                                    SecondAirport = secondAirport,
                                    StartHour = startHour,
                                    EndHour = endHour
                                };
                                var changes = new List<ConnectionViewModel>();
                                foreach(var relation in reader.Current.Values["p"].As<IPath>().Relationships)
                                {
                                    var firstNodeJson = reader.Current.Values["p"].As<IPath>().Nodes.FirstOrDefault(node => node.Id == relation.StartNodeId).Properties;
                                    var secondNodeJson = reader.Current.Values["p"].As<IPath>().Nodes.FirstOrDefault(node => node.Id == relation.EndNodeId).Properties;
                                    changes.Add(GetConnecionViewModel(
                                        JsonConvert.SerializeObject(firstNodeJson),
                                        JsonConvert.SerializeObject(secondNodeJson),
                                        relation.Properties["startHour"].ToString(),
                                        relation.Properties["endHour"].ToString(),
                                        relation.Properties["airlineName"].ToString(),
                                        relation.Properties["airplaneName"].ToString(),
                                        relation.Properties["pilotName"].ToString()
                                        ));
                                }
                                connectionModel.Changes = changes;
                                records.Add(connectionModel);
                            }
                        }
                    });
                }
                return records;
            }
            finally
            {
                // asynchronously close session
                await session.CloseAsync();
            }
        }

        private ConnectionViewModel GetConnecionViewModel(string firstNode, string secondNode, string startHour, string endHour, string airlineName, 
            string airplaneName, string pilotName)
        {
            var firstAirport = JsonConvert.DeserializeObject<AirportViewModel>(firstNode);
            var secondAirport = JsonConvert.DeserializeObject<AirportViewModel>(secondNode);
            return new ConnectionViewModel()
            {
                FirstAirport = firstAirport,
                SecondAirport = secondAirport,
                StartHour = startHour,
                EndHour = endHour,
                AirlineName = airlineName,
                AirplaneName = airplaneName,
                PilotName = pilotName
            };
        }

        public async Task<List<AirportViewModel>> GetAllAirportByName(string name)
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

        public async Task<List<AirportViewModel>> GetAllAirportByCode(string code)
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


        public async Task DeleteAllAirports()
        {
            IAsyncSession session = _driver.AsyncSession();
            try
            {
                await session.WriteTransactionAsync(async tx =>
                {
                    var reader = await tx.RunAsync("MATCH (n:AirportDT) DETACH DELETE n");
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
