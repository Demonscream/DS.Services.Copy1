
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using DS.Services.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace TestHarness.Api.Controllers
{
    [Route("api"), ExcludeFromCodeCoverage]
    public class HomeController(IDSEmailServiceFactory emailServiceFactory) : ControllerBase
    {
        [HttpGet("send_email")]
        public async Task<IActionResult> SendTestEmail()
        {
            using var emailService = emailServiceFactory.Create("Server2");

            Stream fileStr = System.IO.File.OpenRead("D:\\GitHub\\DS.Services\\test.pdf");


            var request = new EmailRequest
            {
                HtmlBody = "Test Body",
                TextBody = "Test Text Body",
                Subject = $"Test Subject {DateTime.Now}",
                ToEmails = ["pete@demonscream.com"],
                Attachments = [new (){FileName = "sendTest.pdf", FileStream = fileStr}]
            };

            await emailService.SendAsync(request);

            return Ok("Done");
        }
    }
}
