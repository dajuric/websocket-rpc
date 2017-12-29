using DotImaging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using WebSocketRPC;
using System.Runtime.CompilerServices;

namespace Serialization
{
    class JpgBase64Converter : JsonConverter
    {
        private Type supportedType = typeof(Bgr<byte>[,]);

        public override bool CanConvert(Type objectType)
        {
            return objectType.Equals(supportedType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var im = value as Bgr<byte>[,];
            var bytes = im.EncodeAsJpeg();
            var jsBase64Jpg = Convert.ToBase64String(bytes);

            writer.WriteValue(jsBase64Jpg);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead => false;
    }

    /// <summary>
    /// The image processing service API.
    /// </summary>
    class ImageProcessingAPI
    {
        const int CHANNEL_COUNT = 3;

        /// <summary>
        /// Swaps the channels for an image provided by a url.
        /// </summary>
        /// <param name="order">Channel ordering. Each value has to be [0..2] range.</param>
        /// <param name="imgUrl">Image url.</param>
        /// <returns>Processed image.</returns>
        public Bgr<byte>[,] SwapImageChannels(Uri imgUrl, int[] order)
        {
            if (order.Any(x => x < 0 || x > CHANNEL_COUNT - 1))
                throw new ArgumentException(String.Format("Each element of the channel order must be in: [{0}..{1}] range.", 0, CHANNEL_COUNT - 1));

            Bgr<byte>[,] image = null;
            try { image = imgUrl.GetBytes().DecodeAsColorImage(); }
            catch(Exception ex) { throw new Exception("The specified url does not point to a valid image.", ex); }

            image.Apply(c => swapChannels(c, order), inPlace: true);
            return image;
        }

        unsafe Bgr<byte> swapChannels(Bgr<byte> c, int[] order)
        { 
            var uC = (byte*)Unsafe.AsPointer(ref c);
            var swapC = new Bgr<byte>(uC[order[0]], uC[order[1]], uC[order[2]]);
            return swapC;
        }    
    }

    class Program
    {
        //if access denied execute: "netsh http delete urlacl url=http://+:8001/" (delete for 'ocalhost', add for public address)
        //open Index.html to run the client
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));

            RPCSettings.MaxMessageSize = 1 * 1024 * 1024; //1MiB
            RPCSettings.AddConverter(new JpgBase64Converter());
            
            //generate js code
            File.WriteAllText($"../../Site/{nameof(ImageProcessingAPI)}.js", RPCJs.GenerateCallerWithDoc<ImageProcessingAPI>());

            //start server and bind its local and remote API
            var cts = new CancellationTokenSource();
            Server.ListenAsync("http://localhost:8001/", cts.Token, (c, ws) => c.Bind(new ImageProcessingAPI())).Wait();


            Console.Write("Running: '{0}'. Press [Enter] to exit.", nameof(Serialization));
            Console.ReadLine();
            cts.Cancel();
        }
    }
}
