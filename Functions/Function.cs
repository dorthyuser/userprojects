using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using LifeTimeCalculatorLambda.Models;
using LifeTimeCalculatorLambda.Services;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LifeTimeCalculatorLambda;

public class Function
{
    private readonly Service _service;

    public Function()
    {
        _service = new Service();
    }

    public async Task<Response> life_time_calculator(Request request, ILambdaContext context)
    {
        try
        {
            return await _service.CalculateAsync(request);
        }
        catch (Exception)
        {
            context.Logger.LogError("An error occurred while processing the request.");
            throw;
        }
    }
}
