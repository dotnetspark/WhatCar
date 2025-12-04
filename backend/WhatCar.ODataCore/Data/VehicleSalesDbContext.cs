using Microsoft.EntityFrameworkCore;
using WhatCar.ODataCore.Models;

namespace WhatCar.ODataCore.Data;

public class VehicleSalesDbContext : DbContext
{
    public VehicleSalesDbContext(DbContextOptions<VehicleSalesDbContext> options) : base(options) { }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<SalesData> SalesData => Set<SalesData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vehicle>()
            .HasIndex(v => v.VehicleHash)
            .IsUnique();

        modelBuilder.Entity<SalesData>()
            .HasIndex(s => new { s.VehicleId, s.Year, s.Quarter })
            .IsUnique();

        modelBuilder.Entity<SalesData>()
            .HasOne(s => s.Vehicle)
            .WithMany(v => v.Sales)
            .HasForeignKey(s => s.VehicleId);
    }
}
