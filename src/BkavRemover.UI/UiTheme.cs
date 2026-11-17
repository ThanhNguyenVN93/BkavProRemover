using System.Drawing;

namespace BkavRemover.UI
{
    internal static class UiTheme
    {
        public static readonly Color Sidebar = ColorTranslator.FromHtml("#1F2A44");
        public static readonly Color SidebarActive = ColorTranslator.FromHtml("#2F8F77");
        public static readonly Color Background = ColorTranslator.FromHtml("#F4F6F8");
        public static readonly Color CardBorder = ColorTranslator.FromHtml("#E3E7ED");
        public static readonly Color TextPrimary = ColorTranslator.FromHtml("#1F2A44");
        public static readonly Color TextSecondary = ColorTranslator.FromHtml("#7C8AA5");
        public static readonly Color Accent = ColorTranslator.FromHtml("#2F8F77");
        public static readonly Color Warning = ColorTranslator.FromHtml("#E2A33D");
        public static readonly Color WarningBg = ColorTranslator.FromHtml("#FDF3E7");
        public static readonly Color Danger = ColorTranslator.FromHtml("#D1453B");
        public static readonly Color DangerBg = ColorTranslator.FromHtml("#FDEDEE");
        public static readonly Color LogBackground = ColorTranslator.FromHtml("#121826");
        public static readonly Color Success = ColorTranslator.FromHtml("#27AE60");
        public static readonly Color SuccessBg = ColorTranslator.FromHtml("#E8F8F0");
        public static readonly Color CardAccent1 = ColorTranslator.FromHtml("#3498DB");
        public static readonly Color CardAccent2 = ColorTranslator.FromHtml("#E67E22");
        public static readonly Color CardAccent3 = ColorTranslator.FromHtml("#9B59B6");
        public static readonly Color CardAccent4 = ColorTranslator.FromHtml("#1ABC9C");

        public static readonly Font FontRegular = new Font("Segoe UI", 9f);
        public static readonly Font FontSmall = new Font("Segoe UI", 8f);
        public static readonly Font FontBold = new Font("Segoe UI", 9f, FontStyle.Bold);
        public static readonly Font FontTitle = new Font("Segoe UI", 13f, FontStyle.Bold);
        public static readonly Font FontMono = new Font("Consolas", 9f);
    }
}
