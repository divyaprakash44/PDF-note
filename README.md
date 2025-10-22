# Project: .NET MAUI PDF Note-Compiler

A cross-platform native application for Android, iOS, and Windows, built with .NET MAUI. This app allows users to open and read PDF documents. As the user highlights text, the app automatically captures those highlights and appends them to a single, persistent Microsoft Word (`.docx`) file. This "notes" file can be exported as a new, clean PDF at any time for quick revision.

---

## Core Features

* **Open Local PDFs:** Load any PDF document from the device's local storage.
* **Automatic Note Capture:** Simply highlighting text in the PDF automatically captures it. There is no need for a separate "copy" action.
* **Persistent Word Document:** All highlights are appended to a single `.docx` file (`notes.docx`) stored securely in the app's private data directory.
* **Cumulative Notes:** Highlights are added cumulatively. The notes file grows over time, every time you read and highlight, even across app restarts.
* **Stateful Persistence:** The notes file is the "source of truth." It is saved instantly on every highlight and is never lost unless the app is uninstalled or the notes are manually cleared.
* **Export to PDF:** Convert the entire collection of highlights (the `.docx` file) into a new, clean, and shareable PDF with a single button press.
* **True Cross-Platform:** A single C# codebase delivers a native app experience on Android, iOS, and Windows.

---

## 1. The Core Workflow (The "Action Protocol")

This is the central logic of the application. It is designed to be seamless for the user.



1.  **App Launch:** The app starts. It immediately checks for the existence of `notes.docx` in the `FileSystem.AppDataDirectory`.
2.  **File Initialization:**
    * **If `notes.docx` is NOT found:** The app uses the **Syncfusion.DocIO** library to create a new, blank `WordDocument` object in memory and saves it to the path as `notes.docx`.
    * **If `notes.docx` IS found:** The app does nothing and waits for user action.
3.  **User Opens PDF:** The user opens a PDF (e.g., "AI_Engineering.pdf"), which is loaded into the `SfPdfViewer` UI component.
4.  **User Highlights Text:** The user long-presses and drags to highlight a sentence.
5.  **The "Trigger":** The moment the user lifts their finger, the `SfPdfViewer` control fires its `TextSelectionChanged` event.
6.  **The "Action" (Code-Behind):**
    a. The C# event handler (`OnPdfTextSelected`) receives the selected text from the event arguments.
    b. The handler immediately calls a service (e.g., `NoteService.AppendHighlightAsync(string text)`).
    c. This service **loads** the `notes.docx` file from disk into a `WordDocument` object using **DocIO**.
    d. It **appends** the new text as a new paragraph (or bullet point) to the end of the `WordDocument` object.
    e. It **saves** the modified `WordDocument` object back to the *same* `notes.docx` file on disk.
7.  **Loop:** This process (Steps 4-6) repeats for every highlight. The `notes.docx` file is updated instantly and incrementally.
8.  **User Exports:** The user presses the "Export" button.
    a. The app loads `notes.docx` into a `WordDocument` object.
    b. It uses the **DocToPDFConverter** library to convert the `WordDocument` into a `PdfDocument` in memory.
    c. It saves this `PdfDocument` to a temporary file and uses the `.NET MAUI Essentials Share` API to open the native "Share" or "Save As" dialog for the user.

---

## 2. Tech Stack & Key Dependencies

This project relies on .NET MAUI and the Syncfusion suite of document processing libraries.

| Component | Technology | Purpose |
| :--- | :--- | :--- |
| **Framework** | **.NET MAUI** (C# + XAML) | The core cross-platform application framework. |
| **PDF Viewer (UI)** | **Syncfusion.Maui.PdfViewer** | The UI component that renders the PDF and provides the `TextSelectionChanged` event. |
| **Word Logic (Headless)**| **Syncfusion.DocIO** | A non-UI library for creating, loading, editing, and saving `.docx` files in code. |
| **PDF Conversion (Headless)**| **Syncfusion.DocToPDFConverter**| A non-UI library that converts a `DocIO` object into a `PdfDocument` object. |
| **File System** | **`Microsoft.Maui.Storage`** | Built-in .NET MAUI Essentials API for accessing the app's secure data directory. |
| **Share API** | **`Microsoft.Maui.ApplicationModel.Share`**| Built-in .NET MAUI Essentials API for triggering the native "Share" dialog. |

### Required NuGet Packages

```xml
<PackageReference Include="Microsoft.Maui.Controls" />
<PackageReference Include="Microsoft.Maui.Controls.Compatibility" />

<PackageReference Include="Syncfusion.Maui.PdfViewer" />
<PackageReference Include="Syncfusion.DocIO.Net.Maui" />
<PackageReference Include="Syncfusion.DocToPDFConverter.Net.Maui" />
