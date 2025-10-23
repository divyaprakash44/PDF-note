using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using System.Threading;
using Microsoft.Maui.ApplicationModel.Communication;
using IWParagraph = Syncfusion.DocIO.DLS.IWParagraph;
// You might need aliases for other conflicting types too:
using WordDocument = Syncfusion.DocIO.DLS.WordDocument;
using FormatType = Syncfusion.DocIO.FormatType;
using IWSection = Syncfusion.DocIO.DLS.IWSection;

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

        public Task<string> PrepareNotesForExportAsync(string pdfFileName)
        {
            MessagingCenter.Send(this, "Log", "[Service]: PrepareNotesForExportAsync (Not Implemented).");
            // Phase 5: Load correct docx, convert to PDF, save to temp location, return temp path
            throw new NotImplementedException();
        }

        public void CleanupExportFiles(string tempFilePath)
        {
            MessagingCenter.Send(this, "Log", "[Service]: CleanupExportFiles (Not Implemented).");
            // Phase 5: Delete the temporary PDF file
            throw new NotImplementedException();
        }
    }
}
