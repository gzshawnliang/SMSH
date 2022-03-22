using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using SMSH.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<MediaStreamService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}


app.UseStaticFiles();
var m3u8FileProvider = new FileExtensionContentTypeProvider();
m3u8FileProvider.Mappings[".m3u8"] = "application/x-mpegurl";

string hlsPath = Path.Combine(Directory.GetCurrentDirectory(), "MediaContent", "HLS");
if(!Directory.Exists(hlsPath))
    Directory.CreateDirectory(hlsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(hlsPath),
    RequestPath = new PathString("/" + SMSH.Global.M3u8FileDir),
    ContentTypeProvider = m3u8FileProvider
    //ServeUnknownFileTypes = true
});

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
