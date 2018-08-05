using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Threading.Tasks;

namespace Alexa.Skill.HomeAutomation
{
    public class ExtServiceHelper
    {
        public async Task<string> GetDataFromService(string ServiceURI, string MethodName, List<object> Parameters)
        {
            string retVal = String.Empty;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(ServiceURI);
                    string parameter = (Parameters[0] != null) ? Parameters[0].ToString() : String.Empty;
                    retVal = await client.GetStringAsync(ServiceURI + MethodName + parameter);
                }
            }
            catch (Exception exp)
            {
                retVal = "There was an error processing your request. " + exp.Message;
            }
            return retVal;
        }
    }
}
