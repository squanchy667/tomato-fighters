using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.VFX
{
    public enum ApiPlatform { Anthropic, OpenRouter, OpenAI }

    public static class ClaudeApiClient
    {
        private static readonly HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        private const int MAX_TOKENS = 4096;
        private const string ENV_KEY = "VFX_API_KEY";

        private static string SYSTEM_PROMPT => VFXPrompts.PARTICLE_SYSTEM;

        // --- .env file I/O ---

        private static string EnvFilePath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".env"));

        public static string GetApiKey()
        {
            string path = EnvFilePath;
            if (!File.Exists(path))
            {
                Debug.Log($"[VFX Generator] .env not found at: {path}");
                return "";
            }

            // Read with auto-detected encoding (handles UTF-8, UTF-16, etc.)
            string text = File.ReadAllText(path, Encoding.UTF8);
            // Strip BOM and null bytes (UTF-16 saved by Notepad)
            text = text.Replace("\0", "").TrimStart('\uFEFF', '\uFFFE');

            foreach (string rawLine in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = rawLine.Trim();
                if (trimmed.StartsWith("#") || !trimmed.Contains("=")) continue;

                int eq = trimmed.IndexOf('=');
                string key = trimmed.Substring(0, eq).Trim();
                // Accept VFX_API_KEY, OPENROUTER_API_KEY, CLAUDE_API_KEY, etc.
                if (!key.EndsWith("API_KEY")) continue;

                string value = trimmed.Substring(eq + 1).Trim();
                if (value.Length >= 2 &&
                    ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                     (value.StartsWith("'") && value.EndsWith("'"))))
                {
                    value = value.Substring(1, value.Length - 2);
                }
                return value;
            }
            return "";
        }

        public static void SetApiKey(string apiKey)
        {
            string path = EnvFilePath;
            string entry = $"{ENV_KEY}={apiKey}";

            if (!File.Exists(path))
            {
                File.WriteAllText(path, entry + "\n");
                return;
            }

            string[] lines = File.ReadAllLines(path);
            bool found = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().StartsWith(ENV_KEY + "="))
                {
                    lines[i] = entry;
                    found = true;
                    break;
                }
            }

            if (found)
                File.WriteAllText(path, string.Join("\n", lines) + "\n");
            else
                File.AppendAllText(path, entry + "\n");
        }

        // --- Platform detection from key prefix ---

        public static ApiPlatform DetectPlatform(string apiKey)
        {
            if (apiKey.StartsWith("sk-ant-")) return ApiPlatform.Anthropic;
            if (apiKey.StartsWith("sk-or-")) return ApiPlatform.OpenRouter;
            return ApiPlatform.OpenAI;
        }

        public static string PlatformLabel(ApiPlatform platform)
        {
            switch (platform)
            {
                case ApiPlatform.Anthropic: return "Anthropic";
                case ApiPlatform.OpenRouter: return "OpenRouter";
                case ApiPlatform.OpenAI: return "OpenAI";
                default: return "Unknown";
            }
        }

        // --- Public API ---

        public static async Task<ParticleConfig> SendPrompt(string userPrompt)
        {
            string text = await SendRawPrompt(SYSTEM_PROMPT, userPrompt);
            return ParseParticleConfig(text);
        }

        public static async Task<ParticleConfig> SendRefinement(string previousConfigJson, string refinementPrompt)
        {
            string combinedPrompt =
                $"Previous configuration:\n{previousConfigJson}\n\nRefinement request: {refinementPrompt}\n\nReturn the complete updated ParticleConfig JSON.";
            string text = await SendRawPrompt(SYSTEM_PROMPT, combinedPrompt);
            return ParseParticleConfig(text);
        }

        /// <summary>
        /// Send a prompt with a custom system prompt and get raw text back.
        /// Used by shader generator and other tools that don't return JSON.
        /// </summary>
        public static async Task<string> SendRawPrompt(string systemPrompt, string userPrompt)
        {
            string apiKey = GetApiKey();
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException(
                    $"No API key found. Looking for *_API_KEY= in: {EnvFilePath}");

            var platform = DetectPlatform(apiKey);
            Debug.Log($"[VFX] Sending {PlatformLabel(platform)} request ({userPrompt.Length} chars)...");
            string responseBody = await CallApi(platform, apiKey, systemPrompt, userPrompt);
            Debug.Log($"[VFX] Response received ({responseBody.Length} chars).");
            return ExtractText(platform, responseBody);
        }

        // --- Platform-specific API calls ---

        private static async Task<string> CallApi(ApiPlatform platform, string apiKey, string systemPrompt, string userMessage)
        {
            switch (platform)
            {
                case ApiPlatform.Anthropic: return await CallAnthropic(apiKey, systemPrompt, userMessage);
                case ApiPlatform.OpenRouter: return await CallOpenAI(apiKey, systemPrompt, userMessage, "https://openrouter.ai/api/v1/chat/completions", "anthropic/claude-sonnet-4.6");
                case ApiPlatform.OpenAI: return await CallOpenAI(apiKey, systemPrompt, userMessage, "https://api.openai.com/v1/chat/completions", "gpt-4o");
                default: throw new InvalidOperationException("Unknown platform");
            }
        }

        private static async Task<string> CallAnthropic(string apiKey, string systemPrompt, string userMessage)
        {
            var requestBody = new AnthropicRequest
            {
                model = "claude-sonnet-4-20250514",
                max_tokens = MAX_TOKENS,
                system = systemPrompt,
                messages = new[] { new ApiMessage { role = "user", content = userMessage } }
            };

            string json = JsonUtility.ToJson(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            return await Send(request);
        }

        private static async Task<string> CallOpenAI(string apiKey, string systemPrompt, string userMessage, string url, string model)
        {
            var requestBody = new OpenAIRequest
            {
                model = model,
                max_tokens = MAX_TOKENS,
                messages = new[]
                {
                    new ApiMessage { role = "system", content = systemPrompt },
                    new ApiMessage { role = "user", content = userMessage }
                }
            };

            string json = JsonUtility.ToJson(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            return await Send(request);
        }

        private static async Task<string> Send(HttpRequestMessage request)
        {
            HttpResponseMessage response = await httpClient.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"API returned {(int)response.StatusCode}: {body}");

            return body;
        }

        // --- Response parsing ---

        private static string ExtractText(ApiPlatform platform, string responseBody)
        {
            string text;

            if (platform == ApiPlatform.Anthropic)
            {
                var resp = JsonUtility.FromJson<AnthropicResponse>(responseBody);
                if (resp?.content == null || resp.content.Length == 0)
                    throw new InvalidOperationException($"Empty Anthropic response: {responseBody}");
                text = resp.content[0].text;
            }
            else
            {
                var resp = JsonUtility.FromJson<OpenAIResponse>(responseBody);
                if (resp?.choices == null || resp.choices.Length == 0)
                    throw new InvalidOperationException($"Empty OpenAI/OpenRouter response: {responseBody}");
                text = resp.choices[0].message.content;
            }

            return StripMarkdownFencing(text);
        }

        public static string StripMarkdownFencing(string text)
        {
            text = text.Trim();
            if (text.StartsWith("```"))
            {
                int nl = text.IndexOf('\n');
                if (nl >= 0) text = text.Substring(nl + 1);
            }
            if (text.EndsWith("```"))
                text = text.Substring(0, text.Length - 3);
            return text.Trim();
        }

        private static ParticleConfig ParseParticleConfig(string text)
        {
            try
            {
                return JsonUtility.FromJson<ParticleConfig>(text);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to parse response: {e.Message}\nRaw text: {text}");
            }
        }

        // --- Serialization types ---

        // Anthropic format
        [Serializable]
        private class AnthropicRequest
        {
            public string model;
            public int max_tokens;
            public string system;
            public ApiMessage[] messages;
        }

        [Serializable]
        private class AnthropicResponse
        {
            public AnthropicContentBlock[] content;
        }

        [Serializable]
        private class AnthropicContentBlock
        {
            public string type;
            public string text;
        }

        // OpenAI / OpenRouter format
        [Serializable]
        private class OpenAIRequest
        {
            public string model;
            public int max_tokens;
            public ApiMessage[] messages;
        }

        [Serializable]
        private class OpenAIResponse
        {
            public OpenAIChoice[] choices;
        }

        [Serializable]
        private class OpenAIChoice
        {
            public ApiMessage message;
        }

        // Shared
        [Serializable]
        private class ApiMessage
        {
            public string role;
            public string content;
        }
    }
}
