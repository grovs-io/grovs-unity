using GrovsSDK;
using NUnit.Framework;

namespace GrovsTestApp.Tests
{
    public class AppContextTests
    {
        [TearDown]
        public void TearDown()
        {
            AppContext.ResetStatics();
            Grovs.ShouldShowInAppMessage = null;
        }

        [Test]
        public void Reset_drops_the_context_so_the_next_access_subscribes_again()
        {
            var first = AppContext.Current;
            Assert.That(AppContext.Current, Is.SameAs(first));

            // What the SDK does to its own hooks when Play Mode starts without a domain reload.
            Grovs.ShouldShowInAppMessage = null;
            AppContext.ResetStatics();

            var second = AppContext.Current;
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(Grovs.ShouldShowInAppMessage, Is.Not.Null);
            second.BlockAll = true;
            Assert.That(Grovs.ShouldShowInAppMessage("c1"), Is.False);
        }
    }
}
