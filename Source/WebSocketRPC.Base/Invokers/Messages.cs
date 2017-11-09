#region License
// Copyright © 2017 Darko Jurić
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace WebSocketRPC
{
    struct Request
    {
        public string FunctionName;
        public JToken[] Arguments;

        public static Request FromJson(string json)
        {
            JObject root = null;
            try { root = JObject.Parse(json); }
            catch { return default(Request); }

            var r = new Request
            {
                FunctionName = root[nameof(FunctionName)]?.Value<string>(),
                Arguments = root[nameof(Arguments)]?.Children().ToArray()
            };

            if (r.FunctionName == null || r.Arguments == null)
                return default(Request);

            return r;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public bool IsEmpty => FunctionName == null && Arguments == null;
    }

    struct Response
    {
        public string FunctionName;
        public JToken ReturnValue;
        public string Error;

        public static Response FromJson(string json)
        {
            JObject root = null;
            try { root = JObject.Parse(json); }
            catch { return default(Response); }

            var r = new Response
            {
                FunctionName = root[nameof(FunctionName)]?.Value<string>(),
                ReturnValue = root[nameof(ReturnValue)]?.Value<JToken>(),
                Error = root[nameof(Error)]?.Value<string>()
            };

            if (r.FunctionName == null || r.ReturnValue == null)
                return default(Response);

            return r;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public bool IsEmpty => FunctionName == null && ReturnValue == null && Error == null;
    }
}
