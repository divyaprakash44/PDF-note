using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Syncfusion.DocIORenderer;
using Syncfusion.DocIO.DLS;
using System.Threading;
using Microsoft.Maui.ApplicationModel.Communication;
using IWParagraph = Syncfusion.DocIO.DLS.IWParagraph;
// You might need aliases for other conflicting types too:
using WordDocument = Syncfusion.DocIO.DLS.WordDocument;
using FormatType = Syncfusion.DocIO.FormatType;
using IWSection = Syncfusion.DocIO.DLS.IWSection;
using Syncfusion.Pdf;

namespace PdfNoteCompiler.Services
{
    public class NoteService : INoteService
    {
        private readonly string _notesDirectory;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public NoteService()
        {
            _notesDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "InScribeNotes");
        }

        public async Task EnsureNoteDirectoryExistsAsync()
        {
            try
            {
                if (!Directory.Exists(_notesDirectory))
                {
                    Directory.CreateDirectory(_notesDirectory);
                }
            }
            catch (Exception ex)
            {
                MessagingCenter.Send(this, "Log", $"[ERROR]: Could not create notes directory: {ex.Message}");
            }
        }
        private string GetNoteFilePath(string pdfFileName)
        {
            // Sanitize filename to remove invalid path characters if necessary (basic example)
            string safeFileName = string.Join("_", pdfFileName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_notesDirectory, $"{safeFileName}_notes.docx");
        }

        public async Task AppendHighlightAsync(string text, string pdfFileName)
        {
            if (string.IsNullOrEmpty(pdfFileName) || string.IsNullOrWhiteSpace(text))
            {
                MessagingCenter.Send(this, "Log", "[Warning]: AppendHighlightAsync called with empty text or filename.");
                return; // Don't proceed if input is invalid
            }
            string filePath = GetNoteFilePath(pdfFileName);
            MessagingCenter.Send(this, "Log", $"[Service]: Requesting lock for '{pdfFileName}'...");
            string tempFilePath = filePath + ".tmp"; // For atomic save

            // Use semaphore to ensure only one write operation happens at a time
            MessagingCenter.Send(this, "Log", $"[Service]: Requesting lock for '{pdfFileName}'...");
            await _semaphore.WaitAsync();
            MessagingCenter.Send(this, "Log", $"[Service]: Lock acquired for '{pdfFileName}'. Saving...");

            try
            {
                // 1. Ensure the specific notes file exists (create if first highlight for this PDF)
                await InitializeNotesFileAsync(pdfFileName, filePath);

                // 2. LOAD the existing DOCX
                WordDocument document;
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)) // Allow reading while opening
                {
                    // Use ReadWrite FileShare if reading and writing happen very close together
                    // If errors persist, consider loading into a MemoryStream first
                    document = new WordDocument(fs, FormatType.Docx);
                }

                // 3. APPEND the new text
                Syncfusion.DocIO.DLS.IWSection section = document.LastSection ?? document.AddSection(); // Get last section or add one if empty
                IWParagraph paragraph = section.AddParagraph();
                paragraph.AppendText(text);
                // Optional styling:
                // paragraph.ApplyStyle(BuiltinStyle.ListParagraph); // Make it a bullet point
                paragraph.ParagraphFormat.AfterSpacing = 8; // Add some space after the paragraph

                // 4. ATOMIC SAVE: Save to temporary file first
                using (FileStream tempFs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    document.Save(tempFs, FormatType.Docx);
                }
                document.Close(); // Close the document object

                // 5. Replace original file with temporary file
                File.Delete(filePath);
                File.Move(tempFilePath, filePath);

