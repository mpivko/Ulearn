﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using Ulearn.Core.Configuration;

namespace Database
{
	public class UlearnDbFactory : IDesignTimeDbContextFactory<UlearnDb>
	{
		public UlearnDb CreateDbContext(string[] args)
		{
			return CreateDbContext(args, null);
		}

		public UlearnDb CreateDbContext(string[] args, ILoggerFactory loggerFactory)
		{
			var optionsBuilder = new DbContextOptionsBuilder<UlearnDb>();
			optionsBuilder.UseLazyLoadingProxies();
			optionsBuilder.UseSqlServer(ApplicationConfiguration.Read<UlearnConfiguration>().Database);
			if (loggerFactory != null)
				optionsBuilder.UseLoggerFactory(loggerFactory);

			return new UlearnDb(optionsBuilder.Options);
		}
	}
}