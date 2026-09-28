using Translumo.Utils.Http;

namespace Translumo.Translation.Ai
{
    public sealed class AiContainer : TranslationContainer
    {
        public HttpReader Reader { get; set; }

        public AiContainer(bool isPrimary = false) : base(isPrimary)
        {
            Reader = CreateReader();
        }

        private HttpReader CreateReader()
        {
            var httpReader = new HttpReader();

            httpReader.ContentType = "application/json";
            httpReader.UserAgent = "Translumo-Client-AI";
            httpReader.Accept = "application/json";

            return httpReader;
        }
    }
}
