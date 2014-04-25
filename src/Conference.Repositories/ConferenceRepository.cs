﻿using MsCampus.Win.Shared.Contracts.Services;
using Conference.Contracts.Models;
using Conference.Contracts.Repositories;
using Conference.Contracts.Services.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MsCampus.Win.Shared.Contracts.Repositories;

namespace Conference.Repositories
{
    public class ConferenceRepository : IConferenceRepository
    {
        private const string ConferenceDataKey = "conferenceData";

        private IConferenceDataService _conferenceDataService;
        private ICacheService _cacheService;
        private ConferenceData _defaultConferenceData;

        public ConferenceRepository(
            IConferenceDataService conferenceDataService,
            ICacheService cacheService,
            ConferenceData defaultConferenceData)
        {
            _conferenceDataService = conferenceDataService;
            _cacheService = cacheService;
            _defaultConferenceData = defaultConferenceData;
        }

        public async Task<RepositoryResult<ConferenceData>> GetConferenceDataAsync()
        {
            var cts = new CancellationTokenSource();
            return await GetConferenceDataAsync(cts.Token);
        }

        public async Task<RepositoryResult<ConferenceData>> GetConferenceDataAsync(CancellationToken cancellationToken)
        {
            var item = await _cacheService.GetAsync<ConferenceData>(ConferenceDataKey).ConfigureAwait(false);
            if (item.HasValue)
            {
                if (cancellationToken.IsCancellationRequested)
                    return item.Value;
                var versionId = item.Value.Version;
                try
                {
                    var latestVersionId = await _conferenceDataService.GetVersionAsync(cancellationToken).ConfigureAwait(false);
                    if (versionId >= latestVersionId)
                    {
                        return item.Value;
                    }
                }
                catch
                {
                    return RepositoryResult<ConferenceData>.Create(item.Value, false);
                }
            }
            try
            {
                var data = await _conferenceDataService.GetConfDataAsync(cancellationToken).ConfigureAwait(false);
                await _cacheService.PutAsync(ConferenceDataKey, data).ConfigureAwait(false);
                return data;
            }
            catch
            {
                return RepositoryResult<ConferenceData>.Create(item.HasValue ? item.Value : _defaultConferenceData, false);
            }
        }
    }
}
