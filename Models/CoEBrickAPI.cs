using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Hosting;
using System.Diagnostics.Metrics;

namespace BrickSchemaTranslation
{
    public class CoEBrickAPI
    {

        ///
        /// Constructor
        ///

        public CoEBrickAPI(IWebHostEnvironment webHostEnvironment)
        {
            WebHostEnvironment = webHostEnvironment;

        }

        public IWebHostEnvironment WebHostEnvironment { get; }
        private IGraph _graph;
        public IGraph Graph
        {
            get
            {
                if (_graph == null)
                {
                    _graph = new Graph();
                    TurtleParser ttlParser = new TurtleParser();
                    //string brickSchemaFileName = "COE_10_10.ttl";
                    string brickSchemaFileName = "COE_02_22_2024-2.ttl";
                    string webRootPath = WebHostEnvironment.WebRootPath;
                    string ttlPath = Path.Combine("Data", brickSchemaFileName);
                    ttlParser.Load(_graph, ttlPath);
                    Console.WriteLine("Worked!");
                }
                return _graph;
            }
        }


        ///
        /// Front End - Getting specific CoE Data
        ///


        // 🔄 Gets all sensors from the graph
        public List<string> GetAllSensors()
        {
            string sparqlQuery = "SELECT ?sensor\r\n" +
                                 "WHERE {\r\n" +
                                 "    ?sensor a ?sensorName ;\r\n" +
                                 "}";
            return ExecuteAndReturnResults(sparqlQuery, "sensor");
        }

        // 🔄 Gets all floors from the graph
        public List<string> GetAllFloors()
        {
            string sparqlQuery = @"SELECT ?floor
                                   WHERE {
                                        ?floor a brick:Floor .
                                   }";

            return ExecuteAndReturnResults(sparqlQuery, "floor");
        }

        // 🔄 Gets rooms for a specific floor from the graph
        public List<string> GetFloorRooms(string floor)
        {
            string sparqlQuery = $@"SELECT ?room
                            WHERE {{
                                bldg:{floor} a brick:Floor ;
                                brick:hasPart ?room .
                            }}";

            return ExecuteAndReturnResults(sparqlQuery, "room");
        }

        // 🔄 Gets sensor and UUID pairs for a specific room from the graph
        public List<(string SensorName, string UUID)> GetRoomSensorAndUUIDs(string room)
        {
            string sparqlQuery = $@"
        SELECT ?sensor ?uuid
        WHERE {{
            ?sensor brick:hasLocation bldg:{room} ;
                    brick:hasTimeseriesReference ?uuid .
        }}";


            SparqlResultSet results = ExecuteSparqlQuery(sparqlQuery);

            if (results != null)
            {
                List<(string SensorName, string UUID)> sensorUuidPairs = new List<(string SensorName, string UUID)>();
                foreach (var result in results)
                {
                    Uri sensorUri = new Uri(result["sensor"].ToString());
                    Uri uuidUri = new Uri(result["uuid"].ToString());
                    string sensorName = sensorUri.Fragment.Trim('#');
                    string uuid = uuidUri.Fragment.Trim('#');

                    Console.WriteLine($"uuid: {uuid}");

                    sensorUuidPairs.Add((SensorName: sensorName, UUID: uuid));
                }
                return sensorUuidPairs;
            }
            else
            {
                Console.WriteLine("No results found.");
                return new List<(string, string)>();
            }
        }

        // 🔄 Gets the UUID for a specific sensor from the graph
        public List<string> GetSensorUUID(string sensor)
        {
            string sparqlQuery = $@"
        SELECT DISTINCT ?uuid
        WHERE {{
            {{
                bldg:{sensor} a ?Sensor ;
                        brick:hasTimeseriesReference ?uuid .
            }}
            UNION
            {{
                bldg:{sensor} brick:hasTimeseriesReference ?uuid .
            }}
        }}";

            return ExecuteAndReturnResults(sparqlQuery, "uuid");
        }


