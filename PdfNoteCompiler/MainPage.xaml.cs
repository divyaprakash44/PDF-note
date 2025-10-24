using Syncfusion.Maui.PdfViewer; // We need this for the "TextSelectionChangedEventArgs"
using System.IO; // Needed for Path.GetFileNameWithoutExtension
using Microsoft.Maui.ApplicationModel;
using PdfNoteCompiler.Services;
using Microsoft.Maui.ApplicationModel.Communication;
using System.Threading.Tasks;
//using AndroidX.Fragment.App.StrictMode;

namespace PdfNoteCompiler;

public partial class MainPage : ContentPage
{
    private bool isLogPanelVisible = true;
    private string _currentPdfFileName = string.Empty; // Store the filename of the currently open PDF

    private readonly INoteService _noteService;

    public MainPage(INoteService noteService)
    {
        InitializeComponent();

        _noteService = noteService ?? throw new ArgumentNullException(nameof(noteService));

        MainLayoutGrid.ColumnDefinitions[1].Width = new GridLength(300);
        ResetUIState();

        MessagingCenter.Subscribe<NoteService, string>(this, "Log", (sender, logText) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LogLabel.Text = logText + "\n" + LogLabel.Text;
            });
        });
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
    private async void OnPdfTextSelected(object sender, TextSelectionChangedEventArgs e)
    {
        if (_noteService == null)
        {
            LogLabel.Text = "[ERROR]: NoteService is not initialized.\n" + LogLabel.Text;
            return;
        }

        if (!string.IsNullOrEmpty(e.SelectedText) && !string.IsNullOrEmpty(_currentPdfFileName))
        {
            string capturedText = e.SelectedText; // Grab the text immediately

            LogLabel.Text = $"[Action]: Auto-adding highlight for '{_currentPdfFileName}'. Text: '{capturedText.Substring(0, Math.Min(capturedText.Length, 30))}...'\n" + LogLabel.Text;

            await _noteService.AppendHighlightAsync(capturedText, _currentPdfFileName); //Call to the actual service
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
    private async void OnExportClicked(object sender, EventArgs e)
    {
        LogLabel.Text = "[Action]: Export Notes clicked.\n" + LogLabel.Text;

        // Check if PDF is loaded
        if (string.IsNullOrEmpty(_currentPdfFileName))
        {
            await DisplayAlert("Export Error", "Please open a PDF document first.", "OK");
            LogLabel.Text = "[Warning]: Export aborted. No PDF loaded.\n" + LogLabel.Text;
            return;
        }

        string tempPdfPath = null;
        try
        {
            ExportSpinner.IsVisible = true;
            ExportSpinner.IsRunning = true;

            tempPdfPath = await _noteService.PrepareNotesForExportAsync(_currentPdfFileName);

            ExportSpinner.IsVisible = false;
            ExportSpinner.IsRunning = false;

            LogLabel.Text = $"[UI Action]: Showing share dialog for {Path.GetFileName(tempPdfPath)}.\n" + LogLabel.Text;
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Notes for {_currentPdfFileName}",
                File = new ShareFile(tempPdfPath)
            });

            LogLabel.Text = "[UI Action]: Share dialog closed.\n" + LogLabel.Text;
        }
        catch (FileNotFoundException fnfEx)
        {
            LogLabel.Text = $"[ERROR] Export failed: {fnfEx.Message}\n" + LogLabel.Text;
            await DisplayAlert("Export Error", "Could not export notes. No notes file found for this PDF. Please add some highlights first.", "OK");
        }
        catch (InvalidOperationException opEx) // Catch specific error from service (empty file)
        {
            LogLabel.Text = $"[ERROR] Export failed: {opEx.Message}\n" + LogLabel.Text;
            await DisplayAlert("Export Error", "Could not export notes. The notes file appears to be empty. Please add some highlights first.", "OK");
        }
        catch (Exception ex) // Catch general errors (conversion, sharing, etc.)
        {
            LogLabel.Text = $"[ERROR] During export process: {ex.Message}\n" + LogLabel.Text;
            await DisplayAlert("Export Error", $"An unexpected error occurred during export:\n\n{ex.Message}", "OK");
        }
        finally
        {
            // Ensure spinner is hidden
            ExportSpinner.IsRunning = false;
            ExportSpinner.IsVisible = false;

            // Cleanup temporary file (pass the file we got)
            if (tempPdfPath != null)
            {
                _noteService.CleanupExportFiles(tempPdfPath);
            }
        }
    }

    // STUB for Phase 5: Toggle Button
    private void OnToggleLogPanelClicked(object sender, EventArgs e)
    {
        isLogPanelVisible = !isLogPanelVisible;

        // Use GridLength animation or direct set for smooth transition (optional)
        // For simplicity, direct set:
        if (isLogPanelVisible)
        {
            MainLayoutGrid.ColumnDefinitions[1].Width = new GridLength(300);
            ToggleLogButton.Text = "<";
            LogLabel.Text = "[UI]: Log panel shown.\n" + LogLabel.Text;
        }
        else
        {
            MainLayoutGrid.ColumnDefinitions[1].Width = new GridLength(0);
            ToggleLogButton.Text = ">";
        }
    }

    protected override void OnDisappearing()
    {
        MessagingCenter.Unsubscribe<NoteService, string>(this, "Log");
        base.OnDisappearing();
    }
}