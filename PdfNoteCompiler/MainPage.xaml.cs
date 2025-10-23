using Syncfusion.Maui.PdfViewer; // We need this for the "TextSelectionChangedEventArgs"
using System.IO; // Needed for Path.GetFileNameWithoutExtension
using Microsoft.Maui.ApplicationModel;

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
        //PdfViewer.IsVisible = false;
        ResetUIState();
    }

    // --- Helper Method to Reset UI State ---
    private void ResetUIState()
    {
        if (PdfViewer.DocumentSource != null) // Only unload if a document is actually loaded
        {
            PdfViewer.UnloadDocument(); // Unload any previously loaded document
        }
        PdfViewer.IsVisible = false; // Hide viewer
        WelcomeContent.IsVisible = true; // Show welcome message
        Title = "PDF Note Compiler"; // Reset title
        _currentPdfFileName = string.Empty; // Clear current file name
    }

    // Phase 2: Implement "Open PDF"
    private async void OnOpenPdfClicked(object sender, EventArgs e)
    {
        ResetUIState();
        LogLabel.Text = "[Action]: Open PDF clicked.\n" + LogLabel.Text;
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
                LogLabel.Text = $"[Info]: User selected: {pickResult.FileName}\n" + LogLabel.Text;
                // Store the file name (without extension) for note file creation
                _currentPdfFileName = Path.GetFileNameWithoutExtension(pickResult.FileName);
                Title = $"InScribe - {_currentPdfFileName}"; // Update page title

                var pdfStream = await pickResult.OpenReadAsync();
                LogLabel.Text = "[Info]: Loading PDF into viewer...\n" + LogLabel.Text;
                PdfViewer.LoadDocument(pdfStream);
                // The PdfViewer.IsVisible = true and WelcomeContent.IsVisible = false 
                // will be handled by OnPdfDocumentLoaded event
            }
            else
            {
                LogLabel.Text = "[Info]: User canceled PDF selection.\n" + LogLabel.Text;
                ResetUIState();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error Loading PDF", $"An error occurred: {ex.Message}", "OK");
            LogLabel.Text = $"[ERROR]: Failed to open PDF: {ex.Message}\n" + LogLabel.Text;
            // Ensure UI is reset if load fails
            ResetUIState();
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
        if (!string.IsNullOrEmpty(e.SelectedText) && !string.IsNullOrEmpty(_currentPdfFileName))
        {
            string capturedText = e.SelectedText; // Grab the text immediately

            LogLabel.Text = $"[Action]: Auto-adding highlight for '{_currentPdfFileName}'. Text: '{capturedText.Substring(0, Math.Min(capturedText.Length, 30))}...'\n" + LogLabel.Text;

            // *** Phase 4/5 Call will go here ***
            // await _noteService.AppendHighlightAsync(capturedText, _currentPdfFileName);

            // Optional: Clear selection visually after processing
            // Do this *after* the await call in Phase 4/5 if needed
            MainThread.BeginInvokeOnMainThread(() => {
                PdfViewer.EnableTextSelection = false;
                PdfViewer.EnableTextSelection = true;
            });

            // No need for e.Handled = true; unless we want to hide the default menu
            // For now, let the default menu show if it wants to, it doesn't interfere
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