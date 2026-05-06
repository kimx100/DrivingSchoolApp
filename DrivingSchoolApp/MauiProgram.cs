using CommunityToolkit.Maui;
using DrivingSchoolApp.Services.API;
using DrivingSchoolApp.Services.API.Implementation;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace DrivingSchoolApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Configuration["api_base_url"] = "http://130.225.170.67:5259";
        
        builder.Services
            .AddScoped<IAdminService, AdminService>()
            .AddScoped<IAuthService, AuthService>()
            .AddScoped<IDrivingSchoolService, DrivingSchoolService>()
            .AddScoped<IInstructorService, InstructorService>()
            .AddScoped<IStudentInviteService, StudentInviteService>()
            .AddScoped<IStudentService, StudentService>();
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
