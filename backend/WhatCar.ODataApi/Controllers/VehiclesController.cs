using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using WhatCar.ODataCore.Data;
using WhatCar.ODataCore.Models;

namespace WhatCar.ODataApi.Controllers;

[ApiController]
[Route("odata/Vehicles")]
public class VehiclesController : ODataController
{
    private readonly VehicleSalesDbContext _context;
    public VehiclesController(VehicleSalesDbContext context) => _context = context;

    [HttpGet]
    [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All, MaxExpansionDepth = 4)]
    public IQueryable<Vehicle> Get() => _context.Vehicles;
}
