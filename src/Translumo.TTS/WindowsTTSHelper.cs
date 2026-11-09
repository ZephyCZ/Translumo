using System.Diagnostics;

namespace Translumo.TTS
{
    public static class WindowsTTSHelper
    {
        public static async Task<bool> InstallTTSLanguageCapability(string languageCode)
        {
            var process = CreateTTSCapabilityInstallProcess(languageCode);
            if (process == null)
                return true;

            process.Start();

            // Wait asynchronously for the process to exit
            await Task.Run(() => process.WaitForExit());

            // Optional: check exit code (0 usually means success)
            return process.ExitCode == 0;
        }

        public static bool IsLanguageTTSCapabilityInstalled(
            string languageTag,
            bool exactMatching = false)
        {
            if (exactMatching)
            {
                return Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices
                    .Any(v => v.Language.Equals(languageTag, StringComparison.OrdinalIgnoreCase));
            }

            return GetAvailableVoicesForLanguage(languageTag).Count != 0;
        }

        private static Process CreateTTSCapabilityInstallProcess(string nameTag)
        {
            string psCommand = $@"
            $env:TERM = 'xterm';
            $Host.UI.RawUI.WindowTitle = 'Installing TTS Language...';
            Write-Host 'Installing {nameTag} TTS Language Capability. Please wait...' -ForegroundColor Yellow;
            Write-Host 'This can take up to 20 minutes. Please be patient.' -ForegroundColor Yellow;

            $cap = Get-WindowsCapability -Online | Where-Object {{ $_.Name -like 'Language.TextToSpeech~~~{nameTag}~*' }} | Select-Object -First 1;

            if ($cap) {{
                try {{
                    Add-WindowsCapability -Online -Name $cap.Name -ErrorAction Stop;
                    Write-Host 'Installation complete!' -ForegroundColor Green;
                    Start-Sleep -Seconds 2;
                }} catch {{
                    Write-Host 'Installation failed. Please try again or use the manual method.' -ForegroundColor Red;
                    Write-Host $_ -ForegroundColor Red;
                    Start-Sleep -Seconds 60;
                }}
            }} else {{
                Write-Host 'Capability not found. Try installing the language manually.' -ForegroundColor Red;
                Start-Sleep -Seconds 60;
            }}
            ";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"{psCommand}\"",
                    Verb = "runas", // enable elevation
                    UseShellExecute = true, // enable elevation prompt
                },
                EnableRaisingEvents = true, // enable process.Exited event
            };

            return process;  // Return the process after completion
        }

        public static List<TtsVoice> GetAvailableVoicesForLanguage(string languageTag)
        {
            var result = new List<TtsVoice>();
            try
            {
                result = Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices
                .Where(v => v.Language.StartsWith(languageTag))
                .Select(v => new TtsVoice(v.DisplayName, v.Language))
                .ToList();
            }
            catch (Exception)
            {

            }

            return result;
        }
    }
}
