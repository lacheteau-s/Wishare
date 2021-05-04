using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Wishare.Data;

namespace Wishare.API
{
	public class Startup
	{
		private readonly IWebHostEnvironment _hostEnvironment;
		private readonly IConfiguration _configuration;

		public Startup(IConfiguration configuration, IWebHostEnvironment hostEnvironment)
		{
			_hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
			_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton<IDatabaseManager>(CreateDatabaseManager);
			services.AddSingleton<IQueryExecutor>(CreateQueryExecutor());
			services.AddControllers();
			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "Wishare.API", Version = "v1" });
			});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseSwagger();
				app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Wishare.API v1"));
			}

			app.UseSerilogRequestLogging();

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}

		private IDatabaseManager CreateDatabaseManager(IServiceProvider serviceProvider)
		{
			var sqlDirectoryPath = Path.Combine(_hostEnvironment.ContentRootPath, "SQL");
			var sqlFileProvider = new PhysicalFileProvider(sqlDirectoryPath);

			return new DatabaseManager(
				sqlFileProvider,
				serviceProvider.GetRequiredService<IQueryExecutor>(),
				serviceProvider.GetRequiredService<ILogger<DatabaseManager>>());
		}

		private IQueryExecutor CreateQueryExecutor()
		{
			var connectionString = _configuration.GetConnectionString("Database");

			return new QueryExecutor(SqlClientFactory.Instance, connectionString);
		}
	}
}
