// -------------------------------------------------------------------------------------------------
// Copyright (c) Integrated Health Information Systems Pte Ltd. All rights reserved.
// -------------------------------------------------------------------------------------------------

using Ihis.FhirEngine.Core;
using Ihis.FhirEngine.Core.Constants;
using Ihis.FhirEngine.Core.Exceptions;
using Ihis.FhirEngine.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Synapxe.TestFhir.CustomResource;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Task = System.Threading.Tasks.Task;

namespace Synapxe.TestFhir.Handlers
{
    [FhirHandlerClass(AcceptedType = nameof(Education))]
    public class EducationDataFhirHandler
    {
        private readonly ConcurrentDictionary<string, Education> educationStore;

        public EducationDataFhirHandler(IMemoryCache memoryCache)
        {
            educationStore = memoryCache.GetOrCreate(typeof(EducationDataFhirHandler), e => new ConcurrentDictionary<string, Education>());
        }

        [FhirHandler(FhirInteractionType.Read)]
        public Task<Education> GetAsync(ResourceKey resourceKey)
        {
            return educationStore.TryGetValue(resourceKey.Id, out var resource) ? Task.FromResult(resource) : throw new ResourceNotFoundException(resourceKey);
        }

        [FhirHandler(FhirInteractionType.Vread)]
        public Task<Education> GetVersionedAsync(ResourceKey resourceKey)
        {
            return educationStore.TryGetValue($"{resourceKey.Id}/{resourceKey.VersionId}", out var resource) ? Task.FromResult(resource) : throw new ResourceNotFoundException(resourceKey);
        }

        [FhirHandler(FhirInteractionType.Create)]
        public async Task<Education> CreateAsync(IFhirContext context, Education education, CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken);
            education.Id = Guid.NewGuid().ToString("N").ToUpperInvariant();
            education.VersionId = "1";
            education.Meta.LastUpdated = DateTimeOffset.UtcNow;

            educationStore.TryAdd(education.Id, education);
            educationStore.TryAdd($"{education.Id}/{education.VersionId}", education);
            context.Response.StatusCode = System.Net.HttpStatusCode.Created;
            context.Response.AddOrAppendHeader(KnownFhirHeaders.Location, $"{context.Request.Path}/{education.Id}");
            return education;
        }

        [FhirHandler(FhirInteractionType.Update)]
        public Task<Education> UpdateAsync(IFhirContext context, string id, Education education)
        {
            if (educationStore.TryGetValue(id!, out var oldEducation) &&
                int.TryParse(oldEducation.VersionId, out var oldVersion))
            {
                if (education.Id != id)
                {
                    throw new BadRequestException("Resource ID in resource does not match with Resource ID in path.");
                }

                education.VersionId = (oldVersion + 1).ToString();
            }
            else
            {
                education.Id = Guid.NewGuid().ToString("N").ToUpperInvariant();
                education.VersionId = "1";
            }

            educationStore[id] = education;
            educationStore.TryAdd($"{education.Id}/{education.VersionId}", education);
            context.Response.SetETagHeader(education);

            return Task.FromResult(education);
        }

        [FhirHandler(FhirInteractionType.SearchType)]
        public async IAsyncEnumerable<Education> SearchAsync(
            IFhirContext context,
            string? institute = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            IEnumerable<Education> result = educationStore.Where(x => !x.Key.Contains("/")).Select(x => x.Value);

            if (institute != null)
            {
                result = result.Where(x => x.Institute?.Reference == institute);
            }

            foreach (var item in result)
            {
                await Task.Delay(10, cancellationToken);
                yield return item;
            }

            context.Response.SearchResponse.Value.IncrementCount(result.Count());
        }
    }
}
