using Microsoft.EntityFrameworkCore;
using TabBlazor.QuickTable.EntityFramework;
using Tabler.Docs;
using Tabler.Docs.Server;
using Tabler.Docs.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IDataService, LocalDataService>();
builder.Services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlite("Data Source=app.db"));
builder.Services.AddQuickTableEntityFrameworkAdapter();
builder.Services.AddScoped<ICodeSnippetService, LocalSnippetService>();
builder.Services.AddDocs();

var app = builder.Build();

SeedData.EnsureSeeded(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Tabler.Docs.Shared.MainLayout).Assembly);

app.Run();
