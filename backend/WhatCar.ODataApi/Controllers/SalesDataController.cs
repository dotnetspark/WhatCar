using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using WhatCar.ODataCore.Data;
using WhatCar.ODataCore.Models;

namespace WhatCar.ODataApi.Controllers;

[ApiController]
[Route("odata/SalesData")]
public class SalesDataController : ODataController
{
    private readonly VehicleSalesDbContext _context;
    public SalesDataController(VehicleSalesDbContext context) => _context = context;

    [HttpGet]
    [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All, MaxExpansionDepth = 4)]
    public IQueryable<SalesData> Get() => _context.SalesData;
}
