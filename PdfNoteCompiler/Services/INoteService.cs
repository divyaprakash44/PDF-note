using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfNoteCompiler.Services
{
    public interface INoteService
    {
        Task EnsureNoteDirectoryExistsAsync();
        Task AppendHighlightAsync(string text, string pdfFileName);
        Task<MemoryStream> PrepareNotesForExportAsync(string pdfFileName);
        // void CleanupExportFiles(string tempFilePath);
    }
}
