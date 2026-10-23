using NUnit.Framework;
using UnityEngine;

namespace GrovsTestApp.Tests
{
    public class QrTests
    {
        [Test]
        public void Encodes_a_link_into_a_square_odd_grid()
        {
            var modules = QrSheet.Encode("https://x.sqd.link/abc");
            var size = modules.GetLength(0);
            Assert.That(modules.GetLength(1), Is.EqualTo(size));
            Assert.That(size % 2, Is.EqualTo(1));
            Assert.That(size, Is.GreaterThanOrEqualTo(21));
        }

        [Test]
        public void Texture_has_a_one_module_white_border_and_point_filtering()
        {
            var modules = QrSheet.Encode("https://x.sqd.link/abc");
            var texture = QrSheet.Render(modules);
            try
            {
                var size = modules.GetLength(0);
                Assert.That(texture.width, Is.EqualTo(size + 2));
                Assert.That(texture.height, Is.EqualTo(size + 2));
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(texture.GetPixel(0, 0), Is.EqualTo(Color.white));
                // The top-left finder pattern starts right inside the border (texture rows run bottom up).
                Assert.That(texture.GetPixel(1, size), Is.EqualTo(Color.black));
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
