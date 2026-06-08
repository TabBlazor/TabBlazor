using System.Xml.Serialization;

namespace TabBlazor
{
    /// <summary>A single slide within a <see cref="Carousel"/>.</summary>
    public partial class CarouselItem : IDisposable
    {

        [CascadingParameter] Carousel Carousel { get; set; }

        /// <summary>Image URL shown for the slide.</summary>
        [Parameter] public string ImageSrc { get; set; }
        /// <summary>Custom slide content, rendered instead of just the image.</summary>
        [Parameter] public RenderFragment ChildContent { get; set; }

        /// <summary>Optional custom indicator content for this slide.</summary>
        [Parameter] public RenderFragment IndicatorTemplate { get; set; }

        /// <summary>Optional caption content overlaid on the slide.</summary>
        [Parameter] public RenderFragment CaptionTemplate { get; set; }



        /// <summary>Arbitrary data associated with the slide.</summary>
        [Parameter] public object Data { get; set; }



        protected override void OnInitialized()
        {
            base.OnInitialized();
            Carousel?.AddCarouselItem(this);
        }

        private bool isActive => Carousel.activeItem == this;


        public void Dispose()
        {
            Carousel?.RemoveCarouselItem(this);
        }
    }
}