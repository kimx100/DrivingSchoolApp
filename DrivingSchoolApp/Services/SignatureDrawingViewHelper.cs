using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace DrivingSchoolApp.Services;

public static class SignatureDrawingViewHelper
{
    public static void EnsureDrawable(DrawingView signaturePad)
    {
        signaturePad.InputTransparent = false;
        signaturePad.IsEnabled = true;
        signaturePad.Lines ??= new ObservableCollection<IDrawingLine>();
    }

    public static void Clear(DrawingView signaturePad)
    {
        EnsureDrawable(signaturePad);
        signaturePad.Lines?.Clear();
        signaturePad.InvalidateMeasure();
    }
}
