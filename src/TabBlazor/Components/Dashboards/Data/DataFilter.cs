namespace TabBlazor.Dashboards
{
    public class DataFilter<TItem> where TItem : class
    {
        private Func<TItem, bool> predicate;
        public string Name { get; set; }
        public Expression<Func<TItem, bool>> Expression { get; set; }

        public Func<TItem, bool> Predicate
        { 
            get
            {
                predicate ??= Expression.Compile();
                return predicate;
            }
        
        }

    }
}
