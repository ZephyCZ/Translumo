using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows.Input;
using Translumo.Dialog;
using Translumo.Dialog.Stages;
using Translumo.Infrastructure.Language;
using Translumo.MVVM.Common;
using Translumo.MVVM.Models;
using Translumo.OCR.Configuration;
using Translumo.OCR.WindowsOCR;
using Translumo.Translation;
using Translumo.Translation.Configuration;
using Translumo.Translation.Ai;
using Translumo.TTS;
using Translumo.Utils;
using Translumo.Services;
using RelayCommand = Microsoft.Toolkit.Mvvm.Input.RelayCommand;
using AsyncRelayCommand = Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand;
using System.Text.Json;

namespace Translumo.MVVM.ViewModels
{
    public sealed class LanguagesSettingsViewModel : BindableBase, IAdditionalPanelController, IDisposable
    {
        public event EventHandler<bool> PanelStateIsChanged;


        public IList<DisplayLanguage> AvailableLanguages { get; set; }
        public IList<DisplayLanguage> AvailableTranslationLanguages { get; set; }

        public TranslationConfiguration Model { get; set; }

        public TtsConfiguration TtsSettings { get; set; }

        private ObservableCollection<VoiceInfo> _availableVoices;
        public ObservableCollection<VoiceInfo> AvailableVoices
        {
            get => _availableVoices;
            set => SetProperty(ref _availableVoices, value);
        }

        private VoiceInfo _selectedVoice;
        public VoiceInfo SelectedVoice
        {
            get => _selectedVoice;
            set
            {
                SetProperty(ref _selectedVoice, value);
                if (value != null)
                {
                    Action updateVoiceAction = () =>
                    {
                        TtsSettings.SelectedVoiceName = value.Name;
                    };

                    _ = ReconfigureTts(TtsSettings.TtsLanguage, TtsSettings.TtsSystem, updateVoiceAction);
                }
            }
        }

        public bool IsTtsWindowsSelected => TtsSettings.TtsSystem == TTSEngines.WindowsTTS;

        public bool IsDeeplSelected => Model.Translator == Translators.Deepl;

        public bool IsYandexSelected => Model.Translator == Translators.Yandex;
        public bool IsLibreTranslateSelected => Model.Translator == Translators.LibreTranslate;

        public bool IsAiTranslatorSelected => Model.Translator == Translators.AiTranslator;

        public bool IsRivaSelected => Model.Translator == Translators.NvidiaRiva;

        public bool IsOnnxSelected => Model.Translator == Translators.Onnx;

        public IEnumerable<AiTranslatorProvider> AvailableAiProviders => Enum.GetValues<AiTranslatorProvider>();

        public string AiModelCaptionDefault
        {
            get
            {
                if (Model.AiProvider == AiTranslatorProvider.Gemini)
                    return LocalizationManager.GetValue("Str.LangSettings.AiTransDefaultGemini"); //"(Default: gemini-3.5-flash)";
                if (Model.AiProvider == AiTranslatorProvider.DeepSeek)
                    return LocalizationManager.GetValue("Str.LangSettings.AiTransDefaultDeepSeek"); //"(Default: deepseek-v4-flash)";
                if (Model.AiProvider == AiTranslatorProvider.OpenRouter)
                    return LocalizationManager.GetValue("Str.LangSettings.AiTransDefaultOpenRouter"); //"";
                if (Model.AiProvider == AiTranslatorProvider.NvidiaNIM)
                    return LocalizationManager.GetValue("Str.LangSettings.AiTransDefaultNIM"); //"(Default: deepseek-ai/deepseek-v4-flash)";
                return "";
            }
        }

