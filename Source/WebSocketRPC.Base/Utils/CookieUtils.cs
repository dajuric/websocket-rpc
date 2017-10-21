using System.Collections.Generic;
using System.Net;

namespace WebSocketRPC
{
    internal static class CookieUtils
    {
        public static Dictionary<string, string> GetCookies(CookieCollection cookieCollection)
        {
            if (cookieCollection == null)
                return null;

            var cc = new Dictionary<string, string>();

            for (int i = 0; i < cookieCollection.Count; i++)
            {
                var k = cookieCollection[i].Name;
                if (cc.ContainsKey(k))
                    continue; //take only the first one 

                cc.Add(k, cookieCollection[k].Value);
            }

            return cc;
        }
    }
}
