using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor.Components.Navigation
{
    public interface INavigationParent
    {
        public void SetExpanded(bool expanded);
        public void SetActive(bool active);

        public void AddChildItem(NavigationItem item);
        public void RemoveChildItem(NavigationItem item);
    }
}
