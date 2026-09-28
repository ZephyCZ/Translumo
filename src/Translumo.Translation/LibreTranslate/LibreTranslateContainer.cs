using Translumo.Utils.Http;

namespace Translumo.Translation.LibreTranslate
{
    public sealed class LibreTranslateContainer : TranslationContainer
    {
        public HttpReader Reader { get; set; }

        public LibreTranslateContainer(bool isPrimary = false) : base(isPrimary)
        {
            Reader = CreateReader();
        }

        private HttpReader CreateReader()
        {
            var httpReader = new HttpReader();

            httpReader.ContentType = "application/json";
            httpReader.UserAgent = "Translumo-Client";
            httpReader.Accept = "application/json";

            return httpReader;
        }
    }
}
