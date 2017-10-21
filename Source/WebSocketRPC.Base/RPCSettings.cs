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
using Newtonsoft.Json.Serialization;
using System;
using System.Text;

namespace WebSocketRPC
{
    /// <summary>
    /// RPC settings.
    /// </summary>
    public static class RPCSettings
    {
        class CamelCaseExceptDictionaryKeysResolver : CamelCasePropertyNamesContractResolver
        {
            protected override JsonDictionaryContract CreateDictionaryContract(Type objectType)
            {
                JsonDictionaryContract contract = base.CreateDictionaryContract(objectType);

                contract.DictionaryKeyResolver = propertyName => propertyName;

                return contract;
            }
        }

        /// <summary>
        /// Gets the messaging serializer.
        /// </summary>
        internal static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings { ContractResolver = new CamelCaseExceptDictionaryKeysResolver() });

        /// <summary>
        /// Adds the serialization type converter.
        /// </summary>
        /// <param name="converter">Converter.</param>
        public static void AddConverter(JsonConverter converter)
        {
            Serializer.Converters.Add(converter);
        }

        /// <summary>
        /// Gets or sets the maximum message size [1..inf].
        /// </summary>
        public static int MaxMessageSize
        {
            get { return Connection.MaxMessageSize; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("The message size must be set to a strictly positive value.");

                Connection.MaxMessageSize = value;
            }
        }

        static Encoding encoding = Encoding.ASCII;
        /// <summary>
        /// Gets or sets the string RPC messaging encoding.
        /// </summary>
        public static Encoding Encoding
        {
            get { return encoding; }
            set
            {
                if (encoding == null)
                    throw new ArgumentException("The provided value must not be null.");

                encoding = value;
            }
        }
    }
}
