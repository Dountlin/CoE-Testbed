using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BrickSchemaTranslation;
using VDS.RDF;


namespace CoETestbedWebsite.Models
{
	public class FloorModel : PageModel
	{
        private readonly ILogger<FloorModel> _logger;
        public CoEBrickAPI BrickAPI;
        public List<string> Floors { get; private set; }

        public FloorModel(CoEBrickAPI brickAPI)
        {
            BrickAPI = brickAPI;
        }

        public void OnGet()
        {
            // Get All Floors in the CoE
            Floors = BrickAPI.GetAllFloors();
            Floors = Floors.OrderBy(floor => floor).ToList();
        }
    }
}

