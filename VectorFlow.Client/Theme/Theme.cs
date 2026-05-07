using MudBlazor;

namespace VectorFlow.Client.Theme;

/// <summary>
/// Single source of truth for VectorFlow's MudBlazor theme.
/// Both light and dark palettes are defined here so the colours
/// stay consistent with the landing page CSS variables:
///
///   --vf-accent: #4339CA   (indigo)
///   --vf-canvas: #F8F7F4   (warm off-white)
///   --vf-ink:    #0D0D0F   (near-black)
///
/// Keeping the theme in a static class means MainLayout doesn't
/// need to rebuild a MudTheme object on every state change.
/// </summary>
public static class VectorFlowTheme
{
    public static readonly MudTheme Instance = new()
    {
        // ── Typography ────────────────────────────────────────────────────────
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["DM Sans", "system-ui", "sans-serif"],
                FontSize = "0.9375rem",
                FontWeight = "400",
                LineHeight = "1.6",
                LetterSpacing = "normal"
            },
            H1 = new H1Typography { FontFamily = ["DM Serif Display", "Georgia", "serif"], FontWeight = "400", FontSize = "3rem", LetterSpacing = "-0.02em" },
            H2 = new H2Typography { FontFamily = ["DM Serif Display", "Georgia", "serif"], FontWeight = "400", FontSize = "2.25rem", LetterSpacing = "-0.02em" },
            H3 = new H3Typography { FontFamily = ["DM Serif Display", "Georgia", "serif"], FontWeight = "400", FontSize = "1.75rem", LetterSpacing = "-0.01em" },
            H4 = new H4Typography { FontFamily = ["DM Sans", "system-ui", "sans-serif"], FontWeight = "600", FontSize = "1.25rem" },
            H5 = new H5Typography { FontFamily = ["DM Sans", "system-ui", "sans-serif"], FontWeight = "600", FontSize = "1.05rem" },
            H6 = new H6Typography { FontFamily = ["DM Sans", "system-ui", "sans-serif"], FontWeight = "600", FontSize = "0.9rem" },
            Subtitle1 = new Subtitle1Typography { FontWeight = "500" },
            Subtitle2 = new Subtitle2Typography { FontWeight = "500" },
            Button = new ButtonTypography { FontFamily = ["DM Sans", "system-ui", "sans-serif"], FontWeight = "600", TextTransform = "none", LetterSpacing = "normal" },
            Caption = new CaptionTypography { FontFamily = ["DM Mono", "monospace"], FontSize = "0.75rem" },
            Overline = new OverlineTypography { FontFamily = ["DM Mono", "monospace"], LetterSpacing = "0.08em" }
        },

        // ── Shape ─────────────────────────────────────────────────────────────
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            DrawerWidthLeft = "260px",
            AppbarHeight = "64px"
        },

        // ── Light palette ─────────────────────────────────────────────────────
        PaletteLight = new PaletteLight
        {
            // Brand
            Primary = "#4339CA",         // --vf-accent (indigo)
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken = "#3530A8",
            PrimaryLighten = "#EAE9FC",  // --vf-accent-light

            Secondary = "#1A7A52",       // green — used for success states
            SecondaryContrastText = "#FFFFFF",

            Tertiary = "#92560F",        // amber — warnings
            TertiaryContrastText = "#FFFFFF",

            // Semantic
            Info = "#2563EB",
            InfoContrastText = "#FFFFFF",
            Success = "#1A7A52",
            SuccessContrastText = "#FFFFFF",
            Warning = "#D97706",
            WarningContrastText = "#FFFFFF",
            Error = "#DC2626",
            ErrorContrastText = "#FFFFFF",

            // Surfaces
            Background = "#F8F7F4",       // --vf-canvas
            BackgroundGray = "#F2F0EB",   // --vf-canvas-warm
            Surface = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "rgba(13,13,15,0.87)",
            DrawerIcon = "rgba(13,13,15,0.6)",

            // Appbar
            AppbarBackground = "#FFFFFF",
            AppbarText = "#0D0D0F",

            // Text
            TextPrimary = "#0D0D0F",      // --vf-ink
            TextSecondary = "#3A3A42",    // --vf-ink-soft
            TextDisabled = "rgba(13,13,15,0.38)",

            // Lines & dividers
            Divider = "rgba(13,13,15,0.1)",
            DividerLight = "rgba(13,13,15,0.06)",
            TableLines = "rgba(13,13,15,0.08)",
            LinesDefault = "rgba(13,13,15,0.12)",
            LinesInputs = "rgba(13,13,15,0.24)",

            // Action states
            ActionDefault = "rgba(13,13,15,0.54)",
            ActionDisabled = "rgba(13,13,15,0.26)",
            ActionDisabledBackground = "rgba(13,13,15,0.12)",

            // Hover / focus / ripple
            HoverOpacity = 0.06,
            //FocusedOpacity = 0.12,
            RippleOpacity = 0.1,
            //SelectedOpacity = 0.08,

            OverlayDark = "rgba(13,13,15,0.5)",
            OverlayLight = "rgba(248,247,244,0.7)",

            White = "#FFFFFF",
            Black = "#0D0D0F",
            GrayDefault = "#7A7A88",
            GrayLight = "#F2F0EB",
            GrayDark = "#3A3A42",
            GrayDarker = "#0D0D0F",
            GrayLighter = "#F8F7F4"
        },

        // ── Dark palette ──────────────────────────────────────────────────────
        PaletteDark = new PaletteDark
        {
            // Brand — lightened for dark backgrounds
            Primary = "#7C75E0",          // --vf-accent-mid (lighter indigo)
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken = "#4339CA",
            PrimaryLighten = "#A09AEA",

            Secondary = "#34D399",        // lighter green
            SecondaryContrastText = "#0D0D0F",

            Tertiary = "#FCD34D",         // lighter amber
            TertiaryContrastText = "#0D0D0F",

            // Semantic
            Info = "#60A5FA",
            InfoContrastText = "#0D0D0F",
            Success = "#34D399",
            SuccessContrastText = "#0D0D0F",
            Warning = "#FBBF24",
            WarningContrastText = "#0D0D0F",
            Error = "#F87171",
            ErrorContrastText = "#0D0D0F",

            // Surfaces — layered dark greys
            Background = "#0F0F11",       // deepest layer
            BackgroundGray = "#18181C",   // mid layer
            Surface = "#1C1C21",          // card/paper surface
            DrawerBackground = "#18181C",
            DrawerText = "rgba(248,247,244,0.87)",
            DrawerIcon = "rgba(248,247,244,0.6)",

            // Appbar
            AppbarBackground = "#18181C",
            AppbarText = "#F8F7F4",

            // Text — warm whites matching landing page canvas colours
            TextPrimary = "#F8F7F4",      // --vf-canvas
            TextSecondary = "#A0A0AE",
            TextDisabled = "rgba(248,247,244,0.38)",

            // Lines
            Divider = "rgba(248,247,244,0.08)",
            DividerLight = "rgba(248,247,244,0.04)",
            TableLines = "rgba(248,247,244,0.06)",
            LinesDefault = "rgba(248,247,244,0.1)",
            LinesInputs = "rgba(248,247,244,0.24)",

            // Action states
            ActionDefault = "rgba(248,247,244,0.54)",
            ActionDisabled = "rgba(248,247,244,0.26)",
            ActionDisabledBackground = "rgba(248,247,244,0.12)",

            HoverOpacity = 0.08,
            //FocusedOpacity = 0.14,
            RippleOpacity = 0.12,
            //SelectedOpacity = 0.1,
            

            OverlayDark = "rgba(0,0,0,0.7)",
            OverlayLight = "rgba(15,15,17,0.7)",

            White = "#F8F7F4",
            Black = "#0F0F11",
            GrayDefault = "#A0A0AE",
            GrayLight = "#18181C",
            GrayDark = "#A0A0AE",
            GrayDarker = "#F8F7F4",
            GrayLighter = "#1C1C21"
        }
    };
}