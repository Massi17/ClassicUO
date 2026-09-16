using ClassicUO.Game.UI.Gumps;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI.Gumps
{
    public class BaseHealthBarGumpTests
    {
        [Fact]
        public void CalculateShieldSegments_OriginalAskExample_TwoThirdsGreenOneThirdPurple()
        {
            var (green, purple, empty) = BaseHealthBarGump.CalculateShieldSegments(100, 100, 50);

            Assert.Equal(2f / 3f, green, 3);
            Assert.Equal(1f / 3f, purple, 3);
            Assert.Equal(0f, empty, 3);
        }

        [Fact]
        public void CalculateShieldSegments_NoShield_MatchesPlainHitsOverHitsMax()
        {
            var (green, purple, empty) = BaseHealthBarGump.CalculateShieldSegments(60, 100, 0);

            Assert.Equal(0.6f, green, 3);
            Assert.Equal(0f, purple, 3);
            Assert.Equal(0.4f, empty, 3);
        }

        [Fact]
        public void CalculateShieldSegments_ShieldExceedsHitsMax_PurpleDominatesGreenStillCorrect()
        {
            var (green, purple, empty) = BaseHealthBarGump.CalculateShieldSegments(50, 50, 200);

            // total = 250; green = 50/250 = 0.2; purple = 200/250 = 0.8
            Assert.Equal(0.2f, green, 3);
            Assert.Equal(0.8f, purple, 3);
            Assert.Equal(0f, empty, 3);
        }

        [Fact]
        public void CalculateShieldSegments_ZeroHits_GreenIsZero()
        {
            var (green, purple, empty) = BaseHealthBarGump.CalculateShieldSegments(0, 100, 50);

            // total = 150; green = 0; purple = 50/150 = 1/3; empty = 2/3
            Assert.Equal(0f, green, 3);
            Assert.Equal(1f / 3f, purple, 3);
            Assert.Equal(2f / 3f, empty, 3);
        }

        [Fact]
        public void CalculateShieldSegments_ZeroHitsMaxAndZeroShield_ReturnsAllZero()
        {
            var (green, purple, empty) = BaseHealthBarGump.CalculateShieldSegments(0, 0, 0);

            Assert.Equal(0f, green);
            Assert.Equal(0f, purple);
            Assert.Equal(0f, empty);
        }
    }
}
