using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Utilities.Samples.Web;
using Microsoft.FeatureManagement.Utilities.Samples.Web.Features.Pricing;
using Microsoft.FeatureManagement.Utilities.Samples.Web.Features.Shipping;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFeatureManagement();
builder.Services.AddShipping();
builder.Services.AddPricing();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options => options.EnableTryItOutByDefault());

app.MapAppEndpoints();

await app.RunAsync();
