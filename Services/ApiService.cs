using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text.Json;

namespace WordVideoGenerator.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        // 请替换为您的阿里云API密钥
        private const string API_KEY = "YOUR_ALIBABA_CLOUD_API_KEY";
        private const string QWEN_FLASH_URL = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation";
        private const string WAN22_IMAGE_URL = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text2image/image-synthesis";

        public ApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_KEY}");
            _httpClient.DefaultRequestHeaders.Add("X-DashScope-Async", "enable");
        }

        public async Task<string[]> GetWordTranslationsAsync(string word)
        {
            var request = new
            {
                model = "qwen-flash",
                input = new
                {
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = $"请给出英文单词 '{word}' 的1个最常用中文释义，直接返回释义，不要包含例句或其他内容。"
                        }
                    }
                },
                parameters = new
                {
                    temperature = 0.7,
                    top_p = 0.9
                }
            };

            var response = await _httpClient.PostAsync(
                QWEN_FLASH_URL,
                new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
            );

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(result);
                var content = jsonResponse.RootElement
                    .GetProperty("output")
                    .GetProperty("text")
                    .GetString();

                // 将返回的文本按换行符分割成数组
                return content.Split(new[] { '\n', '，', '、' }, StringSplitOptions.RemoveEmptyEntries);
            }

            throw new Exception($"API调用失败: {response.StatusCode}");
        }

        public async Task<string> GenerateImageAsync(string word)
        {
            var request = new
            {
                model = "wan2.2-t2i-flash",
                input = new
                {
                    prompt = $"生成一张关于英文单词 '{word}' 含义的清晰插图，图片风格简洁现代，背景简单，主题突出。"
                },
                parameters = new
                {
                    size = "1024*1024"
                }
            };

            var response = await _httpClient.PostAsync(
                WAN22_IMAGE_URL,
                new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
            );

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(result);
                var imageUrl = jsonResponse.RootElement
                    .GetProperty("output")
                    .GetProperty("results")[0]
                    .GetProperty("url")
                    .GetString();

                // 下载图片并保存到临时文件
                var tempImagePath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(), 
                    $"{word}_image.png"
                );

                using (var imageResponse = await _httpClient.GetAsync(imageUrl))
                {
                    using (var imageStream = await imageResponse.Content.ReadAsStreamAsync())
                    using (var fileStream = System.IO.File.Create(tempImagePath))
                    {
                        await imageStream.CopyToAsync(fileStream);
                    }
                }

                return tempImagePath;
            }

            throw new Exception($"图片生成失败: {response.StatusCode}");
        }
    }
} 