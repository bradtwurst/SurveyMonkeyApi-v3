using System.Collections.Generic;

namespace SurveyMonkey.RequestSettings
{
    internal class RequestData : Dictionary<string, object>
    {
        public RequestData(IDictionary<string, object> dictionary) : base(dictionary)
        {
        }

        public RequestData() : base() { }
    }
}