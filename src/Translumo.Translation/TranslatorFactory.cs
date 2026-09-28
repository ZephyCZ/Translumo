using System;
using Microsoft.Extensions.Logging;
using Translumo.Infrastructure.Language;
using Translumo.Translation.Configuration;
using Translumo.Translation.Deepl;
using Translumo.Translation.Google;
using Translumo.Translation.Yandex;
using Translumo.Translation.LibreTranslate;
using Translumo.Translation.Ai;
using Translumo.Translation.Riva;
using Translumo.Translation.Onnx;

namespace Translumo.Translation
{
    public class TranslatorFactory
    {
        private readonly LanguageService _languageService;
        private readonly ILogger _logger;

        public TranslatorFactory(LanguageService languageService, ILogger<TranslatorFactory> logger)
        {
            this._languageService = languageService;
            this._logger = logger;
        }

        public ITranslator CreateTranslator(TranslationConfiguration translatorConfiguration)
        {
            switch (translatorConfiguration.Translator)
            {
                case Translators.Deepl:
                    return new DeepLTranslator(translatorConfiguration, _languageService, _logger);
                case Translators.Yandex:
                    return new YandexTranslator(translatorConfiguration, _languageService, _logger);
                case Translators.Google:
                    return new GoogleTranslator(translatorConfiguration, _languageService, _logger);
                case Translators.LibreTranslate:
                    return new LibreTranslateTranslator(translatorConfiguration, _languageService, _logger);
                case Translators.AiTranslator:
                    return new AiTranslator(translatorConfiguration, _languageService, _logger);
                case Translators.NvidiaRiva:
                    return new RivaTranslator(translatorConfiguration, _languageService, _logger);
                case Translators.Onnx:
                    return new OnnxTranslator(translatorConfiguration, _languageService, _logger);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
