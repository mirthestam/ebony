namespace Ebony.Infrastructure;

public static class AdwStyles
{
    /// <remarks>
    /// https://gnome.pages.gitlab.gnome.org/libadwaita/doc/1.3/style-classes.html#colors
    /// </remarks>
    public static class Colors
    {
        public static string Warning = "warning";
        public static string Success = "success";
        public static string Error = "error";
        public static string Accent = "accent";
    }
    
    /// <summary>
    /// The .dimmed style class make the widget it’s applied to partially transparent.
    /// </summary>
    /// <remarks>
    /// https://gnome.pages.gitlab.gnome.org/libadwaita/doc/1.3/style-classes.html#dimmed
    /// </remarks>
    public static string Dimmed = "dimmed";
}