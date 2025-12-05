#pragma warning disable ASPIRECSHARPAPPS001
using Aspire.Hosting;
using Aspire.Hosting.Redis;

var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sql")
    .WithContainerName("whatcar-sql")
    .WithDataVolume("whatcar-sql-data")
    .WithLifetime(ContainerLifetime.Persistent);

var sqlDatabase = sqlServer.AddDatabase("whatcardb");

var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var csvLoader = builder.AddCSharpApp("csv-loader", "../../tools/CsvLoader.cs")
    .WithReference(sqlDatabase)
    .WaitFor(sqlDatabase);

var odataApi = builder.AddProject<Projects.WhatCar_ODataApi>("whatcar-odata")
    .WithExternalHttpEndpoints()
    .WithReference(sqlDatabase)
    .WaitFor(sqlDatabase);

var webApi = builder.AddProject<Projects.WhatCar_Api>("whatcar-api")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Llm__Endpoint", builder.Configuration["Llm:Endpoint"])
    .WithEnvironment("Llm__Model", builder.Configuration["Llm:Model"])
    .WithReference(redis)
    .WithReference(sqlDatabase)
    .WithReference(odataApi)
    .WaitFor(redis)
    .WaitFor(sqlDatabase)
    .WaitFor(odataApi);

var frontend = builder.AddViteApp("frontend", "../../frontend")
    .WithEnvironment("BROWSER", "none")
    .WithReference(webApi)
    .WaitFor(webApi);

builder.Build().Run();