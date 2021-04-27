using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Wishare.API
{
	public class Program
	{
		public static int Main(string[] args)
		{
			var host = CreateHostBuilder(args).Build();
			var services = host.Services;
			var logger = services.GetService<ILogger<Program>>(); // Consider using CreateBootstrapLogger if we have some complex startup logic further down the line 

			try
			{
				logger.LogInformation("API starting");
				host.Run();
				logger.LogInformation("API stopped");
				return 0;
			}
			catch (Exception ex)
			{
				logger.LogCritical(ex, "An error occurred during startup");
				return 1;
			}
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration))
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>().UseKestrel();
				});
	}
}
