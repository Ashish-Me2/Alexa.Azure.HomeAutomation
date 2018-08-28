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
using System.Web;
using System.Text.RegularExpressions;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializerAttribute(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Alexa.Skill.HomeAutomation
{
    public class SwitchboardHandler
    {
        private ILambdaLogger loggerGlobal;
        public List<FactResource> GetResources()
        {
            List<FactResource> resources = new List<FactResource>();
            FactResource enINResource = new FactResource("en-IN");
            enINResource.SkillName = "Switchboard";
            enINResource.GetFactMessage = "Here's the status of the room's devices";
            enINResource.HelpMessage = "You can say ask Controller to turn on the bulb in the Living Room, or, you can say exit... What can I help you with?";
            enINResource.HelpReprompt = String.Empty;
            enINResource.StopMessage = String.Empty;
            enINResource.Facts.Add("Please speak your desired operation clearly.");
            enINResource.Facts.Add("Kuchh samajh nahi aayaa. Dobaaraa bolo.");
            enINResource.Facts.Add("Sorry, I could not clearly understand the last command. Could you please repeat that?");
            enINResource.Facts.Add("Clearly bolo naa kya karnaa hai!");
            resources.Add(enINResource);
            return resources;
        }

        /// <summary>
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public SkillResponse SwitchHandler(SkillRequest input, ILambdaContext context)
        {
            SkillResponse response = new SkillResponse();
            response.Response = new ResponseBody();
            response.Response.ShouldEndSession = false;
            IOutputSpeech innerResponse = null;
            var log = context.Logger;
            loggerGlobal = log;
            log.LogLine($"Skill Request Object:");
            log.LogLine(JsonConvert.SerializeObject(input));

            var allResources = GetResources();
            var resource = allResources.FirstOrDefault();

            if (input.GetRequestType() == typeof(LaunchRequest))
            {
                log.LogLine($"Default LaunchRequest made: 'Alexa, ask Controller");
                innerResponse = new PlainTextOutputSpeech();
                (innerResponse as PlainTextOutputSpeech).Text = emitNewFact(resource, false);
                response.Response.ShouldEndSession = true;
            }
            else if (input.GetRequestType() == typeof(IntentRequest))
            {
                var intentRequest = (IntentRequest)input.Request;
                try
                {
                    string SLOT_ROOM_NAME = intentRequest.Intent.Slots["ROOM_NAME"].Value;
                    string SLOT_DEVICE_NAME = intentRequest.Intent.Slots["DEVICE_NAME"].Value;
                    string SLOT_DEVICE_STATE = intentRequest.Intent.Slots["DEVICE_STATE"].Value;

                    log.LogLine($"-------------------------------------------------------------");
                    log.LogLine($"INTENT RESOLVER received Intent - " + intentRequest.Intent.Name);
                    log.LogLine($"SLOT RESOLVER received Slots - " + SLOT_DEVICE_NAME + ", " + SLOT_ROOM_NAME + ", " + SLOT_DEVICE_STATE);
                    log.LogLine($"-------------------------------------------------------------");

                    SLOT_DEVICE_NAME = (SLOT_DEVICE_NAME.Equals("LIGHT", StringComparison.CurrentCultureIgnoreCase)) ? "TUBELIGHT" : SLOT_DEVICE_NAME;
                    SLOT_DEVICE_NAME = (SLOT_DEVICE_NAME.Equals("LAMP", StringComparison.CurrentCultureIgnoreCase)) ? "BULB" : SLOT_DEVICE_NAME;

                    string responseText = String.Empty;
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
                        case "AMAZON.FallbackIntent":
                            log.LogLine($"AMAZON.HelpIntent: send HelpMessage");
                            innerResponse = new PlainTextOutputSpeech();
                            (innerResponse as PlainTextOutputSpeech).Text = resource.HelpMessage;
                            break;
                        case "OperateDevice":
                            log.LogLine($"OperateDevice sent: Operate Controller with slot values:" + SLOT_DEVICE_NAME + ", " + SLOT_ROOM_NAME + ", " + SLOT_DEVICE_STATE);
                            innerResponse = new PlainTextOutputSpeech();
                            bool isDeviceCorrect = IsSlotValid(SLOT_DEVICE_NAME, "DEVICE");
                            bool isDeviceStateCorrect = IsSlotValid(SLOT_DEVICE_STATE, "STATE");
                            if (!isDeviceCorrect) responseText = "The device name in your command is not correct. Please repeat the command.";
                            if (!isDeviceStateCorrect) responseText = "The desired device operation in your command is not correct. Please repeat the command.";
                            if (isDeviceCorrect && isDeviceStateCorrect)
                            {
                                responseText = OperateDevice(SLOT_DEVICE_NAME, SLOT_ROOM_NAME, SLOT_DEVICE_STATE).Result;
                            }
                            (innerResponse as PlainTextOutputSpeech).Text = responseText;
                            response.Response.ShouldEndSession = true;
                            break;
                        case "GetStatus":
                            log.LogLine($"GetStatus sent: Get Controller Status with slot value:" + SLOT_DEVICE_NAME + ", " + SLOT_ROOM_NAME + ", " + SLOT_DEVICE_STATE);
                            innerResponse = new PlainTextOutputSpeech();
                            responseText = GetStatus(SLOT_DEVICE_NAME, SLOT_ROOM_NAME).Result;
                            (innerResponse as PlainTextOutputSpeech).Text = responseText;
                            response.Response.ShouldEndSession = true;
                            break;
                        default:
                            log.LogLine($"Unknown intent: " + intentRequest.Intent.Name);
                            innerResponse = new PlainTextOutputSpeech();
                            (innerResponse as PlainTextOutputSpeech).Text = resource.HelpMessage;
                            response.Response.ShouldEndSession = true;
                            break;
                    }
                }
                catch (Exception exp)
                {
                    innerResponse = new PlainTextOutputSpeech();
                    (innerResponse as PlainTextOutputSpeech).Text = emitNewFact(resource, false);
                    response.Response.ShouldEndSession = true;
                }
            }

            response.Response.OutputSpeech = innerResponse;
            response.Version = "1.0";
            log.LogLine($"Skill Response Object...");
            log.LogLine(JsonConvert.SerializeObject(response));
            return response;
        }

        private async Task<int> IsRoomControllerAlive(string RoomName)
        {
            ExtServiceHelper service = new ExtServiceHelper();
            string baseUri = "https://homeautomationapi.azurewebsites.net/api/home/";
            string method = "GetIsControllerAlive?RoomName=" + ReplaceAllSpaces(RoomName);
            string retVal = String.Empty;
            int result = -1;
            try
            {
                string roomStatus = service.GetDataFromService(baseUri, method, new List<object> { null }).Result;
                loggerGlobal.LogLine("Checking controller heartbeat: " + baseUri + method);
                loggerGlobal.LogLine("Heartbeat status: " + roomStatus);
                int.TryParse(roomStatus, out result);
            }
            catch (Exception exp)
            {
                retVal = "An error occurred while executing that operation.";
            }
            loggerGlobal.LogLine("Final Heartbeat status: " + result);
            return result;
        }


        public async Task<string> GetStatus(string DeviceName, string RoomName)
        {
            string retVal = String.Empty;
            if (IsRoomControllerAlive(RoomName).Result < 1)
            {
                retVal = "The controller for " + RoomName + " is not responding at the moment. Please check the controller.";
            }
            else {
                ExtServiceHelper service = new ExtServiceHelper();
                string baseUri = "https://homeautomationapi.azurewebsites.net/api/home/";
                string method = "GetRoomDeviceStatus?RoomName=" + ReplaceAllSpaces(RoomName);

                try
                {
                    string roomStatus = service.GetDataFromService(baseUri, method, new List<object> { null }).Result;
                    retVal = roomStatus.Replace("=", " is ");
                }
                catch (Exception exp)
                {
                    retVal = "An error occurred while executing that operation.";
                }
            }
            return retVal;
        }

        public async Task<string> OperateDevice(string DeviceName, string RoomName, string NewState)
        {
            string retVal = String.Empty;
            
            if (IsRoomControllerAlive(RoomName).Result < 1)
            {
                retVal = "The controller for " + RoomName + " is not responding at the moment. Please check the controller.";
            }
            else
            {
                ExtServiceHelper service = new ExtServiceHelper();
                string baseUri = "https://homeautomationapi.azurewebsites.net/api/home/";
                string method = "GetUpdatedDeviceState?DeviceName=" + DeviceName + "&RoomName=" + ReplaceAllSpaces(RoomName) + "&NewState=" + (NewState.Equals("ON", StringComparison.CurrentCultureIgnoreCase) ? 1 : 0);
                try
                {
                    loggerGlobal.LogLine("Invoking room controller operation: " + baseUri + method);
                    string roomStatus = service.GetDataFromService(baseUri, method, new List<object> { null }).Result;
                    loggerGlobal.LogLine("Controller operation response: " + roomStatus);
                    retVal = "Done";
                }
                catch (Exception exp)
                {
                    retVal = "An error occurred while executing that operation.";
                }
            }
            return retVal;
        }

        private bool IsSlotValid(string SlotValue, string SlotType)
        {
            bool retVal = false;
            switch (SlotType.ToUpper())
            {
                case "DEVICE":
                    if ((SlotValue.ToUpper() == "BULB") || (SlotValue.ToUpper() == "LAMP") || (SlotValue.ToUpper() == "FAN") || (SlotValue.ToUpper() == "TUBELIGHT")) retVal = true;
                    break;
                case "STATE":
                    if ((SlotValue.ToUpper() == "ON") || (SlotValue.ToUpper() == "OFF")) retVal = true;
                    break;
            }
            return retVal;
        }

        public string emitNewFact(FactResource resource, bool withPreface)
        {
            Random r = new Random();
            if (withPreface)
                return resource.GetFactMessage + resource.Facts[r.Next(resource.Facts.Count)];
            return resource.Facts[r.Next(resource.Facts.Count)];
        }

        public static string ReplaceAllSpaces(string str)
        {
            return Regex.Replace(str, @"\s+", "%20");
        }
    }
}