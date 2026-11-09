using System.Collections.Concurrent;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace Translumo.TTS.Engines;

public class WindowsTTSEngine : ITTSEngine
{
    private readonly VoiceInformation _voice;
    private readonly SpeechSynthesizer _synthesizer;
    private readonly MediaPlayer _player = new();
    private readonly ConcurrentQueue<string> _speechQueue = new();
    private bool _isPlaying;

    private readonly object _syncRoot = new();

    public WindowsTTSEngine(string languageCode, string voiceName = null)
    {
        _synthesizer = new SpeechSynthesizer();

        if (!string.IsNullOrWhiteSpace(voiceName))
            _voice = SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.DisplayName.Equals(voiceName, StringComparison.OrdinalIgnoreCase));

        _voice ??= SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.Language.Equals(languageCode, StringComparison.OrdinalIgnoreCase));

        if (_voice != null)
        {
            _synthesizer.Voice = _voice;
            _player.MediaEnded += Player_MediaEnded;
        }
    }

    public void SpeechText(string text)
    {
        if (_voice == null || string.IsNullOrWhiteSpace(text))
            return;

        bool startPlayback = false;

        lock (_syncRoot)
        {
            _speechQueue.Enqueue(text);

            if (!_isPlaying)
            {
                _isPlaying = true;
                startPlayback = true;
            }
        }

        if (startPlayback)
            _ = PlayNext();
    }

    private async void Player_MediaEnded(MediaPlayer sender, object args)
    {
        await PlayNext();
    }

    private async Task PlayNext()
    {
        if (!_speechQueue.TryDequeue(out var text))
        {
            lock (_syncRoot)
            {
                _isPlaying = false;

                if (_speechQueue.IsEmpty)
                    return;

                _isPlaying = true;
            }

            if (!_speechQueue.TryDequeue(out text))
                return;
        }

        var stream = await _synthesizer.SynthesizeTextToStreamAsync(text);
        _player.Source = MediaSource.CreateFromStream(stream, stream.ContentType);
        _player.Play();
    }

    public void Dispose()
    {
        _player.MediaEnded -= Player_MediaEnded;
        _player.Dispose();
        _synthesizer.Dispose();
    }
}