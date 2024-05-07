using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BrickSchemaTranslation;
using System.Collections.Generic;
using CoETestbedWebsite.Models;
using System.Diagnostics;
using CoETestbedWebsite.Models;
using System.Data;

namespace CoETestbedWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly CoEBrickAPI _brickAPI;

        public HomeController(ILogger<HomeController> logger, CoEBrickAPI brickAPI)
        {
            _logger = logger;
            _brickAPI = brickAPI;
        }

        public IActionResult Index()
        {
            // Create an instance of IndexModel and pass the necessary dependencies
            var indexModel = new IndexModel(_brickAPI);

            // Call the OnGet method to perform any necessary initialization
            indexModel.OnGet();

            // Pass the model to the view
            return View(indexModel);
        }

        public IActionResult Floors()
        {
            // Create an instance of IndexModel and pass the necessary dependencies
            var FloorModel = new FloorModel(_brickAPI);

            // Call the OnGet method to perform any necessary initialization
            FloorModel.OnGet();

            // Pass the model to the view
            return View(FloorModel);
        }

        // Other action methods remain unchanged
        public IActionResult Rooms(string floor)
        {
            // Create an instance of RoomsModel and pass the necessary dependencies
            var roomsModel = new RoomModel(_brickAPI);

            // Set the floor number in the model
            roomsModel.FloorNumber = floor;

            // Call the OnGet method to perform any necessary initialization
            roomsModel.OnGet(floor);

            // Pass the model to the view
            return View(roomsModel);
        }
        public IActionResult SensorData(string room)
        {
            var sensorDataModel = new SensorDataModel(_brickAPI);

            // 

            sensorDataModel.RoomNumber = room;

            sensorDataModel.OnGet(room);

            //DataTable latestSensorData = sensorDataModel.GetLatestSensorData(room);

            return View(sensorDataModel);

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
    }
}