        public List<double> GetSensorDataWithDateRange(string uuid, string measurement, DateTime startDate, DateTime endDate)
        {
            List<double> sensorData = new List<double>();
            try
            {
                string dsn = Environment.GetEnvironmentVariable("DB_DSN");
                string user = Environment.GetEnvironmentVariable("DB_USER");
                string password = Environment.GetEnvironmentVariable("DB_PASS");
                string database = Environment.GetEnvironmentVariable("DB_DATABASE");
                string server = Environment.GetEnvironmentVariable("DB_SERVER");

                string connectionString = $"Server={server};Database={database};User Id={user};Password={password};TrustServerCertificate=yes";

                if (string.IsNullOrEmpty(dsn) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(server))
                {
                    Console.WriteLine($"{dsn} {user} {password} {database} {server}");
                    Console.WriteLine("One or more required environment variables for the CoE Database information are missing.");
                    Environment.Exit(1);
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                SELECT iaq." + measurement + @"
                FROM [COE].[dbo].[Sensors] as sensors
                JOIN COE.dbo.IAQ as iaq
                ON sensors.SensorID = iaq.SensorId
                WHERE [BRICK_UUID] = @Uuid
                AND iaq.[DT] BETWEEN @StartDate AND @EndDate
                ORDER BY iaq.[DT] DESC";

                    using (SqlCommand command = new SqlCommand(sql, conn))
                    {
                        command.Parameters.AddWithValue("@Uuid", uuid);
                        command.Parameters.AddWithValue("@StartDate", startDate);
                        command.Parameters.AddWithValue("@EndDate", endDate);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    double value = Convert.ToDouble(reader[measurement]);
                                    sensorData.Add(value);
                                }
                            }
                            else
                            {
                                Console.WriteLine("No rows found.");
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
            Console.WriteLine("RETURNING SENSOR DATA");
            Console.WriteLine(sensorData);
            return sensorData;
        }

        public DataTable GetSensorData(SqlConnection conn, string uuid, int selectedTimeframe = 24)
        {
            if (uuid == null)
            {
                Console.WriteLine("You must pass a UUID.");
                return null;
            }

            string sql = $@"
            SELECT iaq.DT, iaq.PM2pt5, iaq.Temp, iaq.PM10, iaq.O3, iaq.Humidity, iaq.Signal, iaq.CO2, iaq.Beep, iaq.Press, iaq.Motion, iaq.AIQ, iaq.TVOC, iaq.Light
            FROM [COE].[dbo].[Sensors] as sensors
            JOIN COE.dbo.IAQ as iaq
            ON sensors.SensorID = iaq.SensorId
            WHERE [BRICK_UUID] = '{uuid}'";
            //    string sql = $@"
            //SELECT iaq.Temp
            //FROM [COE].[dbo].[Sensors] as sensors
            //JOIN COE.dbo.IAQ as iaq
            //ON sensors.SensorID = iaq.SensorId
            //WHERE [BRICK_UUID] = '{uuid}'";

            // Append the date filters if start and end dates are provided
            //if (startDate != null && endDate != null)
            //{
            //    sql += $@"
            //AND [DT] >= @StartDate
            //AND [DT] <= @EndDate";
            //}
            //else
            //{
            //    // If start and end dates are not provided, use the default 24-hour timeframe
            //    sql += @"
            //AND [DT] >= DATEADD(HOUR, -24, GETDATE())";
            //}

            // Append the date filters based on the selected timeframe
            switch (selectedTimeframe)
            {
                case 7:
                    sql += @"
            AND [DT] >= DATEADD(DAY, -7, GETDATE())";
                    break;
                case 30:
                    sql += @"
            AND [DT] >= DATEADD(DAY, -30, GETDATE())";
                    break;
                case 60:
                    sql += @"
            AND [DT] >= DATEADD(DAY, -60, GETDATE())";
                    break;
                default: // Default to 24 hours
                    sql += @"
            AND[DT] >= DATEADD(DAY, -1, GETDATE())";
                    break;
            }

            //AND[DT] >= DATEADD(HOUR, -24, GETDATE())";

            sql += " ORDER BY DT DESC;";

            Console.WriteLine(sql);
            Console.WriteLine(sql);


            try
            {
                using (SqlCommand command = new SqlCommand(sql, conn))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            DataTable dataTable = new DataTable();
                            dataTable.Load(reader);
                            return dataTable;
                        }
                        else
                        {
                            Console.WriteLine("No rows found.");
                            return null;
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
                return null;
            }
        }



        ///
        /// Back End - Querying Graph with SPA
        ///


        // 🔄 Executes a SPARQL query on the given graph and returns the result as a SparqlResultSet
        private SparqlResultSet ExecuteSparqlQuery(string sparqlQuery)
        {
            SparqlParameterizedString queryString = new SparqlParameterizedString();
            queryString.Namespaces.AddNamespace("bldg", new Uri("http://example.com/COEtest#"));
            queryString.Namespaces.AddNamespace("brick", new Uri("https://brickschema.org/schema/Brick#"));

            queryString.CommandText = sparqlQuery;

            SparqlQueryParser queryParser = new SparqlQueryParser();
            SparqlQuery query = queryParser.ParseFromString(queryString);

            return this.Graph.ExecuteQuery(query) as SparqlResultSet;
        }

        // 📄 Executes a SPARQL query, prints results based on the specified result variable
        private void ExecuteAndPrintResults(string sparqlQuery, string resultVariable)
        {
            SparqlResultSet results = ExecuteSparqlQuery(sparqlQuery);

            if (results != null)
            {
                foreach (var result in results)
                {
                    Uri resultUri = new Uri(result[resultVariable].ToString());
                    string resultName = resultUri.Fragment.Trim('#');
                    Console.WriteLine(resultName);
                }
            }
            else
            {
                Console.WriteLine("No results found.");
            }
        }

        // 📄 Executes a SPARQL query and returns the results based on the specified result variable
        private List<string> ExecuteAndReturnResults(string sparqlQuery, string resultVariable)
        {
            List<string> resultNames = new List<string>();

            SparqlResultSet results = ExecuteSparqlQuery(sparqlQuery);

            if (results != null)
            {
                foreach (var result in results)
                {
                    Uri resultUri = new Uri(result[resultVariable].ToString());
                    string resultName = resultUri.Fragment.Trim('#');
                    resultNames.Add(resultName);
                }
            }
            else
            {
                Console.WriteLine("No results found.");
            }

            return resultNames;
        }
    }
}
