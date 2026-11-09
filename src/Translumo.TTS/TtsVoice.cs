namespace Translumo.TTS
{
    public class TtsVoice(string name, string languageCode)
    {
        public string Name { get; init; } = name;

        public string LanguageCode { get; init; } = languageCode;
    }
}
