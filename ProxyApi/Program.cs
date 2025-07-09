using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

//const string corsPolicyName = "default";

builder.Services.AddCors();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var serviceName = "AviationWeatherProxy";
var otel = builder.Services.AddOpenTelemetry();

// Configure OpenTelemetry Resources with the application name
otel.ConfigureResource(resource => resource
    .AddService(serviceName: builder.Environment.ApplicationName));

// Add Metrics for ASP.NET Core and our custom metrics and export to Prometheus
otel.WithMetrics(metrics => metrics
    // Metrics provider from OpenTelemetry
    .AddAspNetCoreInstrumentation()
    // Metrics provides by ASP.NET Core in .NET 8
    .AddMeter("Microsoft.AspNetCore.Hosting")
    .AddMeter("Microsoft.AspNetCore.Server.Kestrel"));

// Add Tracing for ASP.NET Core and our custom ActivitySource and export to Jaeger
otel.WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation();
    tracing.AddHttpClientInstrumentation();
    tracing.AddConsoleExporter();

});


builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
    options.AddConsoleExporter();
});
builder.Services.AddHttpClient("AviationWeather", client =>
{
    client.BaseAddress = new Uri("https://aviationweather.gov/");

});
var app = builder.Build();
app.UseCors(policy => policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            );
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

char[] _splitter = new char[] { ' ', ',', ';', ':' };

app.MapGet("api/data/metar", async (HttpContext context, IHttpClientFactory clientFactory) =>
{
    var parameters = context.Request.QueryString.ToUriComponent();
    var httpClient = clientFactory.CreateClient("AviationWeather");
    var url = $"api/data/metar{parameters}";

    return await httpClient.GetStringAsync(url);
}).WithName("GetMETAR");

app.MapGet("api/data/taf", async (HttpContext context, IHttpClientFactory clientFactory) =>
{
    var parameters = context.Request.QueryString.ToUriComponent();
    var httpClient = clientFactory.CreateClient("AviationWeather");
    var url = $"api/data/taf{parameters}";

    return await httpClient.GetStringAsync(url);
}).WithName("GetTAF");


app.MapGet("api/data/stationinfo", async (HttpContext context, IHttpClientFactory clientFactory) =>

{
    var parameters = context.Request.QueryString.ToUriComponent();
    var httpClient = clientFactory.CreateClient("AviationWeather");
    var url = $"api/data/stationinfo{parameters}";

    return await httpClient.GetStringAsync(url);
}).WithName("GetStationInfo");

app.Run();

