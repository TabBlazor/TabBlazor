using System.Timers;


namespace TabBlazor
{
    public partial class Carousel : IDisposable
    {
        private List<CarouselItem> carouselItems { get; set; } = new();
        private System.Timers.Timer slideTimer = new System.Timers.Timer();
        internal CarouselItem activeItem;

        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public bool Controls { get; set; }
        [Parameter] public CarouselIndicator Indicators { get; set; }
        [Parameter] public CarouselIndicatorDirection IndicatorsDirection { get; set; }



        [Parameter] public int SlideInterval { get; set; } = 5000;
        [Parameter] public bool AutoSlide { get; set; } = true;
        [Parameter] public EventCallback<CarouselItem> OnItemActive { get; set; }

        public CarouselItem ActiveItem => activeItem;

        public List<CarouselItem> CarouselItems => carouselItems;

        protected override void OnInitialized()
        {
            slideTimer.Elapsed += SlideTimerElapsed;
            slideTimer.Start();
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            slideTimer.Enabled = AutoSlide;
            slideTimer.Interval = SlideInterval;
        }

        private void SlideTimerElapsed(object sender, ElapsedEventArgs e)
        {
            MoveNext();
        }

        private string GetDataBsTarget()
        {

            var suffix = "";

            if (IndicatorsDirection == CarouselIndicatorDirection.Vertical)
            {
                suffix = "-vertical";
            }

            //#carousel-indicators-dot
            switch (Indicators)
            {
                case CarouselIndicator.Dots:
                    return "#carousel-indicators-dot" + suffix;

                case CarouselIndicator.Thumbnail:
                    return "carousel-indicators-thumb" + suffix;

            }

            return "#carousel-indicators" + suffix;
        }
        private string GetIndicatorCss()
        {

            var css = "carousel-indicators ";


            if (IndicatorsDirection == CarouselIndicatorDirection.Vertical)
            {
                css += "carousel-indicators-vertical ";
            }


            switch (Indicators)
            {
                case CarouselIndicator.Dots:
                    css += "carousel-indicators-dot ";
                    break;
                case CarouselIndicator.Thumbnail:
                    css += "carousel-indicators-thumb ";
                    break;
            }

            return css;
        }

        internal void AddCarouselItem(CarouselItem item)
        {
            carouselItems.Add(item);

            if (activeItem == null)
            {
                SetActiveItem(item);
            }

            StateHasChanged();

        }

        public void SetActiveItem(int index)
        {
            var item = carouselItems.ElementAtOrDefault(index);
            if (item != null)
            {
                SetActiveItem(item);
            }
        }

        public void SetActiveItem(CarouselItem item)
        {
            activeItem = item;
            slideTimer.Stop();

            if (AutoSlide)
            {
                slideTimer.Start();
            }

            InvokeAsync(() =>
            {
                OnItemActive.InvokeAsync(activeItem);
                StateHasChanged();
            });
        }

        public void MoveNext()
        {
            if (carouselItems.Count == 0) { return; }
            if (activeItem == null) { SetActiveItem(carouselItems.First()); }

            var index = carouselItems.IndexOf(activeItem);
            if (index < 0 || (index >= carouselItems.Count - 1))
            {
                SetActiveItem(carouselItems.First());
            }
            else
            {
                SetActiveItem(carouselItems[index + 1]);
            }
        }


        public void MovePrevious()
        {
            if (carouselItems.Count == 0) { return; }
            if (activeItem == null) { SetActiveItem(carouselItems.First()); }

            var index = carouselItems.IndexOf(activeItem);
            if (index <= 0 || (index >= carouselItems.Count))
            {
                SetActiveItem(carouselItems.Last());
            }
            else
            {
                SetActiveItem(carouselItems[index - 1]);
            }
        }

        internal void RemoveCarouselItem(CarouselItem item)
        {
            if (carouselItems.Contains(item))
            {
                carouselItems.Remove(item);
                StateHasChanged();
            }

        }

        public void Dispose()
        {
            slideTimer?.Dispose();
            slideTimer = null;
        }
    }
}