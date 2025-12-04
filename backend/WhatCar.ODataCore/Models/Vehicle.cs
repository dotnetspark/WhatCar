using System.Collections.Generic;

namespace WhatCar.ODataCore.Models;

public class Vehicle
{
    public int VehicleId { get; set; }
    public string BodyType { get; set; } = default!;
    public string Make { get; set; } = default!;
    public string GenModel { get; set; } = default!;
    public string Model { get; set; } = default!;
    public string Fuel { get; set; } = default!;
    public string LicenceStatus { get; set; } = default!;
    public string VehicleHash { get; set; } = default!;

    public ICollection<SalesData> Sales { get; set; } = new List<SalesData>();
}
