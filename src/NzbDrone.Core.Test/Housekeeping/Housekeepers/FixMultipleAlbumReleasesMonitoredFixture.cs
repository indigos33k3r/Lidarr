using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Music;
using System.Linq;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class FixMultipleAlbumReleasesMonitoredFixture : DbTest<FixMultipleAlbumReleasesMonitored, AlbumRelease>
    {
        private ReleaseRepository _releaseRepo;
        
        [SetUp]
        public void Setup()
        {
            _releaseRepo = Mocker.Resolve<ReleaseRepository>();
            Mocker.SetConstant<IReleaseRepository>(_releaseRepo);
        }
        
        [Test]
        public void should_unmonitor_some_if_too_many_monitored()
        {
            var releases = Builder<AlbumRelease>
                .CreateListOfSize(10)
                .All()
                .With(x => x.Id = 0)
                .With(x => x.Monitored = false)
                .With(x => x.AlbumId = 1)
                .Random(3)
                .With(x => x.Monitored = true)
                .BuildList();
            
            _releaseRepo.InsertMany(releases);
            
            _releaseRepo.All().Count(x => x.Monitored).Should().Be(3);
        
            Subject.Clean();
            
            _releaseRepo.All().Count(x => x.Monitored).Should().Be(1);
            
            // Count sentry and standard
            ExceptionVerification.ExpectedWarns(2);
        }
        
        [Test]
        public void should_monitor_one_if_none_monitored()
        {
            var releases = Builder<AlbumRelease>
                .CreateListOfSize(10)
                .All()
                .With(x => x.Id = 0)
                .With(x => x.Monitored = false)
                .With(x => x.AlbumId = 1)
                .BuildList();
            
            _releaseRepo.InsertMany(releases);
            
            _releaseRepo.All().Count(x => x.Monitored).Should().Be(0);
        
            Subject.Clean();
            
            _releaseRepo.All().Count(x => x.Monitored).Should().Be(1);

            // Count sentry and standard
            ExceptionVerification.ExpectedWarns(2);
        }
    }
}
