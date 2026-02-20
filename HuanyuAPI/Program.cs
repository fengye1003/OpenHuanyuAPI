using HuanyuAPI.Essencial_Repos;

namespace HuanyuAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.DoLogCleanUp(10);
            int Port = 5003;
            string[] PortArg = new string[] { "--urls", "http://*:" + Port };

            var builder = WebApplication.CreateBuilder(PortArg);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            //app.UseHttpsRedirection();

            //app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
