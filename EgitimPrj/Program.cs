using EgitimPrj.Services.ExamUpload;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IPdfExamTextExtractor, PdfExamTextExtractor>();
builder.Services.AddScoped<IPdfExamResultLineParser, PdfExamResultLineParser>();
builder.Services.AddScoped<IExamStudentMatcher, ExamStudentMatcher>();
builder.Services.AddScoped<ITeacherPanelStudentDirectoryService, TeacherPanelStudentDirectoryService>();
builder.Services.AddScoped<IExamTrialExamApiSaver, ExamTrialExamApiSaver>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8); // iste�e ba�l�
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
