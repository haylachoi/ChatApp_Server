using FirebaseAdmin;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Hellang.Middleware.ProblemDetails;
using Microsoft.IdentityModel.Tokens;
using ChatApp_Server.Helper;
using Microsoft.EntityFrameworkCore;
using ChatApp_Server.Settings;
using ChatApp_Server.Models;
using SignalRChat.Hubs;
using ChatApp_Server.Configs;
using ChatApp_Server.Hubs;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", @"./firestore_credential.json");
Constants.Initialize(builder.Configuration);

builder.Services.AddOptions<AppSettings>().BindConfiguration("AppSettings").ValidateDataAnnotations().ValidateOnStart();
// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddDbContext<ChatAppContext>(opt =>
{
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    opt.UseNpgsql(builder.Configuration.GetConnectionString("ChatApp"));
});
builder.Services.AddProblemDetails(opt =>
{
    opt.ExceptionDetailsPropertyName = "Problem Detail";
    opt.IncludeExceptionDetails = (ctx, ex) => builder.Environment.IsDevelopment() || builder.Environment.IsStaging();
});

var firebaseProjectName = "chapapp-9d9a7";
builder.Services.AddSingleton(FirebaseApp.Create());
builder.Services.AddSingleton(FirestoreDb.Create(firebaseProjectName));
builder.Services.AddSingleton(StorageClient.Create());
builder.Services.AddSingleton(service => service.GetRequiredService<IOptions<AppSettings>>().Value);
builder.Services.AddSingleton<ConnectionMapping<string>>();

//builder.Services.AddSingleton(new FirebaseAuthClient(new FirebaseAuthConfig
//{
//    ApiKey = "AIzaSyDc8JqhrToDLv-8H8aQFdaZo2PqRiuwyug",
//    AuthDomain = $"{firebaseProjectName}.firebaseapp.com",
//    Providers = new FirebaseAuthProvider[]
//    {
//        new EmailProvider(),
//        new GoogleProvider()
//    }
//}));
builder.Services.AddSignalR().AddNewtonsoftJsonProtocol(opt =>
{   
    opt.PayloadSerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Constants.SECRET_KEY_BYTE),

            ClockSkew = TimeSpan.Zero
        };
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                var isValidPath = path.StartsWithSegments("/hub");
                if (!string.IsNullOrEmpty(accessToken)
                    && isValidPath)
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
//builder.Services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();
//builder.Services.AddSingleton<IFirestoreService, FirestoreService>();
builder.Services.RegisterMapsterConfiguration();
builder.Services.RegisterAppService("ChatApp_Server.Repositories");
builder.Services.RegisterAppService("ChatApp_Server.Services");

var app = builder.Build();

// Configure the HTTP request pipeline.

//app.UseHttpsRedirection();
//app.UseCors(opt =>
//{
//    opt.WithOrigins(@"http://localhost:3000", @"http://192.168.1.3:3000");

//    opt.AllowAnyHeader();
//    opt.AllowCredentials();
//    opt.AllowAnyMethod();
//});

app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => "Hello World!");
//app.MapControllers();
//app.MapHub<ChatHub>("/hub/chat");
//app.MapHub<UserHub>("/hub/user");
//app.MapHub<RoomHub>("/hub/room");
//app.MapHub<ClientHub>("/hub/client");

//app.UseProblemDetails();
app.Run();
