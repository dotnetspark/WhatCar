using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using WhatCar.ODataCore;
using WhatCar.ODataCore.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<VehicleSalesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("whatcardb")));

builder.Services.AddControllers()
    .AddOData(opt =>
    {
        opt.EnableAttributeRouting = true;
        opt.Select()
            .Filter()
            .OrderBy()
            .Expand()
            .Count()
            .SetMaxTop(100);
        opt.AddRouteComponents("odata", ODataApiModelBuilder.GetEdmModel())
            .EnableQueryFeatures(maxTopValue: 100);
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

app.Run();