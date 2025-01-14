﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Mockaco
{
    internal class ChaosMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IEnumerable<IStrategyChaos> _strategies;
        private readonly ILogger<ChaosMiddleware> _logger;
        private readonly IOptions<ChaosOptions> _options;

        private List<int> ErrorList { get; set; }
        private int Counter { get; set; }


        public ChaosMiddleware(RequestDelegate next, IEnumerable<IStrategyChaos> strategies, ILogger<ChaosMiddleware> logger, IOptions<ChaosOptions> options
)
        {
            _next = next;
            _strategies = strategies;
            _logger = logger;
            _options = options;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (_options.Value.Enabled)
            {
                Counter++;
                if (Counter > 100)
                    Counter = 1;

                if (Counter == 1)
                    ErrorList = GenerateErrorList(_options.Value.ChaosRate);

                if (ErrorList.Contains(Counter))
                {
                    var selected = _strategies.Random();
                    _logger.LogInformation($"Chaos: {selected?.ToString()}");
                    await selected.Response(httpContext.Response);
                }

                if (httpContext.Response.StatusCode != (int)HttpStatusCode.OK)
                    return;

            }

            await _next(httpContext);

        }

        private List<int> GenerateErrorList(int rate)
        {
            int items = rate;
            var list = new List<int>();
            while (list.Count() < items)
            {
                var item = new Random().Next(1, 100);
                if (!list.Contains(item))
                {
                    list.Add(item);
                }
            }
            return list.OrderBy(x => x).ToList();
        }
    }
}
