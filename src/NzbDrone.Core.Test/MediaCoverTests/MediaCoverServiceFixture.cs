using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Music.Events;

namespace NzbDrone.Core.Test.MediaCoverTests
{
    [TestFixture]
    public class MediaCoverServiceFixture : CoreTest<MediaCoverService>
    {
        Artist _artist;
        Album _album;
        private HttpResponse _httpResponse;

        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<IAppFolderInfo>(new AppFolderInfo(Mocker.Resolve<IStartupContext>()));

            _artist = Builder<Artist>.CreateNew()
                .With(v => v.Id = 2)
                .With(v => v.Metadata.Value.Images = new List<MediaCover.MediaCover> { new MediaCover.MediaCover(MediaCoverTypes.Poster, "") })
                .Build();

            _album = Builder<Album>.CreateNew()
                .With(v => v.Id = 4)
                .With(v => v.Images = new List<MediaCover.MediaCover> { new MediaCover.MediaCover(MediaCoverTypes.Cover, "") })
                .Build();

            _httpResponse = new HttpResponse(null, new HttpHeader(), "");
            Mocker.GetMock<IHttpClient>().Setup(c => c.Head(It.IsAny<HttpRequest>())).Returns(_httpResponse);
        }

        [Test]
        public void should_convert_cover_urls_to_local()
        {
            var covers = new List<MediaCover.MediaCover>
                {
                    new MediaCover.MediaCover {CoverType = MediaCoverTypes.Banner}
                };

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FileGetLastWrite(It.IsAny<string>()))
                  .Returns(new DateTime(1234));

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Subject.ConvertToLocalUrls(12, covers);


            covers.Single().Url.Should().Be("/MediaCover/12/banner.jpg?lastWrite=1234");
        }

        [Test]
        public void should_convert_album_cover_urls_to_local()
        {
            var covers = new List<MediaCover.MediaCover>
                {
                    new MediaCover.MediaCover {CoverType = MediaCoverTypes.Disc}
                };

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FileGetLastWrite(It.IsAny<string>()))
                  .Returns(new DateTime(1234));

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Subject.ConvertToLocalUrls(12, covers, 6);


            covers.Single().Url.Should().Be("/MediaCover/12/6/disc.jpg?lastWrite=1234");
        }

        [Test]
        public void should_convert_media_urls_to_local_without_time_if_file_doesnt_exist()
        {
            var covers = new List<MediaCover.MediaCover>
                {
                    new MediaCover.MediaCover {CoverType = MediaCoverTypes.Banner}
                };


            Subject.ConvertToLocalUrls(12, covers);


            covers.Single().Url.Should().Be("/MediaCover/12/banner.jpg");
        }

        [Test]
        public void should_resize_covers_if_main_downloaded()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<DateTime>(), It.IsAny<string>()))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Subject.HandleAsync(new ArtistUpdatedEvent(_artist));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
        }

        [Test]
        public void should_resize_covers_if_missing()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<DateTime>(), It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(false);

            Subject.HandleAsync(new ArtistUpdatedEvent(_artist));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
        }

        [Test]
        public void should_not_resize_covers_if_exists()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<DateTime>(), It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetFileSize(It.IsAny<string>()))
                  .Returns(1000);

            Subject.HandleAsync(new ArtistUpdatedEvent(_artist));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_resize_covers_if_existing_is_empty()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<DateTime>(), It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetFileSize(It.IsAny<string>()))
                  .Returns(0);

            Subject.HandleAsync(new ArtistUpdatedEvent(_artist));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
        }

        [Test]
        public void should_log_error_if_resize_failed()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<DateTime>(), It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(false);

            Mocker.GetMock<IImageResizer>()
                  .Setup(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                  .Throws<ApplicationException>();

            Subject.HandleAsync(new ArtistUpdatedEvent(_artist));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
        }
    }
}
