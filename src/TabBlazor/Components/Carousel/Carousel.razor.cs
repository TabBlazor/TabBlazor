using System.Timers;

namespace TabBlazor
{
    public partial class Carousel : IDisposable
    {
        private List<CarouselItem> carouselItems { get; set; } = new();
        private System.Timers.Timer slideTimer = new System.Timers.Timer();
        internal CarouselItem activeItem;

        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public int SlideInterval { get; set; } = 5000;
        [Parameter] public bool AutoSlide { get; set; } = true;
        [Parameter] public EventCallback<CarouselItem> OnItemActive { get; set; }

        protected override void OnInitialized()
        {
            slideTimer.Elapsed += slideTimerElapsed;
            slideTimer.Start();    
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            slideTimer.Enabled = AutoSlide;
            slideTimer.Interval = SlideInterval;
        }

        private void slideTimerElapsed(object sender, ElapsedEventArgs e)
        {
            MoveNext();
        }

        internal void AddCarouselItem(CarouselItem item)
        {
            carouselItems.Add(item);

            if(activeItem == null)
            {
                SetActiveItem(item);
            }

            StateHasChanged();

        }

        private void SetActiveItem(CarouselItem item)
        {
            activeItem = item;
           
            slideTimer.Stop();
            slideTimer.Start();
            InvokeAsync(StateHasChanged);

            OnItemActive.InvokeAsync(activeItem);
        }

        private void MoveNext()
        {
            if (carouselItems.Count == 0) { return; }
            if (activeItem == null) { SetActiveItem(carouselItems.First()); }

            var index = carouselItems.IndexOf(activeItem);
            if (index < 0 || (index >= carouselItems.Count -1))
            {
                SetActiveItem(carouselItems.First());
            }
            else {
                SetActiveItem(carouselItems[index + 1]);
            }
        }


        private void MovePrevious()
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