using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Alexa.NET.Response;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializerAttribute(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Alexa.Skill.HomeAutomation
{
    public class LunchMenuHandler
    {
        public List<FactResource> GetResources()
        {
            List<FactResource> resources = new List<FactResource>();
            FactResource enINResource = new FactResource("en-IN");
            enINResource.SkillName = "Switchboard";
            enINResource.GetFactMessage = "Here's the status of the room's devices";
            enINResource.HelpMessage = "You can say ask Switchboard to turn on the bulb in the Living Room, or, you can say exit... What can I help you with?";
            enINResource.HelpReprompt = String.Empty;
            enINResource.StopMessage = String.Empty;
            enINResource.Facts.Add("Umm. Please speak your desired operation clearly...");
            resources.Add(enINResource);
            return resources;
        }

        /// <summary>
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public SkillResponse MenuHandler(SkillRequest input, ILambdaContext context)
        {
            SkillResponse response = new SkillResponse();
            response.Response = new ResponseBody();
            response.Response.ShouldEndSession = false;
            IOutputSpeech innerResponse = null;
            var log = context.Logger;
            log.LogLine($"Skill Request Object:");
            log.LogLine(JsonConvert.SerializeObject(input));

            var allResources = GetResources();
            var resource = allResources.FirstOrDefault();

            if (input.GetRequestType() == typeof(LaunchRequest))
            {
                log.LogLine($"Default LaunchRequest made: 'Alexa, ask Switchboard");
                innerResponse = new PlainTextOutputSpeech();
                (innerResponse as PlainTextOutputSpeech).Text = emitNewFact(resource, true);

            }
            else if (input.GetRequestType() == typeof(IntentRequest))
            {
                var intentRequest = (IntentRequest)input.Request;
                switch (intentRequest.Intent.Name)
                {
                    case "AMAZON.CancelIntent":
                        log.LogLine($"AMAZON.CancelIntent: send StopMessage");
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = resource.StopMessage;
                        response.Response.ShouldEndSession = true;
                        break;
                    case "AMAZON.StopIntent":
                        log.LogLine($"AMAZON.StopIntent: send StopMessage");
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = resource.StopMessage;
                        response.Response.ShouldEndSession = true;
                        break;
                    case "AMAZON.HelpIntent":
                        log.LogLine($"AMAZON.HelpIntent: send HelpMessage");
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = resource.HelpMessage;
                        break;
                    case "OperateDevice":
                        log.LogLine($"GetFactIntent sent: Operate Switchboard with slot value:" + (intentRequest.Intent.Slots["weekday"].Value));
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = GetMenuItems(intentRequest.Intent.Slots["weekday"].Value).Result;
                        break;
                    case "GetStatus":
                        log.LogLine($"GetFactIntent sent: Get Switchboard Status with slot value:" + (intentRequest.Intent.Slots["weekday"].Value));
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = GetMenuItems(intentRequest.Intent.Slots["weekday"].Value).Result;
                        break;
                    default:
                        log.LogLine($"Unknown intent: " + intentRequest.Intent.Name);
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = emitNewFact(resource, false);
                        break;
                }
            }

            response.Response.OutputSpeech = innerResponse;
            response.Version = "1.0";
            log.LogLine($"Skill Response Object...");
            log.LogLine(JsonConvert.SerializeObject(response));
            return response;
        }

        public async Task<string> GetMenuItems(string weekDay)
        {
            ExtServiceHelper service = new ExtServiceHelper();
            string menu = "The menu for " + weekDay + " is " + await service.GetDataFromService("https://ocrserviceapi.azurewebsites.net/", "api/ocr?Weekday=", new List<object> { weekDay });
            return menu;
        }

        public string emitNewFact(FactResource resource, bool withPreface)
        {
            Random r = new Random();
            if (withPreface)
                return resource.GetFactMessage + resource.Facts[r.Next(resource.Facts.Count)];
            return resource.Facts[r.Next(resource.Facts.Count)];
        }
    }
}