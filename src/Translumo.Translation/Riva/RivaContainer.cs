using Translumo.Translation.Configuration;
using Translumo.Utils.Http;

namespace Translumo.Translation.Riva
{
    public sealed class RivaContainer : TranslationContainer
    {
        public HttpReader Reader { get; set; }

        public RivaContainer(bool isPrimary = false) 
            : base(isPrimary)
        {
            Reader = CreateReader();
        }

        private HttpReader CreateReader()
        {
            var httpReader = new HttpReader();

            httpReader.ContentType = "application/json";
            httpReader.UserAgent = "Translumo-Client-Riva";
            httpReader.Accept = "application/json";

            return httpReader;
        }
    }
}
