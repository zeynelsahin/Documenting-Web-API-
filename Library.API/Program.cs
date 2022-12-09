using System.Reflection;
using Library.API;
using Library.API.Contexts;
using Library.API.OperationFİlters;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;

[assembly: ApiConventionType(typeof(DefaultApiConventions))]
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(configure =>
{
    configure.ReturnHttpNotAcceptable = true;//Kabul edilmeyen acceptapt default olarak json dönmüyor
    // configure.Filters.Add(new ProducesResponseTypeAttribute(StatusCodes.Status400BadRequest));
    // configure.Filters.Add(new ProducesResponseTypeAttribute(StatusCodes.Status406NotAcceptable));
    // configure.Filters.Add(new ProducesResponseTypeAttribute(StatusCodes.Status500InternalServerError));
}).AddNewtonsoftJson(setupAction =>
{
    setupAction.SerializerSettings.ContractResolver =
        new CamelCasePropertyNamesContractResolver();
}).AddXmlDataContractSerializerFormatters();

// configure the NewtonsoftJsonOutputFormatter
builder.Services.Configure<MvcOptions>(configureOptions =>
{
    var jsonOutputFormatter = configureOptions.OutputFormatters
        .OfType<NewtonsoftJsonOutputFormatter>().FirstOrDefault();

    if (jsonOutputFormatter != null)
    {
        // remove text/json as it isn't the approved media type
        // for working with JSON at API level
        if (jsonOutputFormatter.SupportedMediaTypes.Contains("text/json"))
        {
            jsonOutputFormatter.SupportedMediaTypes.Remove("text/json");
        }
    }
});

builder.Services.AddDbContext<LibraryContext>(
    dbContextOptions => dbContextOptions.UseSqlite(
        builder.Configuration["ConnectionStrings:LibraryDBConnectionString"]));

builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();
builder.Services.AddScoped<IBookRepository, BookRepository>();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddSwaggerGen(builder =>
{
    builder.SwaggerDoc("LibraryOpenAPISpecification", new()
    {
        Title = "Library API",
        Version = "1",
        Description = "Through this API you can access authors and their books",
        Contact = new ()
        {
            Email = "zeynelsahin@zeynelsahin.com",
            Name = "Zeynel Şahin",
            Url = new Uri("https://www.zeynelsahin.com"),
        },
        License = new ()
        {
            Name = "MIT License",
            Url = new Uri("https://opensourceçorg/licenses/MIT")
        }
    });

    // builder.ResolveConflictingActions(apiDescription =>
    // {
    //     return apiDescription.First();
    //     // var firstDescription = apiDescription.First();
    //     // var secondDescription = apiDescription.ElementAt(1);
    //     // firstDescription.SupportedResponseTypes.AddRange(secondDescription.SupportedResponseTypes.Where(a=>a.StatusCode==200));
    //     // return firstDescription;
    // });
    
    builder.OperationFilter<GetBookOperationFilter>();
    builder.OperationFilter<CreateBookOperationFilter>();
    
    var xmlCommentsFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlCommentsFullPath = Path.Combine(AppContext.BaseDirectory, xmlCommentsFile);
    builder.IncludeXmlComments(xmlCommentsFullPath);
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/LibraryOpenAPISpecification/swagger.json", "Library API");
    options.RoutePrefix = string.Empty;//
});
// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();