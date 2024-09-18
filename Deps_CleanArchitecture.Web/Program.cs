using Autofac;
using Autofac.Extensions.DependencyInjection;
using Deps_CleanArchitecture.Core;
using Deps_CleanArchitecture.Infrastructure;
using Deps_CleanArchitecture.Infrastructure.Data;
using Deps_CleanArchitecture.Web.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;
using System;
using System.Collections.Generic;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    builder.Host.UseNLog();

    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

    builder.Services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(30);
    });

    builder.Services.AddDbContext(); //START CONFIGS INFRA

    const string CORS_POLICY = "CorsPolicy";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: CORS_POLICY,
                          builder =>
                          {
                              builder.AllowAnyOrigin();
                              builder.AllowAnyMethod();
                              builder.AllowAnyHeader();
                          });
    });
    
    builder.Services.AddControllers();

    // builder.Services.AddApiVersioning(setup =>
    // {
    //     setup.DefaultApiVersion = new ApiVersion(1, 0);
    //     setup.ReportApiVersions = true;
    //     setup.ApiVersionReader = new UrlSegmentApiVersionReader();
    // });

    // builder.Services.AddVersionedApiExplorer(setup =>
    // {
    //     setup.GroupNameFormat = "'v'VVV";
    //     setup.SubstituteApiVersionInUrl = true;
    // });

    builder.Services.AddSwaggerGen(c =>
    {
        c.EnableAnnotations();
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Token JWT. Exemplo: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            BearerFormat = "JWT",
            Type = SecuritySchemeType.ApiKey
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
        });

    });

    //builder.Services.ConfigureOptions<ConfigureSwaggerOptions>(); //CONFIGURE SWAGGER

    builder.Services.ConfigureJwt();

    builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
    {
        containerBuilder.RegisterModule(new DefaultCoreModule()); //CONFIGURE CORE
        containerBuilder.RegisterModule(new DefaultInfrastructureModule(builder.Environment.EnvironmentName == "Development"));
        //REGISTROS DO INFRA /\
    });


    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseHsts();
    }

    app.UseRouting();
    app.UseCors(CORS_POLICY);

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        // foreach (var description in provider.ApiVersionDescriptions)
        // {
        //     c.SwaggerEndpoint(
        //         $"/swagger/{description.GroupName}/swagger.json",
        //         description.GroupName.ToUpperInvariant());
        // }

        // Esconder schema na documentação
        c.DefaultModelsExpandDepth(-1);
    });

    app.MapControllers();

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            context.Database.Migrate();
            context.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "An error occurred seeding the DB.");
        }
    }

    app.Run();
}
catch (Exception exception)
{
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}