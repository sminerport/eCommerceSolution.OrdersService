using eCommerce.OrdersMicroservice.BusinessLogicLayer;
using eCommerce.OrdersMicroservice.DataAccessLayer;

using eCommerce.OrdersMicroservice.API.Middleware;

using FluentValidation.AspNetCore;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;
using BusinessLogicLayer.HttpClients;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataAccessLayer(builder.Configuration);
builder.Services.AddBusinessLogicLayer(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddTransient<IUsersMicroservicePolicies, UsersMicroservicePolicies>();
builder.Services.AddTransient<IProductsMicroservicePolicies, ProductsMicroservicePolicies>();
builder.Services.AddTransient<IPollyPolicies, PollyPolicies>();

builder.Services.AddHttpClient<UsersMicroserviceClient>(client =>
{
    client.BaseAddress = new Uri(
      $"http://{builder.Configuration["UsersMicroserviceName"]}" +
      $":{builder.Configuration["UsersMicroservicePort"]}"
    );

    client.Timeout = TimeSpan.FromMinutes(5);
})
    .AddPolicyHandler(builder.Services.BuildServiceProvider()
                                      .GetRequiredService<IUsersMicroservicePolicies>()
                                      .GetCombinedPolicy());

builder.Services.AddHttpClient<ProductMicroserviceClient>(client =>
{
    client.BaseAddress = new Uri(
        $"http://{builder.Configuration["ProductsMicroserviceName"]}" +
        $":{builder.Configuration["ProductsMicroservicePort"]}");

    client.Timeout = TimeSpan.FromMinutes(5);
})
    .AddPolicyHandler(builder.Services.BuildServiceProvider()
                                      .GetRequiredService<IProductsMicroservicePolicies>()
                                      .GetCombinedPolicy());

var app = builder.Build();

app.UseExceptionHandlingMiddleware();

app.UseRouting();

app.UseCors();

app.UseSwagger();

app.UseSwaggerUI();

//app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();