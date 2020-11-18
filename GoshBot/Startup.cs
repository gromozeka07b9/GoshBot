using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoshBot.Jobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace GoshBot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddSingleton<IJobFactory, SingletonJobFactory>();
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
            services.AddSingleton<SendPostJob>();
            string cronExpression = string.Empty;
            cronExpression = Environment.GetEnvironmentVariable("CRONEXPRESSION");
            if (string.IsNullOrEmpty(cronExpression))
            {
                cronExpression = "0 0 18 1/1 * ? *";
                //cronExpression = "0/20 * * * * ?";
            }
            Console.WriteLine($"Started width cron:'{cronExpression}'");
            //services.AddSingleton(new JobSchedule(jobType: typeof(SendPostJob), cronExpression: "0 0 0/12 ? * *"));
            //services.AddSingleton(new JobSchedule(jobType: typeof(SendPostJob), cronExpression: "0/20 * * * * ?"));
            services.AddSingleton(new JobSchedule(jobType: typeof(SendPostJob), cronExpression: cronExpression));
            services.AddHostedService<QuartzHostedService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
