// -------------------------------------------------------------------------------------------------
// Copyright (c) Integrated Health Information Systems Pte Ltd. All rights reserved.
// -------------------------------------------------------------------------------------------------

using Ihis.FhirEngine.WebApi.Extensions.Swashbuckle;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Reads the 'FhirEngine' configuration section to add services
builder.AddFhirEngineServer();

var app = builder.Build();

// IPostConfigureOptions<SwaggerGenOptions> is added if FhirEngine:SystemPlugins:Swagger is present
if (app.Services.GetService<IPostConfigureOptions<SwaggerGenOptions>>() != null)
{
    var swaggerOptions = app.Services.GetService<IOptions<FhirSwaggerOptions>>();
    var version = swaggerOptions!.Value.Version ?? "v1";
    // app.UseSwagger() is unnecessary when FhirEngine:SystemPlugins:Swagger:RegisterMiddleware is true
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint($"/swagger/{version}/swagger.json", version);
        options.DisplayRequestDuration();
    });
}

app.UseHsts()
    .UseRouting()
    .UseFhirEngineMiddlewares()
    .UseAuthentication()
    .UseAuthorization()
    .UseEndpoints(endpoints =>
    {
        endpoints.MapFhirEngineHealthChecks("/health");
        endpoints.MapFhirEngine();
    });

await app.RunAsync();
