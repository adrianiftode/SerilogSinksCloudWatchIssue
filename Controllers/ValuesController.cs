using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace ServerlessWithSerilog.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly ILogger<ValuesController> _logger;

        public ValuesController(ILogger<ValuesController> logger)
        {
            _logger = logger;
        }
        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            _logger.LogInformation("Retrieve values {at}", DateTime.Now);
            return new string[] { "value1", "value2" };
        }

        // GET api/values/some-value
        [HttpGet("{value}")]
        public string Get(string value)
        {
            _logger.LogInformation("Retrieve {value}", value);
            return value.ToString();
        }
    }
}
