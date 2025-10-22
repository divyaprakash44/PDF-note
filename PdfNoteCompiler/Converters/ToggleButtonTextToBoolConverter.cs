using System.Globalization;

namespace PdfNoteCompiler.Converters
{
    public class ToggleButtonTextToBoolConverter : IValueConverter
    {
        // This converts the button text ("<" or ">") to a boolean (true/false) for IsVisible
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If the button text is "<", the panel should be visible (true)
            return value is string buttonText && buttonText == "<";
        }

        // We don't need to convert back, so this can remain empty
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}