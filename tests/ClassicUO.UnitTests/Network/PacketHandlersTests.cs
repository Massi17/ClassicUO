using ClassicUO.Game;
using ClassicUO.Game.GameObjects; // brings in the Dictionary<uint, Mobile>.Add(Mobile) extension (EntityCollection.cs's DictExt)
using ClassicUO.Network;
using Xunit;

namespace ClassicUO.UnitTests.Network
{
    public class PacketHandlersTests
    {
        [Fact]
        public void ApplyMagicShield_SetsTheFieldOnTheMatchingEntity()
        {
            var world = new World();
            var mobile = ClassicUO.Game.GameObjects.Mobile.Create(world, 0x1024);
            world.Mobiles.Add(mobile);

            PacketHandlers.ApplyMagicShield(world, 0x1024, 50);

            Assert.Equal((ushort) 50, mobile.MagicShield);

            // Not world.Clear(): it walks Mobiles and calls Mobile.Destroy(), which reaches
            // GameActions.SendCloseStatus -> Client.Game.UO, and Client.Game is null outside a
            // running game instance (unrelated pre-existing gap, not something this task's
            // production code touches). Removing directly from the dictionary is enough teardown
            // for a locally-scoped World that nothing else references.
            world.Mobiles.Remove(mobile.Serial);
        }

        [Fact]
        public void ApplyMagicShield_UnknownSerial_DoesNothing()
        {
            var world = new World();

            // Valid mobile-range serial (< 0x40000000, per SerialHelper.IsMobile), just never registered - must not throw.
            PacketHandlers.ApplyMagicShield(world, 0x1025, 50);

            world.Clear();
        }

        [Fact]
        public void ApplyMagicShield_ZeroClearsAnExistingShield()
        {
            var world = new World();
            var mobile = ClassicUO.Game.GameObjects.Mobile.Create(world, 0x1024);
            mobile.MagicShield = 50;
            world.Mobiles.Add(mobile);

            PacketHandlers.ApplyMagicShield(world, 0x1024, 0);

            Assert.Equal((ushort) 0, mobile.MagicShield);

            // See the comment in ApplyMagicShield_SetsTheFieldOnTheMatchingEntity above.
            world.Mobiles.Remove(mobile.Serial);
        }
    }
}
