namespace TabBlazor
{
    internal sealed class TabsUrlParameterRegistry
    {
        private readonly Dictionary<string, Tabs> owners = new(StringComparer.OrdinalIgnoreCase);

        public void Register(string urlParameter, Tabs tabs)
        {
            if (owners.TryGetValue(urlParameter, out var owner) && owner != tabs)
            {
                throw new InvalidOperationException(
                    $"Another Tabs component already uses UrlParameter '{urlParameter}'. Give each Tabs on a page its own UrlParameter.");
            }

            owners[urlParameter] = tabs;
        }

        public void Unregister(string urlParameter, Tabs tabs)
        {
            if (urlParameter != null && owners.TryGetValue(urlParameter, out var owner) && owner == tabs)
            {
                owners.Remove(urlParameter);
            }
        }
    }
}
