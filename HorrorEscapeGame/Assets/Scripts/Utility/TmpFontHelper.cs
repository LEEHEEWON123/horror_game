using TMPro;
using UnityEngine;

public static class TmpFontHelper
{
    private static TMP_FontAsset _cachedFont;

    public static void ApplyDefaultFont(TMP_Text text)
    {
        if (text == null) return;

        var font = GetDefaultFont();
        if (font != null)
            text.font = font;
    }

    private static TMP_FontAsset GetDefaultFont()
    {
        if (_cachedFont != null) return _cachedFont;

        _cachedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (_cachedFont != null) return _cachedFont;

        if (Resources.Load<TMP_Settings>("TMP Settings") != null)
        {
            TMP_Settings.LoadDefaultSettings();
            _cachedFont = TMP_Settings.defaultFontAsset;
        }

        return _cachedFont;
    }
}
