
namespace Tabler.Icons
{
    public static class Extensions
    {

        public static string GetIconName(this TablerIconType iconType)
        {
            var iconName = iconType.ToString();
            if (iconName.StartsWith("_"))
            {
                iconName = iconName.Substring(1);
            }
            iconName = iconName.Replace("_", "-");

            return iconName.ToLower();
        }

    }
}
