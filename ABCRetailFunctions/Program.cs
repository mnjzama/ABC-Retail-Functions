using ABCRetailFunctions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RetailAppCore.Services;
using System;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var connectionString = ""; // Replace with your actual Azure Storage connection string
var queueName = ""; // Replace with your actual Azure Queue name
var fileShareName = ""; // Replace with your actual Azure File Share name

// Base URL of HTTP functions (TableStorageFunctions)
var functionApiBaseUrl = ""; // Replace with your actual function app base URL
var customerServiceApiBaseUrl = ""; // Replace with your actual customer service API base URL 
var productServiceApiBaseUrl = ""; // Replace with your actual product service API base URL

builder.Services.AddSingleton<TableStorage>();

builder.Services.AddSingleton(_ => new QueueService(connectionString, queueName));
builder.Services.AddSingleton(_ => new BlobService(connectionString));
builder.Services.AddSingleton(_ => new FileShareService(connectionString, fileShareName));

// HttpClient for functions/services
builder.Services.AddHttpClient<QueueOrderProcessor>(c =>
{
    c.BaseAddress = new Uri(functionApiBaseUrl);
});

builder.Services.AddHttpClient<CustomerService>(c =>
{
    c.BaseAddress = new Uri(customerServiceApiBaseUrl);
});

builder.Services.AddHttpClient<ProductService>(c =>
{
    c.BaseAddress = new Uri(productServiceApiBaseUrl);
});

builder.Services.AddSingleton<ProductService>();
builder.Services.AddSingleton<CustomerService>();

builder.Services.AddApplicationInsightsTelemetryWorkerService()
       .ConfigureFunctionsApplicationInsights();

builder.Build().Run();