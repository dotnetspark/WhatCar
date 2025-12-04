using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using WhatCar.ODataCore.Models;

namespace WhatCar.ODataCore.Data;

public static class ODataApiModelBuilder
{
    public static IEdmModel GetEdmModel()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<Vehicle>("Vehicles");
        builder.EntitySet<SalesData>("SalesData");
        return builder.GetEdmModel();
    }
}
