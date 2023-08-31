namespace TabBlazor.Components.QuickTables.Infrastructure;

internal class EventCallbackSubscriber<T> : IDisposable
{
    private readonly EventCallback<T> _handler;
    private EventCallbackSubscribable<T> _existingSubscription;

    public EventCallbackSubscriber(EventCallback<T> handler)
    {
        _handler = handler;
    }

    public void Dispose()
    {
        _existingSubscription?.Unsubscribe(this);
    }

    public void SubscribeOrMove(EventCallbackSubscribable<T> subscribable)
    {
        if (subscribable != _existingSubscription)
        {
            _existingSubscription?.Unsubscribe(this);
            subscribable?.Subscribe(this, _handler);
            _existingSubscription = subscribable;
        }
    }
}