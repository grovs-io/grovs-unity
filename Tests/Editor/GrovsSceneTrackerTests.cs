using System.Collections.Generic;
using GrovsSDK.Internal;
using NUnit.Framework;

namespace GrovsSDK.Tests
{
    public class GrovsSceneTrackerTests
    {
        FakePlatform _fake;

        [SetUp]
        public void SetUp()
        {
            _fake = new FakePlatform();
        }

        [TearDown]
        public void TearDown()
        {
            Grovs.ResetForTests(new GrovsStub());
        }

        GrovsSceneTracker CreateTracker(bool sceneTracking)
        {
            _fake.SceneTracking = sceneTracking;
            Grovs.ResetForTests(_fake);
            return new GrovsSceneTracker(Grovs.IsSceneTrackingEnabled, name => Grovs.TrackScreenView(name));
        }

        [Test]
        public void SceneTracking_Disabled_NoCalls()
        {
            CreateTracker(false).OnActiveSceneChanged("Level1");

            Assert.AreEqual(0, _fake.Count("TrackScreenView"));
        }

        [Test]
        public void SceneTracking_Enabled_TracksSceneName()
        {
            CreateTracker(true).OnActiveSceneChanged("Level1");

            CollectionAssert.AreEqual(new object[] { "Level1", null }, _fake.Last("TrackScreenView"));
        }

        [Test]
        public void SceneTracking_SameSceneTwice_TracksOnce()
        {
            var tracker = CreateTracker(true);
            tracker.OnActiveSceneChanged("Level1");
            tracker.OnActiveSceneChanged("Level1");

            Assert.AreEqual(1, _fake.Count("TrackScreenView"));
        }

        [Test]
        public void SceneTracking_NewScene_TracksAgain()
        {
            var tracker = CreateTracker(true);
            tracker.OnActiveSceneChanged("Level1");
            tracker.OnActiveSceneChanged("Level2");
            tracker.OnActiveSceneChanged("Level1");

            Assert.AreEqual(3, _fake.Count("TrackScreenView"));
        }

        [Test]
        public void SimulateDeeplink_RaisesEvent()
        {
            Grovs.ResetForTests(_fake);
            var got = new List<GrovsDeeplink>();
            Grovs.DeeplinkReceived += got.Add;

            Grovs.SimulateDeeplink("https://x/y", "{\"k\":1}");
            Grovs.Router.Drain();

            Assert.AreEqual(1, got.Count);
            Assert.AreEqual("https://x/y", got[0].Link);
            Assert.AreEqual(1L, got[0].Data["k"]);
        }

        [Test]
        public void SimulateDeeplink_BuffersUntilSubscriber()
        {
            Grovs.ResetForTests(_fake);
            Grovs.SimulateDeeplink("https://x/y", null);
            Grovs.Router.Drain();

            var got = new List<GrovsDeeplink>();
            Grovs.DeeplinkReceived += got.Add;

            Assert.AreEqual(1, got.Count);
            Assert.AreEqual(0, got[0].Data.Count);
        }
    }
}