                MessagingCenter.Send(this, "Log", $"[Service]: Save complete for '{pdfFileName}'. OK.");
            }
            catch (IOException ioEx) // Handle file-in-use or disk full
            {
                MessagingCenter.Send(this, "Log", $"[ERROR] IO Exception during save for '{pdfFileName}': {ioEx.Message}");
                // Clean up temp file if it exists
                if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
                // Consider re-throwing or notifying the user
            }
            catch (Exception ex) // Catch other potential errors (DocIO, etc.)
            {
                MessagingCenter.Send(this, "Log", $"[ERROR] General Exception during save for '{pdfFileName}': {ex.Message}");
                // Clean up temp file if it exists
                if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
                // Consider re-throwing or notifying the user
            }
            finally
            {
                // *** CRITICAL: Always release the semaphore ***
                _semaphore.Release();
                MessagingCenter.Send(this, "Log", $"[Service]: Lock released for '{pdfFileName}'.");
            }
        }

        private async Task InitializeNotesFileAsync(string pdfFileName, string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessagingCenter.Send(this, "Log", $"[Service]: Creating new notes file for '{pdfFileName}'.");
                try
                {
                    // Create a new blank document in memory
                    using (WordDocument document = new WordDocument())
                    {
                        document.EnsureMinimal(); // Adds default styles, etc.
                        IWSection section = document.AddSection();
                        IWParagraph para = section.AddParagraph(); // Add an empty paragraph to avoid issues
                        para.AppendText($"Notes for: {pdfFileName}.pdf"); // Add a title maybe
                        para.ApplyStyle(BuiltinStyle.Heading1);
                        section.AddParagraph(); // Add a blank paragraph after title

                        // Save it to disk
                        using (FileStream fs = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write))
                        {
                            document.Save(fs, FormatType.Docx); // Use SaveAsync
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessagingCenter.Send(this, "Log", $"[ERROR]: Failed to initialize notes file '{filePath}': {ex.Message}");
                    // Handle or re-throw as needed
                }
            }
        }

        public async Task<MemoryStream> PrepareNotesForExportAsync(string pdfFileName)
        {
            if (string.IsNullOrEmpty(pdfFileName))
            {
                MessagingCenter.Send(this, "Log", "[Warning]: PrepareNotesForExportAsync called with empty filename.");
                throw new ArgumentException(nameof(pdfFileName), "PDF file name cannot be null or empty.");
            }
            string noteFilePath = GetNoteFilePath(pdfFileName);
            string tempPdfPath = Path.Combine(Path.GetTempPath(), $"{pdfFileName}_notes_export.pdf");

            // Check if source notes file exists
            if(!File.Exists(noteFilePath))
            {
                MessagingCenter.Send(this, "Log", $"[Warning]: No notes file found for '{pdfFileName}' to export.");
                throw new FileNotFoundException("Notes file for this PDF does not exist yet. Add some highlights first.", noteFilePath);
            }

            // Optional: Check if the file is empty (has minimal content)
            // You could load it and check paragraph count if needed, but a simple length check might suffice.
            FileInfo fileInfo = new FileInfo(noteFilePath);
            if (fileInfo.Length < 100) // Arbitrary small size threshold
            {
                MessagingCenter.Send(this, "Log", $"[Warning]: Notes file for '{pdfFileName}' appears to be empty.");
                throw new InvalidOperationException("Notes file appears to be empty. Please add highlights before exporting.");
            }

            // Use semaphore to prevent conflict if user highlights while exporting (less likely but possible)
            await _semaphore.WaitAsync();
            MessagingCenter.Send(this, "Log", $"[Service]: Lock acquired for export of '{pdfFileName}'. Converting...");
            MemoryStream pdfMemoryStream = new MemoryStream();

            try
            {
                // 1. LOAD the existing DOCX
                WordDocument document;
                using (FileStream fs = new FileStream(noteFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // Use alias if needed: WordDocument document = new WordDocument(fs, FormatType.Docx);
                    document = new WordDocument(fs, FormatType.Docx);
                }

                // 2. CONVERT to PDF in memory
                using (DocIORenderer converter = new DocIORenderer())
                {
                    // PdfDocument is from Syncfusion.Pdf namespace
                    using (PdfDocument pdfDocument = converter.ConvertToPDF(document))
                    {
                        document.Close(); // Close the Word doc object

                        // 3. SAVE PDF to temporary location
                        pdfDocument.Save(pdfMemoryStream); // Save the PDF document to the stream
                        pdfMemoryStream.Position = 0; // Reset stream position before reading
                    }
                } // Converter is implicitly disposed by using statement

                MessagingCenter.Send(this, "Log", $"[Service]: Conversion complete. PDF saved to temp path: {Path.GetFileName(tempPdfPath)}");
                return pdfMemoryStream; // Return the path to the temporary PDF
            }
            catch (Exception ex)
            {
                MessagingCenter.Send(this, "Log", $"[ERROR] During PDF export for '{pdfFileName}': {ex.Message}");
                // Clean up temp file if conversion failed partway
                pdfMemoryStream.Dispose();
                throw; // Re-throw the exception so the UI knows it failed
            }
            finally
            {
                _semaphore.Release();
                MessagingCenter.Send(this, "Log", $"[Service]: Export lock released for '{pdfFileName}'.");
            }
        }

        /*public void CleanupExportFiles(string tempFilePath)
        {
            if (string.IsNullOrEmpty(tempFilePath)) return;

            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                    MessagingCenter.Send(this, "Log", $"[Service]: Cleaned up temporary export file: {Path.GetFileName(tempFilePath)}");
                }
            }
            catch (Exception ex)
            {
                //Log failure to delete the temp file, prevernt application crash
                MessagingCenter.Send(this, "Log", $"[ERROR]: Could not delete temporary export file '{Path.GetFileName(tempFilePath)}': {ex.Message}");
            }
        }

        private void CleanupExportFileInternal(string tempFilePath)
        {
            if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                    MessagingCenter.Send(this, "Log", $"[Service]: Cleaned up temporary export file after failure: {Path.GetFileName(tempFilePath)}");
                }
                catch (Exception ex)
                {
                    //Log failure to delete the temp file, prevent application crash
                    MessagingCenter.Send(this, "Log", $"[ERROR]: Could not delete temporary export file '{Path.GetFileName(tempFilePath)}' after failure: {ex.Message}");
                }
            }
        }*/
    }
}