        public string CurrentAiApiKey
        {
            get
            {
                if (Model.AiProvider == AiTranslatorProvider.Gemini) return Model.GeminiApiKey;
                if (Model.AiProvider == AiTranslatorProvider.DeepSeek) return Model.DeepSeekApiKey;
                if (Model.AiProvider == AiTranslatorProvider.OpenRouter) return Model.OpenRouterApiKey;
                if (Model.AiProvider == AiTranslatorProvider.NvidiaNIM) return Model.NvidiaNIMApiKey;
                return string.Empty;
            }
            set
            {
                if (Model.AiProvider == AiTranslatorProvider.Gemini) Model.GeminiApiKey = value;
                else if (Model.AiProvider == AiTranslatorProvider.DeepSeek) Model.DeepSeekApiKey = value;
                else if (Model.AiProvider == AiTranslatorProvider.OpenRouter) Model.OpenRouterApiKey = value;
                else if (Model.AiProvider == AiTranslatorProvider.NvidiaNIM) Model.NvidiaNIMApiKey = value;
                OnPropertyChanged(nameof(CurrentAiApiKey));
            }
        }

        public string CurrentAiModel
        {
            get
            {
                if (Model.AiProvider == AiTranslatorProvider.Gemini) return Model.GeminiAiModel;
                if (Model.AiProvider == AiTranslatorProvider.DeepSeek) return Model.DeepSeekAiModel;
                if (Model.AiProvider == AiTranslatorProvider.OpenRouter) return Model.OpenRouterAiModel;
                if (Model.AiProvider == AiTranslatorProvider.NvidiaNIM) return Model.NvidiaNIMAiModel;
                return string.Empty;
            }
            set
            {
                if (Model.AiProvider == AiTranslatorProvider.Gemini) Model.GeminiAiModel = value;
                else if (Model.AiProvider == AiTranslatorProvider.DeepSeek) Model.DeepSeekAiModel = value;
                else if (Model.AiProvider == AiTranslatorProvider.OpenRouter) Model.OpenRouterAiModel = value;
                else if (Model.AiProvider == AiTranslatorProvider.NvidiaNIM) Model.NvidiaNIMAiModel = value;
                OnPropertyChanged(nameof(CurrentAiModel));
            }
        }



