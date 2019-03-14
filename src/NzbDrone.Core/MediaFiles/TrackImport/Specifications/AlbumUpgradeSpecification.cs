using System.Linq;
using NLog;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.DecisionEngine.Specifications;

namespace NzbDrone.Core.MediaFiles.TrackImport.Specifications
{
    public class AlbumUpgradeSpecification : IImportDecisionEngineSpecification<LocalAlbumRelease>
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly UpgradableSpecification _upgradableSpecification;
        private readonly Logger _logger;

        public AlbumUpgradeSpecification(UpgradableSpecification qualityUpgradableSpecification,
                                         IMediaFileService mediaFileService,
                                         Logger logger)
        {
            _upgradableSpecification = qualityUpgradableSpecification;
            _mediaFileService = mediaFileService;
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalAlbumRelease localAlbumRelease)
        {
            var artist = localAlbumRelease.AlbumRelease.Album.Value.Artist.Value;
            var qualityComparer = new QualityModelComparer(artist.QualityProfile);

            // check if we are changing release
            var currentRelease = localAlbumRelease.AlbumRelease.Album.Value.AlbumReleases.Value.Single(x => x.Monitored);
            var newRelease = localAlbumRelease.AlbumRelease;

            // if we are, check we are upgrading
            if (newRelease.Id != currentRelease.Id)
            {
                var trackFiles = _mediaFileService.GetFilesByAlbum(localAlbumRelease.AlbumRelease.AlbumId);
                var currentQualities = trackFiles.Select(c => c.Quality).ToList();

                // min quality of all new tracks
                var newMinQuality = localAlbumRelease.LocalTracks.Select(x => x.Quality).OrderBy(x => x, qualityComparer).First();
                _logger.Debug("Min quality of new files: {0}", newMinQuality);

                if (!_upgradableSpecification.IsQualityUpgradable(artist.QualityProfile,
                                                                  currentQualities,
                                                                  newMinQuality))
                {
                    _logger.Debug("This album isn't a quality upgrade. Skipping {0}", localAlbumRelease);
                    return Decision.Reject("Not an upgrade for existing album file(s)");
                }
            }

            return Decision.Accept();
        }
    }
}
