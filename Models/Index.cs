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
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        public CoEBrickAPI BrickAPI;
        public List<object[]> SensorData { get; set; }
        public string SensorDataJson { get; set; }


        public IndexModel(CoEBrickAPI brickAPI)
        {
            BrickAPI = brickAPI;
        }

        public void OnGet()
        {
            // Get All Floors in the CoE

            SensorData = GetDataFromDatabase("8d15fd43-27cd-4897-9499-2bfc382f30d2");
            SensorDataJson = JsonConvert.SerializeObject(SensorData);

            // Deserialize the JSON string back to a list of lists
            List<List<object>> sensorDataList = JsonConvert.DeserializeObject<List<List<object>>>(SensorDataJson);

            // Flatten the list of lists into a single list
            List<object> flattenedList = new List<object>();
            foreach (var item in sensorDataList)
            {
                flattenedList.Add(item[0]);
            }

            // Serialize the flattened list again
            SensorDataJson = JsonConvert.SerializeObject(flattenedList);

            // Pass the JSON string to the view


        }

        public List<object[]> GetDataFromDatabase(string uuid)
        {
            List<object[]> sensorDataList = new List<object[]>();

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
                    Console.WriteLine("Connection opened! now querying sensor data...");

                    DataTable sensorData = BrickAPI.GetSensorData(conn, uuid);

                    // Convert each DataRow to an array and add it to the list
                    foreach (DataRow row in sensorData.Rows)
                    {
                        sensorDataList.Add(row.ItemArray);
                    }

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

            // Output debugging information
            foreach (var rowData in sensorDataList)
            {
                foreach (var item in rowData)
                {
                    Debug.Write(item + "\t");
                }
                Debug.WriteLine("\n");
            }

            if (sensorDataList.Count == 0)
            {
                Debug.WriteLine("No data returned from the database.");
            }

            return sensorDataList;
        }

    }
}

