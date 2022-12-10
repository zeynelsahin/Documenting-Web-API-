using System.Reflection;
using Library.API;
using Library.API.Contexts;
using Library.API.OperationFİlters;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;

[assembly: ApiConventionType(typeof(DefaultApiConventions))]
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(configure =>
{
    configure.ReturnHttpNotAcceptable = true; //Kabul edilmeyen accept default olarak json dönmüyor
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

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.AddVersionedApiExplorer(options => { options.GroupNameFormat = "'v'VV"; });
var apiVersionDescriptionProvider = builder.Services.BuildServiceProvider().GetService<IApiVersionDescriptionProvider>();

builder.Services.AddSwaggerGen(setupAction =>
{
    foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
    {
        setupAction.SwaggerDoc($"LibraryOpenAPISpecification{description.GroupName}", new()
        {
            Title = "Library API",
            Version = description.ApiVersion.ToString(),
            Description = "Through this API you can access authors and their books",
            Contact = new()
            {
                Email = "zeynelsahin@zeynelsahin.com",
                Name = "Zeynel Şahin",
                Url = new Uri("https://www.zeynelsahin.com"),
            },
            License = new()
            {
                Name = "MIT License",
                Url = new Uri("https://opensourceçorg/licenses/MIT")
            }
        });
    }

    setupAction.DocInclusionPredicate((documentName, apiDescription) =>
    {
        var actionApiVersionModel = apiDescription.ActionDescriptor.GetApiVersionModel(ApiVersionMapping.Explicit | ApiVersionMapping.Implicit);
        if (actionApiVersionModel == null)
        {
            return true;
        }

        if (actionApiVersionModel.DeclaredApiVersions.Any())
        {
            return actionApiVersionModel.DeclaredApiVersions.Any(v => $"LibraryOpenAPISpecificationv{v}" == documentName);
        }

        return actionApiVersionModel.ImplementedApiVersions.Any(v => $"LibraryOpenAPISpecificationv{v}" == documentName);
    });
    // setupAction.SwaggerDoc("LibraryOpenAPISpecification", new()
    // {
    //     Title = "Library API",
    //     Version = "1",
    //     Description = "Through this API you can access authors and their books",
    //     Contact = new()
    //     {
    //         Email = "zeynelsahin@zeynelsahin.com",
    //         Name = "Zeynel Şahin",
    //         Url = new Uri("https://www.zeynelsahin.com"),
    //     },
    //     License = new()
    //     {
    //         Name = "MIT License",
    //         Url = new Uri("https://opensourceçorg/licenses/MIT")
    //     }
    // });
    // builder.SwaggerDoc("LibraryOpenAPISpecificationAuthors", new()
    // {
    //     Title = "Library API (Authors)",
    //     Version = "1",
    //     Description = "Through this API you can access authors",
    //     Contact = new ()
    //     {
    //         Email = "zeynelsahin@zeynelsahin.com",
    //         Name = "Zeynel Şahin",
    //         Url = new Uri("https://www.zeynelsahin.com"),
    //     },
    //     License = new ()
    //     {
    //         Name = "MIT License",
    //         Url = new Uri("https://opensourceçorg/licenses/MIT")
    //     }
    // });
    // builder.SwaggerDoc("LibraryOpenAPISpecificationBooks", new()
    // {
    //     Title = "Library API (Books)",
    //     Version = "1",
    //     Description = "Through this API you can access books",
    //     Contact = new ()
    //     {
    //         Email = "zeynelsahin@zeynelsahin.com",
    //         Name = "Zeynel Şahin",
    //         Url = new Uri("https://www.zeynelsahin.com"),
    //     },
    //     License = new ()
    //     {
    //         Name = "MIT License",
    //         Url = new Uri("https://opensourceçorg/licenses/MIT")
    //     }
    // });
    // builder.ResolveConflictingActions(apiDescription =>
    // {
    //     return apiDescription.First();
    //     // var firstDescription = apiDescription.First();
    //     // var secondDescription = apiDescription.ElementAt(1);
    //     // firstDescription.SupportedResponseTypes.AddRange(secondDescription.SupportedResponseTypes.Where(a=>a.StatusCode==200));
    //     // return firstDescription;
    // });

    setupAction.OperationFilter<GetBookOperationFilter>();
    setupAction.OperationFilter<CreateBookOperationFilter>();

    var xmlCommentsFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlCommentsFullPath = Path.Combine(AppContext.BaseDirectory, xmlCommentsFile);
    setupAction.IncludeXmlComments(xmlCommentsFullPath);
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
    {
        options.SwaggerEndpoint($"/swagger/LibraryOpenAPISpecification{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
    }

    // options.SwaggerEndpoint("/swagger/LibraryOpenAPISpecification/swagger.json", "Library API");
    // options.SwaggerEndpoint("/swagger/LibraryOpenAPISpecificationAuthors/swagger.json", "Library API (Authors)");
    // options.SwaggerEndpoint("/swagger/LibraryOpenAPISpecificationBooks/swagger.json", "Library API (Books)");
    options.RoutePrefix = string.Empty; //
});
// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();