        public Translators Translator
        {
            get => Model.Translator;
            set
            {
                Model.Translator = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDeeplSelected));
                OnPropertyChanged(nameof(IsYandexSelected));
            }
        }

        public string DeeplApiKey
        {
            get => Model.DeeplApiKey;
            set
            {
                Model.DeeplApiKey = value;
                OnPropertyChanged();
            }
        }

        public string YandexApiKey
        {
            get => Model.YandexApiKey;
            set
            {
                Model.YandexApiKey = value;
                OnPropertyChanged();
            }
        }

        public bool IsTtsEnabled => TtsSettings.TtsSystem != TTSEngines.None;



        public Languages TranslateFromLang
        {
            get => Model.TranslateFromLang;
            set
            {
                ChangeSourceLanguage(value);
            }
        }

        public Languages TranslateToLang
        {
            get => Model.TranslateToLang;
            set
            {
                ChangeTargetLanguage(value);
            }
        }

        public TTSEngines TtsSystem
        {
            get => TtsSettings.TtsSystem;
            set
            {
                ChangeTtsSystem(value);
            }
        }

        private string _libreTranslateTestResult;
        public string LibreTranslateTestResult
        {
            get => _libreTranslateTestResult;
            set => SetProperty(ref _libreTranslateTestResult, value);
        }

        private string _libreTranslateTestResultColor = "Gray";
        public string LibreTranslateTestResultColor
        {
            get => _libreTranslateTestResultColor;
            set => SetProperty(ref _libreTranslateTestResultColor, value);
        }

        private bool _isLibreTranslateTesting;
        public bool IsLibreTranslateTesting
        {
            get => _isLibreTranslateTesting;
            set
            {
                SetProperty(ref _isLibreTranslateTesting, value);
                OnPropertyChanged(nameof(CanTestLibreTranslate));
            }
        }

        public bool CanTestLibreTranslate => !IsLibreTranslateTesting;

        public ICommand ToggleLibreTranslateGuideCommand => new RelayCommand(() =>
        {
            var title = LocalizationManager.GetValue("Str.Stages.LibreTranslateGuideTitle");
            var text = LocalizationManager.GetValue("Str.Stages.LibreTranslateGuide", lineBreakReplacement: true);
            var _ = _dialogService.ShowDialogAsync(SimpleDialogViewModel.Create(text, SimpleDialogTypes.Info, title));
        });

        public ICommand ToggleOnnxManualGuideCommand => new RelayCommand(() =>
        {
            var title = LocalizationManager.GetValue("Str.Stages.OnnxManualTitle");
            var text = $"{LocalizationManager.GetValue("Str.Stages.OnnxGuideText", lineBreakReplacement: true)}\n\n{LocalizationManager.GetValue("Str.Stages.OnnxManualGuide", lineBreakReplacement: true)}";
            var _ = _dialogService.ShowDialogAsync(SimpleDialogViewModel.Create(text, SimpleDialogTypes.Info, title));
        });

        public ICommand RunLibreTranslateCommand => new RelayCommand(OnRunLibreTranslate);

        private string _aiTestResult;
        public string AiTestResult
        {
            get => _aiTestResult;
            set => SetProperty(ref _aiTestResult, value);
        }

        private string _aiTestResultColor = "Gray";
        public string AiTestResultColor
        {
            get => _aiTestResultColor;
            set => SetProperty(ref _aiTestResultColor, value);
        }

        private bool _isAiTesting;
        public bool IsAiTesting
        {
            get => _isAiTesting;
            set
            {
                SetProperty(ref _isAiTesting, value);
                OnPropertyChanged(nameof(CanTestAi));
            }
        }

        public bool CanTestAi => !IsAiTesting;

        public ICommand TestAiCommand => new AsyncRelayCommand(OnTestAiAsync);

        private string _rivaTestResult;
        public string RivaTestResult
        {
            get => _rivaTestResult;
            set => SetProperty(ref _rivaTestResult, value);
        }

        private string _rivaTestResultColor = "Gray";
        public string RivaTestResultColor
        {
            get => _rivaTestResultColor;
            set => SetProperty(ref _rivaTestResultColor, value);
        }

        private bool _isRivaTesting;
        public bool IsRivaTesting
        {
            get => _isRivaTesting;
            set
            {
                SetProperty(ref _isRivaTesting, value);
                OnPropertyChanged(nameof(CanTestRiva));
            }
        }

        public bool CanTestRiva => !IsRivaTesting;

        public ICommand TestRivaCommand => new AsyncRelayCommand(OnTestRivaAsync);

        private readonly DialogService _dialogService;
        private readonly OcrGeneralConfiguration _ocrConfiguration;
        private readonly LanguageService _languageService;
        private readonly LibreTranslateManager _libreTranslateManager;
        private readonly ILogger _logger;

        public LanguagesSettingsViewModel(LanguageService languageService, TranslationConfiguration translationConfiguration,
            OcrGeneralConfiguration ocrConfiguration, TtsConfiguration ttsConfiguration, DialogService dialogService,
            LibreTranslateManager libreTranslateManager, ILogger<LanguagesSettingsViewModel> logger)
        {
            var languages = languageService.GetAll(true)
                .Select(lang => (lang.TranslationOnly, new DisplayLanguage(lang, GetLanguageDisplayName(lang))))
                .ToArray();
            this.AvailableLanguages = languages
                .Where(lang => !lang.TranslationOnly)
                .OrderBy(lang => lang.Item2.DisplayName)
                .Select(lang => lang.Item2)
                .ToList();

            this.AvailableTranslationLanguages = languages
                .OrderBy(lang => lang.Item2.DisplayName)
                .Select(lang => lang.Item2)
                .ToList();

            this.Model = translationConfiguration;
            this.Model.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(Model.Translator))
                {
                    OnPropertyChanged(nameof(IsLibreTranslateSelected));
                    OnPropertyChanged(nameof(IsAiTranslatorSelected));
                    OnPropertyChanged(nameof(IsRivaSelected));
                    OnPropertyChanged(nameof(IsOnnxSelected));
                }
                else if (args.PropertyName == nameof(Model.AiProvider))
                {
                    OnPropertyChanged(nameof(AiModelCaptionDefault));
                    OnPropertyChanged(nameof(CurrentAiApiKey));
                    OnPropertyChanged(nameof(CurrentAiModel));
                }
            };
            this.TtsSettings = ttsConfiguration;
            this.TtsSettings.TtsLanguage = this.Model.TranslateToLang;

            this.AvailableVoices = new ObservableCollection<VoiceInfo>();

            if (this.TtsSettings.TtsSystem == TTSEngines.WindowsTTS)
            {
                var languageCode = languageService.GetLanguageDescriptor(this.TtsSettings.TtsLanguage).Code;
                LoadAvailableVoices(languageCode);
            }

            _languageService = languageService;
            _dialogService = dialogService;
            _ocrConfiguration = ocrConfiguration;
            _libreTranslateManager = libreTranslateManager;
            _logger = logger;

            OnPropertyChanged(nameof(AiModelCaptionDefault));
            ResetLibreTranslateState();
        }

        private void LoadAvailableVoices(string languageCode)
        {
            try
            {
                var voices = GetAvailableVoicesForLanguage(languageCode);
                AvailableVoices = new ObservableCollection<VoiceInfo>(voices);

                if (!string.IsNullOrEmpty(TtsSettings.SelectedVoiceName))
                {
                    _selectedVoice = AvailableVoices.FirstOrDefault(v =>
                        v.Name.Equals(TtsSettings.SelectedVoiceName, StringComparison.OrdinalIgnoreCase));
                }

                if (_selectedVoice == null && AvailableVoices.Count > 0)
                {
                    _selectedVoice = AvailableVoices[0];
                    TtsSettings.SelectedVoiceName = _selectedVoice.Name;
                }

                OnPropertyChanged(nameof(SelectedVoice));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Load available voices error");
                AvailableVoices = new ObservableCollection<VoiceInfo>();
            }
        }

        private List<VoiceInfo> GetAvailableVoicesForLanguage(string languageTag)
        {
            using var synth = new SpeechSynthesizer();
            var result = new List<VoiceInfo>();

            try
            {
                var voices = synth.GetInstalledVoices(new CultureInfo(languageTag));
                if (voices.Count > 0)
                {
                    result.AddRange(voices.Select(v => v.VoiceInfo));
                    return result;
                }
            }
            catch
            {
            }

            try
            {
                var shortTag = languageTag.Split('-')[0];
                var voices = synth.GetInstalledVoices(new CultureInfo(shortTag));
                if (voices.Count > 0)
                {
                    result.AddRange(voices.Select(v => v.VoiceInfo));
                }
            }
            catch
            {
            }

            return result;
        }

        private async Task<bool> ValidateOpenRouterModelAsync(string modelName)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Translumo-Client-AI");
                    var response = await httpClient.GetAsync("https://openrouter.ai/api/v1/models");
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        using (var doc = JsonDocument.Parse(jsonString))
                        {
                            if (doc.RootElement.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var element in dataProp.EnumerateArray())
                                {
                                    if (element.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
                                    {
                                        if (idProp.GetString().Equals(modelName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate model against OpenRouter meta API.");
                return true;
            }
            return false;
        }



        private async Task OnTestAiAsync()
        {
            if (IsAiTesting)
            {
                return;
            }

            IsAiTesting = true;
            AiTestResult = "Testing AI connection...";
            AiTestResultColor = "Orange";

            if (string.IsNullOrWhiteSpace(CurrentAiModel))
            {
                AiTestResult = "Validation Error: Please enter a Model Identifier first.";
                AiTestResultColor = "Red";
                IsAiTesting = false;
                return;
            }

            try
            {
                if (Model.AiProvider == AiTranslatorProvider.OpenRouter)
                {
                    AiTestResult = "Validating OpenRouter model identifier...";
                    bool isModelValid = await ValidateOpenRouterModelAsync(CurrentAiModel);
                    if (!isModelValid)
                    {
                        AiTestResult = $"Validation Error: Model '{CurrentAiModel}' not found on OpenRouter. Check openrouter.ai/models for valid IDs.";
                        AiTestResultColor = "Red";
                        IsAiTesting = false;
                        return;
                    }
                    AiTestResult = "Testing OpenRouter connection...";
                }

                var testTranslator = new Translation.Ai.AiTranslator(Model, _languageService, _logger);

                string testText = "Hello";
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                string translated = await testTranslator.TranslateTextAsync(testText);
                stopwatch.Stop();

                AiTestResult = $"Connection successful! Test: '{testText}' -> '{translated}' ({stopwatch.ElapsedMilliseconds}ms)";
                AiTestResultColor = "Green";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI connection test failed");
                AiTestResult = $"Connection failed: {ex.Message}";
                AiTestResultColor = "Red";
            }
            finally
            {
                IsAiTesting = false;
            }
        }

        private async Task OnTestRivaAsync()
        {
            if (IsRivaTesting)
            {
                return;
            }

            IsRivaTesting = true;
            RivaTestResult = "Testing Riva connection...";
            RivaTestResultColor = "Orange";

            if (string.IsNullOrWhiteSpace(Model.RivaApiKey))
            {
                RivaTestResult = "Validation Error: Please enter an API Key first.";
                RivaTestResultColor = "Red";
                IsRivaTesting = false;
                return;
            }

            try
            {
                var testTranslator = new Translumo.Translation.Riva.RivaTranslator(Model, _languageService, _logger);

                string testText = "Hello";
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                string translated = await testTranslator.TranslateTextAsync(testText);
                stopwatch.Stop();

                RivaTestResult = $"Connection successful! Test: '{testText}' -> '{translated}' ({stopwatch.ElapsedMilliseconds}ms)";
                RivaTestResultColor = "Green";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Riva connection test failed");
                RivaTestResult = $"Connection failed: {ex.Message}";
                RivaTestResultColor = "Red";
            }
            finally
            {
                IsRivaTesting = false;
            }
        }

        private void OnRunLibreTranslate()
        {
            try
            {
                IsLibreTranslateTesting = true;

                var sourceLang = _languageService.GetLanguageDescriptor(TranslateFromLang).IsoCode;
                var targetLang = _languageService.GetLanguageDescriptor(TranslateToLang).IsoCode;

                _libreTranslateManager.EnsureServerRunning(sourceLang, targetLang);

                LibreTranslateTestResult = $"{sourceLang.ToUpper()}_{targetLang.ToUpper()}";
                LibreTranslateTestResultColor = "Green";
            }
            catch (Exception ex)
            {
                IsLibreTranslateTesting = false;
                _logger.LogError(ex, "Failed to run local LibreTranslate");
                LibreTranslateTestResult = "Failed to start server";
                LibreTranslateTestResultColor = "Red";
            }
        }

        private void ResetLibreTranslateState()
        {
            IsLibreTranslateTesting = false;

            var sourceLang = _languageService.GetLanguageDescriptor(TranslateFromLang)?.IsoCode ?? "EN";
            var targetLang = _languageService.GetLanguageDescriptor(TranslateToLang)?.IsoCode ?? "ID";

            LibreTranslateTestResult = $"{sourceLang.ToUpper()}_{targetLang.ToUpper()}";
            LibreTranslateTestResultColor = _libreTranslateManager.IsRunning ? "Green" : "Red";
        }

        private async Task ChangeSourceLanguage(Languages language)
        {
            try
            {
                var changeLangStage = StagesFactory.CreateLanguageChangeStages(_dialogService, () => Model.TranslateFromLang = language,
                    _logger);

                if (_ocrConfiguration.GetConfiguration<WindowsOCRConfiguration>().Enabled)
                {
                    var langCode = _languageService.GetLanguageDescriptor(language).Code;
                    changeLangStage = StagesFactory.CreateWindowsOcrCheckingStages(_dialogService, langCode, changeLangStage, _logger);
                }

                await changeLangStage.ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during source language change");
            }

            OnPropertyChanged(nameof(TranslateFromLang));
            ResetLibreTranslateState();
        }

        private async Task ChangeTargetLanguage(Languages language)
        {
            var changeLanguageAction = () =>
            {
                this.TtsSettings.TtsLanguage = language;
                this.Model.TranslateToLang = language;

                if (TtsSettings.TtsSystem == TTSEngines.WindowsTTS)
                {
                    var langCode = _languageService.GetLanguageDescriptor(language).Code;
                    LoadAvailableVoices(langCode);
                }
            };

            await this.ReconfigureTts(language, TtsSettings.TtsSystem, changeLanguageAction);
            OnPropertyChanged(nameof(TranslateToLang));
            OnPropertyChanged(nameof(IsTtsWindowsSelected));
            OnPropertyChanged(nameof(IsTtsEnabled));
            ResetLibreTranslateState();
        }

        private async Task ChangeTtsSystem(TTSEngines engine)
        {
            Action changeTtsEngineAction = () =>
            {
                this.TtsSettings.TtsSystem = engine;

                if (engine == TTSEngines.WindowsTTS)
                {
                    var langCode = _languageService.GetLanguageDescriptor(TtsSettings.TtsLanguage).Code;
                    LoadAvailableVoices(langCode);
                }
                else
                {
                    AvailableVoices = new ObservableCollection<VoiceInfo>();
                    _selectedVoice = null;
                    OnPropertyChanged(nameof(SelectedVoice));
                }
            };

            await this.ReconfigureTts(TtsSettings.TtsLanguage, engine, changeTtsEngineAction);
            OnPropertyChanged(nameof(TtsSystem));
            OnPropertyChanged(nameof(IsTtsWindowsSelected));
            OnPropertyChanged(nameof(IsTtsEnabled));
        }

        private async Task ReconfigureTts(Languages language, TTSEngines engine, Action changeParameter)
        {
            try
            {
                var changeLangStage = StagesFactory.CreateLanguageChangeStages(
                    _dialogService,
                    changeParameter,
                    _logger);

                if (engine == TTSEngines.WindowsTTS
                    && !TtsSettings.InstalledWinTtsLanguages.Contains(language))
                {
                    var langCode = _languageService.GetLanguageDescriptor(language).Code;
                    changeLangStage.AddNextStage(new ActionInteractionStage(_dialogService, () =>
                    {
                        this.TtsSettings.InstalledWinTtsLanguages.Add(language);
                        return Task.CompletedTask;
                    }));
                    changeLangStage = StagesFactory.CreateWindowsTtsCheckingStages(_dialogService, langCode, changeLangStage, _logger);
                }
                //else if (engine == TTSEngines.SileroTTS)
                //{
                //    var languageDescriptor = _languageService.GetLanguageDescriptor(language);
                //    changeLangStage = StagesFactory.CreateSileroTtsCheckingStages(languageDescriptor, _dialogService, changeLangStage, _logger);
                //}

                await changeLangStage.ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during source language change");
            }
        }

        private string GetLanguageDisplayName(LanguageDescriptor languageDescriptor)
        {
            return LocalizationManager.GetValue($"Str.Languages.{languageDescriptor.Language}", false,
               OnLocalizedValueChanged, this);
        }

        private void OnLocalizedValueChanged(string key, string oldValue)
        {
            var availableLang = AvailableTranslationLanguages.First(lang => lang.DisplayName == oldValue);
            availableLang.DisplayName = LocalizationManager.GetValue(key, false, OnLocalizedValueChanged, this);
        }


        public void ClosePanel()
        {
        }

        public void Dispose()
        {
            LocalizationManager.ReleaseChangedValuesCallbacks(this);
        }
    }
}