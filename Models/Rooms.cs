using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BrickSchemaTranslation;
using VDS.RDF;


namespace CoETestbedWebsite.Models
{
    public class RoomModel : PageModel
    {
        private readonly ILogger<FloorModel> _logger;
        public CoEBrickAPI BrickAPI;
        public string FloorNumber { get; set; }
        public List<string> Rooms { get; private set; }

        public RoomModel(CoEBrickAPI brickAPI)
        {
            BrickAPI = brickAPI;
        }

        public void OnGet(string floor)
        {
            FloorNumber = floor;
            Rooms = BrickAPI.GetFloorRooms(FloorNumber);
            Rooms = Rooms.OrderBy(room => room).ToList();
        }
    }
}

