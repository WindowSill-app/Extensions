using System.Reflection;
using Microsoft.UI.Input;

namespace WindowSill.TextFinder.Core;

internal class GeneralHelper
{
    /// <summary>
    /// Changes the cursor appearance for a specified UI element.
    /// </summary>
    /// <param name="uiElement">The visual component whose cursor appearance will be modified.</param>
    /// <param name="cursor">The new cursor style to be applied to the specified UI element.</param>
    public static void ChangeCursor(UIElement uiElement, InputCursor cursor)
    {
        Type type = typeof(UIElement);
        type.InvokeMember(
            "ProtectedCursor",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance,
            null,
            uiElement,
            [cursor]);
    }
}
