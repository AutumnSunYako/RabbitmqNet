using Microsoft.AspNetCore.Mvc;
using PublishDemo.Messages;
using RabbitmqNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PublishDemo.Controllers
{
    public class TestController : ControllerBase
    {
        private readonly Client _client;
        public TestController(Client client)
        {
            _client = client;
        }
        [Route("/test")]
        public async Task PublishMessage()
        {
            var message = new PubMessage();
            message.Name = "测试2";
            await _client.Publish("Test.queue5.Pub","TestExc.jqy",  message);
        }
    }
}