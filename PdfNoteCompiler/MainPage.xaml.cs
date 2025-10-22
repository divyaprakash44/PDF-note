using Syncfusion.Maui.PdfViewer; // We need this for the "TextSelectionChangedEventArgs"
using System.IO; // Needed for Path.GetFileNameWithoutExtension

namespace PdfNoteCompiler;

public partial class MainPage : ContentPage
{
    private bool isLogPanelVisible = true;
    private string _currentPdfFileName = string.Empty; // Store the filename of the currently open PDF

    public MainPage()
    {
        InitializeComponent();

        MainLayoutGrid.ColumnDefinitions[1].Width = new GridLength(300);

        // Hide the PDF viewer until a document is loaded
        PdfViewer.IsVisible = false;
    }

    // Phase 2: Implement "Open PDF"
    private async void OnOpenPdfClicked(object sender, EventArgs e)
    {
        try
        {
            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "com.adobe.pdf" } },
                    { DevicePlatform.Android, new[] { "application/pdf" } },
                    { DevicePlatform.WinUI, new[] { ".pdf" } },
                    { DevicePlatform.macOS, new[] { "pdf" } },
                });

            var pickResult = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Pick a PDF document",
                FileTypes = customFileType,
            });

            if (pickResult != null)
            {
                // Store the file name (without extension) for note file creation
                _currentPdfFileName = Path.GetFileNameWithoutExtension(pickResult.FileName);
                Title = $"PDF Note Compiler - {_currentPdfFileName}"; // Update page title

                var pdfStream = await pickResult.OpenReadAsync();
                PdfViewer.LoadDocument(pdfStream);
                // The PdfViewer.IsVisible = true and WelcomeContent.IsVisible = false 
                // will be handled by OnPdfDocumentLoaded event
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error Loading PDF", $"An error occurred: {ex.Message}", "OK");
            LogLabel.Text = $"[ERROR]: Failed to open PDF: {ex.Message}\n" + LogLabel.Text;
            // Ensure UI is reset if load fails
            PdfViewer.IsVisible = false;
            WelcomeContent.IsVisible = true;
            Title = "PDF Note Compiler";
            _currentPdfFileName = string.Empty;
        }
    }

    // New Event: Triggered after the PDF viewer successfully loads a document
    private void OnPdfDocumentLoaded(object sender, EventArgs e)
    {
        WelcomeContent.IsVisible = false; // Hide welcome message
        PdfViewer.IsVisible = true;      // Show PDF viewer
        LogLabel.Text = $"[Loaded]: {_currentPdfFileName}.pdf successfully.\n" + LogLabel.Text;
    }


    // STUB for Phase 3: Highlight Trigger
    private void OnPdfTextSelected(object sender, TextSelectionChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.SelectedText))
        {
            LogLabel.Text = $"[Captured]: {e.SelectedText.Substring(0, Math.Min(e.SelectedText.Length, 20))}...\n" + LogLabel.Text;
        }
    }

    // STUB for Phase 5: Export Button
    private void OnExportClicked(object sender, EventArgs e)
    {
        LogLabel.Text = "OnExportClicked (Not Implemented)\n" + LogLabel.Text;
    }

    // STUB for Phase 5: Toggle Button
    private void OnToggleLogPanelClicked(object sender, EventArgs e)
    {
        isLogPanelVisible = !isLogPanelVisible;

        if (isLogPanelVisible)
        {
            MainLayoutGrid.ColumnDefinitions[1].Width = new GridLength(300);
            ToggleLogButton.Text = "<";
        }
        else
        {
            MainLayoutGrid.ColumnDefinitions[1].Width = new GridLength(0);
            ToggleLogButton.Text = ">";
        }
    }
}