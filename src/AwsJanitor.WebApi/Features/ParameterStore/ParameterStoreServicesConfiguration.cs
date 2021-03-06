using AwsJanitor.WebApi.Infrastructure.Aws;
using AwsJanitor.WebApi.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Amazon;
namespace AwsJanitor.WebApi.Controllers
{
    public static class ParameterStoreServicesConfiguration
    {
        public static void ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            services.AddTransient(serviceProvider => RegionEndpoint.GetBySystemName(configuration["AWS_REGION"]));
            services.AddSingleton(new KubernetesClusterName(configuration["KUBERNETES_CLUSTER_NAME"]));
            services.AddTransient<IParameterStore, ParameterStore>();
        }
    }
}