using System;
using System.Linq;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Profiles.Releases;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class CutoffSpecification : IDecisionEngineSpecification
    {
        private readonly UpgradableSpecification _upgradableSpecification;
        private readonly IMediaFileService _mediaFileService;
        private readonly ITrackService _trackService;
        private readonly Logger _logger;
        private readonly ICached<bool> _missingFilesCache;
        private readonly IPreferredWordService _preferredWordServiceCalculator;
        
        public CutoffSpecification(UpgradableSpecification upgradableSpecification,
                                   Logger logger,
                                   ICacheManager cacheManager,
                                   IMediaFileService mediaFileService,
                                   IPreferredWordService preferredWordServiceCalculator,
                                   ITrackService trackService)
        {
            _upgradableSpecification = upgradableSpecification;
            _mediaFileService = mediaFileService;
            _trackService = trackService;
            _missingFilesCache = cacheManager.GetCache<bool>(GetType());
            _preferredWordServiceCalculator = preferredWordServiceCalculator;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteAlbum subject, SearchCriteriaBase searchCriteria)
        {

            var profile = subject.Artist.QualityProfile.Value;

            foreach (var album in subject.Albums)
            {
                var tracksMissing = _missingFilesCache.Get(album.Id.ToString(), () => _trackService.TracksWithoutFiles(album.Id).Any(),
                                                           TimeSpan.FromSeconds(30));
                var trackFiles = _mediaFileService.GetFilesByAlbum(album.Id);

                if (!tracksMissing && trackFiles.Any())
                {
                    var lowestQuality = trackFiles.Select(c => c.Quality).OrderBy(c => c.Quality.Id).First();

                    _logger.Debug("Comparing file quality and language with report. Existing file is {0}", lowestQuality.Quality);

                    if (!_upgradableSpecification.CutoffNotMet(profile,
                                                               subject.Artist.LanguageProfile,
                                                               lowestQuality,
                                                               trackFiles[0].Language,
                                                               _preferredWordServiceCalculator.Calculate(subject.Artist, trackFiles[0].GetSceneOrFileName()),
                                                               subject.ParsedAlbumInfo.Quality,
                                                               subject.PreferredWordScore))
                    {
                        _logger.Debug("Cutoff already met, rejecting.");
                        var qualityCutoffIndex = profile.GetIndex(profile.Cutoff);
                        var qualityCutoff = profile.Items[qualityCutoffIndex.Index];

                        return Decision.Reject("Existing file meets cutoff: {0} - {1}", qualityCutoff, subject.Artist.LanguageProfile.Value.Cutoff);
                    }

                }
            }

            return Decision.Accept();
        }
    }
}
