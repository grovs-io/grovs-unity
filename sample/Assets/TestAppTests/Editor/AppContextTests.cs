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

        [Test]
        public void A_new_scaffold_opens_on_the_selected_tab_and_remembers_changes()
        {
            var ctx = AppContext.Current;
            Assert.That(ctx.SelectedTab, Is.EqualTo(Tab.Home));

            ctx.SelectedTab = Tab.InApp;
            var scaffold = new AppScaffold(ctx);
            Assert.That(scaffold.Current, Is.EqualTo(Tab.InApp));

            scaffold.ShowTab(Tab.Links);
            Assert.That(ctx.SelectedTab, Is.EqualTo(Tab.Links));
            Assert.That(new AppScaffold(ctx).Current, Is.EqualTo(Tab.Links));
        }

        [Test]
        public void In_app_messages_start_running()
        {
            Assert.That(AppContext.Current.InAppRunning, Is.True);
        }

        [Test]
        public void Excluded_screens_match_ignoring_case()
        {
            var prefs = new InAppPrefs();
            Assert.That(prefs.Excludes("ExcludedScreen"), Is.True);
            Assert.That(prefs.Excludes("excludedscreen"), Is.True);
            Assert.That(prefs.Excludes("TestApp"), Is.False);

            prefs.Excluded.Clear();
            Assert.That(prefs.Excludes("ExcludedScreen"), Is.False);
        }
    }
}
