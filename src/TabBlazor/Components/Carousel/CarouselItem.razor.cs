using System.Xml.Serialization;

namespace TabBlazor
{
    public partial class CarouselItem : IDisposable
    {

        [CascadingParameter] Carousel Carousel { get; set; }

        [Parameter] public string ImageSrc { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public RenderFragment IndicatorTemplate { get; set; }

        [Parameter] public RenderFragment CaptionTemplate { get; set; }



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