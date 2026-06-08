using System.Timers;


namespace TabBlazor
{
    /// <summary>
    /// A slideshow of <see cref="CarouselItem"/> panels with optional controls, indicators and auto-advance.
    /// </summary>
    public partial class Carousel : IDisposable
    {
        private List<CarouselItem> carouselItems { get; set; } = new();
        private System.Timers.Timer slideTimer = new System.Timers.Timer();
        internal CarouselItem activeItem;

        /// <summary>The carousel items.</summary>
        [Parameter] public RenderFragment ChildContent { get; set; }

        /// <summary>When true, shows previous/next controls. Defaults to false.</summary>
        [Parameter] public bool Controls { get; set; }
        /// <summary>The indicator style (none, dots, thumbnails).</summary>
        [Parameter] public CarouselIndicator Indicators { get; set; }
        /// <summary>The orientation of the indicators.</summary>
        [Parameter] public CarouselIndicatorDirection IndicatorsDirection { get; set; }



        /// <summary>Time between auto-advance slides, in milliseconds. Defaults to 5000.</summary>
        [Parameter] public int SlideInterval { get; set; } = 5000;
        /// <summary>When true, slides advance automatically. Defaults to true.</summary>
        [Parameter] public bool AutoSlide { get; set; } = true;
        /// <summary>Raised when the active item changes.</summary>
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

        /// <summary>Activates the item at the given index.</summary>
        public void SetActiveItem(int index)
        {
            var item = carouselItems.ElementAtOrDefault(index);
            if (item != null)
            {
                SetActiveItem(item);
            }
        }

        /// <summary>Activates the given item.</summary>
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

        /// <summary>Advances to the next item, wrapping to the first.</summary>
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


        /// <summary>Moves to the previous item, wrapping to the last.</summary>
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