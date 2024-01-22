using Ihis.FhirEngine.WebApi.Testing.Fixtures;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace Synapxe.TestFhir.IntegrationTests
{
    [Binding]
    public static class Hooks
    {
        private static readonly ConcurrentDictionary<string, FhirWebApplicationFactory<Program>> applicationCache = new();

        private static FhirWebApplicationFactory<Program> CreateApplication(string environmentName)
            // customize the application fixture here
            => new FhirWebApplicationFactory<Program>(environmentName);

        [BeforeFeature]
        public static async Task BeforeFeature(FeatureContext featureContext)
        {
            var environmentName = featureContext.FeatureInfo.Tags
                .Where(x => x.StartsWith("Environment:"))
                .Select(x => x[12..])
                .SingleOrDefault() ?? "Integration";
            var application = applicationCache.GetOrAdd(environmentName, CreateApplication);
            featureContext.FeatureContainer.RegisterInstanceAs<IFhirWebApplication>(application);
            await application.WaitUntilHealthy();
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            foreach (var app in applicationCache)
            {
                app.Value.Dispose();
            }
        }
    }
}
