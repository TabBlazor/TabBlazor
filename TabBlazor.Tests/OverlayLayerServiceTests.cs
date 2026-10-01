using TabBlazor.Services;

namespace TabBlazor.Tests
{
    public class OverlayLayerServiceTests
    {
        private readonly OverlayLayerService layers = new();

        [Fact]
        public void Registers_first_overlay_at_base_z_index()
        {
            Assert.Equal(1200, layers.Register(new object()));
        }

        [Fact]
        public void Stacks_each_overlay_above_the_previous()
        {
            layers.Register(new object());

            Assert.Equal(1210, layers.Register(new object()));
        }

        [Fact]
        public void Returns_existing_z_index_when_owner_registered_twice()
        {
            var owner = new object();
            layers.Register(owner);
            layers.Register(new object());

            Assert.Equal(1200, layers.Register(owner));
        }

        [Fact]
        public void Places_new_overlay_above_remaining_ones_when_lower_overlay_closed_first()
        {
            var lower = new object();
            layers.Register(lower);
            layers.Register(new object());

            layers.Unregister(lower);

            Assert.Equal(1220, layers.Register(new object()));
        }

        [Fact]
        public void Restarts_at_base_z_index_when_all_overlays_closed()
        {
            var owner = new object();
            layers.Register(owner);
            layers.Unregister(owner);

            Assert.Equal(1200, layers.Register(new object()));
        }

        [Fact]
        public void Is_top_most_only_for_highest_z_index()
        {
            var lowerZIndex = layers.Register(new object());
            var upperZIndex = layers.Register(new object());

            Assert.False(layers.IsTopMost(lowerZIndex));
            Assert.True(layers.IsTopMost(upperZIndex));
        }

        [Fact]
        public void Is_top_most_when_no_overlays_registered()
        {
            Assert.True(layers.IsTopMost(1200));
        }

        [Fact]
        public void Raises_changed_when_overlay_registered_or_unregistered()
        {
            var changes = 0;
            layers.OnChanged += () => changes++;
            var owner = new object();

            layers.Register(owner);
            layers.Register(owner);
            layers.Unregister(owner);
            layers.Unregister(owner);

            Assert.Equal(2, changes);
        }
    }
}
