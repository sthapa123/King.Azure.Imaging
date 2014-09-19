﻿namespace King.Azure.Imaging.Unit.Test
{
    using ImageProcessor.Imaging.Formats;
    using King.Azure.Data;
    using King.Azure.Imaging.Models;
    using NSubstitute;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    [TestFixture]
    public class ProcessorTests
    {
        [Test]
        public void Constructor()
        {
            var imaging = Substitute.For<IImaging>();
            var store = Substitute.For<IDataStore>();
            new Processor(imaging, store, new Dictionary<string, IImageVersion>());
        }

        [Test]
        public void IsIProcessor()
        {
            var imaging = Substitute.For<IImaging>();
            var store = Substitute.For<IDataStore>();
            Assert.IsNotNull(new Processor(imaging, store, new Dictionary<string, IImageVersion>()) as IProcessor<ImageQueued>);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorImagingNull()
        {
            var store = Substitute.For<IDataStore>();
            new Processor(null, store, new Dictionary<string, IImageVersion>());
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorStoreeNull()
        {
            var imaging = Substitute.For<IImaging>();
            new Processor(imaging, null, new Dictionary<string, IImageVersion>());
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorVersionsNull()
        {
            var imaging = Substitute.For<IImaging>();
            var store = Substitute.For<IDataStore>();
            new Processor(imaging, store, null);
        }

        [Test]
        public async Task Process()
        {
            var bytes = File.ReadAllBytes(Environment.CurrentDirectory + @"\icon.png");
            var data = new ImageQueued()
            {
                Identifier = Guid.NewGuid(),
                FileNameFormat = "good_{0}_file",
                OriginalExtension = string.Empty,
            };
            var versions = new Dictionary<string, IImageVersion>();
            var version = new ImageVersion()
            {
                Width = 100,
                Height = 100,
                Format = new JpegFormat { Quality = 70 },
            };
            versions.Add("temp", version);
            var fileName = string.Format(data.FileNameFormat, "temp");
            var original = string.Format(data.FileNameFormat, Naming.Original);

            var imaging = Substitute.For<IImaging>();
            imaging.Resize(bytes, version).Returns(bytes);
            var streamer = Substitute.For<IStreamer>();
            streamer.GetBytes(original).Returns(Task.FromResult(bytes));
            var store = Substitute.For<IDataStore>();
            store.Streamer.Returns(streamer);
            store.Save(fileName, bytes, "temp", version.Format.MimeType, data.Identifier, false, null, version.Format.Quality);

            var ip = new Processor(imaging, store, versions);
            var result = await ip.Process(data);

            Assert.IsTrue(result);

            imaging.Received().Resize(bytes, version);
            streamer.Received().GetBytes(original);
            var s = store.Streamer.Received();
            store.Received().Save(fileName, bytes, "temp", version.Format.MimeType, data.Identifier, false, null, version.Format.Quality);
        }
    }
}