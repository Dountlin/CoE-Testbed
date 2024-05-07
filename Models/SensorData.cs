using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BrickSchemaTranslation;
using VDS.RDF;

using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Odbc;
using System.Diagnostics;
using Newtonsoft.Json;

namespace CoETestbedWebsite.Models
{
    public class SensorDataModel : PageModel
    {
        private readonly ILogger<FloorModel> _logger;
        public CoEBrickAPI BrickAPI;
        public string RoomNumber { get; set; }
        public List<string> SensorNames { get; set; }
        public DataTable SensorData { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public SensorDataModel(CoEBrickAPI brickAPI)
        {
            BrickAPI = brickAPI;
        }

        public void OnGet(string room)
        {
            RoomNumber = room;
            List<(string SensorName, string UUID)> sensorAndUuidList = BrickAPI.GetRoomSensorAndUUIDs(room);


            SensorNames = sensorAndUuidList.Select(sensor => sensor.SensorName).ToList();
            SensorData = GetDataFromDatabase(sensorAndUuidList);


        }


        public DataTable GetLatestSensorData(string room)
        {
            List<(string SensorName, string UUID)> sensorAndUuidList = BrickAPI.GetRoomSensorAndUUIDs(room);
            return GetDataFromDatabase(sensorAndUuidList);
        }

        //private DataTable GetDataFromDatabaseRanged(List<(string SensorName, string UUID)> sensorAndUuidList)
        //{
        //    DataTable sensorData = new DataTable();
        //    try
        //    {
        //        string dsn = Environment.GetEnvironmentVariable("DB_DSN");
        //        string user = Environment.GetEnvironmentVariable("DB_USER");
        //        string password = Environment.GetEnvironmentVariable("DB_PASS");
        //        string database = Environment.GetEnvironmentVariable("DB_DATABASE");
        //        string server = Environment.GetEnvironmentVariable("DB_SERVER");

        //        string connectionString = $"Server={server};Database={database};User Id={user};Password={password};TrustServerCertificate=yes";

        //        if (string.IsNullOrEmpty(dsn) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(server))
        //        {
        //            /// 😢 One or more required environment variables for the CoE Database information are missing.
        //            Console.WriteLine($"{dsn} {user} {password} {database} {server}");
        //            Console.WriteLine("One or more required environment variables for the CoE Database information are missing.");
        //            Environment.Exit(1);  // Exit the script with an error code
        //        }
        //    }
        //    catch (SqlException ex)
        //    {
        //        // Handle SQL exceptions
        //        Console.WriteLine($"SQL Exception: {ex.Message}");
        //        // You might want to log the exception or take appropriate actions based on your application's requirements.
        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle other exceptions
        //        Console.WriteLine($"Exception: {ex.Message}");
        //        // You might want to log the exception or take appropriate actions based on your application's requirements.
        //    }
        //    return sensorData;
        //}

        private DataTable GetDataFromDatabase(List<(string SensorName, string UUID)> sensorAndUuidList)
        {
            DataTable sensorData = new DataTable();

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
                    /// 😢 One or more required environment variables for the CoE Database information are missing.
                    Console.WriteLine($"{dsn} {user} {password} {database} {server}");
                    Console.WriteLine("One or more required environment variables for the CoE Database information are missing.");
                    Environment.Exit(1);  // Exit the script with an error code
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Your further logic using the database connection goes here
                    Console.WriteLine("Connection opened! now querying all sensors...");
                    string firstUuid = sensorAndUuidList[0].UUID;

                    sensorData = BrickAPI.GetSensorData(conn, firstUuid);

                    conn.Close();
                }


                // now connect to DB with connString!

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Your further logic using the database connection goes here
                    Console.WriteLine("Connection opened! now querying all sensors...");
                    string firstUuid = sensorAndUuidList[0].UUID;

                    //sensorData = BrickAPI.GetSensorData(conn, firstUuid);
                    // Pass start and end dates if they have values
                    //if (sele)
                    //{
                    //    sensorData = BrickAPI.GetSensorData(conn, firstUuid, startDate.Value, endDate.Value);
                    //}
                    //else
                    //{
                    //    sensorData = BrickAPI.GetSensorData(conn, firstUuid);
                    //}
                    sensorData = BrickAPI.GetSensorData(conn, firstUuid);

                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                // Handle SQL exceptions
                Console.WriteLine($"SQL Exception: {ex.Message}");
                // You might want to log the exception or take appropriate actions based on your application's requirements.
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                Console.WriteLine($"Exception: {ex.Message}");
                // You might want to log the exception or take appropriate actions based on your application's requirements.
            }

            // ...

            if (sensorData != null && sensorData.Rows.Count > 0)
            {
                foreach (DataColumn column in sensorData.Columns)
                {
                    Debug.Write(column.ColumnName + "\t");
                }
                Debug.WriteLine("\n");

                foreach (DataRow row in sensorData.Rows)
                {
                    foreach (var item in row.ItemArray)
                    {
                        Debug.Write(item + "\t");
                    }
                    Debug.WriteLine("\n");
                }
            }
            else
            {
                Debug.WriteLine("No data returned from the database.");
            }

            // ...

            return sensorData;
        }


    }
}

