using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core.Hosting;
using PdfNoteCompiler.Services;

namespace PdfNoteCompiler
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWH9ccXVWRGVZWUF0W0tWYEg=");

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureSyncfusionCore();

            builder.Services.AddSingleton<INoteService, NoteService>();
            builder.Services.AddSingleton<MainPage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            var noteService = app.Services.GetRequiredService<INoteService>();
            _ = Task.Run(async () => await noteService.EnsureNoteDirectoryExistsAsync());

            return app;
        }
    }
}
