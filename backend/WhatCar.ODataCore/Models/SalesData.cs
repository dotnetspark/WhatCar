using System.ComponentModel.DataAnnotations;

namespace WhatCar.ODataCore.Models;

public class SalesData
{
    [Key]
    public int SalesId { get; set; }
    public int VehicleId { get; set; }
    public int Year { get; set; }
    public int Quarter { get; set; }
    public int UnitsSold { get; set; }

    public Vehicle Vehicle { get; set; } = default!;
}
